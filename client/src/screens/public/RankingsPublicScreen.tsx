import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import KoalaIllustration from "../../components/KoalaIllustration";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiEdition, ApiRanking, ApiSubedition } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";

type DisplayRanking = Pick<
    ApiRanking,
    "teamName" | "totalScore" | "maxScore" | "tier" | "latestUpdatedAt"
>;

export default function RankingsPublicScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionFilter, setSubeditionFilter] = useState("ALL");
    const [rankings, setRankings] = useState<DisplayRanking[]>([]);
    const [rankingsBySubedition, setRankingsBySubedition] = useState<
        Record<string, DisplayRanking[]>
    >({});
    const [tier, setTier] = useState("ALL");
    const [loading, setLoading] = useState(true);
    const [loadingRankings, setLoadingRankings] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const items = [...(data ?? [])].sort((a, b) => {
                const createdDifference = Date.parse(b.createdAt) - Date.parse(a.createdAt);
                if (Number.isFinite(createdDifference) && createdDifference !== 0)
                    return createdDifference;
                return Date.parse(b.teamRegisterClose) - Date.parse(a.teamRegisterClose);
            });
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
            setRankings([]);
            setRankingsBySubedition({});
            return;
        }
        let active = true;
        setLoadingRankings(true);
        setRankings([]);
        setRankingsBySubedition({});
        setSubeditionFilter("ALL");
        setError("");
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(async ({ data: subeditions }) => {
            if (!active) return;
            if (!subeditions) {
                setError("Nie udało się pobrać podedycji edycji.");
                setLoadingRankings(false);
                return;
            }
            setSubeditions(subeditions);

            const rankingResults = await Promise.all(
                subeditions.map((subedition) =>
                    apiListRequestResult<ApiRanking>(
                        `${apiEndpoints.rankings}?subeditionId=${encodeURIComponent(subedition.id)}&${firstPage}`,
                        null,
                        "GET",
                        navigate
                    )
                )
            );
            if (!active) return;

            const totals = new Map<string, DisplayRanking>();
            const maxScoreByTier = new Map<string, number>();
            const bySubedition: Record<string, DisplayRanking[]> = {};
            for (const [index, result] of rankingResults.entries()) {
                const rows = result.data ?? [];
                const subeditionId = subeditions[index].id;
                bySubedition[subeditionId] = [...rows].sort(
                    (a, b) => b.totalScore - a.totalScore || a.teamName.localeCompare(b.teamName)
                );
                const stageMaxByTier = new Map<string, number>();
                for (const row of rows) {
                    stageMaxByTier.set(
                        row.tier,
                        Math.max(stageMaxByTier.get(row.tier) ?? 0, row.maxScore)
                    );
                }
                for (const [stageTier, stageMax] of stageMaxByTier) {
                    maxScoreByTier.set(stageTier, (maxScoreByTier.get(stageTier) ?? 0) + stageMax);
                }
                for (const row of rows) {
                    const key = `${row.teamName}\u0000${row.tier}`;
                    const current = totals.get(key);
                    if (!current) {
                        totals.set(key, { ...row, maxScore: 0 });
                        continue;
                    }
                    current.totalScore += row.totalScore;
                    if (
                        row.latestUpdatedAt &&
                        (!current.latestUpdatedAt ||
                            Date.parse(row.latestUpdatedAt) > Date.parse(current.latestUpdatedAt))
                    ) {
                        current.latestUpdatedAt = row.latestUpdatedAt;
                    }
                }
            }

            for (const ranking of totals.values()) {
                ranking.maxScore = maxScoreByTier.get(ranking.tier) ?? ranking.maxScore;
            }

            setRankings(
                [...totals.values()].sort(
                    (a, b) => b.totalScore - a.totalScore || a.teamName.localeCompare(b.teamName)
                )
            );
            setRankingsBySubedition(bySubedition);
            setLoadingRankings(false);
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate]);

    const selectedSubedition = subeditions.find((item) => item.id === subeditionFilter);
    const displayedRankings =
        subeditionFilter === "ALL" ? rankings : (rankingsBySubedition[subeditionFilter] ?? []);
    const visibleRankings = displayedRankings.filter(
        (item) => tier === "ALL" || item.tier === tier
    );
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
                            Ranking edycji sumuje opublikowane wyniki ze wszystkich jej podedycji.
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
                    aria-label="Wybór edycji"
                >
                    {loading ? (
                        <p className="text-sm text-ink-muted">Ładowanie edycji…</p>
                    ) : editions.length === 0 ? (
                        <p className="text-sm text-ink-muted">Nie znaleziono edycji.</p>
                    ) : (
                        <div className="grid gap-5">
                            <label className="block max-w-xl text-sm font-medium text-ink">
                                Edycja
                                <select
                                    className="mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading"
                                    value={editionId}
                                    onChange={(event) => setEditionId(event.target.value)}
                                >
                                    {editions.map((item) => (
                                        <option value={item.id} key={item.id}>
                                            {item.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                            <div
                                className="flex flex-wrap items-center gap-2"
                                aria-label="Filtruj według podedycji"
                            >
                                <span className="mr-1 text-sm font-medium text-ink">
                                    Podedycja:
                                </span>
                                {[
                                    { value: "ALL", label: "Cała edycja" },
                                    ...subeditions.map((item) => ({
                                        value: item.id,
                                        label: item.name,
                                    })),
                                ].map((item) => (
                                    <button
                                        key={item.value}
                                        type="button"
                                        onClick={() => setSubeditionFilter(item.value)}
                                        aria-pressed={subeditionFilter === item.value}
                                        className={`rounded-lg px-3 py-2 text-sm font-medium ${subeditionFilter === item.value ? "bg-brand-soft text-brand-text-strong" : "bg-surface-muted text-ink-secondary hover:bg-surface"}`}
                                    >
                                        {item.label}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )}
                </section>
                {editionId && (
                    <section className="mt-8" aria-labelledby="published-ranking-title">
                        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
                            <div>
                                <h2
                                    id="published-ranking-title"
                                    className="text-xl font-semibold text-ink-heading"
                                >
                                    {selectedSubedition
                                        ? `Ranking: ${selectedSubedition.name}`
                                        : "Ranking całej edycji"}
                                </h2>
                                <p className="mt-1 text-sm text-ink-muted">
                                    {selectedSubedition
                                        ? "Wyniki wybranej podedycji."
                                        : "Łączna liczba punktów z opublikowanych rankingów podedycji."}
                                </p>
                            </div>
                            <div className="flex gap-2" aria-label="Filtruj według szkoły">
                                {[
                                    { value: "ALL", label: "Wszystkie" },
                                    { value: "TIER_1", label: "Podstawowa" },
                                    { value: "TIER_2", label: "Średnia" },
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
                                {selectedSubedition
                                    ? "Nie ma jeszcze opublikowanego rankingu dla tej podedycji."
                                    : "Nie ma jeszcze opublikowanych rankingów podedycji dla tej edycji."}
                            </p>
                        ) : (
                            <div className="overflow-x-auto rounded-2xl border border-line bg-surface">
                                <table className="w-full text-left text-sm">
                                    <thead className="bg-surface-muted text-xs uppercase tracking-wide text-ink-secondary">
                                        <tr>
                                            <th className="px-4 py-3">Miejsce</th>
                                            <th className="px-4 py-3">Drużyna</th>
                                            <th className="px-4 py-3">Szkoła</th>
                                            <th className="px-4 py-3 text-right">Punkty łącznie</th>
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
                                                        ? "Średnia"
                                                        : "Podstawowa"}
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
