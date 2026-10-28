import type { FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import SchoolsTable from "../../components/SchoolsTable";
import type {
    ApiSchool,
    ApiTeam,
    ApiTeamJoinCode,
    ApiTeamMember,
    School,
} from "../../types/models";
import { adaptSchool } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";
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
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [createError, setCreateError] = useState("");
    const [saving, setSaving] = useState(false);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;
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
    }, [navigate]);

    const selectedSchool = useMemo(
        () => schools.find((school) => school.id === schoolId),
        [schoolId, schools]
    );
    const isCaptain = team?.teamMembers.some(
        (member) => member.id === currentUserId && member.position === "CAPTAIN"
    );

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
                                        <Button
                                            text="Edytuj nazwę"
                                            onClick={() => setEditingName(true)}
                                            className="mt-5"
                                        />
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
                            <ul className="mt-4 divide-y divide-line-subtle">
                                {team.teamMembers.map((member) => (
                                    <li
                                        key={member.id}
                                        className="flex flex-wrap items-center justify-between gap-3 py-3"
                                    >
                                        <div>
                                            <p className="font-mono text-sm text-ink">
                                                {member.id}
                                            </p>
                                            <p className="text-xs text-ink-muted">
                                                {member.position}
                                            </p>
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
                                    </li>
                                ))}
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
            <PublicFooter />
        </div>
    );
};

export default CaptainHomeScreen;
