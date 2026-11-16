import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type {
    ApiCompetitionTask,
    ApiEdition,
    ApiSubedition,
    ApiTaskSubmission,
    ApiTeam,
    Role,
    User,
} from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

const USERS_ENDPOINT = apiEndpoints.users;
const editableRoles = [
    "ORGANIZATION_ADMIN",
    "ORGANIZATION_EDITOR",
    "ORGANIZATION_REVIEWER",
] as const satisfies readonly Role[];
const roleLabels: Partial<Record<Role, string>> = {
    ORGANIZATION_ADMIN: "administrator",
    ORGANIZATION_EDITOR: "redaktor",
    ORGANIZATION_REVIEWER: "recenzent",
};
const organizationUsersQuery = new URLSearchParams({
    PageNumber: "0",
    PageSize: "100",
});
editableRoles.forEach((role) => organizationUsersQuery.append("UserRoles", role));
const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";

type Feedback = { tone: "success" | "error"; message: string } | null;
type ReviewProgress = {
    totalSubmissions: number;
    unassigned: number;
    incomplete: number;
    pairs: { total: number; reviewer1Reviewed: number; reviewer2Reviewed: number }[];
};
type ReviewOverview = {
    submissions: number;
    expectedSubmissions: number;
    assignedSlots: number;
    reviewedSlots: number;
    conflictSlots: number;
    conflicts: number;
};
type EditionOverview = {
    edition: ApiEdition;
    subeditions: ApiSubedition[];
    tier1Teams: number;
    tier2Teams: number;
    teamsWithoutGuardians: number;
    rodo: { submitted: number; accepted: number; rejected: number; missing: number };
};

const sortUsers = (users: User[]) =>
    [...users].sort((first, second) => first.email.localeCompare(second.email, "pl"));

