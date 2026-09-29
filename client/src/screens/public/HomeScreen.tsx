import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiEdition, ApiPost, Edition, Post } from "../../types/models";
import { adaptEdition, adaptPost } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const POSTS_ENDPOINT = `${apiEndpoints.posts}?${firstPage}&ShowHidden=false`;
const ACTIVE_EDITIONS_ENDPOINT = `${apiEndpoints.editions}?${firstPage}&ShowActive=true`;

const HomeScreen = () => {
    const navigate = useNavigate();
    const [posts, setPosts] = useState<Post[]>([]);
    const [editions, setEditions] = useState<Edition[]>([]);
    const [loadedAt, setLoadedAt] = useState<number | null>(null);
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequestResult<ApiPost[]>(POSTS_ENDPOINT, null, "GET", navigate),
            apiRequestResult<ApiEdition[]>(ACTIVE_EDITIONS_ENDPOINT, null, "GET", navigate),
        ]).then(([postsResult, editionResult]) => {
            if (!active) return;
            const postData = postsResult.data ?? [];
            const editionData = editionResult.data?.[0];
            setPosts((postData ?? []).filter((post) => post.isVisible).map(adaptPost));
            setEditions(editionData ? [adaptEdition(editionData)] : []);
            setLoadedAt(Date.now());
            setHasError(
                postsResult.status !== 200 || editionResult.status !== 200
            );
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const visiblePosts = useMemo(() => {
        const editionsById = new Map(editions.map((edition) => [edition.id, edition]));
        return posts
            .filter((post) => {
                const edition = editionsById.get(post.editionId);
                if (!edition) return false;
                const start = new Date(edition.startDate).getTime();
                const end = new Date(edition.endDate).getTime();
                return loadedAt !== null && start <= loadedAt && loadedAt <= end;
            })
            .sort(
                (first, second) =>
                    new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime()
            );
    }, [editions, loadedAt, posts]);

    const editionNames = useMemo(
        () => new Map(editions.map((edition) => [edition.id, edition.title])),
        [editions]
    );

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-emerald-700">
                        Konkurs KOALA
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Aktualności</h1>
                    <p className="mt-3 max-w-2xl text-slate-600">
                        Najważniejsze informacje, terminy i komunikaty dotyczące bieżącej edycji.
                    </p>
                </header>

                {hasError && (
                    <p
                        role="status"
                        className="mb-5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
                    >
                        Nie udało się pobrać wszystkich danych. Wyświetlana lista może być niepełna.
                    </p>
                )}

                <section aria-label="Aktualności konkursu" className="space-y-5">
                    {loading ? (
                        <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center text-sm text-slate-500 shadow-sm">
                            Ładowanie aktualności…
                        </div>
                    ) : visiblePosts.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
                            <h2 className="text-lg font-semibold text-slate-800">
                                Brak aktualności
                            </h2>
                            <p className="mt-2 text-sm text-slate-500">
                                {editions.length === 0
                                    ? "Nie rozpoczęto jeszcze aktywnej edycji konkursu."
                                    : "Dla trwającej edycji nie opublikowano jeszcze żadnych wiadomości."}
                            </p>
                        </div>
                    ) : (
                        visiblePosts.map((post) => (
                            <article
                                key={post.id}
                                className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-8"
                            >
                                <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                    <time dateTime={post.createdAt}>
                                        {new Date(post.createdAt).toLocaleDateString("pl-PL", {
                                            year: "numeric",
                                            month: "long",
                                            day: "numeric",
                                        })}
                                    </time>
                                    {editionNames.get(post.editionId)
                                        ? ` · ${editionNames.get(post.editionId)}`
                                        : ""}
                                </p>
                                <h2 className="mt-2 break-words text-xl font-semibold text-slate-900">
                                    {post.title}
                                </h2>
                                <div className="mt-5">
                                    <MarkdownRenderer content={post.markdownBody} />
                                </div>
                            </article>
                        ))
                    )}
                </section>
            </main>
            <PublicFooter />
        </div>
    );
};

export default HomeScreen;
