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
        void apiRequestResult<ApiTeam[]>(`${apiEndpoints.teams}?${firstPage}`, null, "GET", navigate).then(
            ({ data }) => {
                if (!active) return;
                setTeams(data ?? []);
                setHasError(data === null);
                setLoading(false);
            }
        );
        return () => {
            active = false;
        };
    }, [navigate]);

    const visibleTeams = useMemo(() => {
        const normalized = query.trim().toLocaleLowerCase("pl-PL");
        if (!normalized) return teams;
        return teams.filter((team) =>
            [team.name, team.schoolId, team.editionId, ...team.teamMembers.map((member) => member.id)]
                .join(" ")
                .toLocaleLowerCase("pl-PL")
                .includes(normalized)
        );
    }, [query, teams]);

    return (
        <div className="min-h-screen bg-slate-50">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="border-b border-slate-100 pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Administracja</p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Zarejestrowane drużyny</h1>
                    <p className="mt-3 text-slate-600">
                        Lista korzysta z nowego modelu członkostwa. Edycja, zatwierdzanie i administracyjne usuwanie pozostają niedostępne do czasu dodania endpointów backendu.
                    </p>
                </header>

                <label className="mt-6 block max-w-xl text-sm font-medium text-slate-700">
                    Szukaj po nazwie, identyfikatorze szkoły, edycji lub członka
                    <input type="search" value={query} onChange={(event) => setQuery(event.target.value)} className="mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100" />
                </label>

                {hasError && <p role="alert" className="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">Nie udało się pobrać listy drużyn.</p>}

                {loading ? (
                    <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-12 text-center text-sm text-slate-500">Ładowanie listy drużyn…</div>
                ) : visibleTeams.length === 0 ? (
                    <div className="mt-6 rounded-2xl border border-dashed border-slate-300 bg-white p-12 text-center text-sm text-slate-500">Brak drużyn spełniających kryteria.</div>
                ) : (
                    <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[900px] text-left text-sm">
                                <thead className="bg-slate-100 text-slate-700">
                                    <tr><th className="px-4 py-3">Nazwa</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Członkowie</th><th className="px-4 py-3">Szkoła</th><th className="px-4 py-3">Edycja</th><th className="px-4 py-3">Utworzono</th></tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {visibleTeams.map((team) => (
                                        <tr key={team.id} className="hover:bg-slate-50">
                                            <td className="px-4 py-4 font-semibold text-slate-900">{team.name}</td>
                                            <td className="px-4 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${team.isCensored ? "bg-amber-100 text-amber-800" : "bg-emerald-100 text-emerald-800"}`}>{team.isCensored ? "oczekuje" : "zaakceptowana"}</span></td>
                                            <td className="px-4 py-4 text-slate-600">{team.teamMembers.length}</td>
                                            <td className="px-4 py-4 font-mono text-xs text-slate-600">{team.schoolId}</td>
                                            <td className="px-4 py-4 font-mono text-xs text-slate-600">{team.editionId}</td>
                                            <td className="px-4 py-4 text-slate-600">{new Date(team.createdAt).toLocaleDateString("pl-PL")}</td>
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