const PanelScreen = () => {
    const navigate = useNavigate();
    const currentUserId = useMemo(() => localStorage.getItem("userId"), []);
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [email, setEmail] = useState("");
    const [newRoles, setNewRoles] = useState<Role[]>([]);
    const [createError, setCreateError] = useState("");
    const [creating, setCreating] = useState(false);
    const [editingUser, setEditingUser] = useState<User | null>(null);
    const [editedRoles, setEditedRoles] = useState<Role[]>([]);
    const [editError, setEditError] = useState("");
    const [updatingRoles, setUpdatingRoles] = useState(false);
    const [reviewOverview, setReviewOverview] = useState<ReviewOverview | null>(null);
    const [reviewOverviewLoading, setReviewOverviewLoading] = useState(true);
    const [hasActiveSubedition, setHasActiveSubedition] = useState(false);
    const [editionOverview, setEditionOverview] = useState<EditionOverview | null>(null);

    useEffect(() => {
        let active = true;
        const loadReviewOverview = async () => {
            const editions = await apiListRequestResult<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}&States=ACTIVE`,
                null,
                "GET",
                navigate
            );
            if (!editions.data) {
                if (active) {
                    setHasActiveSubedition(false);
                    setReviewOverviewLoading(false);
                }
                return;
            }
            let submissions = 0,
                expectedSubmissions = 0,
                assignedSlots = 0,
                reviewedSlots = 0,
                conflictSlots = 0,
                conflicts = 0;
            let activeSubeditionFound = false;
            let overview: EditionOverview | null = null;
            for (const edition of editions.data) {
                const teams = await apiListRequestResult<ApiTeam>(
                    `${apiEndpoints.teams}?${firstPage}&EditionId=${edition.id}`,
                    null,
                    "GET",
                    navigate
                );
                const subeditions = await apiListRequestResult<ApiSubedition>(
                    `${apiEndpoints.subeditions(edition.id)}?${firstPage}`,
                    null,
                    "GET",
                    navigate
                );
                if (!overview) {
                    const editionTeams = teams.data ?? [];
                    const members = editionTeams.flatMap((team) => team.teamMembers ?? []);
                    const accepted = members.filter(
                        (member) => member.rodoState === "RODO_ACCEPTED"
                    ).length;
                    const rejected = members.filter(
                        (member) => member.rodoState === "RODO_REJECTED"
                    ).length;
                    const submitted = members.filter(
                        (member) => member.rodoState === "RODO_UNVERIFIED"
                    ).length;
                    overview = {
                        edition,
                        subeditions: subeditions.data ?? [],
                        tier1Teams: editionTeams.filter((team) => team.tier === "TIER_1").length,
                        tier2Teams: editionTeams.filter((team) => team.tier === "TIER_2").length,
                        teamsWithoutGuardians: editionTeams.filter((team) => !team.teamGuardian)
                            .length,
                        rodo: {
                            submitted,
                            accepted,
                            rejected,
                            missing: members.filter((member) => member.rodoState === "RODO_NONE")
                                .length,
                        },
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
                    for (const task of taskList.data ?? []) {
                        expectedSubmissions += (teams.data ?? []).filter(
                            (team) =>
                                team.tier === "TIER_2" ||
                                (team.tier === "TIER_1" && task.tier === "TIER_1")
                        ).length;
                        const result = await apiRequestResult<ReviewProgress>(
                            apiEndpoints.taskReviewProgress(task.id),
                            null,
                            "GET",
                            navigate
                        );
                        if (!result.data) continue;
                        submissions += result.data.totalSubmissions;
                        for (const pair of result.data.pairs) {
                            assignedSlots += pair.total * 2;
                            reviewedSlots += pair.reviewer1Reviewed + pair.reviewer2Reviewed;
                        }
                        const taskSubmissions = await apiListRequestResult<ApiTaskSubmission>(
                            apiEndpoints.taskSubmissions(task.id),
                            null,
                            "GET",
                            navigate
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
                    expectedSubmissions,
                    assignedSlots,
                    reviewedSlots,
                    conflictSlots,
                    conflicts,
                });
                setReviewOverviewLoading(false);
            }
        };
        void loadReviewOverview();
        return () => {
            active = false;
        };
    }, [navigate]);

    useEffect(() => {
        let active = true;
        void apiListRequestResult<User>(
            `${USERS_ENDPOINT}?${organizationUsersQuery}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) setUsers(sortUsers(data));
            else setFeedback({ tone: "error", message: "Nie udało się pobrać użytkowników." });
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const closeCreate = () => {
        if (creating) return;
        setIsCreateOpen(false);
        setEmail("");
        setNewRoles([]);
        setCreateError("");
    };

    const openRoleEditor = (user: User) => {
        setEditingUser(user);
        setEditedRoles(
            user.roles.filter((role) =>
                editableRoles.includes(role as (typeof editableRoles)[number])
            )
        );
        setEditError("");
    };

    const closeRoleEditor = () => {
        if (updatingRoles) return;
        setEditingUser(null);
        setEditedRoles([]);
        setEditError("");
    };

    const toggleRole = (
        role: Role,
        roles: Role[],
        setter: React.Dispatch<React.SetStateAction<Role[]>>
    ) => {
        setter(roles.includes(role) ? roles.filter((item) => item !== role) : [...roles, role]);
    };

    const createUser = async () => {
        const normalizedEmail = email.trim().toLowerCase();
        setCreateError("");
        if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) {
            setCreateError("Podaj prawidłowy adres e-mail.");
            return;
        }
        if (newRoles.length === 0) {
            setCreateError("Wybierz co najmniej jedną rolę.");
            return;
        }

        setCreating(true);
        const { status } = await apiRequestResult<null>(
            USERS_ENDPOINT,
            { email: normalizedEmail, userRoles: newRoles },
            "POST",
            navigate
        );
        if (status !== 201) {
            setCreateError("Nie udało się wysłać zaproszenia. Adres może być już zajęty.");
            setCreating(false);
            return;
        }

        setCreating(false);
        setIsCreateOpen(false);
        setEmail("");
        setNewRoles([]);
        setCreateError("");
        setFeedback({
            tone: "success",
            message: `Wysłano zaproszenie na adres ${normalizedEmail}.`,
        });
    };

    const deleteUser = async (user: User) => {
        if (user.id === currentUserId) {
            setFeedback({ tone: "error", message: "Nie można usunąć aktualnie używanego konta." });
            return;
        }
        if (!window.confirm(`Czy na pewno chcesz usunąć użytkownika ${user.email}?`)) return;

        setDeletingId(user.id);
        setFeedback(null);
        const { status } = await apiRequestResult<null>(
            `${USERS_ENDPOINT}/${user.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status !== 200 && status !== 204) {
            setFeedback({ tone: "error", message: "Nie udało się usunąć użytkownika." });
        } else {
            setUsers((current) => current.filter((item) => item.id !== user.id));
            setFeedback({ tone: "success", message: `Usunięto konto ${user.email}.` });
        }
        setDeletingId(null);
    };

    const updateRoles = async () => {
        if (!editingUser) return;
        setEditError("");
        if (editedRoles.length === 0) {
            setEditError("Wybierz co najmniej jedną rolę.");
            return;
        }
        if (
            editedRoles.length === editingUser.roles.length &&
            editedRoles.every((role) => editingUser.roles.includes(role))
        ) {
            setEditError("Nie wprowadzono żadnych zmian.");
            return;
        }

        setUpdatingRoles(true);
        const { data, error } = await apiRequestResult<User>(
            `${USERS_ENDPOINT}/${editingUser.id}/roles`,
            { userRoles: editedRoles, version: editingUser.version },
            "PUT",
            navigate
        );
        if (!data) {
            setEditError(error?.message ?? "Nie udało się zapisać ról użytkownika.");
            setUpdatingRoles(false);
            return;
        }

        setUsers((current) =>
            sortUsers(current.map((user) => (user.id === data.id ? data : user)))
        );
        setFeedback({ tone: "success", message: `Zmieniono role konta ${data.email}.` });
        setUpdatingRoles(false);
        setEditingUser(null);
        setEditedRoles([]);
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                {editionOverview && (
                    <section className="mb-8 rounded-2xl border border-line bg-surface p-6 shadow-sm">
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Aktywna edycja
                        </p>
                        <h1 className="mt-1 text-2xl font-bold">{editionOverview.edition.name}</h1>
                        <div className="mt-5 grid gap-5 lg:grid-cols-2">
                            <div className="rounded-xl bg-canvas p-4">
                                <h2 className="font-semibold">Podedycje</h2>
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
                                    <p className="mt-2 text-sm text-ink-secondary">
                                        Brak podedycji.
                                    </p>
                                )}
                            </div>
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
                                                aria-label={`Poziom I: ${editionOverview.tier1Teams}, poziom II: ${editionOverview.tier2Teams}`}
                                            >
                                                <div
                                                    className="bg-brand"
                                                    style={{ width: `${tier1Width}%` }}
                                                />
                                                <div
                                                    className="bg-emerald-800"
                                                    style={{ width: `${100 - tier1Width}%` }}
                                                />
                                            </div>
                                            <div className="mt-2 flex justify-between text-sm text-ink-secondary">
                                                <span>
                                                    Podstawówka: <b>{editionOverview.tier1Teams}</b>
                                                </span>
                                                <span>
                                                    Liceum: <b>{editionOverview.tier2Teams}</b>
                                                </span>
                                            </div>
                                        </>
                                    );
                                })()}
                            </div>
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
                            {editionOverview.subeditions.some((item) => {
                                const deadline = Date.parse(item.answerOpen);
                                return (
                                    item.state === "INACTIVE" &&
                                    Number.isFinite(deadline) &&
                                    deadline <= Date.now()
                                );
                            }) && (
                                <div
                                    role="status"
                                    className="rounded-xl border border-warning-border bg-warning-soft p-4 text-sm text-warning-text lg:col-span-2"
                                >
                                    <h2 className="font-semibold">
                                        Termin odsyłania zadań już się rozpoczął
                                    </h2>
                                    <ul className="mt-2 list-inside list-disc">
                                        {editionOverview.subeditions
                                            .filter((item) => {
                                                const deadline = Date.parse(item.answerOpen);
                                                return (
                                                    item.state === "INACTIVE" &&
                                                    Number.isFinite(deadline) &&
                                                    deadline <= Date.now()
                                                );
                                            })
                                            .map((item) => (
                                                <li key={item.id}>
                                                    {item.name} — podedycja nie została aktywowana.
                                                </li>
                                            ))}
                                    </ul>
                                </div>
                            )}

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
                                                {reviewOverview.assignedSlots
                                                    ? Math.round(
                                                          (reviewOverview.reviewedSlots /
                                                              reviewOverview.assignedSlots) *
                                                              100
                                                      )
                                                    : 0}
                                                %
                                            </p>
                                            <p className="mt-1 text-sm text-ink-secondary">
                                                {reviewOverview.reviewedSlots} z{" "}
                                                {reviewOverview.assignedSlots} przydzielonych ocen
                                                uzupełnionych · {reviewOverview.submissions}{" "}
                                                nadesłanych odpowiedzi ·{" "}
                                                {Math.max(
                                                    0,
                                                    reviewOverview.expectedSubmissions -
                                                        reviewOverview.submissions
                                                )}{" "}
                                                odpowiedzi brakuje
                                            </p>
                                            {reviewOverview.conflicts > 0 && (
                                                <p className="mt-2 text-sm font-semibold text-red-700">
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
                                                    reviewOverview.assignedSlots
                                                        ? Math.round(
                                                              (reviewOverview.reviewedSlots /
                                                                  reviewOverview.assignedSlots) *
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
                                                        width: `${reviewOverview.assignedSlots ? (reviewOverview.reviewedSlots / reviewOverview.assignedSlots) * 100 : 0}%`,
                                                    }}
                                                >
                                                    <div
                                                        className="h-full bg-brand"
                                                        style={{
                                                            width: `${reviewOverview.reviewedSlots ? (Math.max(0, reviewOverview.reviewedSlots - reviewOverview.conflictSlots) / reviewOverview.reviewedSlots) * 100 : 0}%`,
                                                        }}
                                                    />
                                                    <div
                                                        className="h-full bg-red-600"
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
                <section className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">
                            Użytkownicy panelu
                        </h1>
                        <p className="mt-2 max-w-2xl text-sm text-ink-secondary">
                            Zapraszaj użytkowników panelu i przypisuj im role. Zaproszona osoba
                            kończy rejestrację przez odnośnik wysłany w wiadomości e-mail.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Zaproś użytkownika</span>
                            </>
                        }
                        onClick={() => setIsCreateOpen(true)}
                    />
                </section>

                {feedback && (
                    <div
                        role="status"
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success" ? "app-status-success" : "app-status-danger"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                <section className="mt-8 space-y-4">
                    {loading ? (
                        <div className="rounded-2xl border border-line bg-surface p-8 text-center text-sm text-ink-muted shadow-sm">
                            Ładowanie użytkowników…
                        </div>
                    ) : users.length === 0 ? (
                        <p className="rounded-2xl border border-dashed border-line-strong bg-surface p-10 text-center text-sm text-ink-muted">
                            Brak użytkowników do wyświetlenia.
                        </p>
                    ) : (
                        users.map((user) => {
                            const isCurrentUser = user.id === currentUserId;
                            return (
                                <article
                                    key={user.id}
                                    className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6"
                                >
                                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                        <div className="min-w-0">
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h2 className="truncate text-lg font-semibold">
                                                    {user.email}
                                                </h2>
                                                {isCurrentUser && (
                                                    <span className="rounded-full bg-surface-muted px-2.5 py-1 text-xs font-medium text-ink-secondary">
                                                        Twoje konto
                                                    </span>
                                                )}
                                            </div>
                                            <div className="mt-3 flex flex-wrap gap-2">
                                                {user.roles.map((role) => (
                                                    <span
                                                        key={role}
                                                        className="rounded-full bg-brand-soft px-3 py-1 text-xs font-semibold text-brand-text-strong"
                                                    >
                                                        {roleLabels[
                                                            role as keyof typeof roleLabels
                                                        ] ?? role.toLowerCase()}
                                                    </span>
                                                ))}
                                            </div>
                                        </div>
                                        <div className="flex shrink-0 gap-2">
                                            <Button
                                                text={
                                                    <>
                                                        <FaEdit />
                                                        <span className="sr-only">Edytuj role</span>
                                                    </>
                                                }
                                                disabled={isCurrentUser || updatingRoles}
                                                onClick={() => openRoleEditor(user)}
                                                title={
                                                    isCurrentUser
                                                        ? "Nie można zmienić ról aktualnie używanego konta."
                                                        : "Edytuj role"
                                                }
                                                className="px-3"
                                            />
                                            <Button
                                                text={
                                                    <>
                                                        <FaTrash />
                                                        <span className="sr-only">
                                                            Usuń użytkownika
                                                        </span>
                                                    </>
                                                }
                                                disabled={isCurrentUser || deletingId === user.id}
                                                onClick={() => void deleteUser(user)}
                                                className="bg-danger px-3 hover:bg-danger-hover"
                                            />
                                        </div>
                                    </div>
                                </article>
                            );
                        })
                    )}
                </section>
            </main>

            <Modal
                isOpen={isCreateOpen}
                onClose={closeCreate}
                title="Zaproś użytkownika"
                maxWidth="md"
            >
                <form
                    className="space-y-4"
                    onSubmit={(event) => {
                        event.preventDefault();
                        void createUser();
                    }}
                >
                    {createError && (
                        <p role="alert" className="rounded-lg border p-3 text-sm app-status-danger">
                            {createError}
                        </p>
                    )}
                    <label className="block text-sm font-semibold text-ink">
                        E-mail
                        <input
                            className={inputClass}
                            type="email"
                            autoComplete="email"
                            value={email}
                            disabled={creating}
                            onChange={(event) => setEmail(event.target.value)}
                            required
                        />
                    </label>
                    <div>
                        <p className="mb-2 text-sm font-semibold text-ink">Role</p>
                        <div className="flex flex-wrap gap-3">
                            {editableRoles.map((role) => (
                                <label
                                    key={role}
                                    className="flex cursor-pointer items-center gap-2 rounded-lg border border-line px-3 py-2 text-sm font-medium text-ink transition hover:bg-brand-soft"
                                >
                                    <input
                                        type="checkbox"
                                        checked={newRoles.includes(role)}
                                        disabled={creating}
                                        onChange={() => toggleRole(role, newRoles, setNewRoles)}
                                    />
                                    {roleLabels[role]}
                                </label>
                            ))}
                        </div>
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={creating}
                            onClick={closeCreate}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={creating ? "Wysyłanie…" : "Wyślij zaproszenie"}
                            disabled={creating}
                        />
                    </div>
                </form>
            </Modal>

            <Modal
                isOpen={editingUser !== null}
                onClose={closeRoleEditor}
                title="Edytuj role użytkownika"
                maxWidth="md"
            >
                <form
                    className="space-y-4"
                    onSubmit={(event) => {
                        event.preventDefault();
                        void updateRoles();
                    }}
                >
                    <p className="text-sm text-ink-secondary">
                        Konto: <span className="font-semibold text-ink">{editingUser?.email}</span>
                    </p>
                    {editError && (
                        <p role="alert" className="rounded-lg border p-3 text-sm app-status-danger">
                            {editError}
                        </p>
                    )}
                    <div>
                        <p className="mb-2 text-sm font-semibold text-ink">Role</p>
                        <div className="flex flex-wrap gap-3">
                            {editableRoles.map((role) => (
                                <label
                                    key={role}
                                    className="flex cursor-pointer items-center gap-2 rounded-lg border border-line px-3 py-2 text-sm font-medium text-ink transition hover:bg-brand-soft"
                                >
                                    <input
                                        type="checkbox"
                                        checked={editedRoles.includes(role)}
                                        disabled={updatingRoles}
                                        onChange={() =>
                                            toggleRole(role, editedRoles, setEditedRoles)
                                        }
                                    />
                                    {roleLabels[role]}
                                </label>
                            ))}
                        </div>
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={updatingRoles}
                            onClick={closeRoleEditor}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={updatingRoles ? "Zapisywanie…" : "Zapisz role"}
                            disabled={updatingRoles}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
};

export default PanelScreen;
