import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import type { ApiTeam } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const AdminTeamsScreen = () => {
    const navigate = useNavigate();
    const [teams, setTeams] = useState<ApiTeam[]>([]);
    const [query, setQuery] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void apiRequestResult<ApiTeam[]>(
            `${apiEndpoints.teams}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            setTeams(data ?? []);
            setHasError(data === null);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const visibleTeams = useMemo(() => {
        const normalized = query.trim().toLocaleLowerCase("pl-PL");
        if (!normalized) return teams;
        return teams.filter((team) =>
            [
                team.name,
                team.schoolId,
                team.editionId,
                ...team.teamMembers.map((member) => member.id),
            ]
                .join(" ")
                .toLocaleLowerCase("pl-PL")
                .includes(normalized)
        );
    }, [query, teams]);

    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="border-b border-line-subtle pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Administracja
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">
                        Zarejestrowane drużyny
                    </h1>
                    <p className="mt-3 text-ink-secondary">
                        Lista korzysta z nowego modelu członkostwa. Edycja, zatwierdzanie i
                        administracyjne usuwanie pozostają niedostępne do czasu dodania endpointów
                        backendu.
                    </p>
                </header>

                <label className="mt-6 block max-w-xl text-sm font-medium text-ink">
                    Szukaj po nazwie, identyfikatorze szkoły, edycji lub członka
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
                                        <th className="px-4 py-3">Status</th>
                                        <th className="px-4 py-3">Członkowie</th>
                                        <th className="px-4 py-3">Szkoła</th>
                                        <th className="px-4 py-3">Edycja</th>
                                        <th className="px-4 py-3">Utworzono</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-line-subtle">
                                    {visibleTeams.map((team) => (
                                        <tr key={team.id} className="hover:bg-canvas">
                                            <td className="px-4 py-4 font-semibold text-ink-heading">
                                                {team.name}
                                            </td>
                                            <td className="px-4 py-4">
                                                <span
                                                    className={`rounded-full px-2.5 py-1 text-xs font-semibold ${team.isCensored ? "bg-warning-muted text-warning-text" : "bg-brand-muted text-brand-text-strong"}`}
                                                >
                                                    {team.isCensored ? "oczekuje" : "zaakceptowana"}
                                                </span>
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {team.teamMembers.length}
                                            </td>
                                            <td className="px-4 py-4 font-mono text-xs text-ink-secondary">
                                                {team.schoolId}
                                            </td>
                                            <td className="px-4 py-4 font-mono text-xs text-ink-secondary">
                                                {team.editionId}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {new Date(team.createdAt).toLocaleDateString(
                                                    "pl-PL"
                                                )}
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
