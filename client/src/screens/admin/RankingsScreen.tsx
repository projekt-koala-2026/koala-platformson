import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import SelectListbox from "../../components/SelectListbox";
import type {
    ApiAdminTeam,
    ApiCompetitionTask,
    ApiEdition,
    ApiRanking,
    ApiSubedition,
    ApiTaskSubmission,
} from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

const rankingsQuery = `${firstPage}`;

export default function RankingsScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [stagesByEdition, setStagesByEdition] = useState<Record<string, ApiSubedition[]>>({});
    const [readiness, setReadiness] = useState<{ id: string; reason: string } | null>(null);
    const [revision, setRevision] = useState(0);
    const subeditions = stagesByEdition[editionId] ?? [];
    const [subeditionId, setSubeditionId] = useState("");
    const [rankings, setRankings] = useState<ApiRanking[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingRanking, setLoadingRanking] = useState(false);
    const [busy, setBusy] = useState(false);
    const [published, setPublished] = useState(false);
    const [notice, setNotice] = useState("");
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then(async ({ data }) => {
            if (!active) return;
            if (!data) {
                setError("Nie uda\u0142o si\u0119 pobra\u0107 edycji.");
                setLoading(false);
                return;
            }
            const results = await Promise.all(
                data.map(async (edition) => ({
                    edition,
                    result: await apiListRequestResult<ApiSubedition>(
                        `${apiEndpoints.subeditions(edition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    ),
                }))
            );
            if (!active) return;
            if (results.some(({ result }) => !result.data)) {
                setError(
                    "Nie uda\u0142o si\u0119 pobra\u0107 wszystkich etap\u00f3w. Od\u015bwie\u017c stron\u0119."
                );
                setLoading(false);
                return;
            }
            const eligible = results.filter(({ result }) => result.data!.length > 0);
            const items = eligible
                .map(({ edition }) => edition)
                .sort((a, b) => Date.parse(b.teamRegisterClose) - Date.parse(a.teamRegisterClose));
            const stages = Object.fromEntries(
                eligible.map(({ edition, result }) => [edition.id, result.data!])
            );
            const selected = items.find((edition) => edition.state === "ACTIVE") ?? items[0];
            setStagesByEdition(stages);
            setEditions(items);
            setEditionId(selected?.id ?? "");
            setSubeditionId(selected ? stages[selected.id][0].id : "");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    useEffect(() => {
        if (!subeditionId) {
            setRankings([]);
            setPublished(false);
            return;
        }
        let active = true;
        setLoadingRanking(true);
        setRankings([]);
        setPublished(false);
        setReadiness(null);
        setNotice("");
        setError("");
        const load = async () => {
            const [draft, publicRanking, tasks, teams] = await Promise.all([
                apiListRequestResult<ApiRanking>(
                    `${apiEndpoints.rankingDraft(subeditionId)}?${rankingsQuery}`,
                    null,
                    "GET",
                    navigate
                ),
                apiListRequestResult<ApiRanking>(
                    `${apiEndpoints.rankings}?subeditionId=${encodeURIComponent(subeditionId)}&${rankingsQuery}`,
                    null,
                    "GET",
                    navigate
                ),
                apiListRequestResult<ApiCompetitionTask>(
                    `${apiEndpoints.competitionTasks(editionId, subeditionId)}?${firstPage}`,
                    null,
                    "GET",
                    navigate
                ),
                apiListRequestResult<ApiAdminTeam>(
                    `${apiEndpoints.adminTeams}?EditionId=${encodeURIComponent(editionId)}&${firstPage}`,
                    null,
                    "GET",
                    navigate
                ),
            ]);
            if (!active) return;
            if (!draft.data || !publicRanking.data) {
                setError("Nie udało się pobrać rankingu. Spróbuj ponownie.");
                setLoadingRanking(false);
                return;
            }
            setRankings(draft.data);
            setPublished(Boolean(publicRanking.data.length));
            if (editions.find((item) => item.id === editionId)?.state === "CLOSED") {
                setReadiness({
                    id: subeditionId,
                    reason: "Edycja jest zakończona. Dane konkursowe zostały usunięte, więc nie można ponownie wygenerować rankingu. Zapisany ranking można nadal przeglądać i opublikować.",
                });
                setLoadingRanking(false);
                return;
            }
            if (!tasks.data || !teams.data) {
                setError(
                    "Nie uda\u0142o si\u0119 sprawdzi\u0107 gotowo\u015bci rankingu. Spr\u00f3buj ponownie."
                );
                setLoadingRanking(false);
                return;
            }
            const results = await Promise.all(
                tasks.data.map((task) =>
                    apiListRequestResult<ApiTaskSubmission>(
                        `${apiEndpoints.taskSubmissions(task.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    )
                )
            );
            if (!active) return;
            if (results.some((result) => !result.data)) {
                setError(
                    "Nie uda\u0142o si\u0119 pobra\u0107 wszystkich ocen. Spr\u00f3buj ponownie."
                );
                setLoadingRanking(false);
                return;
            }
            const submissions = results.flatMap((result) => result.data!);
            const selected = stagesByEdition[editionId]?.find((stage) => stage.id === subeditionId);
            const edition = editions.find((item) => item.id === editionId);
            const unfinished = submissions.filter(
                (row) =>
                    !row.reviewer1Id ||
                    !row.reviewer2Id ||
                    row.reviewer1Id === row.reviewer2Id ||
                    row.review1Score === null ||
                    row.review2Score === null ||
                    row.review1Score !== row.review2Score ||
                    row.autoReviewState === null
            ).length;
            let reason = "";
            if (!selected || !edition) reason = "Wybierz etap konkursu.";
            else if (
                edition.state !== "CLOSED" &&
                (!Number.isFinite(Date.parse(selected.answerClose)) ||
                    Date.now() <= Date.parse(selected.answerClose))
            )
                reason =
                    "Najpierw musi up\u0142yn\u0105\u0107 termin nadsy\u0142ania odpowiedzi tego etapu.";
            else if (!teams.data.length)
                reason = "Do edycji nie zg\u0142oszono jeszcze dru\u017cyn.";
            else if (teams.data.some((team) => !["TIER_1", "TIER_2"].includes(team.tier)))
                reason = "Najpierw popraw poziomy dru\u017cyn w edycji.";
            else if (unfinished)
                reason = `Najpierw doko\u0144cz ocenianie: ${unfinished} odpowiedzi wymaga uzupe\u0142nienia przydzia\u0142u, dw\u00f3ch zgodnych ocen lub wyniku sprawdzarki.`;
            setReadiness({ id: subeditionId, reason });
            setRankings(draft.data);
            setPublished(Boolean(publicRanking.data.length));
            setLoadingRanking(false);
        };
        void load();
        return () => {
            active = false;
        };
    }, [subeditionId, editionId, editions, stagesByEdition, navigate, revision]);

    const canGenerate =
        readiness?.id === subeditionId && readiness.reason === "" && !loadingRanking;
    const canPublish =
        readiness?.id === subeditionId &&
        !loadingRanking &&
        (editions.find((item) => item.id === editionId)?.state === "CLOSED" || canGenerate) &&
        rankings.length > 0 &&
        !published;
    const changeEdition = (id: string) => {
        if (busy) return;
        setEditionId(id);
        setSubeditionId(stagesByEdition[id]?.[0]?.id ?? "");
    };
    const changeStage = (id: string) => {
        if (!busy) setSubeditionId(id);
    };

    const generate = async () => {
        if (!subeditionId || busy || !canGenerate) return;
        setBusy(true);
        setError("");
        setNotice("");
        const result = await apiRequestResult(
            apiEndpoints.generateRanking(subeditionId),
            null,
            "POST",
            navigate
        );
        if (!result.error && result.status && result.status >= 200 && result.status < 300) {
            const data = await apiListRequestResult<ApiRanking>(
                `${apiEndpoints.rankingDraft(subeditionId)}?${rankingsQuery}`,
                null,
                "GET",
                navigate
            );
            if (data.data) {
                setRankings(data.data);
                setPublished(false);
                setNotice("Ranking został wygenerowany. Możesz go teraz opublikować.");
            } else {
                setReadiness(null);
                setError(
                    "Ranking wygenerowano, ale nie udało się pobrać jego podglądu. Odśwież stan rankingu."
                );
            }
        } else setError(result.error?.message ?? "Nie udało się wygenerować rankingu.");
        setBusy(false);
    };

    const publish = async () => {
        if (!subeditionId || busy || !canPublish) return;
        setBusy(true);
        setError("");
        setNotice("");
        const result = await apiRequestResult(
            apiEndpoints.publishRanking(subeditionId),
            null,
            "PUT",
            navigate
        );
        if (!result.error && result.status && result.status >= 200 && result.status < 300) {
            setPublished(true);
            setNotice("Ranking został opublikowany na stronie publicznej.");
        } else setError(result.error?.message ?? "Nie udało się opublikować rankingu.");
        setBusy(false);
    };

    const selectedSubedition = subeditions.find((item) => item.id === subeditionId);
    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Panel administracyjny
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">Rankingi drużyn</h1>
                    <p className="mt-3 max-w-2xl text-ink-secondary">
                        Generuj ranking na podstawie zatwierdzonych punktów, sprawdź wynik i
                        opublikuj go dla uczestników.
                    </p>
                </header>
                {error && (
                    <p role="alert" className="app-status-danger mb-4 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                {notice && (
                    <p role="status" className="app-status-success mb-4 rounded-xl border p-3">
                        {notice}
                    </p>
                )}
                <section className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-7">
                    {loading ? (
                        <p className="text-sm text-ink-muted">Ładowanie edycji…</p>
                    ) : editions.length === 0 ? (
                        <p className="text-sm text-ink-muted">
                            {error
                                ? "Wybór etapów jest niedostępny do czasu poprawnego pobrania danych."
                                : "Obecnie żadna edycja nie ma etapu."}
                        </p>
                    ) : (
                        <div className="grid gap-4 sm:grid-cols-2">
                            <div>
                                <span
                                    id="ranking-edition-label"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Edycja
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="ranking-edition-label"
                                    value={editionId}
                                    options={editions.map((item) => ({
                                        value: item.id,
                                        label: item.name,
                                    }))}
                                    onChange={changeEdition}
                                />
                            </div>
                            <div>
                                <span
                                    id="ranking-subedition-label"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Etap
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="ranking-subedition-label"
                                    value={subeditionId}
                                    options={subeditions.map((item) => ({
                                        value: item.id,
                                        label: item.name,
                                    }))}
                                    onChange={changeStage}
                                />
                            </div>
                        </div>
                    )}
                    {selectedSubedition && (
                        <>
                            <p className="mt-4 text-sm text-ink-secondary">
                                Ranking dla etapu{" "}
                                <strong className="text-ink-heading">
                                    {selectedSubedition.name}
                                </strong>
                                . Ranking etapu można wygenerować po zakończeniu czasu odpowiedzi i
                                rozstrzygnięciu wszystkich ocen, nawet gdy edycja trwa.
                            </p>
                            <div className="mt-5 flex flex-wrap gap-3">
                                <Button
                                    text={busy ? "Przetwarzanie…" : "Generuj ranking"}
                                    disabled={busy || !canGenerate}
                                    onClick={() => void generate()}
                                />
                                <Button
                                    text={
                                        busy
                                            ? "Przetwarzanie…"
                                            : published
                                              ? "Opublikowano"
                                              : "Opublikuj ranking"
                                    }
                                    disabled={busy || !canPublish}
                                    onClick={() => void publish()}
                                />
                            </div>
                            {loadingRanking ? (
                                <p role="status" className="mt-3 text-sm text-ink-secondary">
                                    Sprawdzanie gotowości rankingu…
                                </p>
                            ) : readiness?.id === subeditionId && readiness.reason ? (
                                <p
                                    role="status"
                                    className="app-status-warning mt-3 rounded-xl border p-3 text-sm"
                                >
                                    {readiness.reason}
                                </p>
                            ) : null}
                            <button
                                type="button"
                                disabled={busy || loadingRanking}
                                onClick={() => setRevision((value) => value + 1)}
                                className="mt-3 text-sm text-brand-text underline disabled:opacity-50"
                            >
                                Odśwież stan ocen i rankingu
                            </button>
                        </>
                    )}
                </section>
                {subeditionId && (
                    <section className="mt-8" aria-labelledby="draft-ranking-heading">
                        <div className="mb-4 flex items-end justify-between gap-3">
                            <div>
                                <h2
                                    id="draft-ranking-heading"
                                    className="text-xl font-semibold text-ink-heading"
                                >
                                    Podgląd rankingu
                                </h2>
                                <p className="mt-1 text-sm text-ink-muted">
                                    Posortowany według liczby zdobytych punktów.
                                </p>
                            </div>
                            <span className="text-xs text-ink-muted">
                                {published ? "Opublikowany" : "Roboczy"}
                            </span>
                        </div>
                        {loadingRanking ? (
                            <p className="rounded-xl border border-line bg-surface p-6 text-sm text-ink-muted">
                                Ładowanie rankingu…
                            </p>
                        ) : rankings.length === 0 ? (
                            <p className="rounded-xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                {editions.find((item) => item.id === editionId)?.state === "CLOSED"
                                    ? "Brak zapisanego rankingu dla tego etapu. Po zakończeniu edycji nie można go już wygenerować."
                                    : "Brak rankingu. Wygeneruj go po zakończeniu oceniania tego etapu."}
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
                                        {rankings.map((item, index) => (
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
        </div>
    );
}
