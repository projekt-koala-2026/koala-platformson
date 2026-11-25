import { useState, type FormEvent } from "react";
import { FaEye, FaEyeSlash, FaPlus, FaRedo } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import RankingResults from "../../components/RankingResults";
import SelectListbox from "../../components/SelectListbox";
import { useRankingBrowser } from "../../hooks/useRankingBrowser";
import type { ApiRankingData } from "../../types/models";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

type Mode = "add" | "edit" | "delete" | "generate-series" | "generate-total" | "publish";
const titles: Record<Mode, string> = {
    add: "Dodaj wynik drużyny",
    edit: "Edytuj wynik drużyny",
    delete: "Usuń wynik drużyny",
    "generate-series": "Przelicz rankingi serii",
    "generate-total": "Przelicz sumę I etapu",
    publish: "Widoczność rankingów",
};
const inputClass =
    "mt-1.5 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";

export default function RankingsScreen() {
    const browser = useRankingBrowser();
    const navigate = useNavigate();
    const [mode, setMode] = useState<Mode | null>(null);
    const [busy, setBusy] = useState(false);
    const [notice, setNotice] = useState("");
    const [row, setRow] = useState<ApiRankingData | null>(null);
    const [name, setName] = useState("");
    const [points, setPoints] = useState("0");
    const [maximum, setMaximum] = useState("1");
    const [qualified, setQualified] = useState(false);
    const [confirmed, setConfirmed] = useState(false);
    const ranking = browser.ranking;
    const selectedSeries = browser.series.find((series) => series.id === ranking?.etap1SeriesId);
    const isSeries = Boolean(ranking?.etap1SeriesId);
    const allClosed =
        browser.seriesReady &&
        browser.series.length > 0 &&
        browser.series.every((s) => s.state === "CLOSED");
    const canGenerateSeries =
        browser.seriesReady &&
        selectedSeries?.state === "CLOSED" &&
        browser.edition?.state !== "CLOSED";
    const disabled = busy || browser.loading;

    const open = (next: Mode, item?: ApiRankingData) => {
        browser.setError("");
        setNotice("");
        setRow(item ?? null);
        setName(item?.teamName ?? "");
        setPoints(String(item?.pointsAchived ?? 0));
        setMaximum(String(item?.pointsMax ?? 1));
        setQualified(item?.hasQualifiedToNextEtap ?? false);
        setConfirmed(false);
        setMode(next);
    };
    const perform = async (
        url: string,
        method: "POST" | "PUT" | "DELETE",
        body: unknown,
        message: string
    ) => {
        if (busy) return;
        setBusy(true);
        browser.setError("");
        setNotice("");
        const result = await apiRequestResult(url, body, method, navigate);
        if (result.status !== null && result.status >= 200 && result.status < 300) {
            setMode(null);
            setNotice(message);
            browser.refresh();
        } else
            browser.setError(
                result.error?.message ??
                    "Nie udało się zapisać zmian. Odśwież wyniki przed ponowną edycją."
            );
        setBusy(false);
    };
    const submit = (event: FormEvent) => {
        event.preventDefault();
        if (busy || !mode || !ranking) return;
        if (mode === "add" || mode === "edit") {
            const achieved = Number(points),
                max = Number(maximum);
            if (
                !name.trim() ||
                !points.trim() ||
                !maximum.trim() ||
                !Number.isSafeInteger(achieved) ||
                !Number.isSafeInteger(max) ||
                achieved < 0 ||
                max < 0 ||
                achieved > max
            ) {
                browser.setError(
                    "Podaj nazwę oraz całkowite punkty od zera, nieprzekraczające maksimum."
                );
                return;
            }
            void perform(
                apiEndpoints.rankingData(ranking.rankingId, row?.id ?? crypto.randomUUID()),
                "PUT",
                {
                    teamName: name.trim(),
                    hasQualifiedToNextEtap: qualified,
                    pointsAchived: achieved,
                    pointsMax: max,
                    version: row?.version ?? null,
                },
                "Zapisano wynik drużyny."
            );
        } else if (mode === "delete" && row) {
            void perform(
                apiEndpoints.rankingData(ranking.rankingId, row.id),
                "DELETE",
                null,
                "Usunięto wynik drużyny."
            );
        } else if (
            mode === "generate-series" &&
            ranking.etap1SeriesId &&
            canGenerateSeries &&
            confirmed
        ) {
            void perform(
                apiEndpoints.generateRanking(browser.editionId, ranking.etap1SeriesId),
                "POST",
                null,
                "Przeliczono rankingi obu kategorii tej serii."
            );
        } else if (mode === "generate-total" && allClosed && confirmed) {
            void perform(
                apiEndpoints.generateTotalRanking(browser.editionId),
                "POST",
                null,
                "Przeliczono rankingi sumaryczne obu kategorii I etapu."
            );
        } else if (mode === "publish") {
            const url = ranking.etap1SeriesId
                ? apiEndpoints.seriesRankingPublication(ranking.etap1SeriesId, !ranking.isPublic)
                : apiEndpoints.totalRankingPublication(browser.editionId, !ranking.isPublic);
            void perform(
                url,
                "PUT",
                null,
                ranking.isPublic
                    ? "Ukryto rankingi obu kategorii."
                    : "Opublikowano rankingi obu kategorii."
            );
        }
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Wyniki konkursu
                    </p>
                    <h1 className="mt-1 text-3xl font-bold">Rankingi I etapu</h1>
                    <p className="mt-2 text-ink-secondary">
                        Przelicz wyniki serii, przygotuj sumę I etapu i oznacz drużyny awansujące
                        dalej.
                    </p>
                </header>
                {browser.error && !mode && (
                    <p role="alert" className="app-status-danger mb-5 rounded-xl border p-4">
                        {browser.error}
                    </p>
                )}
                {notice && (
                    <p role="status" className="app-status-success mb-5 rounded-xl border p-4">
                        {notice}
                    </p>
                )}
                <section className="rounded-2xl border border-line bg-surface p-5">
                    <fieldset
                        disabled={busy || mode !== null}
                        className="grid gap-4 sm:grid-cols-2"
                    >
                        <div>
                            <p id="ranking-edition-label" className="mb-2 text-sm font-semibold">
                                Edycja
                            </p>
                            <SelectListbox
                                value={browser.editionId}
                                options={browser.editions.map((e) => ({
                                    value: e.id,
                                    label: e.name,
                                }))}
                                onChange={browser.chooseEdition}
                                ariaLabelledBy="ranking-edition-label"
                            />
                        </div>
                        <div>
                            <p id="ranking-label" className="mb-2 text-sm font-semibold">
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
                                ariaLabelledBy="ranking-label"
                            />
                        </div>
                    </fieldset>
                    <div className="mt-4 flex flex-wrap gap-3">
                        <Button
                            text="Odśwież"
                            icon={<FaRedo />}
                            disabled={busy || mode !== null}
                            onClick={browser.refresh}
                        />
                        <Button
                            text="Przelicz sumę I etapu"
                            disabled={disabled || !allClosed || !ranking}
                            onClick={() => open("generate-total")}
                        />
                    </div>
                    {!allClosed && browser.editionId && (
                        <p className="mt-3 text-sm text-ink-secondary">
                            Sumę I etapu można przeliczyć po zamknięciu wszystkich jego serii.
                            Najpierw przelicz rankingi każdej serii.
                        </p>
                    )}
                </section>
                {browser.loading ? (
                    <p role="status" className="py-8 text-ink-secondary">
                        Ładowanie rankingów…
                    </p>
                ) : !ranking ? (
                    <p className="py-8 text-ink-muted">
                        Brak rankingów w wybranej edycji. Nowe rankingi powstają automatycznie przy
                        dodawaniu edycji i serii.
                    </p>
                ) : (
                    <section className="mt-8">
                        <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
                            <div>
                                <h2 className="text-xl font-semibold">{ranking.name}</h2>
                                <p className="mt-1 text-sm text-ink-secondary">
                                    {isSeries
                                        ? "Ranking serii I etapu"
                                        : "Ranking sumaryczny I etapu"}{" "}
                                    · {ranking.isPublic ? "opublikowany" : "nieopublikowany"}
                                </p>
                            </div>
                            <div className="flex flex-wrap gap-3">
                                {isSeries && (
                                    <Button
                                        text="Przelicz rankingi serii"
                                        disabled={disabled || !canGenerateSeries}
                                        onClick={() => open("generate-series")}
                                    />
                                )}
                                <Button
                                    text="Dodaj wynik"
                                    icon={<FaPlus />}
                                    disabled={disabled}
                                    onClick={() => open("add")}
                                />
                                <Button
                                    text={
                                        ranking.isPublic
                                            ? "Ukryj obie kategorie"
                                            : "Opublikuj obie kategorie"
                                    }
                                    icon={ranking.isPublic ? <FaEyeSlash /> : <FaEye />}
                                    disabled={disabled}
                                    onClick={() => open("publish")}
                                />
                            </div>
                        </div>
                        {isSeries && !canGenerateSeries && (
                            <p className="mb-4 text-sm text-ink-secondary">
                                Przeliczanie serii jest dostępne po jej zamknięciu, przed
                                zakończeniem całej edycji.
                            </p>
                        )}
                        {ranking.isPublic && (
                            <p className="app-status-warning mb-4 rounded-xl border p-3 text-sm">
                                Zmiany wyników i awansu są od razu widoczne publicznie. Możesz ukryć
                                obie kategorie na czas edycji.
                            </p>
                        )}
                        <RankingResults
                            rows={browser.rows}
                            total={browser.total}
                            page={browser.page}
                            onPage={browser.setPage}
                            onEdit={(item) => open("edit", item)}
                            onDelete={(item) => open("delete", item)}
                            disabled={disabled}
                        />
                    </section>
                )}
            </main>
            <Modal
                isOpen={mode !== null}
                onClose={() => {
                    if (!busy) {
                        setMode(null);
                        browser.setError("");
                    }
                }}
                title={mode ? titles[mode] : "Ranking"}
                maxWidth="lg"
            >
                <form onSubmit={submit} className="space-y-4">
                    {browser.error && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3">
                            {browser.error}
                        </p>
                    )}
                    <fieldset disabled={busy} className="space-y-4">
                        {(mode === "add" || mode === "edit") && (
                            <>
                                <label className="block text-sm font-medium">
                                    Nazwa drużyny
                                    <input
                                        autoFocus
                                        required
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        className={inputClass}
                                    />
                                </label>
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <label className="block text-sm font-medium">
                                        Zdobyte punkty
                                        <input
                                            required
                                            type="number"
                                            min="0"
                                            step="1"
                                            value={points}
                                            onChange={(e) => setPoints(e.target.value)}
                                            className={inputClass}
                                        />
                                    </label>
                                    <label className="block text-sm font-medium">
                                        Maksymalna liczba punktów
                                        <input
                                            required
                                            type="number"
                                            min="0"
                                            step="1"
                                            value={maximum}
                                            onChange={(e) => setMaximum(e.target.value)}
                                            className={inputClass}
                                        />
                                    </label>
                                </div>
                                <label className="flex items-center gap-3">
                                    <input
                                        type="checkbox"
                                        checked={qualified}
                                        onChange={(e) => setQualified(e.target.checked)}
                                        className="h-4 w-4 accent-brand"
                                    />
                                    Awans do kolejnego etapu
                                </label>
                                {isSeries && (
                                    <p className="text-sm text-ink-secondary">
                                        Oznaczenie awansu dotyczy tej pozycji. Nie przenosi się
                                        automatycznie do rankingu sumarycznego.
                                    </p>
                                )}
                            </>
                        )}
                        {mode === "delete" && (
                            <p>
                                Usunąć wynik drużyny „{row?.teamName}”? Ponowne przeliczenie może
                                odtworzyć tę pozycję.
                            </p>
                        )}
                        {(mode === "generate-series" || mode === "generate-total") && (
                            <>
                                <p>
                                    {mode === "generate-series"
                                        ? `Przeliczyć obie kategorie serii „${selectedSeries?.name}”?`
                                        : "Zsumować wyniki wszystkich serii I etapu w obu kategoriach?"}
                                </p>
                                <p className="app-status-warning rounded-xl border p-3">
                                    Przeliczenie zastąpi dotychczasowe wyniki i usunie ich ręczne
                                    korekty oraz oznaczenia awansu. Widoczność publiczna pozostanie
                                    bez zmian.
                                </p>
                                {mode === "generate-total" && (
                                    <p className="text-sm text-ink-secondary">
                                        Upewnij się, że wyniki każdej serii zostały już przeliczone.
                                        Samo zamknięcie serii nie uruchamia sprawdzania rankingu.
                                    </p>
                                )}
                                <label className="flex items-center gap-3">
                                    <input
                                        required
                                        type="checkbox"
                                        checked={confirmed}
                                        onChange={(e) => setConfirmed(e.target.checked)}
                                        className="h-4 w-4 accent-brand"
                                    />
                                    Potwierdzam zastąpienie wyników obu kategorii
                                </label>
                            </>
                        )}
                        {mode === "publish" && (
                            <p>
                                {ranking?.isPublic ? "Ukryć" : "Opublikować"} rankingi{" "}
                                <strong>obu kategorii</strong>{" "}
                                {isSeries
                                    ? `serii „${selectedSeries?.name ?? ranking?.name}”`
                                    : "sumaryczne I etapu"}
                                ?{" "}
                                {ranking?.isPublic
                                    ? ""
                                    : "Przed publikacją sprawdź wyniki obu kategorii."}
                            </p>
                        )}
                        <div className="flex flex-wrap justify-end gap-3">
                            <Button
                                text="Anuluj"
                                onClick={() => {
                                    setMode(null);
                                    browser.setError("");
                                }}
                            />
                            <Button
                                text={
                                    busy
                                        ? "Zapisywanie…"
                                        : mode === "delete"
                                          ? "Usuń wynik"
                                          : "Potwierdź"
                                }
                                type="submit"
                                disabled={
                                    busy ||
                                    ((mode === "generate-series" || mode === "generate-total") &&
                                        !confirmed)
                                }
                            />
                        </div>
                    </fieldset>
                </form>
            </Modal>
        </div>
    );
}
