import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import KoalaIllustration from "../../components/KoalaIllustration";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiEdition, ApiRanking, ApiSubedition } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";

export default function RankingsPublicScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [rankings, setRankings] = useState<ApiRanking[]>([]);
    const [tier, setTier] = useState("ALL");
    const [loading, setLoading] = useState(true);
    const [loadingRankings, setLoadingRankings] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const items = data ?? [];
            setEditions(items);
            setEditionId(items[0]?.id ?? "");
            if (!data) setError("Nie udało się pobrać edycji.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    useEffect(() => {
        if (!editionId) {
            setSubeditions([]);
            setSubeditionId("");
            return;
        }
        let active = true;
        setSubeditions([]);
        setSubeditionId("");
        setRankings([]);
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const items = data ?? [];
            setSubeditions(items);
            setSubeditionId(items[0]?.id ?? "");
            if (!data) setError("Nie udało się pobrać podedycji.");
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate]);

    useEffect(() => {
        if (!subeditionId) {
            setRankings([]);
            return;
        }
        let active = true;
        setLoadingRankings(true);
        setRankings([]);
        setError("");
        const query = new URLSearchParams({
            subeditionId,
            ...Object.fromEntries(firstPage.split("&").map((part) => part.split("="))),
        });
        void apiListRequestResult<ApiRanking>(
            `${apiEndpoints.rankings}?${query.toString()}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) setRankings(data);
            else setError("Nie udało się pobrać opublikowanego rankingu.");
            setLoadingRankings(false);
        });
        return () => {
            active = false;
        };
    }, [subeditionId, navigate]);

    const visibleRankings = rankings.filter((item) => tier === "ALL" || item.tier === tier);
    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="app-public-page flex-1">
                <header className="mb-8 flex items-center justify-between gap-4 sm:gap-8">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Wyniki konkursu
                        </p>
                        <h1 className="mt-2 text-3xl font-bold text-ink-heading">
                            Rankingi drużyn
                        </h1>
                        <p className="mt-3 max-w-2xl text-ink-secondary">
                            Sprawdź opublikowane wyniki drużyn w poszczególnych edycjach i
                            podedycjach.
                        </p>
                    </div>
                    <KoalaIllustration
                        src={`${import.meta.env.BASE_URL}images/koala/koala_mis2d.png`}
                    />
                </header>
                {error && (
                    <p
                        role="status"
                        className="app-status-warning mb-4 rounded-xl border p-3 text-sm"
                    >
                        {error}
                    </p>
                )}
                <section
                    className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-7"
                    aria-label="Wybór rankingu"
                >
                    {loading ? (
                        <p className="text-sm text-ink-muted">Ładowanie edycji…</p>
                    ) : editions.length === 0 ? (
                        <p className="text-sm text-ink-muted">Nie ma jeszcze zamkniętych edycji.</p>
                    ) : (
                        <div className="grid gap-4 sm:grid-cols-2">
                            <label className="text-sm font-medium text-ink">
                                Edycja
                                <select
                                    className="mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading"
                                    value={editionId}
                                    onChange={(e) => setEditionId(e.target.value)}
                                >
                                    <option value="">Wybierz edycję</option>
                                    {editions.map((item) => (
                                        <option value={item.id} key={item.id}>
                                            {item.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                            <label className="text-sm font-medium text-ink">
                                Podedycja
                                <select
                                    className="mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading"
                                    value={subeditionId}
                                    onChange={(e) => setSubeditionId(e.target.value)}
                                    disabled={!subeditions.length}
                                >
                                    <option value="">Wybierz podedycję</option>
                                    {subeditions.map((item) => (
                                        <option value={item.id} key={item.id}>
                                            {item.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>
                    )}
                </section>
                {subeditionId && (
                    <section className="mt-8" aria-labelledby="published-ranking-title">
                        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
                            <div>
                                <h2
                                    id="published-ranking-title"
                                    className="text-xl font-semibold text-ink-heading"
                                >
                                    {subeditions.find((item) => item.id === subeditionId)?.name}
                                </h2>
                                <p className="mt-1 text-sm text-ink-muted">
                                    Kolejność według zdobytych punktów.
                                </p>
                            </div>
                            <div className="flex gap-2" aria-label="Filtruj według poziomu">
                                {[
                                    { value: "ALL", label: "Wszystkie" },
                                    { value: "TIER_1", label: "Poziom I" },
                                    { value: "TIER_2", label: "Poziom II" },
                                ].map((item) => (
                                    <button
                                        key={item.value}
                                        type="button"
                                        onClick={() => setTier(item.value)}
                                        aria-pressed={tier === item.value}
                                        className={`rounded-lg px-3 py-2 text-sm font-medium ${tier === item.value ? "bg-brand-soft text-brand-text-strong" : "bg-surface text-ink-secondary hover:bg-surface-muted"}`}
                                    >
                                        {item.label}
                                    </button>
                                ))}
                            </div>
                        </div>
                        {loadingRankings ? (
                            <p className="rounded-xl border border-line bg-surface p-6 text-sm text-ink-muted">
                                Ładowanie rankingu…
                            </p>
                        ) : visibleRankings.length === 0 ? (
                            <p className="rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                Ranking dla wybranej podedycji nie został jeszcze opublikowany.
                            </p>
                        ) : (
                            <div className="overflow-x-auto rounded-2xl border border-line bg-surface">
                                <table className="w-full text-left text-sm">
                                    <thead className="bg-surface-muted text-xs uppercase tracking-wide text-ink-secondary">
                                        <tr>
                                            <th className="px-4 py-3">Miejsce</th>
                                            <th className="px-4 py-3">Drużyna</th>
                                            <th className="px-4 py-3">Poziom</th>
                                            <th className="px-4 py-3 text-right">Punkty</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {visibleRankings.map((item, index) => (
                                            <tr
                                                key={`${item.teamName}-${item.tier}`}
                                                className="border-t border-line"
                                            >
                                                <td className="px-4 py-3 font-semibold text-ink-heading">
                                                    {index + 1}.
                                                </td>
                                                <td className="px-4 py-3 font-medium text-ink-heading">
                                                    {item.teamName}
                                                </td>
                                                <td className="px-4 py-3 text-ink-secondary">
                                                    {item.tier === "TIER_2"
                                                        ? "Poziom II"
                                                        : "Poziom I"}
                                                </td>
                                                <td className="px-4 py-3 text-right font-semibold text-ink-heading">
                                                    {item.totalScore} / {item.maxScore}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                )}
            </main>
            <PublicFooter />
        </div>
    );
}
