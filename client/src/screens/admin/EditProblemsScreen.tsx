import { useEffect, useMemo, useState, type FormEvent } from "react";
import { FaFilePdf, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import FileUploader from "../../components/FileUploader";
import Modal from "../../components/Modal";
import type {
    ApiEdition,
    ApiStaticPage,
    Edition,
    ProblemFile,
    ProblemsByEdition,
} from "../../types/models";
import { adaptEdition, staticPageProblems } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import {
    apiRequest,
    apiRequestResult,
    resolveApiAssetUrl,
    uploadFile,
} from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const MAX_PDF_SIZE = 64 * 1024 * 1024;

type Feedback = { tone: "success" | "warning" | "error"; message: string } | null;

const validatePdf = (file: File) => {
    if (file.size === 0) return "Wybrany plik jest pusty.";
    const hasPdfExtension = file.name.toLowerCase().endsWith(".pdf");
    const hasPdfMime = !file.type || file.type === "application/pdf";
    if (!hasPdfExtension || !hasPdfMime) return "Dozwolone są wyłącznie pliki PDF.";
    if (file.size > MAX_PDF_SIZE) return "Plik przekracza maksymalny rozmiar 64 MB.";
    return null;
};

const EditProblemsScreen = () => {
    const navigate = useNavigate();
    const canEdit = useMemo(() => isAdmin() || isEditor(), []);
    const [editions, setEditions] = useState<Edition[]>([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [allProblemsData, setAllProblemsData] = useState<ProblemsByEdition>({});
    const [taskPage, setTaskPage] = useState<ApiStaticPage | null>(null);
    const [subpointInputValue, setSubpointInputValue] = useState("");
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [loading, setLoading] = useState(true);
    const [uploading, setUploading] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [isAddModalOpen, setIsAddModalOpen] = useState(false);

    useEffect(() => {
        if (!canEdit) {
            navigate("/admin/login");
            return;
        }

        let active = true;
        void Promise.all([
            apiRequest<ApiEdition[]>(
                `${apiEndpoints.editions}?${firstPage}&ShowActive=false`,
                null,
                "GET",
                navigate
            ),
            apiRequest<ApiStaticPage[]>(apiEndpoints.staticPages, null, "GET", navigate),
        ]).then(([editionsData, staticPages]) => {
            if (!active) return;
            const loadedEditions = (editionsData ?? []).map(adaptEdition);
            const loadedTaskPage = staticPages?.find((page) => page.name === "TASKS_PAGE") ?? null;
            setEditions(loadedEditions);
            setSelectedEditionId(loadedEditions[0]?.id ?? "");
            setTaskPage(loadedTaskPage);
            setAllProblemsData(staticPageProblems(loadedTaskPage ?? undefined));
            if (!editionsData || !loadedTaskPage) {
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
        if (!taskPage) return false;
        const { data: response } = await apiRequestResult<ApiStaticPage>(
            `${apiEndpoints.staticPages}/${taskPage.id}`,
            {
                contentJson: JSON.stringify({ markdownBody: JSON.stringify(data) }),
                version: taskPage.version,
            },
            "PUT",
            navigate
        );
        if (response) setTaskPage(response);
        return response !== null;
    };

    const resetUploadForm = () => {
        setSubpointInputValue("");
        setSelectedFile(null);
    };

    const closeAddModal = () => {
        if (uploading) return;
        setIsAddModalOpen(false);
        resetUploadForm();
        if (feedback?.tone === "error") setFeedback(null);
    };

    const openAddModal = () => {
        resetUploadForm();
        setFeedback(null);
        setIsAddModalOpen(true);
    };

    const addEntry = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
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
        const validationError = validatePdf(selectedFile);
        if (validationError) {
            setFeedback({ tone: "error", message: validationError });
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
            uploadedFile.filePath ||
            uploadedFile.url ||
            `/api/koala/content/${uploadedFile.id}.pdf`;
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
                `${apiEndpoints.publicFiles}/${uploadedFile.id}`,
                null,
                "DELETE",
                navigate
            );
            setFeedback({
                tone: "error",
                message:
                    rollback.status === 200 || rollback.status === 204
                        ? "Nie udało się zapisać metadanych. Wysłany plik został wycofany."
                        : "Nie udało się zapisać metadanych ani wycofać wysłanego pliku. Sprawdź pliki na serwerze.",
            });
            setUploading(false);
            return;
        }

        setAllProblemsData(updatedData);
        resetUploadForm();
        setIsAddModalOpen(false);
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
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.publicFiles}/${pdf.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status !== 200 && status !== 204) {
            setFeedback({
                tone: "warning",
                message: "Dokument usunięto z listy, ale serwer nie potwierdził usunięcia pliku.",
            });
        } else setFeedback({ tone: "success", message: "Dokument został usunięty." });
        setDeletingId(null);
    };

    const currentEditionData = allProblemsData[selectedEditionId] ?? {};
    const documentCount = Object.values(currentEditionData).reduce(
        (sum, files) => sum + files.length,
        0
    );
    const selectedEdition = editions.find((edition) => edition.id === selectedEditionId);

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Materiały konkursowe
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">
                        Zadania i dokumenty PDF
                    </h1>
                    <p className="mt-2 max-w-3xl text-sm text-ink-secondary">
                        Porządkuj pliki w sekcje osobno dla każdej edycji konkursu.
                    </p>
                </div>

                {feedback && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "app-status-success"
                                : feedback.tone === "warning"
                                  ? "app-status-warning"
                                  : "app-status-danger"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-line bg-surface p-8 text-center text-ink-muted shadow-sm">
                        Ładowanie dokumentów…
                    </div>
                ) : (
                    <div className="space-y-6">
                        <section className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:flex sm:items-end sm:justify-between sm:gap-6 sm:p-6">
                            <label className="flex min-w-0 flex-1 flex-col gap-2 text-sm font-medium text-ink">
                                <span>Dokumenty dla edycji</span>
                                <select
                                    value={selectedEditionId}
                                    disabled={uploading || deletingId !== null}
                                    onChange={(event) => setSelectedEditionId(event.target.value)}
                                    className="app-select w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted sm:max-w-xl"
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
                            <div className="mt-4 flex shrink-0 items-center justify-between gap-4 sm:mt-0 sm:justify-end">
                                <p className="text-sm text-ink-muted">
                                    {documentCount}{" "}
                                    {documentCount === 1 ? "dokument" : "dokumentów"}
                                </p>
                                <Button
                                    text="Dodaj dokument"
                                    disabled={editions.length === 0 || !selectedEditionId}
                                    onClick={openAddModal}
                                />
                            </div>
                        </section>

                        <section>
                            <div className="mb-4 flex items-end justify-between gap-4">
                                <div>
                                    <h2 className="text-xl font-semibold">Dokumentacja edycji</h2>
                                    <p className="mt-1 text-sm text-ink-muted">
                                        {selectedEdition?.title ?? "Nie wybrano edycji"}
                                    </p>
                                </div>
                            </div>

                            {Object.keys(currentEditionData).length === 0 ? (
                                <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                    Brak dokumentów dla wybranej edycji.
                                </div>
                            ) : (
                                <div className="space-y-5">
                                    {Object.entries(currentEditionData).map(([subpoint, files]) => (
                                        <article
                                            key={subpoint}
                                            className="overflow-hidden rounded-2xl border border-line bg-surface shadow-sm"
                                        >
                                            <div className="border-b border-line-subtle bg-canvas px-5 py-4">
                                                <h3 className="font-semibold">{subpoint}</h3>
                                                <p className="mt-0.5 text-xs text-ink-muted">
                                                    {files.length}{" "}
                                                    {files.length === 1 ? "plik" : "plików"}
                                                </p>
                                            </div>
                                            <ul className="divide-y divide-line-subtle">
                                                {files.map((pdf) => (
                                                    <li
                                                        key={pdf.id}
                                                        className="flex items-center justify-between gap-4 px-5 py-4"
                                                    >
                                                        <a
                                                            href={resolveApiAssetUrl(pdf.url)}
                                                            target="_blank"
                                                            rel="noopener noreferrer"
                                                            className="group flex min-w-0 items-center gap-3 text-sm font-medium text-ink hover:text-brand-text"
                                                        >
                                                            <FaFilePdf className="shrink-0 text-xl text-danger-icon" />
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
                                                            className="shrink-0 bg-danger px-3 hover:bg-danger-hover"
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

            <Modal
                isOpen={isAddModalOpen}
                onClose={closeAddModal}
                title="Dodaj dokument"
                maxWidth="lg"
            >
                <form onSubmit={(event) => void addEntry(event)}>
                    <p className="mb-5 text-sm text-ink-secondary">
                        Dokument zostanie dodany do edycji{" "}
                        <strong className="font-semibold text-ink-heading">
                            {selectedEdition?.title ?? "—"}
                        </strong>
                        .
                    </p>

                    {feedback?.tone === "error" && (
                        <p
                            role="alert"
                            className="mb-5 rounded-xl border px-4 py-3 text-sm app-status-danger"
                        >
                            {feedback.message}
                        </p>
                    )}

                    <label className="block text-sm font-medium text-ink">
                        Nazwa sekcji
                        <input
                            type="text"
                            value={subpointInputValue}
                            onChange={(event) => setSubpointInputValue(event.target.value)}
                            placeholder="np. Etap szkolny"
                            autoFocus
                            disabled={uploading}
                            className="mt-2 w-full rounded-xl border border-line-strong px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted"
                        />
                    </label>

                    <div className="mt-5">
                        <p className="mb-2 text-sm font-medium text-ink">Plik PDF</p>
                        <FileUploader
                            accept="application/pdf,.pdf"
                            label={
                                selectedFile
                                    ? selectedFile.name
                                    : "Kliknij lub przeciągnij plik PDF"
                            }
                            hint="PDF · maksymalnie 64 MB"
                            disabled={uploading}
                            validateFile={validatePdf}
                            onValidationError={(message) => setFeedback({ tone: "error", message })}
                            onFileSelect={(file) => {
                                setSelectedFile(file);
                                setFeedback(null);
                            }}
                        />
                    </div>

                    <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
                        <Button
                            text="Anuluj"
                            disabled={uploading}
                            onClick={closeAddModal}
                            className="bg-surface-subtle text-ink-strong hover:bg-surface-disabled active:bg-surface-active"
                        />
                        <Button
                            type="submit"
                            text={uploading ? "Zapisywanie…" : "Dodaj dokument"}
                            disabled={uploading || !selectedEditionId}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
};

export default EditProblemsScreen;
