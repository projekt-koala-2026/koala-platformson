import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import KoalaIllustration from "../../components/KoalaIllustration";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import RankingResults from "../../components/RankingResults";
import SelectListbox from "../../components/SelectListbox";
import { useRankingBrowser } from "../../hooks/useRankingBrowser";

export default function RankingsPublicScreen() {
    const navigate = useNavigate();
    const browser = useRankingBrowser(true);
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
                            Sprawdź wyniki poszczególnych serii i sumę I etapu konkursu.
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
                            Ranking serii lub suma I etapu
                        </p>
                        <SelectListbox
                            value={browser.rankingId}
                            options={browser.rankings.map((r) => ({
                                value: r.rankingId,
                                label: r.etap1SeriesId
                                    ? r.name
                                    : `Suma I etapu · ${r.tier === "TIER_1" ? "szkoły podstawowe" : "szkoły ponadpodstawowe"}`,
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
                        <h2 className="text-xl font-semibold text-ink-heading">
                            {browser.ranking.name}
                        </h2>
                        <p className="mb-4 mt-2 text-sm text-ink-secondary">
                            {browser.ranking.etap1SeriesId
                                ? "Wyniki serii I etapu."
                                : "Łączne wyniki serii I etapu."}{" "}
                            Kolumna awansu pokazuje kwalifikację oznaczoną przez organizatorów w tym
                            rankingu.
                        </p>
                        <RankingResults
                            rows={browser.rows}
                            total={browser.total}
                            page={browser.page}
                            onPage={browser.setPage}
                        />
                    </section>
                )}
            </main>
            <PublicFooter />
        </div>
    );
}
