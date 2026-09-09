import type { FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import SchoolsTable from "../../components/SchoolsTable";
import type { School, Team, TeamPayload } from "../../types/models";
import { apiRequest, apiRequestResult } from "../../utils/apiFetcher";

interface TeamForm {
    teamName: string;
    name1: string;
    name2: string;
    name3: string;
    name4: string;
    schoolRSPO: string;
}

const emptyTeamForm: TeamForm = {
    teamName: "",
    name1: "",
    name2: "",
    name3: "",
    name4: "",
    schoolRSPO: "",
};
const inputClass =
    "mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:bg-slate-100";

const formFromTeam = (team: Team): TeamForm => ({
    teamName: team.teamName ?? "",
    name1: team.name1 ?? "",
    name2: team.name2 ?? "",
    name3: team.name3 ?? "",
    name4: team.name4 ?? "",
    schoolRSPO: team.schoolRSPO ? String(team.schoolRSPO) : "",
});

const CaptainHomeScreen = () => {
    const navigate = useNavigate();
    const [team, setTeam] = useState<Team | null>(null);
    const [schools, setSchools] = useState<School[]>([]);
    const [teamForm, setTeamForm] = useState<TeamForm>(emptyTeamForm);
    const [loading, setLoading] = useState(true);
    const [isEditing, setIsEditing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [feedback, setFeedback] = useState<{ tone: "success" | "error"; message: string } | null>(
        null
    );

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequestResult<Team>("/api/teams/my-team", null, "GET", navigate),
            apiRequest<School[]>("/api/admin/school/school", null, "GET", navigate),
        ]).then(([teamResult, schoolData]) => {
            if (!active) return;
            setSchools(schoolData ?? []);
            if (teamResult.data) {
                setTeam(teamResult.data);
                setTeamForm(formFromTeam(teamResult.data));
            } else if (teamResult.status === 404) {
                setTeam(null);
                setTeamForm(emptyTeamForm);
            } else
                setFeedback({
                    tone: "error",
                    message: "Nie udało się pobrać danych zespołu. Spróbuj odświeżyć stronę.",
                });
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const selectedSchool = useMemo(
        () => schools.find((school) => school.rspo === Number(teamForm.schoolRSPO)),
        [schools, teamForm.schoolRSPO]
    );
    const members = team
        ? [team.name1, team.name2, team.name3, team.name4].filter((member): member is string =>
              Boolean(member?.trim())
          )
        : [];

    const submitTeam = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFeedback(null);
        const schoolRSPO = Number(teamForm.schoolRSPO);
        if (!teamForm.teamName.trim()) {
            setFeedback({ tone: "error", message: "Podaj nazwę zespołu." });
            return;
        }
        if (!Number.isInteger(schoolRSPO) || schoolRSPO <= 0) {
            setFeedback({ tone: "error", message: "Wybierz szkołę z tabeli." });
            return;
        }
        const payload: TeamPayload = {
            teamName: teamForm.teamName.trim(),
            name1: teamForm.name1.trim(),
            name2: teamForm.name2.trim(),
            name3: teamForm.name3.trim(),
            name4: teamForm.name4.trim(),
            schoolRSPO,
        };
        setIsSaving(true);
        const saved = team
            ? await apiRequest<Team>("/api/teams", { ...payload, id: team.id }, "PUT", navigate)
            : await apiRequest<Team>("/api/teams", payload, "POST", navigate);
        if (saved) {
            setTeam(saved);
            setTeamForm(formFromTeam(saved));
            setIsEditing(false);
            setFeedback({
                tone: "success",
                message: team ? "Dane zespołu zostały zapisane." : "Zespół został zarejestrowany.",
            });
        } else
            setFeedback({
                tone: "error",
                message: "Nie udało się zapisać zespołu. Sprawdź dane i spróbuj ponownie.",
            });
        setIsSaving(false);
    };

    const deleteTeam = async () => {
        if (
            !team ||
            !window.confirm(
                `Usunąć zespół „${team.teamName || "bez nazwy"}”? Tej operacji nie można cofnąć.`
            )
        )
            return;
        setIsSaving(true);
        setFeedback(null);
        const deleted = await apiRequest<boolean>("/api/teams", null, "DELETE", navigate);
        if (deleted) {
            setTeam(null);
            setTeamForm(emptyTeamForm);
            setIsEditing(false);
            setFeedback({ tone: "success", message: "Zespół został usunięty." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć zespołu." });
        setIsSaving(false);
    };

    const cancelEdit = () => {
        if (team) setTeamForm(formFromTeam(team));
        setIsEditing(false);
        setFeedback(null);
    };
    const showForm = !team || isEditing;

    return (
        <>
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 sm:py-10">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Panel kapitana
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Twoja drużyna</h1>
                    <p className="mt-3 max-w-2xl text-slate-600">
                        Zarejestruj skład konkursowy, wybierz szkołę i aktualizuj dane zespołu.
                    </p>
                </header>
                {feedback && (
                    <div
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`mb-6 rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-700"}`}
                    >
                        {feedback.message}
                    </div>
                )}
                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-14 text-center text-sm text-slate-500 shadow-sm">
                        Ładowanie danych zespołu…
                    </div>
                ) : !showForm && team ? (
                    <section className="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
                        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-6 text-center">
                            <p className="text-xs font-semibold uppercase tracking-wider text-emerald-700">
                                Zarejestrowany zespół
                            </p>
                            <h2 className="mt-2 text-2xl font-bold text-emerald-950">
                                {team.teamName || "Zespół bez nazwy"}
                            </h2>
                            <p className="mt-2 text-sm text-emerald-800">
                                {selectedSchool?.name ?? `Szkoła RSPO ${team.schoolRSPO ?? "—"}`}
                            </p>
                        </div>
                        <div className="mt-6">
                            <h3 className="font-semibold text-slate-900">Skład drużyny</h3>
                            {members.length ? (
                                <ol className="mt-3 grid gap-2 sm:grid-cols-2">
                                    {members.map((member, index) => (
                                        <li
                                            key={`${member}-${index}`}
                                            className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-700"
                                        >
                                            <span className="mr-2 font-semibold text-emerald-700">
                                                {index + 1}.
                                            </span>
                                            {member}
                                        </li>
                                    ))}
                                </ol>
                            ) : (
                                <p className="mt-3 rounded-xl border border-dashed border-slate-200 p-5 text-sm text-slate-500">
                                    Nie dodano jeszcze członków drużyny.
                                </p>
                            )}
                        </div>
                        <div className="mt-7 flex flex-wrap gap-3">
                            <Button
                                text="Edytuj dane"
                                onClick={() => {
                                    setIsEditing(true);
                                    setFeedback(null);
                                }}
                            />
                            <Button
                                text={isSaving ? "Usuwanie…" : "Usuń zespół"}
                                onClick={() => void deleteTeam()}
                                disabled={isSaving}
                                className="bg-red-600 hover:bg-red-700 focus:ring-red-500"
                            />
                        </div>
                    </section>
                ) : (
                    <form onSubmit={submitTeam} className="space-y-6">
                        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7">
                            <div className="flex flex-col gap-2 border-b border-slate-100 pb-5 sm:flex-row sm:items-start sm:justify-between">
                                <div>
                                    <h2 className="text-xl font-semibold text-slate-900">
                                        {team ? "Edytuj dane zespołu" : "Zarejestruj nowy zespół"}
                                    </h2>
                                    <p className="mt-1 text-sm text-slate-500">
                                        Nazwa zespołu i wybór szkoły są wymagane.
                                    </p>
                                </div>
                                {team && (
                                    <button
                                        type="button"
                                        onClick={cancelEdit}
                                        className="self-start rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
                                    >
                                        Anuluj edycję
                                    </button>
                                )}
                            </div>
                            <div className="mt-5 grid gap-4 sm:grid-cols-2">
                                <label className="text-sm font-medium text-slate-700 sm:col-span-2">
                                    Nazwa zespołu *
                                    <input
                                        value={teamForm.teamName}
                                        onChange={(event) =>
                                            setTeamForm((previous) => ({
                                                ...previous,
                                                teamName: event.target.value,
                                            }))
                                        }
                                        className={inputClass}
                                        required
                                    />
                                </label>
                                {(["name1", "name2", "name3", "name4"] as const).map(
                                    (field, index) => (
                                        <label
                                            key={field}
                                            className="text-sm font-medium text-slate-700"
                                        >
                                            Członek {index + 1}
                                            <input
                                                value={teamForm[field]}
                                                onChange={(event) =>
                                                    setTeamForm((previous) => ({
                                                        ...previous,
                                                        [field]: event.target.value,
                                                    }))
                                                }
                                                className={inputClass}
                                            />
                                        </label>
                                    )
                                )}
                            </div>
                        </section>
                        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7">
                            <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                                <div>
                                    <h2 className="text-xl font-semibold text-slate-900">
                                        Wybierz szkołę
                                    </h2>
                                    <p className="mt-1 text-sm text-slate-500">
                                        Użyj filtrów i kliknij właściwy wiersz.
                                    </p>
                                </div>
                                <div
                                    className={`rounded-xl border px-4 py-3 text-sm ${selectedSchool ? "border-emerald-200 bg-emerald-50 text-emerald-900" : "border-amber-200 bg-amber-50 text-amber-800"}`}
                                >
                                    {selectedSchool ? (
                                        <>
                                            <span className="block font-semibold">
                                                {selectedSchool.name}
                                            </span>
                                            <span className="text-xs">
                                                RSPO {selectedSchool.rspo}
                                            </span>
                                        </>
                                    ) : (
                                        "Nie wybrano szkoły"
                                    )}
                                </div>
                            </div>
                            <SchoolsTable
                                schools={schools}
                                selectedRspo={selectedSchool?.rspo ?? null}
                                onRowClick={(school) =>
                                    setTeamForm((previous) => ({
                                        ...previous,
                                        schoolRSPO: String(school.rspo),
                                    }))
                                }
                            />
                        </section>
                        <div className="flex flex-wrap gap-3">
                            <Button
                                type="submit"
                                text={
                                    isSaving
                                        ? "Zapisywanie…"
                                        : team
                                          ? "Zapisz zmiany"
                                          : "Załóż zespół"
                                }
                                disabled={isSaving}
                            />
                            {team && (
                                <button
                                    type="button"
                                    onClick={cancelEdit}
                                    disabled={isSaving}
                                    className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
                                >
                                    Anuluj
                                </button>
                            )}
                        </div>
                    </form>
                )}
            </main>
            <PublicFooter />
        </>
    );
};

export default CaptainHomeScreen;
