import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import KoalaIllustration from "../../components/KoalaIllustration";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import RankingResults from "../../components/RankingResults";
import SelectListbox from "../../components/SelectListbox";
import { useRankingBrowser } from "../../hooks/useRankingBrowser";
import type { ApiWinner } from "../../types/models";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

export default function RankingsPublicScreen() {
    const navigate = useNavigate();
    const browser = useRankingBrowser(true);
    const [winners, setWinners] = useState<{ editionId: string; items: ApiWinner[] }>({
        editionId: "",
        items: [],
    });
    const [winnerError, setWinnerError] = useState("");
    useEffect(() => {
        if (!browser.editionId || !browser.edition?.areWinnersPublic) return;
        let active = true;
        void apiRequestResult<ApiWinner[]>(
            apiEndpoints.editionWinners(browser.editionId),
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            setWinners({ editionId: browser.editionId, items: result.data ?? [] });
            setWinnerError(result.data ? "" : browser.editionId);
        });
        return () => {
            active = false;
        };
    }, [browser.editionId, browser.edition?.areWinnersPublic, navigate]);
    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="app-public-page">
                <header className="mb-8 flex items-start justify-between gap-4 sm:gap-8">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Wyniki konkursu
                        </p>
                        <h1 className="mt-2 text-3xl font-bold text-ink-heading">
                            Rankingi drużyn
                        </h1>
                        <p className="mt-3 text-ink-secondary">
                            Wybierz edycję i opublikowany ranking konkursu.
                        </p>
                    </div>
                    <KoalaIllustration
                        src={`${import.meta.env.BASE_URL}images/koala/koala_mis2d.png`}
                    />
                </header>
                <section className="mb-6 grid gap-4 rounded-2xl border border-line bg-surface p-5 sm:grid-cols-2">
                    <div>
                        <p
                            id="public-ranking-edition"
                            className="mb-2 text-sm font-semibold text-ink-heading"
                        >
                            Edycja
                        </p>
                        <SelectListbox
                            value={browser.editionId}
                            options={browser.editions.map((e) => ({ value: e.id, label: e.name }))}
                            onChange={browser.chooseEdition}
                            ariaLabelledBy="public-ranking-edition"
                        />
                    </div>
                    <div>
                        <p
                            id="public-ranking-name"
                            className="mb-2 text-sm font-semibold text-ink-heading"
                        >
                            Ranking
                        </p>
                        <SelectListbox
                            value={browser.rankingId}
                            options={browser.rankings.map((r) => ({
                                value: r.rankingId,
                                label: `${r.name} · ${r.tier === "TIER_1" ? "szkoły podstawowe" : "szkoły ponadpodstawowe"}`,
                            }))}
                            onChange={browser.chooseRanking}
                            ariaLabelledBy="public-ranking-name"
                        />
                    </div>
                </section>
                {browser.error ? (
                    <div role="alert" className="app-status-warning mb-6 rounded-xl border p-4">
                        {browser.error}
                        <Button text="Spróbuj ponownie" onClick={browser.refresh} />
                    </div>
                ) : browser.loading ? (
                    <p role="status" className="py-6 text-ink-secondary">
                        Ładowanie wyników…
                    </p>
                ) : !browser.ranking ? (
                    <p className="rounded-xl border border-dashed border-line-strong bg-surface p-8 text-center text-ink-muted">
                        Brak opublikowanych rankingów w wybranej edycji.
                    </p>
                ) : (
                    <section>
                        <h2 className="mb-4 text-xl font-semibold text-ink-heading">
                            {browser.ranking.name}
                        </h2>
                        <RankingResults
                            rows={browser.rows}
                            total={browser.total}
                            page={browser.page}
                            onPage={browser.setPage}
                        />
                    </section>
                )}
                {browser.edition?.areWinnersPublic &&
                    winners.editionId === browser.editionId &&
                    winners.items.length > 0 && (
                        <section className="mt-8 rounded-2xl border border-brand-border bg-brand-soft p-5">
                            <h2 className="text-xl font-semibold text-ink-heading">
                                Zwycięzcy edycji
                            </h2>
                            <ul className="mt-4 space-y-2">
                                {winners.items.map((w) => (
                                    <li key={w.id} className="text-ink">
                                        <span className="font-semibold">{w.teamName}</span> ·{" "}
                                        {w.tier === "TIER_1"
                                            ? "szkoły podstawowe"
                                            : "szkoły ponadpodstawowe"}
                                    </li>
                                ))}
                            </ul>
                        </section>
                    )}
                {browser.edition?.areWinnersPublic && winnerError === browser.editionId && (
                    <p role="status" className="app-status-warning mt-4 rounded-xl border p-4">
                        Nie udało się pobrać zwycięzców edycji.
                    </p>
                )}
            </main>
            <PublicFooter />
        </div>
    );
}
