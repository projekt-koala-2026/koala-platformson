import type { FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { FaDownload, FaExchangeAlt, FaUpload } from "react-icons/fa";
import { FaTrashCan } from "react-icons/fa6";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import SchoolsTable from "../../components/SchoolsTable";
import type {
    ApiRodo,
    ApiSchool,
    ApiTeam,
    ApiTeamJoinCode,
    ApiTeamMember,
    School,
} from "../../types/models";
import { adaptSchool } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult, apiUrl } from "../../utils/apiFetcher";
import { getStoredUserId, getUserRoles } from "../../utils/authService";

const inputClass =
    "mt-1.5 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:bg-surface-muted";

type Feedback = { tone: "success" | "error"; message: string } | null;

const CaptainHomeScreen = () => {
    const navigate = useNavigate();
    const currentUserId = useMemo(() => getStoredUserId(), []);
    const roles = useMemo(() => getUserRoles(), []);
    const [teams, setTeams] = useState<ApiTeam[]>([]);
    const [team, setTeam] = useState<ApiTeam | null>(null);
    const [schools, setSchools] = useState<School[]>([]);
    const [teamName, setTeamName] = useState("");
    const [schoolId, setSchoolId] = useState("");
    const [joinCode, setJoinCode] = useState("");
    const [loading, setLoading] = useState(true);
    const [editingName, setEditingName] = useState(false);
    const [addingMember, setAddingMember] = useState(false);
    const [newMembers, setNewMembers] = useState([
        { nameFirst: "", nameLast: "" },
        { nameFirst: "", nameLast: "" },
    ]);
    const [memberError, setMemberError] = useState("");
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [rodoByMember, setRodoByMember] = useState<Record<string, boolean>>({});
    const [rodoStateByMember, setRodoStateByMember] = useState<Record<string, string | null>>({});
    const [createError, setCreateError] = useState("");
    const [saving, setSaving] = useState(false);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;

        if (loading) {
            void Promise.all([
                apiRequestResult<ApiTeam[]>(
                    `${apiEndpoints.teams}/me?${firstPage}`,
                    null,
                    "GET",
                    navigate
                ),
                apiRequestResult<ApiSchool[]>(
                    `${apiEndpoints.schools}?${firstPage}`,
                    null,
                    "GET",
                    navigate
                ),
            ]).then(([teamResult, schoolResult]) => {
                if (!active) return;

                const loadedTeams = teamResult.data ?? [];
                const loadedTeam = loadedTeams[0] ?? null;

                setTeams(loadedTeams);
                setTeam(loadedTeam);
                setTeamName(loadedTeam?.name ?? "");
                setSchoolId(loadedTeam?.schoolId ?? "");
                setSchools((schoolResult.data ?? []).map(adaptSchool));

                if (!teamResult.data || !schoolResult.data) {
                    setFeedback({
                        tone: "error",
                        message: "Nie udało się pobrać wszystkich danych panelu drużyny.",
                    });
                }

                setLoading(false);
            });

            return () => {
                active = false;
            };
        }

        if (!team) {
            setRodoByMember({});
            setRodoStateByMember({});
            return () => {
                active = false;
            };
        }

        const memberIds: (string | null)[] = [
            null,
            ...(team.teamMembers ?? []).map((member) => member.id),
        ];

        setRodoByMember(Object.fromEntries(memberIds.map((id) => [id ?? "captain", false])));
        setRodoStateByMember(Object.fromEntries(memberIds.map((id) => [id ?? "captain", null])));

        void Promise.all(
            memberIds.map(async (memberId) => {
                const result = await apiRequestResult<ApiRodo>(
                    `${apiEndpoints.teams}/${team.id}/rodos/${memberId ?? "null"}`,
                    null,
                    "GET",
                    navigate
                );

                const hasRodo = result.status === 200;

                return [
                    memberId ?? "captain",
                    { hasRodo, state: result.data?.state ?? null },
                ] as const;
            })
        ).then((statuses) => {
            if (active) {
                setRodoByMember(
                    Object.fromEntries(statuses.map(([key, value]) => [key, value.hasRodo]))
                );
                setRodoStateByMember(
                    Object.fromEntries(statuses.map(([key, value]) => [key, value.state]))
                );
            }
        });

        return () => {
            active = false;
        };
    }, [loading, navigate, team]);

    const statusMeta: Record<
        "UNVERIFIED" | "ACCEPTED" | "REJECTED",
        { label: string; className: string }
    > = {
        UNVERIFIED: {
            label: "Do weryfikacji",
            className: "app-status-warning",
        },
        ACCEPTED: {
            label: "Zatwierdzona",
            className: "app-status-success",
        },
        REJECTED: {
            label: "Odrzucona",
            className: "app-status-danger",
        },
    };

    const rodoStatusFor = (memberKey: string) => {
        const state = rodoStateByMember[memberKey];
        if (!state || !(state in statusMeta)) {
            return {
                label: "Brak zgody",
                className: "border-line bg-surface-muted text-ink-secondary",
            };
        }
        return statusMeta[state as keyof typeof statusMeta];
    };

    const selectedSchool = useMemo(
        () => schools.find((school) => school.id === schoolId),
        [schoolId, schools]
    );

    const isCaptain = team?.teamCaptain?.id === currentUserId;

    const submitTeam = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const normalizedName = teamName.trim();
        if (!normalizedName) {
            if (team) setFeedback({ tone: "error", message: "Podaj nazwę drużyny." });
            else setCreateError("Podaj nazwę drużyny.");
            return;
        }
        if (!team && !schoolId) {
            setCreateError("Wybierz szkołę z tabeli.");
            return;
        }

        setSaving(true);
        setFeedback(null);
        setCreateError("");
        const result = team
            ? await apiRequestResult<ApiTeam>(
                  `${apiEndpoints.teams}/${team.id}/name`,
                  { name: normalizedName },
                  "PUT",
                  navigate
              )
            : await apiRequestResult<ApiTeam>(
                  apiEndpoints.teams,
                  { name: normalizedName, schoolId },
                  "POST",
                  navigate
              );

        if (result.data) {
            setTeam(result.data);
            setTeams((current) => {
                const exists = current.some((item) => item.id === result.data?.id);
                return exists
                    ? current.map((item) => (item.id === result.data?.id ? result.data! : item))
                    : [...current, result.data!];
            });
            setTeamName(result.data.name);
            setSchoolId(result.data.schoolId);
            setEditingName(false);
            setIsCreateOpen(false);
            setFeedback({
                tone: "success",
                message: team ? "Nazwa drużyny została zapisana." : "Drużyna została utworzona.",
            });
        } else if (team) setFeedback({ tone: "error", message: "Nie udało się zapisać drużyny." });
        else setCreateError("Nie udało się utworzyć drużyny. Sprawdź dane i spróbuj ponownie.");
        setSaving(false);
    };

    const openCreateTeam = () => {
        setTeamName("");
        setSchoolId("");
        setCreateError("");
        setFeedback(null);
        setIsCreateOpen(true);
    };

    const closeCreateTeam = () => {
        if (saving) return;
        setIsCreateOpen(false);
        setTeamName("");
        setSchoolId("");
        setCreateError("");
    };

    const selectTeam = (selectedTeam: ApiTeam) => {
        setTeam(selectedTeam);
        setTeamName(selectedTeam.name);
        setSchoolId(selectedTeam.schoolId);
        setEditingName(false);
        setFeedback(null);
    };

    const joinTeam = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const normalizedCode = joinCode.trim().toUpperCase();
        if (!normalizedCode) return;
        setSaving(true);
        setFeedback(null);
        const { data } = await apiRequestResult<ApiTeamMember[]>(
            `${apiEndpoints.teams}/join/${encodeURIComponent(normalizedCode)}`,
            null,
            "POST",
            navigate
        );
        if (!data) {
            setFeedback({
                tone: "error",
                message: "Nie udało się dołączyć do drużyny. Sprawdź kod.",
            });
            setSaving(false);
            return;
        }
        const refreshed = await apiRequestResult<ApiTeam[]>(
            `${apiEndpoints.teams}/me?${firstPage}`,
            null,
            "GET",
            navigate
        );
        if (refreshed.data) {
            const previousIds = new Set(teams.map((item) => item.id));
            const joinedTeam =
                refreshed.data.find((item) => !previousIds.has(item.id)) ??
                refreshed.data[0] ??
                null;
            setTeams(refreshed.data);
            setTeam(joinedTeam);
            setTeamName(joinedTeam?.name ?? "");
            setSchoolId(joinedTeam?.schoolId ?? "");
            setJoinCode("");
            setFeedback({ tone: "success", message: "Dołączono do drużyny." });
        } else
            setFeedback({
                tone: "error",
                message: "Dołączono do drużyny, ale nie udało się odświeżyć jej danych.",
            });
        setSaving(false);
    };

    const generateJoinCode = async () => {
        if (!team) return;
        setSaving(true);
        setFeedback(null);
        const { data } = await apiRequestResult<ApiTeamJoinCode>(
            `${apiEndpoints.teams}/${team.id}/new-join-code`,
            null,
            "POST",
            navigate
        );
        if (data) {
            const updated = { ...team, joinCode: data };
            setTeam(updated);
            setTeams((items) => items.map((item) => (item.id === updated.id ? updated : item)));
            setFeedback({ tone: "success", message: "Wygenerowano nowy kod dołączenia." });
        } else setFeedback({ tone: "error", message: "Nie udało się wygenerować kodu." });
        setSaving(false);
    };

    const removeMember = async (memberId: string) => {
        if (!team || !window.confirm("Usunąć tego członka z drużyny?")) return;
        setSaving(true);
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.teams}/${team.id}/member/${memberId}`,
            null,
            "DELETE",
            navigate
        );
        if (status === 200 || status === 204) {
            const updated = {
                ...team,
                teamMembers: team.teamMembers.filter((item) => item.id !== memberId),
            };
            setTeam(updated);
            setTeams((items) => items.map((item) => (item.id === updated.id ? updated : item)));
            setFeedback({ tone: "success", message: "Członek został usunięty z drużyny." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć członka drużyny." });
        setSaving(false);
    };

    const deleteTeam = async () => {
        if (
            !team ||
            !window.confirm(`Usunąć drużynę „${team.name}”? Tej operacji nie można cofnąć.`)
        )
            return;
        setSaving(true);
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.teams}/${team.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status === 200 || status === 204) {
            setTeams((current) => current.filter((item) => item.id !== team.id));
            setTeam(null);
            setTeamName("");
            setSchoolId("");
            setFeedback({ tone: "success", message: "Drużyna została usunięta." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć drużyny." });
        setSaving(false);
    };

    const addMember = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!team) return;

        const membersToAdd = newMembers
            .map((member) => ({
                NameFirst: member.nameFirst.trim(),
                NameLast: member.nameLast.trim(),
            }))
            .filter((member) => member.NameFirst || member.NameLast);

        if (membersToAdd.length === 0) {
            setMemberError("Wpisz imię i nazwisko przynajmniej jednej osoby.");
            return;
        }

        if (membersToAdd.some((member) => !member.NameFirst || !member.NameLast)) {
            setMemberError("Uzupełnij imię i nazwisko każdej rozpoczętej osoby.");
            return;
        }

        setSaving(true);
        setMemberError("");
        setFeedback(null);
        let addedCount = 0;
        for (const member of membersToAdd) {
            const result = await apiRequestResult<unknown>(
                `${apiEndpoints.teams}/${team.id}/member`,
                member,
                "POST",
                navigate
            );
            if (result.status !== 200 && result.status !== 201 && result.status !== 204) break;
            addedCount += 1;
        }

        if (addedCount > 0) {
            const refreshed = await apiRequestResult<ApiTeam[]>(
                `${apiEndpoints.teams}/me?${firstPage}`,
                null,
                "GET",
                navigate
            );
            if (refreshed.data) {
                setTeams(refreshed.data);
                setTeam(refreshed.data.find((item) => item.id === team.id) ?? team);
            }
        }

        if (addedCount === membersToAdd.length) {
            setAddingMember(false);
            setNewMembers([
                { nameFirst: "", nameLast: "" },
                { nameFirst: "", nameLast: "" },
            ]);
            setFeedback({
                tone: "success",
                message: `Dodano ${addedCount} ${addedCount === 1 ? "osobę" : "osoby"} do drużyny.`,
            });
        } else if (addedCount > 0) {
            setAddingMember(false);
            setFeedback({
                tone: "error",
                message: `Dodano ${addedCount} z ${membersToAdd.length} osób. Sprawdź limit miejsc przed ponowną próbą.`,
            });
        } else {
            setMemberError("Nie udało się dodać osób do drużyny.");
        }
        setSaving(false);
    };

    const closeAddMember = () => {
        if (saving) return;
        setAddingMember(false);
        setMemberError("");
        setNewMembers([
            { nameFirst: "", nameLast: "" },
            { nameFirst: "", nameLast: "" },
        ]);
    };

    const addRodo = async (memberId: string | null, file?: File) => {
        if (!team || !file) return;
        if (file.type !== "application/pdf") {
            setFeedback({ tone: "error", message: "Wybierz plik PDF." });
            return;
        }

        const formData = new FormData();
        formData.append("File", file);
        setSaving(true);
        setFeedback(null);

        const memberKey = memberId ?? "captain";
        const hasRodo = rodoByMember[memberKey] === true;
        const result = await apiRequestResult<unknown>(
            `${apiEndpoints.teams}/${team.id}/rodos/${memberId ?? "null"}${hasRodo ? "/file" : ""}`,
            formData,
            hasRodo ? "PUT" : "POST",
            navigate
        );

        if (result.status === 200 || result.status === 204) {
            setRodoByMember((current) => ({ ...current, [memberKey]: true }));
            setRodoStateByMember((current) => ({ ...current, [memberKey]: "UNVERIFIED" }));
            setFeedback({ tone: "success", message: "Plik PDF został zapisany." });
        } else setFeedback({ tone: "error", message: "Nie udało się zapisać pliku PDF." });
        setSaving(false);
    };

    const removeRodo = async (memberId: string | null) => {
        if (!team) return;
        const memberKey = memberId ?? "captain";
        setSaving(true);
        const result = await apiRequestResult<unknown>(
            `${apiEndpoints.teams}/${team.id}/rodos/${memberId ?? "null"}`,
            null,
            "DELETE",
            navigate
        );
        if (result.status === 200 || result.status === 204) {
            setRodoByMember((current) => ({ ...current, [memberKey]: false }));
            setRodoStateByMember((current) => ({ ...current, [memberKey]: null }));
            setFeedback({ tone: "success", message: "Zgoda RODO została usunięta." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć zgody RODO." });
        setSaving(false);
    };

    const downloadRodo = async (memberId: string | null) => {
        if (!team) return;
        setSaving(true);
        try {
            const response = await fetch(
                `${apiUrl}${apiEndpoints.teams}/${team.id}/rodos/${memberId ?? "null"}/file`,
                { credentials: "include" }
            );
            if (!response.ok) throw new Error("RODO download failed");
            const objectUrl = URL.createObjectURL(await response.blob());
            const link = document.createElement("a");
            link.href = objectUrl;
            link.download = `rodo-${memberId ?? "captain"}.pdf`;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
        } catch {
            setFeedback({ tone: "error", message: "Nie udało się pobrać pliku RODO." });
        }
        setSaving(false);
    };

    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6 sm:py-10">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Panel drużyn
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">Twoje drużyny</h1>
                    <p className="mt-3 max-w-2xl text-ink-secondary">
                        Utwórz drużynę, zarządzaj jej nazwą, członkami oraz kodem dołączenia.
                    </p>
                </header>
                {teams.length > 1 && (
                    <label className="mb-6 block max-w-xl text-sm font-medium text-ink">
                        Wyświetlana drużyna
                        <select
                            value={team?.id ?? ""}
                            onChange={(event) => {
                                const selected = teams.find(
                                    (item) => item.id === event.target.value
                                );
                                if (selected) selectTeam(selected);
                            }}
                            className={inputClass}
                        >
                            {teams.map((item) => (
                                <option key={item.id} value={item.id}>
                                    {item.name}
                                </option>
                            ))}
                        </select>
                    </label>
                )}
                {team && roles.isGuardian && (
                    <form
                        onSubmit={joinTeam}
                        className="mb-6 rounded-2xl border border-line bg-surface p-5 shadow-sm"
                    >
                        <h2 className="font-semibold text-ink-heading">
                            Dołącz do kolejnej drużyny
                        </h2>
                        <div className="mt-3 flex flex-col gap-3 sm:flex-row">
                            <input
                                value={joinCode}
                                onChange={(event) => setJoinCode(event.target.value)}
                                className={`${inputClass} mt-0 font-mono uppercase`}
                                placeholder="Kod dołączenia"
                                disabled={saving}
                                required
                            />
                            <Button
                                type="submit"
                                text={saving ? "Dołączanie…" : "Dołącz"}
                                disabled={saving}
                            />
                        </div>
                    </form>
                )}
                {feedback && (
                    <p
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`mb-6 rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "app-status-success" : "app-status-danger"}`}
                    >
                        {feedback.message}
                    </p>
                )}
                {loading ? (
                    <div className="rounded-2xl border border-line bg-surface p-14 text-center text-sm text-ink-muted shadow-sm">
                        Ładowanie danych drużyny…
                    </div>
                ) : team ? (
                    <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
                        <section className="rounded-2xl border border-line bg-surface p-6 shadow-sm">
                            {editingName ? (
                                <form onSubmit={submitTeam} className="space-y-4">
                                    <label className="block text-sm font-medium text-ink">
                                        Nazwa drużyny
                                        <input
                                            className={inputClass}
                                            value={teamName}
                                            onChange={(event) => setTeamName(event.target.value)}
                                            disabled={saving}
                                            required
                                        />
                                    </label>
                                    <div className="flex gap-3">
                                        <Button
                                            type="submit"
                                            text={saving ? "Zapisywanie…" : "Zapisz nazwę"}
                                            disabled={saving}
                                        />
                                        <button
                                            type="button"
                                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted"
                                            onClick={() => {
                                                setTeamName(team.name);
                                                setEditingName(false);
                                            }}
                                        >
                                            Anuluj
                                        </button>
                                    </div>
                                </form>
                            ) : (
                                <>
                                    <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                        Zarejestrowana drużyna
                                    </p>
                                    <h2 className="mt-2 text-2xl font-bold text-ink-heading">
                                        {team.name}
                                    </h2>
                                    <p className="mt-2 text-sm text-ink-muted">
                                        {selectedSchool?.name ?? `Szkoła: ${team.schoolId}`}
                                    </p>
                                    <p className="mt-1 text-sm text-ink-muted">
                                        Status nazwy:{" "}
                                        {team.isCensored
                                            ? "oczekuje na akceptację"
                                            : "zaakceptowana"}
                                    </p>
                                    {isCaptain && (
                                        <div className="flex items-center gap-2">
                                            <Button
                                                text="Edytuj nazwę"
                                                onClick={() => setEditingName(true)}
                                                className="mt-5"
                                            />
                                            <Button
                                                text="Dodaj członka"
                                                onClick={() => setAddingMember(true)}
                                                disabled={
                                                    saving || (team.teamMembers?.length ?? 0) >= 2
                                                }
                                                className="mt-5"
                                            />
                                        </div>
                                    )}
                                </>
                            )}
                        </section>
                        <section className="rounded-2xl border border-line bg-surface p-6 shadow-sm">
                            <h2 className="text-lg font-semibold text-ink-heading">
                                Kod dołączenia
                            </h2>
                            {team.joinCode ? (
                                <div className="mt-4 rounded-xl bg-brand-soft p-4 text-center">
                                    <p className="font-mono text-2xl font-bold tracking-widest text-brand-text-strong">
                                        {team.joinCode.joinCode}
                                    </p>
                                    <p className="mt-2 text-xs text-brand-text">
                                        Ważny do{" "}
                                        {new Date(team.joinCode.expiresAt).toLocaleString("pl-PL")}
                                    </p>
                                </div>
                            ) : (
                                <p className="mt-3 text-sm text-ink-muted">Brak aktywnego kodu.</p>
                            )}
                            {isCaptain && (
                                <Button
                                    text={team.joinCode ? "Wygeneruj nowy kod" : "Wygeneruj kod"}
                                    onClick={() => void generateJoinCode()}
                                    disabled={saving}
                                    className="mt-4"
                                />
                            )}
                        </section>
                        <section className="rounded-2xl border border-line bg-surface p-6 shadow-sm lg:col-span-2">
                            <h2 className="text-lg font-semibold text-ink-heading">
                                Członkowie drużyny
                            </h2>
                            <ul className="mt-4 divide-y divide-slate-100">
                                <li className="relative flex flex-wrap items-center justify-between gap-3 py-3">
                                    <div>
                                        <p className="font-mono text-sm text-ink">
                                            {team.teamCaptain?.nameFirst}{" "}
                                            {team.teamCaptain?.nameLast}
                                        </p>

                                        <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                            Kapitan drużyny
                                        </p>
                                    </div>
                                    <div className="absolute left-1/2 -translate-x-1/2 text-center">
                                        {roles.isGuardian && rodoStatusFor("captain") && (
                                            <span
                                                className={`mt-1 inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${rodoStatusFor("captain")!.className}`}
                                            >
                                                {rodoStatusFor("captain")!.label}
                                            </span>
                                        )}
                                    </div>
                                    {roles.isGuardian && rodoByMember.captain === false && (
                                        <label className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-brand-soft px-4 py-2 text-xs font-semibold text-brand-text hover:bg-brand-muted">
                                            <FaUpload />
                                            <span>Zgoda RODO</span>
                                            <input
                                                type="file"
                                                accept=".pdf,application/pdf"
                                                className="sr-only"
                                                disabled={saving}
                                                onChange={(event) => {
                                                    const file = event.currentTarget.files?.[0];
                                                    if (file) void addRodo(null, file);
                                                    event.currentTarget.value = "";
                                                }}
                                            />
                                        </label>
                                    )}
                                    {roles.isGuardian && rodoByMember.captain === true && (
                                        <div className="flex items-center gap-1">
                                            <button
                                                type="button"
                                                disabled={saving}
                                                onClick={() => void downloadRodo(null)}
                                                className="inline-flex items-center gap-2 rounded-lg bg-success-soft px-4 py-2 text-xs font-semibold text-success-text hover:brightness-95 disabled:opacity-50"
                                            >
                                                <FaDownload />
                                                <span>Zgoda RODO</span>
                                            </button>

                                            <button
                                                type="button"
                                                disabled={saving}
                                                onClick={() => void removeRodo(null)}
                                                className="rounded-lg bg-danger-soft px-2 py-2 text-xs font-semibold text-danger-text hover:bg-danger-muted disabled:opacity-50"
                                                aria-label="Usuń zgodę RODO kapitana"
                                            >
                                                <FaTrashCan />
                                            </button>

                                            <button
                                                type="button"
                                                disabled={saving}
                                                onClick={() =>
                                                    document
                                                        .getElementById("captain-rodo-replace")
                                                        ?.click()
                                                }
                                                aria-label="Zastąp zgodę RODO kapitana"
                                                className="rounded-lg bg-brand-soft px-2 py-2 text-xs font-semibold text-brand-text hover:bg-brand-muted disabled:opacity-50"
                                            >
                                                <FaExchangeAlt />
                                            </button>
                                            <input
                                                id="captain-rodo-replace"
                                                type="file"
                                                accept=".pdf,application/pdf"
                                                className="sr-only"
                                                disabled={saving}
                                                onChange={(event) => {
                                                    const file = event.currentTarget.files?.[0];
                                                    if (file) void addRodo(null, file);
                                                    event.currentTarget.value = "";
                                                }}
                                            />
                                        </div>
                                    )}
                                </li>
                                {team.teamMembers?.map((member) => (
                                    <li
                                        key={member.id}
                                        className="relative flex flex-wrap items-center justify-between gap-3 py-3"
                                    >
                                        <div>
                                            <p className="font-mono text-sm text-ink">
                                                {member.nameFist} {member.nameLast}
                                            </p>
                                            <p className="text-xs text-ink-muted">
                                                {member.position}
                                            </p>
                                        </div>
                                        <div className="absolute left-1/2 -translate-x-1/2 text-center">
                                            {roles.isGuardian && rodoStatusFor(member.id) && (
                                                <span
                                                    className={`mt-1 inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${rodoStatusFor(member.id)!.className}`}
                                                >
                                                    {rodoStatusFor(member.id)!.label}
                                                </span>
                                            )}
                                        </div>
                                        {isCaptain && member.position !== "CAPTAIN" && (
                                            <button
                                                type="button"
                                                disabled={saving}
                                                onClick={() => void removeMember(member.id)}
                                                className="rounded-lg bg-danger-soft px-3 py-2 text-xs font-semibold text-danger-text hover:bg-danger-muted disabled:opacity-50"
                                            >
                                                Usuń z drużyny
                                            </button>
                                        )}
                                        {roles.isGuardian && rodoByMember[member.id] === false && (
                                            <label className="inline-flex cursor-pointer items-center gap-2 rounded-lg bg-brand-soft px-4 py-2 text-xs font-semibold text-brand-text hover:bg-brand-muted">
                                                <FaUpload />
                                                <span>Zgoda RODO</span>
                                                <input
                                                    type="file"
                                                    accept=".pdf,application/pdf"
                                                    className="sr-only"
                                                    disabled={saving}
                                                    onChange={(event) => {
                                                        const file = event.currentTarget.files?.[0];
                                                        if (file) void addRodo(member.id, file);
                                                        event.currentTarget.value = "";
                                                    }}
                                                />
                                            </label>
                                        )}
                                        {roles.isGuardian && rodoByMember[member.id] === true && (
                                            <div className="flex items-center gap-1">
                                                <button
                                                    type="button"
                                                    disabled={saving}
                                                    onClick={() => void downloadRodo(member.id)}
                                                    className="inline-flex items-center gap-2 rounded-lg bg-success-soft px-4 py-2 text-xs font-semibold text-success-text hover:brightness-95 disabled:opacity-50"
                                                >
                                                    <FaDownload />
                                                    <span>Zgoda RODO</span>
                                                </button>

                                                <button
                                                    type="button"
                                                    disabled={saving}
                                                    onClick={() => void removeRodo(member.id)}
                                                    className="rounded-lg bg-danger-soft px-2 py-2 text-xs font-semibold text-danger-text hover:bg-danger-muted disabled:opacity-50"
                                                    aria-label={`Usuń zgodę RODO ${member.nameFist} ${member.nameLast}`}
                                                >
                                                    <FaTrashCan />
                                                </button>

                                                <button
                                                    type="button"
                                                    disabled={saving}
                                                    onClick={() =>
                                                        document
                                                            .getElementById(
                                                                `rodo-replace-${member.id}`
                                                            )
                                                            ?.click()
                                                    }
                                                    aria-label={`Zastąp zgodę RODO ${member.nameFist} ${member.nameLast}`}
                                                    className="rounded-lg bg-brand-soft px-2 py-2 text-xs font-semibold text-brand-text hover:bg-brand-muted disabled:opacity-50"
                                                >
                                                    <FaExchangeAlt />
                                                </button>
                                                <input
                                                    id={`rodo-replace-${member.id}`}
                                                    type="file"
                                                    accept=".pdf,application/pdf"
                                                    className="sr-only"
                                                    disabled={saving}
                                                    onChange={(event) => {
                                                        const file = event.currentTarget.files?.[0];
                                                        if (file) void addRodo(member.id, file);
                                                        event.currentTarget.value = "";
                                                    }}
                                                />
                                            </div>
                                        )}
                                    </li>
                                ))}
                                {team.teamAdmin && team.teamAdmin.id !== team.teamCaptain?.id && (
                                    <li className="flex flex-wrap items-center justify-between gap-3 py-3">
                                        <div>
                                            <p className="font-mono text-sm text-ink">
                                                {team.teamAdmin?.nameFirst}{" "}
                                                {team.teamAdmin?.nameLast}
                                            </p>
                                            <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                                Opiekun drużyny
                                            </p>
                                        </div>
                                        {isCaptain && (
                                            <button
                                                type="button"
                                                // Brakuje endpointu do usuwania opiekuna drużyny, więc przycisk jest disabled
                                                disabled
                                                onClick={() =>
                                                    void removeMember(team.teamAdmin?.id ?? "")
                                                }
                                                className="rounded-lg bg-danger-soft px-3 py-2 text-xs font-semibold text-danger-text hover:bg-danger-muted disabled:opacity-50"
                                            >
                                                Usuń z drużyny
                                            </button>
                                        )}
                                    </li>
                                )}
                            </ul>
                            {isCaptain && (
                                <Button
                                    text={saving ? "Usuwanie…" : "Usuń drużynę"}
                                    onClick={() => void deleteTeam()}
                                    disabled={saving}
                                    className="mt-6 bg-danger hover:bg-danger-hover focus:ring-danger-emphasis"
                                />
                            )}
                        </section>
                    </div>
                ) : (
                    <div className="space-y-6">
                        {roles.isGuardian && (
                            <form
                                onSubmit={joinTeam}
                                className="rounded-2xl border border-line bg-surface p-6 shadow-sm"
                            >
                                <h2 className="text-xl font-semibold text-ink-heading">
                                    Dołącz do drużyny
                                </h2>
                                <p className="mt-1 text-sm text-ink-muted">
                                    Wpisz kod otrzymany od kapitana.
                                </p>
                                <div className="mt-4 flex flex-col gap-3 sm:flex-row">
                                    <input
                                        value={joinCode}
                                        onChange={(event) => setJoinCode(event.target.value)}
                                        className={`${inputClass} mt-0 font-mono uppercase`}
                                        placeholder="Kod dołączenia"
                                        disabled={saving}
                                        required
                                    />
                                    <Button
                                        type="submit"
                                        text={saving ? "Dołączanie…" : "Dołącz"}
                                        disabled={saving}
                                    />
                                </div>
                            </form>
                        )}
                        {roles.isCaptain && (
                            <Button text="Utwórz nową drużynę" onClick={openCreateTeam} />
                        )}
                    </div>
                )}
            </main>
            <Modal
                isOpen={isCreateOpen}
                onClose={closeCreateTeam}
                title="Utwórz drużynę"
                maxWidth="xl"
            >
                <form onSubmit={submitTeam} className="space-y-6">
                    <label className="block text-sm font-medium text-ink">
                        Nazwa drużyny
                        <input
                            className={inputClass}
                            value={teamName}
                            onChange={(event) => setTeamName(event.target.value)}
                            disabled={saving}
                            required
                            autoFocus
                        />
                    </label>

                    <section aria-labelledby="create-team-school-heading">
                        <div className="mb-4">
                            <h3
                                id="create-team-school-heading"
                                className="font-semibold text-ink-heading"
                            >
                                Wybierz szkołę
                            </h3>
                            <p className="mt-1 text-sm text-ink-muted">
                                Kliknij odpowiedni wiersz w tabeli.
                            </p>
                            {selectedSchool && (
                                <p className="mt-2 text-sm font-semibold text-brand-text">
                                    Wybrano: {selectedSchool.name}
                                </p>
                            )}
                        </div>
                        <SchoolsTable
                            schools={schools}
                            selectedRspo={selectedSchool?.rspo ?? null}
                            onRowClick={(school) => {
                                setSchoolId(school.id ?? "");
                                setCreateError("");
                            }}
                        />
                    </section>

                    {createError && (
                        <p role="alert" className="rounded-xl border p-3 text-sm app-status-danger">
                            {createError}
                        </p>
                    )}

                    <div className="flex flex-col-reverse gap-3 border-t border-line-subtle pt-5 sm:flex-row sm:justify-end">
                        <button
                            type="button"
                            onClick={closeCreateTeam}
                            disabled={saving}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={saving ? "Tworzenie…" : "Utwórz drużynę"}
                            disabled={saving}
                        />
                    </div>
                </form>
            </Modal>
            <Modal
                isOpen={addingMember}
                onClose={closeAddMember}
                title="Dodaj członków drużyny"
                maxWidth="lg"
            >
                <form onSubmit={addMember} className="space-y-5">
                    <p className="text-sm text-ink-muted">
                        Podaj imię i nazwisko osób, które chcesz dodać do drużyny.
                    </p>
                    {newMembers.map((member, index) => (
                        <fieldset
                            key={index}
                            className="grid gap-4 rounded-xl border border-line-subtle p-4 sm:grid-cols-2"
                        >
                            <legend className="px-1 text-sm font-semibold text-ink-heading">
                                Osoba {index + 1}
                            </legend>
                            <label className="block text-sm font-medium text-ink">
                                Imię
                                <input
                                    className={inputClass}
                                    value={member.nameFirst}
                                    onChange={(event) =>
                                        setNewMembers((current) =>
                                            current.map((item, itemIndex) =>
                                                itemIndex === index
                                                    ? { ...item, nameFirst: event.target.value }
                                                    : item
                                            )
                                        )
                                    }
                                    disabled={saving}
                                    autoFocus={index === 0}
                                />
                            </label>
                            <label className="block text-sm font-medium text-ink">
                                Nazwisko
                                <input
                                    className={inputClass}
                                    value={member.nameLast}
                                    onChange={(event) =>
                                        setNewMembers((current) =>
                                            current.map((item, itemIndex) =>
                                                itemIndex === index
                                                    ? { ...item, nameLast: event.target.value }
                                                    : item
                                            )
                                        )
                                    }
                                    disabled={saving}
                                />
                            </label>
                        </fieldset>
                    ))}
                    {memberError && (
                        <p role="alert" className="rounded-xl border p-3 text-sm app-status-danger">
                            {memberError}
                        </p>
                    )}
                    <div className="flex flex-col-reverse gap-3 border-t border-line-subtle pt-5 sm:flex-row sm:justify-end">
                        <button
                            type="button"
                            onClick={closeAddMember}
                            disabled={saving}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={saving ? "Dodawanie…" : "Dodaj osoby"}
                            disabled={saving}
                        />
                    </div>
                </form>
            </Modal>
            <PublicFooter />
        </div>
    );
};

export default CaptainHomeScreen;
