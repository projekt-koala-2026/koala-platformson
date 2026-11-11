import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import CaptainTasksPanel from "../../components/CaptainTasksPanel";
import KoalaIllustration from "../../components/KoalaIllustration";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiTeam } from "../../types/models";
import { firstPage, apiEndpoints } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";
import { getStoredUserId } from "../../utils/authService";

export default function CapitanTasksScreen() {
    const navigate = useNavigate();
    const [team, setTeam] = useState<ApiTeam | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiTeam>(`${apiEndpoints.teams}/me?${firstPage}`, null, "GET", navigate)
            .then(({ data }) => {
                if (!active) return;
                const captainTeam = (data ?? []).find((item) => item.teamCaptain?.id === getStoredUserId());
                setTeam(captainTeam ?? null);
                if (!data) setError("Nie udało się pobrać drużyny.");
                else if (!captainTeam) setError("Nie znaleziono drużyny przypisanej do tego konta.");
                setLoading(false);
            });
        return () => { active = false; };
    }, [navigate]);

    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="app-public-page flex-1">
                <header className="mb-8 flex items-center justify-between gap-4 sm:gap-8">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">Panel kapitana</p>
                        <h1 className="mt-2 text-3xl font-bold text-ink-heading">Zadania</h1>
                        <p className="mt-3 max-w-2xl text-ink-secondary">Wybierz podedycję i odpowiadaj na zadania w imieniu swojej drużyny.</p>
                    </div>
                    <KoalaIllustration src={`${import.meta.env.BASE_URL}images/koala/koala_mis2b.png`} />
                </header>
                {loading ? <p className="text-ink-secondary">Ładowanie drużyny…</p> : error ? <p role="alert" className="app-status-danger rounded-xl border p-4">{error}</p> : team ? <CaptainTasksPanel key={team.id} team={team} /> : null}
            </main>
            <PublicFooter />
        </div>
    );
}
