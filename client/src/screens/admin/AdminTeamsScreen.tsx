import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import RecordDates from "../../components/RecordDates";
import type { ApiAdminTeam, ApiEdition, ApiSchool, ApiTeam } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";
import { isRodoModerator } from "../../utils/authService";

const AdminTeamsScreen = () => {
    const navigate = useNavigate();
    const canViewMembers = useMemo(() => isRodoModerator(), []);
    const [teams, setTeams] = useState<(ApiAdminTeam | ApiTeam)[]>([]);
    const [schoolNames, setSchoolNames] = useState<Record<string, string>>({});
    const [editionNames, setEditionNames] = useState<Record<string, string>>({});
    const [query, setQuery] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    const [lookupError, setLookupError] = useState(false);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiListRequestResult<ApiAdminTeam | ApiTeam>(
                `${canViewMembers ? apiEndpoints.teams : apiEndpoints.adminTeams}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiSchool>(
                `${apiEndpoints.schools}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
        ]).then(([teamsResult, schoolsResult, editionsResult]) => {
            if (!active) return;
            setTeams(teamsResult.data ?? []);
            setHasError(teamsResult.data === null);
            setSchoolNames(
                Object.fromEntries(
                    (schoolsResult.data ?? []).map((school) => [school.id, school.nameFull])
                )
            );
            setEditionNames(
                Object.fromEntries(
                    (editionsResult.data ?? []).map((edition) => [edition.id, edition.name])
                )
            );
            setLookupError(schoolsResult.data === null || editionsResult.data === null);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate, canViewMembers]);

    const visibleTeams = useMemo(() => {
        const normalized = query.trim().toLocaleLowerCase("pl-PL");
        if (!normalized) return teams;
        return teams.filter((team) =>
            [
                team.name,
                schoolNames[team.schoolId],
                editionNames[team.editionId],
                team.schoolId,
                team.editionId,
                ...("teamMembers" in team
                    ? team.teamMembers.map((member) => member.teamMemberId)
                    : []),
            ]
                .join(" ")
                .toLocaleLowerCase("pl-PL")
                .includes(normalized)
        );
    }, [editionNames, query, schoolNames, teams]);

    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="border-b border-line-subtle pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Administracja
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">
                        Zarejestrowane drużyny
                    </h1>
                    <p className="mt-3 text-ink-secondary">
                        Przeglądaj drużyny, szkoły i edycje konkursu. Dane członków są dostępne
                        wyłącznie dla moderatora RODO. Cenzurowanie nazw drużyn nie jest jeszcze
                        dostępne.
                    </p>
                </header>

                <label className="mt-6 block max-w-xl text-sm font-medium text-ink">
                    Szukaj po nazwie drużyny, szkole lub edycji
                    <input
                        type="search"
                        value={query}
                        onChange={(event) => setQuery(event.target.value)}
                        className="mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                    />
                </label>

                {hasError && (
                    <p
                        role="alert"
                        className="mt-6 rounded-xl border p-4 text-sm app-status-danger"
                    >
                        Nie udało się pobrać listy drużyn.
                    </p>
                )}
                {lookupError && (
                    <p
                        role="status"
                        className="mt-4 rounded-xl border p-4 text-sm app-status-warning"
                    >
                        Nie udało się pobrać nazw wszystkich szkół lub edycji.
                    </p>
                )}

                {loading ? (
                    <div className="mt-6 rounded-2xl border border-line bg-surface p-12 text-center text-sm text-ink-muted">
                        Ładowanie listy drużyn…
                    </div>
                ) : visibleTeams.length === 0 ? (
                    <div className="mt-6 rounded-2xl border border-dashed border-line-strong bg-surface p-12 text-center text-sm text-ink-muted">
                        Brak drużyn spełniających kryteria.
                    </div>
                ) : (
                    <div className="mt-6 overflow-hidden rounded-2xl border border-line bg-surface shadow-sm">
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[900px] text-left text-sm">
                                <thead className="bg-surface-muted text-ink">
                                    <tr>
                                        <th className="px-4 py-3">Nazwa</th>
                                        <th className="px-4 py-3">Poziom</th>
                                        {canViewMembers && (
                                            <th className="px-4 py-3">Nazwa drużyny</th>
                                        )}
                                        {canViewMembers && (
                                            <th className="px-4 py-3">Członkowie</th>
                                        )}
                                        <th className="px-4 py-3">Szkoła</th>
                                        <th className="px-4 py-3">Edycja</th>
                                        <th className="px-4 py-3">Daty</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-line-subtle">
                                    {visibleTeams.map((team) => (
                                        <tr key={team.id} className="hover:bg-canvas">
                                            <td className="px-4 py-4 font-semibold text-ink-heading">
                                                {team.name}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {team.tier === "TIER_2" ? "Średnia" : "Podstawowa"}
                                            </td>
                                            {canViewMembers && (
                                                <td className="px-4 py-4">
                                                    <span
                                                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${team.isCensored ? "bg-warning-muted text-warning-text" : "bg-brand-muted text-brand-text-strong"}`}
                                                    >
                                                        {team.isCensored
                                                            ? "do weryfikacji"
                                                            : "zaakceptowana"}
                                                    </span>
                                                </td>
                                            )}
                                            {canViewMembers && (
                                                <td className="px-4 py-4 text-ink-secondary">
                                                    {"teamMembers" in team
                                                        ? team.teamMembers.length
                                                        : "—"}
                                                </td>
                                            )}
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {schoolNames[team.schoolId] ?? "Nieznana szkoła"}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {editionNames[team.editionId] ?? "Nieznana edycja"}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                <RecordDates
                                                    createdAt={team.createdAt}
                                                    updatedAt={team.updatedAt}
                                                />
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
};

export default AdminTeamsScreen;
