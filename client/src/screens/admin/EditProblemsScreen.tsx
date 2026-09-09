import { useEffect, useMemo, useRef, useState } from "react";
import { FaFilePdf, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import type { Edition, ManagedFile, ProblemFile, ProblemsByEdition } from "../../types/models";
import {
    apiRequest,
    apiRequestResult,
    resolveApiAssetUrl,
    uploadFile,
} from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const EDITIONS_ENDPOINT = "/api/edition";
const PROBLEMS_CONTENT_ENDPOINT = "/content/problems/problems.json";
const PROBLEMS_SAVE_ENDPOINT = "/api/static-pages/problems";
const FILES_ENDPOINT = "/api/admin/file/public/files";
const MAX_PDF_SIZE = 64 * 1024 * 1024;

type Feedback = { tone: "success" | "warning" | "error"; message: string } | null;

const EditProblemsScreen = () => {
    const navigate = useNavigate();
    const canEdit = useMemo(() => isAdmin() || isEditor(), []);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [editions, setEditions] = useState<Edition[]>([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [allProblemsData, setAllProblemsData] = useState<ProblemsByEdition>({});
    const [subpointInputValue, setSubpointInputValue] = useState("");
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [loading, setLoading] = useState(true);
    const [uploading, setUploading] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        if (!canEdit) {
            navigate("/admin/login");
            return;
        }

        let active = true;
        void Promise.all([
            apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate),
            apiRequest<ProblemsByEdition>(PROBLEMS_CONTENT_ENDPOINT, null, "GET", navigate),
        ]).then(([editionsData, problemsData]) => {
            if (!active) return;
            const loadedEditions = editionsData ?? [];
            setEditions(loadedEditions);
            setSelectedEditionId(loadedEditions[0]?.id ?? "");
            setAllProblemsData(problemsData ?? {});
            if (!editionsData || !problemsData) {
                setFeedback({
                    tone: "error",
                    message: "Nie udało się pobrać wszystkich danych o zadaniach.",
                });
            }
            setLoading(false);
        });

        return () => {
            active = false;
        };
    }, [canEdit, navigate]);

    const saveMetadata = async (data: ProblemsByEdition) => {
        const { data: response } = await apiRequestResult(
            PROBLEMS_SAVE_ENDPOINT,
            { markdownBody: JSON.stringify(data) },
            "PUT",
            navigate
        );
        return response !== null;
    };

    const resetUploadForm = () => {
        setSubpointInputValue("");
        setSelectedFile(null);
        if (fileInputRef.current) fileInputRef.current.value = "";
    };

    const addEntry = async () => {
        const subpoint = subpointInputValue.trim();
        if (!selectedEditionId) {
            setFeedback({ tone: "error", message: "Wybierz edycję konkursu." });
            return;
        }
        if (!subpoint) {
            setFeedback({ tone: "error", message: "Wpisz nazwę sekcji dokumentów." });
            return;
        }
        if (!selectedFile) {
            setFeedback({ tone: "error", message: "Wybierz plik PDF." });
            return;
        }
        const hasPdfExtension = selectedFile.name.toLowerCase().endsWith(".pdf");
        const hasPdfMime = !selectedFile.type || selectedFile.type === "application/pdf";
        if (!hasPdfExtension || !hasPdfMime) {
            setFeedback({ tone: "error", message: "Dozwolone są wyłącznie pliki PDF." });
            return;
        }
        if (selectedFile.size > MAX_PDF_SIZE) {
            setFeedback({ tone: "error", message: "Plik przekracza maksymalny rozmiar 64 MB." });
            return;
        }

        setUploading(true);
        setFeedback(null);
        const uploadedFile = await uploadFile(
            selectedFile,
            selectedFile.name,
            "problems",
            navigate
        );
        if (!uploadedFile) {
            setFeedback({ tone: "error", message: "Nie udało się wysłać pliku na serwer." });
            setUploading(false);
            return;
        }

        const fileUrl =
            uploadedFile.filePath || uploadedFile.url || `/content/problems/${uploadedFile.id}.pdf`;
        const newProblem: ProblemFile = {
            id: uploadedFile.id,
            title: selectedFile.name,
            fileName: fileUrl.split("/").pop() || selectedFile.name,
            url: fileUrl,
        };
        const editionData = allProblemsData[selectedEditionId] ?? {};
        const updatedData: ProblemsByEdition = {
            ...allProblemsData,
            [selectedEditionId]: {
                ...editionData,
                [subpoint]: [...(editionData[subpoint] ?? []), newProblem],
            },
        };

        if (!(await saveMetadata(updatedData))) {
            const rollback = await apiRequestResult(
                FILES_ENDPOINT,
                { id: uploadedFile.id },
                "DELETE",
                navigate
            );
            setFeedback({
                tone: "error",
                message:
                    rollback.data !== null
                        ? "Nie udało się zapisać metadanych. Wysłany plik został wycofany."
                        : "Nie udało się zapisać metadanych ani wycofać wysłanego pliku. Sprawdź pliki na serwerze.",
            });
            setUploading(false);
            return;
        }

        setAllProblemsData(updatedData);
        resetUploadForm();
        setFeedback({ tone: "success", message: "Dokument został dodany." });
        setUploading(false);
    };

    const deletePdf = async (subpoint: string, pdf: ProblemFile) => {
        if (!window.confirm(`Czy na pewno chcesz usunąć plik „${pdf.title}”?`)) return;
        setDeletingId(pdf.id);
        setFeedback(null);

        const editionData = allProblemsData[selectedEditionId] ?? {};
        const remainingFiles = (editionData[subpoint] ?? []).filter((item) => item.id !== pdf.id);
        const updatedEditionData = { ...editionData };
        if (remainingFiles.length > 0) updatedEditionData[subpoint] = remainingFiles;
        else delete updatedEditionData[subpoint];
        const updatedData = { ...allProblemsData, [selectedEditionId]: updatedEditionData };

        if (!(await saveMetadata(updatedData))) {
            setFeedback({
                tone: "error",
                message: "Nie udało się zaktualizować listy. Plik nie został usunięty.",
            });
            setDeletingId(null);
            return;
        }

        setAllProblemsData(updatedData);
        const { data } = await apiRequestResult<string>(
            FILES_ENDPOINT,
            { id: pdf.id },
            "DELETE",
            navigate
        );
        if (data === null) {
            setFeedback({
                tone: "warning",
                message: "Dokument usunięto z listy, ale serwer nie potwierdził usunięcia pliku.",
            });
        } else {
            const registeredFiles = await apiRequest<ManagedFile[]>(
                `${FILES_ENDPOINT}?Folder=problems`,
                null,
                "GET",
                navigate
            );
            const stillRegistered = registeredFiles?.some((file) => file.id === pdf.id) ?? false;
            setFeedback(
                stillRegistered
                    ? {
                          tone: "warning",
                          message:
                              "Dokument usunięto z listy zadań, ale nadal jest zarejestrowany w magazynie plików.",
                      }
                    : { tone: "success", message: "Dokument został usunięty." }
            );
        }
        setDeletingId(null);
    };

    const currentEditionData = allProblemsData[selectedEditionId] ?? {};
    const documentCount = Object.values(currentEditionData).reduce(
        (sum, files) => sum + files.length,
        0
    );

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Materiały konkursowe
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">
                        Zadania i dokumenty PDF
                    </h1>
                    <p className="mt-2 max-w-3xl text-sm text-slate-600">
                        Porządkuj pliki w sekcje osobno dla każdej edycji konkursu.
                    </p>
                </div>

                {feedback && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                                : feedback.tone === "warning"
                                  ? "border-amber-200 bg-amber-50 text-amber-800"
                                  : "border-red-200 bg-red-50 text-red-800"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-slate-500 shadow-sm">
                        Ładowanie dokumentów…
                    </div>
                ) : (
                    <div className="space-y-6">
                        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                            <label className="block max-w-xl text-sm font-medium text-slate-700">
                                Edycja konkursu
                                <select
                                    value={selectedEditionId}
                                    onChange={(event) => setSelectedEditionId(event.target.value)}
                                    className="app-select mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                                >
                                    {editions.length === 0 && (
                                        <option value="">Brak dostępnych edycji</option>
                                    )}
                                    {editions.map((edition) => (
                                        <option key={edition.id} value={edition.id}>
                                            {edition.title}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </section>

                        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                            <h2 className="text-xl font-semibold">Dodaj dokument</h2>
                            <p className="mt-1 text-sm text-slate-500">
                                Obsługiwane są pliki PDF o rozmiarze do 64 MB.
                            </p>
                            <div className="mt-5 grid gap-4 md:grid-cols-2">
                                <label className="block text-sm font-medium text-slate-700">
                                    Nazwa sekcji
                                    <input
                                        type="text"
                                        value={subpointInputValue}
                                        onChange={(event) =>
                                            setSubpointInputValue(event.target.value)
                                        }
                                        placeholder="np. Etap szkolny"
                                        className="mt-2 w-full rounded-xl border border-slate-300 px-3 py-2.5 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                                    />
                                </label>
                                <label className="block text-sm font-medium text-slate-700">
                                    Plik PDF
                                    <input
                                        ref={fileInputRef}
                                        type="file"
                                        accept="application/pdf,.pdf"
                                        onChange={(event) =>
                                            setSelectedFile(event.target.files?.[0] ?? null)
                                        }
                                        className="mt-2 block w-full rounded-xl border border-slate-300 bg-white text-sm text-slate-600 file:mr-4 file:border-0 file:bg-slate-100 file:px-4 file:py-2.5 file:font-medium file:text-slate-700 hover:file:bg-slate-200"
                                    />
                                </label>
                            </div>
                            <Button
                                text={uploading ? "Zapisywanie…" : "Dodaj dokument"}
                                disabled={uploading || editions.length === 0}
                                onClick={() => void addEntry()}
                                className="mt-5"
                            />
                        </section>

                        <section>
                            <div className="mb-4 flex items-end justify-between gap-4">
                                <div>
                                    <h2 className="text-xl font-semibold">Dokumentacja edycji</h2>
                                    <p className="mt-1 text-sm text-slate-500">
                                        {documentCount}{" "}
                                        {documentCount === 1 ? "dokument" : "dokumentów"}
                                    </p>
                                </div>
                            </div>

                            {Object.keys(currentEditionData).length === 0 ? (
                                <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
                                    Brak dokumentów dla wybranej edycji.
                                </div>
                            ) : (
                                <div className="space-y-5">
                                    {Object.entries(currentEditionData).map(([subpoint, files]) => (
                                        <article
                                            key={subpoint}
                                            className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"
                                        >
                                            <div className="border-b border-slate-100 bg-slate-50 px-5 py-4">
                                                <h3 className="font-semibold">{subpoint}</h3>
                                                <p className="mt-0.5 text-xs text-slate-500">
                                                    {files.length}{" "}
                                                    {files.length === 1 ? "plik" : "plików"}
                                                </p>
                                            </div>
                                            <ul className="divide-y divide-slate-100">
                                                {files.map((pdf) => (
                                                    <li
                                                        key={pdf.id}
                                                        className="flex items-center justify-between gap-4 px-5 py-4"
                                                    >
                                                        <a
                                                            href={resolveApiAssetUrl(pdf.url)}
                                                            target="_blank"
                                                            rel="noopener noreferrer"
                                                            className="group flex min-w-0 items-center gap-3 text-sm font-medium text-slate-700 hover:text-emerald-700"
                                                        >
                                                            <FaFilePdf className="shrink-0 text-xl text-red-500" />
                                                            <span className="truncate group-hover:underline">
                                                                {pdf.title}
                                                            </span>
                                                        </a>
                                                        <Button
                                                            text={
                                                                <>
                                                                    <FaTrash />
                                                                    <span className="sr-only">
                                                                        Usuń {pdf.title}
                                                                    </span>
                                                                </>
                                                            }
                                                            disabled={deletingId === pdf.id}
                                                            onClick={() =>
                                                                void deletePdf(subpoint, pdf)
                                                            }
                                                            className="shrink-0 bg-red-600 px-3 hover:bg-red-700"
                                                        />
                                                    </li>
                                                ))}
                                            </ul>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    </div>
                )}
            </main>
        </div>
    );
};

export default EditProblemsScreen;
