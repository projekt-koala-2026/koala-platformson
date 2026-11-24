import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaEye, FaEyeSlash, FaLayerGroup, FaPlus, FaRedo, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import RankingResults from "../../components/RankingResults";
import SelectListbox from "../../components/SelectListbox";
import { useRankingBrowser } from "../../hooks/useRankingBrowser";
import type { ApiRankingData, ApiSubedition } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

type Mode =
    | "create"
    | "metadata"
    | "add-row"
    | "edit-row"
    | "merge"
    | "generate"
    | "delete-ranking"
    | "delete-row";
const tiers = [
    { value: "TIER_1", label: "Szkoły podstawowe" },
    { value: "TIER_2", label: "Szkoły ponadpodstawowe" },
];
const inputClass =
    "mt-1.5 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";
const titles: Record<Mode, string> = {
    create: "Nowy ranking ręczny",
    metadata: "Edytuj ranking",
    "add-row": "Dodaj drużynę do rankingu",
    "edit-row": "Edytuj wynik drużyny",
    merge: "Połącz rankingi",
    generate: "Wygeneruj rankingi z etapu",
    "delete-ranking": "Usuń ranking",
    "delete-row": "Usuń pozycję rankingu",
};

export default function RankingsScreen() {
    const browser = useRankingBrowser();
    const navigate = useNavigate();
    const [mode, setMode] = useState<Mode | null>(null);
    const [busy, setBusy] = useState(false);
    const [notice, setNotice] = useState("");
    const [name, setName] = useState("");
    const [tier, setTier] = useState("TIER_1");
    const [row, setRow] = useState<ApiRankingData | null>(null);
    const [points, setPoints] = useState("0");
    const [maximum, setMaximum] = useState("1");
    const [firstId, setFirstId] = useState("");
    const [secondId, setSecondId] = useState("");
    const [stages, setStages] = useState<ApiSubedition[]>([]);
    const [stageId, setStageId] = useState("");
    const [stagesLoading, setStagesLoading] = useState(false);
    const secondOptions = browser.rankings.filter(
        (r) =>
            r.rankingId !== firstId &&
            r.tier === browser.rankings.find((r) => r.rankingId === firstId)?.tier
    );

    useEffect(() => {
        if (mode !== "generate" || !browser.editionId) return;
        let active = true;
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(browser.editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            const closed = (result.data ?? []).filter((s) => s.state === "CLOSED");
            setStages(closed);
            setStageId(closed[0]?.id ?? "");
            setStagesLoading(false);
            if (!result.data) browser.setError("Nie udało się pobrać etapów.");
        });
        return () => {
            active = false;
        };
    }, [mode, browser.editionId, navigate, browser.setError]);

    const open = (next: Mode, item?: ApiRankingData) => {
        browser.setError("");
        setNotice("");
        setRow(item ?? null);
        setName(next === "metadata" ? (browser.ranking?.name ?? "") : (item?.teamName ?? ""));
        setTier(browser.ranking?.tier ?? "TIER_1");
        setPoints(String(item?.pointsAchived ?? 0));
        setMaximum(String(item?.pointsMax ?? 1));
        const first = browser.ranking ?? browser.rankings[0];
        setFirstId(first?.rankingId ?? "");
        setSecondId(
            browser.rankings.find((r) => r.rankingId !== first?.rankingId && r.tier === first?.tier)
                ?.rankingId ?? ""
        );
        if (next === "generate") {
            setStages([]);
            setStageId("");
            setStagesLoading(true);
        }
        setMode(next);
    };
    const perform = async (
        url: string,
        method: "POST" | "PUT" | "DELETE",
        body: unknown,
        message: string,
        resetSelection = false
    ) => {
        if (busy) return;
        setBusy(true);
        browser.setError("");
        setNotice("");
        const result = await apiRequestResult(url, body, method, navigate);
        if (result.status !== null && result.status >= 200 && result.status < 300) {
            setMode(null);
            setNotice(message);
            if (resetSelection) browser.chooseRanking("");
            browser.refresh();
        } else
            browser.setError(
                result.error?.message ?? "Nie udało się zapisać zmian. Spróbuj ponownie."
            );
        setBusy(false);
    };
    const submit = (event: FormEvent) => {
        event.preventDefault();
        const e = browser.editionId,
            r = browser.ranking;
        if (busy || !mode || !e) return;
        if (["create", "metadata", "merge", "add-row", "edit-row"].includes(mode) && !name.trim()) {
            browser.setError("Podaj nazwę.");
            return;
        }
        if (mode === "create")
            void perform(
                apiEndpoints.createRanking(e),
                "POST",
                { rankingName: name.trim(), rankingTier: tier, rankingData: [] },
                "Utworzono ranking. Dodaj do niego wyniki drużyn.",
                true
            );
        if (mode === "metadata" && r)
            void perform(
                apiEndpoints.ranking(e, r.rankingId),
                "PUT",
                { name: name.trim(), tier, version: r.version },
                "Zapisano dane rankingu."
            );
        if ((mode === "add-row" || mode === "edit-row") && r) {
            const achieved = Number(points),
                max = Number(maximum);
            if (
                !points.trim() ||
                !maximum.trim() ||
                !Number.isSafeInteger(achieved) ||
                !Number.isSafeInteger(max) ||
                achieved < 0 ||
                max < 0 ||
                achieved > max
            ) {
                browser.setError(
                    "Punkty muszą być liczbami całkowitymi od zera, a wynik nie może przekraczać maksimum."
                );
                return;
            }
            void perform(
                apiEndpoints.rankingData(e, r.rankingId, row?.id ?? crypto.randomUUID()),
                "PUT",
                {
                    teamName: name.trim(),
                    pointsAchived: achieved,
                    pointsMax: max,
                    version: row?.version ?? null,
                },
                "Zapisano wynik drużyny."
            );
        }
        if (mode === "merge") {
            if (
                !firstId ||
                !secondId ||
                !secondOptions.some((item) => item.rankingId === secondId)
            ) {
                browser.setError("Wybierz dwa różne rankingi tej samej kategorii.");
                return;
            }
            void perform(
                apiEndpoints.mergeRankings(e),
                "POST",
                { mergeRankingName: name.trim(), ranking1Id: firstId, ranking2Id: secondId },
                "Połączono rankingi w nowy, nieopublikowany ranking.",
                true
            );
        }
        if (mode === "generate" && stageId)
            void perform(
                apiEndpoints.generateRanking(e, stageId),
                "POST",
                null,
                "Wygenerowano rankingi obu kategorii. Sprawdź wyniki przed publikacją.",
                true
            );
        if (mode === "delete-ranking" && r)
            void perform(
                apiEndpoints.ranking(e, r.rankingId),
                "DELETE",
                null,
                "Usunięto ranking.",
                true
            );
        if (mode === "delete-row" && r && row)
            void perform(
                apiEndpoints.rankingData(e, r.rankingId, row.id),
                "DELETE",
                null,
                "Usunięto pozycję rankingu."
            );
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Wyniki konkursu
                    </p>
                    <h1 className="mt-1 text-3xl font-bold">Rankingi</h1>
                    <p className="mt-2 text-ink-secondary">
                        Twórz rankingi ręcznie lub na podstawie zakończonych etapów. Łącz wyniki i
                        publikuj je po sprawdzeniu.
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
                                Ranking
                            </p>
                            <SelectListbox
                                value={browser.rankingId}
                                options={browser.rankings.map((r) => ({
                                    value: r.rankingId,
                                    label: `${r.name} · ${r.tier === "TIER_1" ? "szkoły podstawowe" : "szkoły ponadpodstawowe"} · ${r.isPublic ? "publiczny" : "roboczy"}`,
                                }))}
                                onChange={browser.chooseRanking}
                                ariaLabelledBy="ranking-label"
                            />
                        </div>
                    </fieldset>
                    <div className="mt-5 flex flex-wrap gap-3">
                        <Button
                            text="Nowy ranking ręczny"
                            icon={<FaPlus />}
                            disabled={!browser.editionId || busy}
                            onClick={() => open("create")}
                        />
                        <Button
                            text="Generuj z etapu"
                            icon={<FaPlus />}
                            disabled={
                                !browser.editionId || busy || browser.edition?.state === "CLOSED"
                            }
                            onClick={() => open("generate")}
                        />
                        <Button
                            text="Połącz rankingi"
                            icon={<FaLayerGroup />}
                            disabled={browser.rankings.length < 2 || browser.loading || busy}
                            onClick={() => open("merge")}
                        />
                        <Button
                            text="Odśwież"
                            icon={<FaRedo />}
                            disabled={busy}
                            onClick={browser.refresh}
                        />
                    </div>
                    {browser.edition?.state === "CLOSED" && (
                        <p className="mt-3 text-sm text-ink-muted">
                            Edycja jest zakończona. Możesz zarządzać zapisanymi rankingami lub
                            utworzyć ranking ręczny.
                        </p>
                    )}
                </section>
                {browser.loading ? (
                    <p role="status" className="py-8 text-ink-secondary">
                        Ładowanie rankingów…
                    </p>
                ) : !browser.editionId ? (
                    <p className="py-8 text-ink-muted">Brak edycji konkursu.</p>
                ) : !browser.ranking ? (
                    <p className="py-8 text-ink-muted">
                        Brak rankingów w tej edycji. Utwórz ranking ręczny lub wygeneruj wyniki z
                        zakończonego etapu.
                    </p>
                ) : (
                    <section className="mt-8">
                        <div className="mb-5 flex flex-wrap items-center justify-between gap-4">
                            <div>
                                <h2 className="text-xl font-semibold">{browser.ranking.name}</h2>
                                <p className="mt-1 text-sm text-ink-secondary">
                                    {browser.ranking.isPublic
                                        ? "Widoczny na stronie publicznej"
                                        : "Ranking roboczy — niewidoczny publicznie"}
                                </p>
                            </div>
                            <div className="flex flex-wrap gap-2">
                                <Button
                                    text="Dodaj drużynę"
                                    icon={<FaPlus />}
                                    disabled={busy}
                                    onClick={() => open("add-row")}
                                />
                                <Button
                                    text="Edytuj ranking"
                                    icon={<FaEdit />}
                                    disabled={busy}
                                    onClick={() => open("metadata")}
                                />
                                <Button
                                    text={
                                        browser.ranking.isPublic
                                            ? "Ukryj ranking"
                                            : "Opublikuj ranking"
                                    }
                                    icon={browser.ranking.isPublic ? <FaEyeSlash /> : <FaEye />}
                                    disabled={
                                        busy || (!browser.ranking.isPublic && browser.total === 0)
                                    }
                                    onClick={() =>
                                        void perform(
                                            apiEndpoints.rankingPublication(
                                                browser.editionId,
                                                browser.rankingId,
                                                !browser.ranking!.isPublic
                                            ),
                                            "PUT",
                                            null,
                                            browser.ranking!.isPublic
                                                ? "Ukryto ranking."
                                                : "Opublikowano ranking."
                                        )
                                    }
                                />
                                <Button
                                    text="Usuń ranking"
                                    icon={<FaTrash />}
                                    disabled={busy}
                                    className="bg-danger hover:bg-danger-hover"
                                    onClick={() => open("delete-ranking")}
                                />
                            </div>
                        </div>
                        {browser.ranking.isPublic && (
                            <p className="app-status-warning mb-4 rounded-xl border p-3 text-sm">
                                Zmiany w tym rankingu są od razu widoczne publicznie. Możesz go
                                ukryć na czas edycji.
                            </p>
                        )}
                        <RankingResults
                            rows={browser.rows}
                            total={browser.total}
                            page={browser.page}
                            onPage={browser.setPage}
                            onEdit={(item) => open("edit-row", item)}
                            onDelete={(item) => open("delete-row", item)}
                            disabled={busy}
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
                        {mode &&
                            ["create", "metadata", "merge", "add-row", "edit-row"].includes(
                                mode
                            ) && (
                                <label className="block text-sm font-medium">
                                    {mode.includes("row") ? "Nazwa drużyny" : "Nazwa rankingu"}
                                    <input
                                        autoFocus
                                        required
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        className={inputClass}
                                    />
                                </label>
                            )}
                        {(mode === "create" || mode === "metadata") && (
                            <div>
                                <p id="ranking-tier-label" className="mb-2 text-sm font-medium">
                                    Kategoria
                                </p>
                                <SelectListbox
                                    value={tier}
                                    options={tiers}
                                    onChange={setTier}
                                    ariaLabelledBy="ranking-tier-label"
                                />
                            </div>
                        )}
                        {(mode === "add-row" || mode === "edit-row") && (
                            <div className="grid gap-4 sm:grid-cols-2">
                                <label className="text-sm font-medium">
                                    Zdobyte punkty
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        required
                                        value={points}
                                        onChange={(e) => setPoints(e.target.value)}
                                        className={inputClass}
                                    />
                                </label>
                                <label className="text-sm font-medium">
                                    Maksymalna liczba punktów
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        required
                                        value={maximum}
                                        onChange={(e) => setMaximum(e.target.value)}
                                        className={inputClass}
                                    />
                                </label>
                            </div>
                        )}
                        {mode === "merge" && (
                            <>
                                <p className="text-sm text-ink-secondary">
                                    Wyniki drużyn o identycznej nazwie zostaną zsumowane. Powstanie
                                    nowy ranking; źródłowe rankingi pozostaną bez zmian.
                                </p>
                                <div>
                                    <p id="merge-first-label" className="mb-2 text-sm font-medium">
                                        Pierwszy ranking
                                    </p>
                                    <SelectListbox
                                        value={firstId}
                                        options={browser.rankings.map((r) => ({
                                            value: r.rankingId,
                                            label: r.name,
                                        }))}
                                        onChange={(id) => {
                                            setFirstId(id);
                                            setSecondId(
                                                browser.rankings.find(
                                                    (r) =>
                                                        r.rankingId !== id &&
                                                        r.tier ===
                                                            browser.rankings.find(
                                                                (first) => first.rankingId === id
                                                            )?.tier
                                                )?.rankingId ?? ""
                                            );
                                        }}
                                        ariaLabelledBy="merge-first-label"
                                    />
                                </div>
                                <div>
                                    <p id="merge-second-label" className="mb-2 text-sm font-medium">
                                        Drugi ranking tej samej kategorii
                                    </p>
                                    <SelectListbox
                                        value={secondId}
                                        options={secondOptions.map((r) => ({
                                            value: r.rankingId,
                                            label: r.name,
                                        }))}
                                        onChange={setSecondId}
                                        ariaLabelledBy="merge-second-label"
                                    />
                                </div>
                                {!secondOptions.length && (
                                    <p className="text-sm text-warning-text">
                                        Brak drugiego rankingu tej samej kategorii.
                                    </p>
                                )}
                            </>
                        )}
                        {mode === "generate" && (
                            <>
                                {stagesLoading ? (
                                    <p role="status">Ładowanie etapów…</p>
                                ) : !stages.length ? (
                                    <p>Brak zakończonych etapów w tej edycji.</p>
                                ) : (
                                    <div>
                                        <p
                                            id="generate-stage-label"
                                            className="mb-2 text-sm font-medium"
                                        >
                                            Zakończony etap
                                        </p>
                                        <SelectListbox
                                            value={stageId}
                                            options={stages.map((s) => ({
                                                value: s.id,
                                                label: s.name,
                                            }))}
                                            onChange={setStageId}
                                            ariaLabelledBy="generate-stage-label"
                                        />
                                    </div>
                                )}
                                <p className="text-sm text-ink-secondary">
                                    Wszystkie przesłane odpowiedzi muszą mieć uzgodnione oceny.
                                    Powstaną dwa rankingi robocze, po jednym dla każdej kategorii
                                    szkół.
                                </p>
                            </>
                        )}
                        {mode === "delete-ranking" && (
                            <p>
                                Usunąć ranking „{browser.ranking?.name}” i wszystkie jego wyniki?
                                Tej operacji nie można cofnąć.
                            </p>
                        )}
                        {mode === "delete-row" && (
                            <p>Usunąć wynik drużyny „{row?.teamName}” z rankingu?</p>
                        )}
                        <div className="flex justify-end gap-3 pt-2">
                            <Button
                                text="Anuluj"
                                className="bg-surface-muted text-ink hover:bg-surface-disabled"
                                onClick={() => {
                                    setMode(null);
                                    browser.setError("");
                                }}
                            />
                            <Button
                                type="submit"
                                text={
                                    busy
                                        ? "Zapisywanie…"
                                        : mode?.startsWith("delete")
                                          ? "Usuń"
                                          : mode === "generate"
                                            ? "Generuj"
                                            : mode === "merge"
                                              ? "Połącz"
                                              : "Zapisz"
                                }
                                disabled={
                                    busy ||
                                    (mode === "generate" && (!stageId || stagesLoading)) ||
                                    (mode === "merge" && !secondId)
                                }
                            />
                        </div>
                    </fieldset>
                </form>
            </Modal>
        </div>
    );
}
