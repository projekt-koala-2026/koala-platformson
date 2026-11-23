import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import type {
    ApiCompetitionTask,
    ApiEdition,
    ApiSubedition,
    ApiTaskSubmission,
} from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

type ReviewProgress = {
    totalSubmissions: number;
    unassigned: number;
    incomplete: number;
    pairs: { total: number; reviewer1Reviewed: number; reviewer2Reviewed: number }[];
};
type ReviewOverview = {
    submissions: number;
    // expectedSubmissions: number; // Requires access to the full teams endpoint.
    requiredSlots: number;
    unassigned: number;
    reviewedSlots: number;
    conflictSlots: number;
    conflicts: number;
};
type EditionOverview = {
    edition: ApiEdition;
    subeditions: ApiSubedition[];
    inactiveSubeditionsPastStart: ApiSubedition[];
    // tier1Teams: number; // Requires access to the full teams endpoint.
    // tier2Teams: number;
    // teamsWithoutGuardians: number;
    // rodo: { submitted: number; accepted: number; rejected: number; missing: number };
};

const PanelScreen = () => {
    const navigate = useNavigate();
    const [reviewOverview, setReviewOverview] = useState<ReviewOverview | null>(null);
    const [reviewOverviewLoading, setReviewOverviewLoading] = useState(true);
    const [summaryError, setSummaryError] = useState(false);
    const [summaryRevision, setSummaryRevision] = useState(0);
    const [hasActiveSubedition, setHasActiveSubedition] = useState(false);
    const [editionOverview, setEditionOverview] = useState<EditionOverview | null>(null);

    useEffect(() => {
        let active = true;
        const loadReviewOverview = async () => {
            setReviewOverviewLoading(true);
            setSummaryError(false);
            try {
                const editions = await apiListRequestResult<ApiEdition>(
                    `${apiEndpoints.editions}?${firstPage}&States=ACTIVE`,
                    null,
                    "GET",
                    navigate
                );
                if (!editions.data) throw new Error("Editions unavailable");
                let submissions = 0,
                    // expectedSubmissions = 0, // Requires access to the full teams endpoint.
                    requiredSlots = 0,
                    unassigned = 0,
                    reviewedSlots = 0,
                    conflictSlots = 0,
                    conflicts = 0;
                let activeSubeditionFound = false;
                let overview: EditionOverview | null = null;
                for (const edition of editions.data) {
                    /* Admins cannot use this full team endpoint; its dependent panel widgets are commented out below.
                    const teams = await apiListRequestResult<ApiTeam>(
                        `${apiEndpoints.teams}?${firstPage}&EditionId=${edition.id}`,
                        null,
                        "GET",
                        navigate
                    );
                    const editionTeams = teams.data ?? [];
                    */
                    const subeditions = await apiListRequestResult<ApiSubedition>(
                        `${apiEndpoints.subeditions(edition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    );
                    if (!subeditions.data) throw new Error("Edition details unavailable");
                    if (!overview) {
                        //const editionTeams = teams.data ?? [];
                        const editionSubeditions = subeditions.data ?? [];
                        const now = Date.now();
                        /* RODO endpoints require a moderator role; keep this calculation commented for future access.
                        const rodoResults = await Promise.all(
                            editionTeams.map((team) =>
                                apiListRequestResult<ApiTeamMember>(
                                    `${apiEndpoints.teamRodos(team.id)}?${firstPage}`,
                                    null,
                                    "GET",
                                    navigate
                                )
                            )
                        );
                        if (rodoResults.some((result) => !result.data))
                            throw new Error("Consent data unavailable");
                        const rodoDocuments = rodoResults.flatMap((result) => result.data ?? []);
                        const accepted = rodoDocuments.filter(
                            (member) => member.rodoState === "RODO_ACCEPTED"
                        ).length;
                        const rejected = rodoDocuments.filter(
                            (member) => member.rodoState === "RODO_REJECTED"
                        ).length;
                        const submitted = rodoDocuments.filter(
                            (member) => member.rodoState === "RODO_UNVERIFIED"
                        ).length;
                        */
                        overview = {
                            edition,
                            subeditions: editionSubeditions,
                            inactiveSubeditionsPastStart: editionSubeditions.filter((item) => {
                                const deadline = Date.parse(item.answerOpen);
                                return (
                                    item.state === "INACTIVE" &&
                                    Number.isFinite(deadline) &&
                                    deadline <= now
                                );
                            }),
                            /* tier1Teams: editionTeams.filter((team) => team.tier === "TIER_1")
                                .length,
                            tier2Teams: editionTeams.filter((team) => team.tier === "TIER_2")
                                .length,
                            teamsWithoutGuardians: editionTeams.filter((team) => !team.teamGuardian)
                                .length,
                            rodo: {
                                submitted,
                                accepted,
                                rejected,
                                missing: rodoDocuments.filter(
                                    (member) => member.rodoState === "RODO_NONE"
                                ).length,
                            }, */
                        };
                    }
                    for (const subedition of (subeditions.data ?? []).filter(
                        (item) => item.state === "ACTIVE"
                    )) {
                        activeSubeditionFound = true;
                        const taskList = await apiListRequestResult<ApiCompetitionTask>(
                            `${apiEndpoints.competitionTasks(edition.id, subedition.id)}?${firstPage}`,
                            null,
                            "GET",
                            navigate
                        );
                        if (!taskList.data) throw new Error("Tasks unavailable");
                        for (const task of taskList.data) {
                            /* expectedSubmissions += (teams.data ?? []).filter(
                                (team) =>
                                    team.tier === "TIER_2" ||
                                    (team.tier === "TIER_1" && task.tier === "TIER_1")
                            ).length; */
                            const result = await apiRequestResult<ReviewProgress>(
                                apiEndpoints.taskReviewProgress(task.id),
                                null,
                                "GET",
                                navigate
                            );
                            if (!result.data) throw new Error("Review progress unavailable");
                            submissions += result.data.totalSubmissions;
                            requiredSlots += result.data.totalSubmissions * 2;
                            unassigned += result.data.unassigned + result.data.incomplete;
                            const taskSubmissions = await apiListRequestResult<ApiTaskSubmission>(
                                apiEndpoints.taskSubmissions(task.id),
                                null,
                                "GET",
                                navigate
                            );
                            if (!taskSubmissions.data) throw new Error("Submissions unavailable");
                            reviewedSlots += taskSubmissions.data.reduce(
                                (count, submission) =>
                                    count +
                                    Number(submission.review1Score !== null) +
                                    Number(submission.review2Score !== null),
                                0
                            );
                            const taskConflicts = (taskSubmissions.data ?? []).filter(
                                (submission) =>
                                    submission.review1Score !== null &&
                                    submission.review2Score !== null &&
                                    submission.review1Score !== submission.review2Score
                            ).length;
                            conflicts += taskConflicts;
                            conflictSlots += taskConflicts * 2;
                        }
                    }
                }
                if (active) {
                    setHasActiveSubedition(activeSubeditionFound);
                    setEditionOverview(overview);
                    setReviewOverview({
                        submissions,
                        // expectedSubmissions,
                        requiredSlots,
                        unassigned,
                        reviewedSlots,
                        conflictSlots,
                        conflicts,
                    });
                }
            } catch {
                if (active) {
                    setSummaryError(true);
                    setEditionOverview(null);
                    setReviewOverview(null);
                    setHasActiveSubedition(false);
                }
            } finally {
                if (active) setReviewOverviewLoading(false);
            }
        };
        void loadReviewOverview();
        return () => {
            active = false;
        };
    }, [navigate, summaryRevision]);

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                {reviewOverviewLoading && (
                    <p role="status" className="mb-6 text-ink-secondary">
                        Ładowanie podsumowania konkursu…
                    </p>
                )}
                {summaryError && (
                    <div role="alert" className="app-status-warning mb-6 rounded-xl border p-4">
                        Nie udało się pobrać kompletnego podsumowania. Spróbuj ponownie — brak
                        danych nie oznacza wyniku zero.
                        <Button
                            text="Spróbuj ponownie"
                            onClick={() => setSummaryRevision((value) => value + 1)}
                        />
                    </div>
                )}
                {!reviewOverviewLoading && !summaryError && !editionOverview && (
                    <p className="mb-6 rounded-xl border border-line bg-surface p-4 text-ink-secondary">
                        Brak aktywnej edycji. Możesz przygotować kolejną w sekcji Edycje.
                    </p>
                )}
                {editionOverview && (
                    <section className="mb-8 rounded-2xl border border-line bg-surface p-6 shadow-sm">
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Aktywna edycja
                        </p>
                        <h1 className="mt-1 text-2xl font-bold">{editionOverview.edition.name}</h1>
                        <div className="mt-5 grid gap-5 lg:grid-cols-2">
                            <div className="rounded-xl bg-canvas p-4">
                                <h2 className="font-semibold">Etapy</h2>
                                {editionOverview.subeditions.length ? (
                                    <ul className="mt-2 flex flex-wrap gap-2">
                                        {editionOverview.subeditions.map((item) => (
                                            <li
                                                key={item.id}
                                                className={`rounded-full px-3 py-1 text-sm ${item.state === "ACTIVE" ? "border border-success-border bg-success-soft text-success-text" : "border border-line bg-surface-muted text-ink-secondary"}`}
                                            >
                                                {item.name}
                                                {item.state === "ACTIVE" ? " · aktywna" : ""}
                                            </li>
                                        ))}
                                    </ul>
                                ) : (
                                    <p className="mt-2 text-sm text-ink-secondary">Brak etapów.</p>
                                )}
                            </div>
                            {/* Team counts need the full team endpoint, which is restricted to RODO moderators.
                            <div className="rounded-xl bg-canvas p-4">
                                <h2 className="font-semibold">Zespoły według szkół</h2>
                                {(() => {
                                    const total =
                                        editionOverview.tier1Teams + editionOverview.tier2Teams;
                                    const tier1Width = total
                                        ? (editionOverview.tier1Teams / total) * 100
                                        : 0;
                                    return (
                                        <>
                                            <div
                                                className="mt-3 flex h-4 overflow-hidden rounded-full bg-surface-muted"
                                                role="img"
                                                aria-label={`Podstawowa: ${editionOverview.tier1Teams}, Średnia: ${editionOverview.tier2Teams}`}
                                            >
                                                <div
                                                    className="bg-brand"
                                                    style={{ width: `${tier1Width}%` }}
                                                />
                                                <div
                                                    className="bg-brand-muted"
                                                    style={{
                                                        width: `${total ? 100 - tier1Width : 0}%`,
                                                    }}
                                                />
                                            </div>
                                            <div className="mt-2 flex justify-between text-sm text-ink-secondary">
                                                <span>
                                                    Podstawowa: <b>{editionOverview.tier1Teams}</b>
                                                </span>
                                                <span>
                                                    Średnia: <b>{editionOverview.tier2Teams}</b>
                                                </span>
                                            </div>
                                        </>
                                    );
                                })()}
                            </div>
                            */}
                            {/* Guardian counts also depend on the restricted full-team endpoint.
                            {editionOverview.teamsWithoutGuardians > 0 && (
                                <div className="rounded-xl bg-canvas p-4">
                                    <h2 className="font-semibold">Opieka drużyn</h2>
                                    <p className="mt-2 text-sm text-ink-secondary">
                                        Drużyny bez opiekunów:{" "}
                                        <b className="text-ink-heading">
                                            {editionOverview.teamsWithoutGuardians}
                                        </b>
                                    </p>
                                </div>
                            )}
                            */}
                            {editionOverview.inactiveSubeditionsPastStart.length > 0 && (
                                <div
                                    role="status"
                                    className="rounded-xl border border-warning-border bg-warning-soft p-4 text-sm text-warning-text lg:col-span-2"
                                >
                                    <h2 className="font-semibold">
                                        Termin odsyłania zadań już się rozpoczął
                                    </h2>
                                    <ul className="mt-2 list-inside list-disc">
                                        {editionOverview.inactiveSubeditionsPastStart.map(
                                            (item) => (
                                                <li key={item.id}>
                                                    {item.name} — etap nie został aktywowany.
                                                </li>
                                            )
                                        )}
                                    </ul>
                                </div>
                            )}
                            {/* RODO summary is disabled because admins cannot access consent endpoints.
                            {editionOverview.rodo.submitted +
                                editionOverview.rodo.rejected +
                                editionOverview.rodo.missing >
                                0 && (
                                <div className="rounded-xl bg-canvas p-4 lg:col-span-2">
                                    <h2 className="font-semibold">Zgody RODO</h2>
                                    {(() => {
                                        const { submitted, accepted, rejected, missing } =
                                            editionOverview.rodo;
                                        const total = submitted + accepted + rejected + missing;
                                        const width = (count: number) =>
                                            `${total ? (count / total) * 100 : 0}%`;
                                        return (
                                            <>
                                                <div
                                                    className="mt-3 flex h-4 overflow-hidden rounded-full bg-surface-muted"
                                                    role="img"
                                                    aria-label={`Złożone: ${submitted}, potwierdzone: ${accepted}, odrzucone: ${rejected}, brakujące: ${missing}`}
                                                >
                                                    <div
                                                        className="bg-warning"
                                                        style={{ width: width(submitted) }}
                                                        title="Złożone, do weryfikacji"
                                                    />
                                                    <div
                                                        className="bg-brand"
                                                        style={{ width: width(accepted) }}
                                                        title="Potwierdzone"
                                                    />
                                                    <div
                                                        className="bg-danger"
                                                        style={{ width: width(rejected) }}
                                                        title="Odrzucone"
                                                    />
                                                    <div
                                                        className="bg-surface-muted"
                                                        style={{ width: width(missing) }}
                                                        title="Brak zgody"
                                                    />
                                                </div>

                                                <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-sm text-ink-secondary">
                                                    <span>
                                                        <b className="text-warning-text">
                                                            {submitted}
                                                        </b>{" "}
                                                        złożone
                                                    </span>
                                                    <span>
                                                        <b className="text-success-text">
                                                            {accepted}
                                                        </b>{" "}
                                                        potwierdzone
                                                    </span>
                                                    <span>
                                                        <b className="text-danger-text">
                                                            {rejected}
                                                        </b>{" "}
                                                        odrzucone
                                                    </span>
                                                    <span>
                                                        <b>{missing}</b> brakuje
                                                    </span>
                                                </div>
                                            </>
                                        );
                                    })()}
                                </div>
                            )}
                            */}
                            {hasActiveSubedition && (
                                <div className="rounded-xl bg-canvas p-4 lg:col-span-2">
                                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                                        Ocenianie zadań
                                    </p>
                                    <h2 className="mt-1 text-2xl font-bold text-ink-heading">
                                        Pokrycie ocen przez recenzentów
                                    </h2>
                                    {reviewOverviewLoading ? (
                                        <p className="mt-3 text-sm text-ink-secondary">
                                            Ładowanie postępu ocen…
                                        </p>
                                    ) : reviewOverview ? (
                                        <>
                                            <p className="mt-3 text-4xl font-bold text-brand-text">
                                                {reviewOverview.requiredSlots
                                                    ? Math.round(
                                                          (reviewOverview.reviewedSlots /
                                                              reviewOverview.requiredSlots) *
                                                              100
                                                      )
                                                    : 0}
                                                %
                                            </p>
                                            <p className="mt-1 text-sm text-ink-secondary">
                                                {reviewOverview.reviewedSlots} z{" "}
                                                {reviewOverview.requiredSlots} wymaganych ocen
                                                uzupełnionych · {reviewOverview.submissions}{" "}
                                                nadesłanych odpowiedzi
                                                {/* Missing submission totals require full team access.
                                                ·{" "}{Math.max(
                                                    0,
                                                    reviewOverview.expectedSubmissions -
                                                        reviewOverview.submissions
                                                )}{" "}
                                                odpowiedzi brakuje
                                                */}
                                            </p>
                                            {reviewOverview.unassigned > 0 && (
                                                <p className="mt-2 text-warning-text">
                                                    Odpowiedzi bez pełnego przydziału recenzentów:{" "}
                                                    {reviewOverview.unassigned}.
                                                </p>
                                            )}
                                            {reviewOverview.conflicts > 0 && (
                                                <p className="mt-2 text-sm font-semibold text-danger-text">
                                                    Występuje {reviewOverview.conflicts}{" "}
                                                    {reviewOverview.conflicts === 1
                                                        ? "konflikt"
                                                        : reviewOverview.conflicts >= 2 &&
                                                            reviewOverview.conflicts <= 4
                                                          ? "konflikty"
                                                          : "konfliktów"}{" "}
                                                    w ocenach.
                                                </p>
                                            )}
                                            <div
                                                className="mt-4 h-3 overflow-hidden rounded-full bg-surface-muted"
                                                role="progressbar"
                                                aria-label="Pokrycie ocen recenzentów"
                                                aria-valuenow={
                                                    reviewOverview.requiredSlots
                                                        ? Math.round(
                                                              (reviewOverview.reviewedSlots /
                                                                  reviewOverview.requiredSlots) *
                                                                  100
                                                          )
                                                        : 0
                                                }
                                                aria-valuemin={0}
                                                aria-valuemax={100}
                                            >
                                                <div
                                                    className="flex h-full"
                                                    style={{
                                                        width: `${reviewOverview.requiredSlots ? (reviewOverview.reviewedSlots / reviewOverview.requiredSlots) * 100 : 0}%`,
                                                    }}
                                                >
                                                    <div
                                                        className="h-full bg-brand"
                                                        style={{
                                                            width: `${reviewOverview.reviewedSlots ? (Math.max(0, reviewOverview.reviewedSlots - reviewOverview.conflictSlots) / reviewOverview.reviewedSlots) * 100 : 0}%`,
                                                        }}
                                                    />
                                                    <div
                                                        className="h-full bg-danger"
                                                        title="Oceny w konflikcie"
                                                        style={{
                                                            width: `${reviewOverview.reviewedSlots ? (reviewOverview.conflictSlots / reviewOverview.reviewedSlots) * 100 : 0}%`,
                                                        }}
                                                    />
                                                </div>
                                            </div>
                                        </>
                                    ) : (
                                        <p className="mt-3 text-sm text-ink-secondary">
                                            Nie udało się pobrać postępu ocen.
                                        </p>
                                    )}
                                </div>
                            )}
                        </div>
                    </section>
                )}
            </main>
        </div>
    );
};

export default PanelScreen;
