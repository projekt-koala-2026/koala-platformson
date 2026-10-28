import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiEdition, ApiPost, ApiStaticPage, Edition, Post } from "../../types/models";
import { adaptEdition, adaptPost, staticPageMarkdown } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequest } from "../../utils/apiFetcher";

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
            apiRequest<ApiStaticPage[]>(apiEndpoints.staticPages, null, "GET", navigate),
            apiRequest<ApiEdition[]>(
                `${apiEndpoints.editions}?${firstPage}&ShowActive=false`,
                null,
                "GET",
                navigate
            ),
            apiRequest<ApiPost[]>(
                `${apiEndpoints.posts}?${firstPage}&ShowHidden=false`,
                null,
                "GET",
                navigate
            ),
        ]).then(([staticPages, editionData, postData]) => {
            if (!active) return;
            const historyPage = staticPages?.find((item) => item.name === "HISTORY_PAGE");
            const now = Date.now();
            const pastEditions = (editionData ?? [])
                .map(adaptEdition)
                .filter((edition) => new Date(edition.endDate).getTime() < now)
                .sort(
                    (first, second) =>
                        new Date(second.endDate).getTime() - new Date(first.endDate).getTime()
                );
            setHistory(staticPageMarkdown(historyPage));
            setEditions(pastEditions);
            setPosts((postData ?? []).map(adaptPost));
            setSelectedEditionId(pastEditions[0]?.id ?? "");
            setHasError(!historyPage || !editionData || !postData);
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
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-brand-text">
                        O konkursie
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">Historia</h1>
                    <p className="mt-3 max-w-2xl text-ink-secondary">
                        Poznaj początki konkursu i najważniejsze wydarzenia z poprzednich edycji.
                    </p>
                </header>

                {hasError && (
                    <p
                        role="status"
                        className="mb-5 rounded-xl border px-4 py-3 text-sm app-status-warning"
                    >
                        Nie udało się pobrać wszystkich danych. Część strony może być niedostępna.
                    </p>
                )}

                <section className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-8">
                    <div>
                        {loading ? (
                            <p className="text-sm text-ink-muted">Ładowanie historii…</p>
                        ) : history.trim() ? (
                            <MarkdownRenderer content={history} />
                        ) : (
                            <p className="text-sm text-ink-muted">
                                Historia konkursu nie została jeszcze opublikowana.
                            </p>
                        )}
                    </div>
                </section>

                <section className="mt-10" aria-labelledby="archive-heading">
                    <h2 id="archive-heading" className="text-xl font-semibold text-ink-heading">
                        Wpisy z poprzednich edycji
                    </h2>
                    {loading ? (
                        <p className="mt-4 text-sm text-ink-muted">Ładowanie archiwum…</p>
                    ) : editions.length === 0 ? (
                        <div className="mt-4 rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                            Brak zakończonych edycji konkursu.
                        </div>
                    ) : (
                        <>
                            <div
                                className="mt-4 flex gap-2 overflow-x-auto rounded-xl bg-surface-subtle p-1"
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
                                            className={`whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium transition focus:outline-none focus:ring-2 focus:ring-brand-focus ${
                                                isSelected
                                                    ? "bg-surface text-brand-text-strong shadow-sm"
                                                    : "text-ink-secondary hover:text-ink-heading"
                                            }`}
                                        >
                                            {edition.title}
                                        </button>
                                    );
                                })}
                            </div>
                            <div className="mt-6 space-y-4" role="tabpanel">
                                {selectedPosts.length === 0 ? (
                                    <p className="rounded-xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                        Brak wpisów dla tej edycji.
                                    </p>
                                ) : (
                                    selectedPosts.map((post) => (
                                        <article
                                            key={post.id}
                                            className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6"
                                        >
                                            <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
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
                                            <h3 className="mt-2 break-words text-xl font-semibold text-ink-heading">
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
