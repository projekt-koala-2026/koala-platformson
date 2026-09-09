import { useEffect, useMemo, useState } from "react";
import { FaFilePdf } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { Edition, ProblemsByEdition } from "../../types/models";
import { apiRequest, resolveApiAssetUrl } from "../../utils/apiFetcher";

const EDITIONS_ENDPOINT = "/api/edition";
const PROBLEMS_ENDPOINT = "/content/problems/problems.json";

const ProblemsPublicScreen = () => {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<Edition[]>([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [problemsData, setProblemsData] = useState<ProblemsByEdition>({});
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate),
            apiRequest<ProblemsByEdition>(PROBLEMS_ENDPOINT, null, "GET", navigate),
        ]).then(([editionData, problemData]) => {
            if (!active) return;
            const sortedEditions = [...(editionData ?? [])].sort(
                (first, second) =>
                    new Date(second.startDate).getTime() - new Date(first.startDate).getTime()
            );
            setEditions(sortedEditions);
            setSelectedEditionId(sortedEditions[0]?.id ?? "");
            setProblemsData(problemData ?? {});
            setHasError(!editionData || !problemData);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const problems = problemsData[selectedEditionId] ?? {};
    const selectedEdition = useMemo(
        () => editions.find((edition) => edition.id === selectedEditionId),
        [editions, selectedEditionId]
    );

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-emerald-700">
                        Materiały konkursowe
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Archiwum zadań</h1>
                    <p className="mt-3 max-w-2xl text-slate-600">
                        Wybierz edycję i pobierz oficjalne zestawy zadań oraz dokumentację.
                    </p>
                </header>

                {hasError && (
                    <p
                        role="status"
                        className="mb-5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
                    >
                        Nie udało się pobrać wszystkich danych. Lista może być niepełna.
                    </p>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center text-sm text-slate-500 shadow-sm">
                        Ładowanie materiałów…
                    </div>
                ) : editions.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">
                        Brak dostępnych edycji konkursu.
                    </div>
                ) : (
                    <>
                        <div className="mb-8 rounded-2xl border border-emerald-200 bg-emerald-50 p-5">
                            <label className="block text-sm font-semibold text-emerald-950">
                                Edycja konkursu
                                <select
                                    value={selectedEditionId}
                                    onChange={(event) => setSelectedEditionId(event.target.value)}
                                    className="app-select mt-2 w-full rounded-xl border border-emerald-200 bg-white px-3 py-2.5 text-slate-900 outline-none focus:ring-2 focus:ring-emerald-500"
                                >
                                    {editions.map((edition) => (
                                        <option key={edition.id} value={edition.id}>
                                            {edition.title}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>

                        {Object.keys(problems).length === 0 ? (
                            <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
                                <h2 className="text-lg font-semibold text-slate-800">
                                    Brak materiałów
                                </h2>
                                <p className="mt-2 text-sm text-slate-500">
                                    Dla wybranej edycji nie dodano jeszcze zadań ani dokumentów.
                                </p>
                            </div>
                        ) : (
                            <div className="space-y-8">
                                {Object.entries(problems).map(([stage, files], stageIndex) => (
                                    <section
                                        key={stage}
                                        aria-labelledby={`stage-${selectedEditionId}-${stageIndex}`}
                                    >
                                        <div className="mb-3 flex flex-wrap items-center gap-3">
                                            <h2
                                                id={`stage-${selectedEditionId}-${stageIndex}`}
                                                className="text-xl font-semibold text-slate-900"
                                            >
                                                {stage}
                                            </h2>
                                            {selectedEdition && (
                                                <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-800">
                                                    {selectedEdition.title}
                                                </span>
                                            )}
                                        </div>
                                        <div className="space-y-3">
                                            {files.map((file) => (
                                                <a
                                                    key={file.id}
                                                    href={resolveApiAssetUrl(file.url)}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className="flex items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-emerald-500 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-emerald-500 sm:p-5"
                                                >
                                                    <span className="flex min-w-0 items-center gap-4">
                                                        <FaFilePdf className="shrink-0 text-3xl text-red-500" />
                                                        <span className="min-w-0">
                                                            <span className="block truncate font-semibold text-slate-800">
                                                                {file.title}
                                                            </span>
                                                            <span className="mt-1 block text-sm text-slate-500">
                                                                Dokument PDF
                                                            </span>
                                                        </span>
                                                    </span>
                                                    <span className="hidden shrink-0 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white sm:block">
                                                        Otwórz PDF
                                                    </span>
                                                </a>
                                            ))}
                                        </div>
                                    </section>
                                ))}
                            </div>
                        )}
                    </>
                )}
            </main>
            <PublicFooter />
        </div>
    );
};

export default ProblemsPublicScreen;
