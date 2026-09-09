import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { Edition, MarkdownStaticPage, Post } from "../../types/models";
import { apiRequest } from "../../utils/apiFetcher";

const HISTORY_ENDPOINT = "/content/history/history.json";
const EDITIONS_ENDPOINT = "/api/edition";
const POSTS_ENDPOINT = "/api/admin/post";

const HistoryScreen = () => {
    const navigate = useNavigate();
    const [history, setHistory] = useState("");
    const [editions, setEditions] = useState<Edition[]>([]);
    const [posts, setPosts] = useState<Post[]>([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequest<MarkdownStaticPage>(HISTORY_ENDPOINT, null, "GET", navigate),
            apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate),
            apiRequest<Post[]>(POSTS_ENDPOINT, null, "GET", navigate),
        ]).then(([historyData, editionData, postData]) => {
            if (!active) return;
            const now = Date.now();
            const pastEditions = (editionData ?? [])
                .filter((edition) => new Date(edition.endDate).getTime() < now)
                .sort(
                    (first, second) =>
                        new Date(second.endDate).getTime() - new Date(first.endDate).getTime()
                );
            setHistory(historyData?.markdownBody ?? "");
            setEditions(pastEditions);
            setPosts(postData ?? []);
            setSelectedEditionId(pastEditions[0]?.id ?? "");
            setHasError(!historyData || !editionData || !postData);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const selectedPosts = useMemo(
        () =>
            posts
                .filter((post) => post.editionId === selectedEditionId)
                .sort(
                    (first, second) =>
                        new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime()
                ),
        [posts, selectedEditionId]
    );

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                {hasError && (
                    <p
                        role="status"
                        className="mb-5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
                    >
                        Nie udało się pobrać wszystkich danych. Część strony może być niedostępna.
                    </p>
                )}

                <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-emerald-700">
                        O konkursie
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Historia</h1>
                    <div className="mt-6">
                        {loading ? (
                            <p className="text-sm text-slate-500">Ładowanie historii…</p>
                        ) : history.trim() ? (
                            <MarkdownRenderer content={history} />
                        ) : (
                            <p className="text-sm text-slate-500">
                                Historia konkursu nie została jeszcze opublikowana.
                            </p>
                        )}
                    </div>
                </section>

                <section className="mt-10" aria-labelledby="archive-heading">
                    <h2 id="archive-heading" className="text-xl font-semibold text-slate-900">
                        Wpisy z poprzednich edycji
                    </h2>
                    {loading ? (
                        <p className="mt-4 text-sm text-slate-500">Ładowanie archiwum…</p>
                    ) : editions.length === 0 ? (
                        <div className="mt-4 rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
                            Brak zakończonych edycji konkursu.
                        </div>
                    ) : (
                        <>
                            <div
                                className="mt-4 flex gap-2 overflow-x-auto rounded-xl bg-slate-200 p-1"
                                role="tablist"
                                aria-label="Edycje konkursu"
                            >
                                {editions.map((edition) => {
                                    const isSelected = selectedEditionId === edition.id;
                                    return (
                                        <button
                                            type="button"
                                            role="tab"
                                            aria-selected={isSelected}
                                            key={edition.id}
                                            onClick={() => setSelectedEditionId(edition.id)}
                                            className={`whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium transition focus:outline-none focus:ring-2 focus:ring-emerald-500 ${
                                                isSelected
                                                    ? "bg-white text-emerald-800 shadow-sm"
                                                    : "text-slate-600 hover:text-slate-900"
                                            }`}
                                        >
                                            {edition.title}
                                        </button>
                                    );
                                })}
                            </div>
                            <div className="mt-6 space-y-4" role="tabpanel">
                                {selectedPosts.length === 0 ? (
                                    <p className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
                                        Brak wpisów dla tej edycji.
                                    </p>
                                ) : (
                                    selectedPosts.map((post) => (
                                        <article
                                            key={post.id}
                                            className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6"
                                        >
                                            <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                                <time dateTime={post.createdAt}>
                                                    {new Date(post.createdAt).toLocaleDateString(
                                                        "pl-PL",
                                                        {
                                                            year: "numeric",
                                                            month: "long",
                                                            day: "numeric",
                                                        }
                                                    )}
                                                </time>
                                            </p>
                                            <h3 className="mt-2 break-words text-xl font-semibold text-slate-900">
                                                {post.title}
                                            </h3>
                                            <div className="mt-4">
                                                <MarkdownRenderer content={post.markdownBody} />
                                            </div>
                                        </article>
                                    ))
                                )}
                            </div>
                        </>
                    )}
                </section>
            </main>
            <PublicFooter />
        </div>
    );
};

export default HistoryScreen;
