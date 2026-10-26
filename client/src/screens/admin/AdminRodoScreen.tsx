import { useEffect, useMemo, useState } from "react";
import { FaCheck, FaEye, FaRedo, FaTimes, FaUndo } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { ApiRodo, ApiTeamDetails, RodoState } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult, apiUrl } from "../../utils/apiFetcher";

type StatusFilter = "ALL" | "UNVERIFIED" | "ACCEPTED" | "REJECTED";
type Feedback = { tone: "success" | "warning" | "error"; message: string } | null;

interface RodoApplication {
    key: string;
    rodo: ApiRodo;
    team: ApiTeamDetails;
    personName: string;
    personEmail: string | null;
    personRole: "Kapitan" | "Uczestnik";
}

const statusMeta: Record<
    "UNVERIFIED" | "ACCEPTED" | "REJECTED" | "UNKNOWN",
    { label: string; className: string }
> = {
    UNVERIFIED: {
        label: "Do weryfikacji",
        className: "border-amber-200 bg-amber-50 text-amber-800",
    },
    ACCEPTED: {
        label: "Zatwierdzona",
        className: "border-emerald-200 bg-emerald-50 text-emerald-800",
    },
    REJECTED: {
        label: "Odrzucona",
        className: "border-red-200 bg-red-50 text-red-700",
    },
    UNKNOWN: {
        label: "Nieznany status",
        className: "border-slate-200 bg-slate-100 text-slate-700",
    },
};

const statusFor = (state: string) =>
    statusMeta[state as keyof typeof statusMeta] ?? {
        ...statusMeta.UNKNOWN,
        label: state || statusMeta.UNKNOWN.label,
    };

const personFor = (team: ApiTeamDetails, memberId: string | null) => {
    if (memberId === null) {
        const captain = team.teamCaptain;
        return {
            name: captain
                ? `${captain.nameFirst} ${captain.nameLast}`.trim() || "Kapitan drużyny"
                : "Kapitan drużyny",
            email: captain?.email ?? null,
            role: "Kapitan" as const,
        };
    }

    const member = team.teamMembers?.find((item) => item.id === memberId);
    return {
        name: member
            ? `${member.nameFist} ${member.nameLast}`.trim() || "Uczestnik"
            : "Nieznany uczestnik",
        email: null,
        role: "Uczestnik" as const,
    };
};

const AdminRodoScreen = () => {
    const navigate = useNavigate();
    const [teams, setTeams] = useState<ApiTeamDetails[]>([]);
    const [rodosByTeam, setRodosByTeam] = useState<Record<string, ApiRodo[]>>({});
    const [query, setQuery] = useState("");
    const [statusFilter, setStatusFilter] = useState<StatusFilter>("ALL");
    const [selectedKey, setSelectedKey] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);
    const [savingKey, setSavingKey] = useState<string | null>(null);
    const [refreshKey, setRefreshKey] = useState(0);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;

        void (async () => {
            const teamsResult = await apiRequestResult<ApiTeamDetails[]>(
                `${apiEndpoints.teams}?${firstPage}`,
                null,
                "GET",
                navigate
            );
            if (!active) return;

            if (!teamsResult.data) {
                setTeams([]);
                setRodosByTeam({});
                setFeedback({ tone: "error", message: "Nie udało się pobrać listy drużyn." });
                setLoading(false);
                return;
            }

            const loadedTeams = teamsResult.data;
            const results = await Promise.all(
                loadedTeams.map(async (team) => ({
                    teamId: team.id,
                    result: await apiRequestResult<ApiRodo[]>(
                        `${apiEndpoints.teamRodos(team.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    ),
                }))
            );
            if (!active) return;

            const nextRodos: Record<string, ApiRodo[]> = {};
            let failedTeams = 0;
            results.forEach(({ teamId, result }) => {
                nextRodos[teamId] = result.data ?? [];
                if (!result.data) failedTeams += 1;
            });

            setTeams(loadedTeams);
            setRodosByTeam(nextRodos);
            if (failedTeams > 0) {
                setFeedback({
                    tone: "warning",
                    message: `Nie udało się pobrać zgód dla ${failedTeams} ${
                        failedTeams === 1 ? "drużyny" : "drużyn"
                    }. Lista może być niepełna.`,
                });
            }
            setLoading(false);
        })();

        return () => {
            active = false;
        };
    }, [navigate, refreshKey]);

    const applications = useMemo<RodoApplication[]>(
        () =>
            teams.flatMap((team) =>
                (rodosByTeam[team.id] ?? []).map((rodo, index) => {
                    const person = personFor(team, rodo.memberId);
                    return {
                        key: `${team.id}:${rodo.memberId ?? "captain"}:${index}`,
                        rodo,
                        team,
                        personName: person.name,
                        personEmail: person.email,
                        personRole: person.role,
                    };
                })
            ),
        [rodosByTeam, teams]
    );

    const counts = useMemo(
        () => ({
            all: applications.length,
            unverified: applications.filter((item) => item.rodo.state === "UNVERIFIED").length,
            accepted: applications.filter((item) => item.rodo.state === "ACCEPTED").length,
            rejected: applications.filter((item) => item.rodo.state === "REJECTED").length,
        }),
        [applications]
    );

    const visibleApplications = useMemo(() => {
        const normalized = query.trim().toLocaleLowerCase("pl-PL");
        return applications.filter((application) => {
            if (statusFilter !== "ALL" && application.rodo.state !== statusFilter) return false;
            if (!normalized) return true;
            return [
                application.personName,
                application.personEmail,
                application.personRole,
                application.team.name,
                application.rodo.memberId,
            ]
                .filter(Boolean)
                .join(" ")
                .toLocaleLowerCase("pl-PL")
                .includes(normalized);
        });
    }, [applications, query, statusFilter]);

    const selectedApplication =
        applications.find((application) => application.key === selectedKey) ?? null;

    const refresh = () => {
        setLoading(true);
        setFeedback(null);
        setRefreshKey((current) => current + 1);
    };

    const setRodoState = async (application: RodoApplication, state: RodoState) => {
        setSavingKey(application.key);
        setFeedback(null);
        const memberPath = application.rodo.memberId ?? "null";
        const { data } = await apiRequestResult<ApiRodo>(
            `${apiEndpoints.teamRodos(application.team.id)}/${memberPath}/state`,
            { state },
            "PUT",
            navigate
        );

        if (!data) {
            setFeedback({ tone: "error", message: "Nie udało się zmienić statusu zgody." });
            setSavingKey(null);
            return;
        }

        setRodosByTeam((current) => ({
            ...current,
            [application.team.id]: (current[application.team.id] ?? []).map((rodo) =>
                rodo.memberId === data.memberId ? data : rodo
            ),
        }));
        setFeedback({
            tone: "success",
            message: `Status zgody dla ${application.personName} został zmieniony na „${
                statusFor(data.state).label
            }”.`,
        });
        setSavingKey(null);
    };

    const fileUrlFor = (application: RodoApplication) => {
        const memberPath = application.rodo.memberId ?? "null";
        return `${apiUrl}${apiEndpoints.teamRodos(application.team.id)}/${memberPath}/file`;
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">Zgody RODO</h1>
                        <p className="mt-2 max-w-3xl text-sm text-slate-600">
                            Przeglądaj dokumenty przekazane przez opiekunów i ustawiaj wynik ich
                            weryfikacji.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaRedo aria-hidden="true" />
                                Odśwież
                            </>
                        }
                        onClick={refresh}
                        disabled={loading}
                    />
                </header>

                {feedback && (
                    <p
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                                : feedback.tone === "warning"
                                  ? "border-amber-200 bg-amber-50 text-amber-800"
                                  : "border-red-200 bg-red-50 text-red-700"
                        }`}
                    >
                        {feedback.message}
                    </p>
                )}

                <section
                    className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"
                    aria-label="Podsumowanie zgód"
                >
                    {[
                        { label: "Wszystkie", value: counts.all, tone: "text-slate-900" },
                        {
                            label: "Do weryfikacji",
                            value: counts.unverified,
                            tone: "text-amber-700",
                        },
                        {
                            label: "Zatwierdzone",
                            value: counts.accepted,
                            tone: "text-emerald-700",
                        },
                        { label: "Odrzucone", value: counts.rejected, tone: "text-red-700" },
                    ].map((item) => (
                        <article
                            key={item.label}
                            className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                        >
                            <p className="text-sm font-medium text-slate-500">{item.label}</p>
                            <p className={`mt-2 text-3xl font-bold ${item.tone}`}>{item.value}</p>
                        </article>
                    ))}
                </section>

                <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                    <div className="grid gap-4 md:grid-cols-[1fr_240px]">
                        <label className="block text-sm font-medium text-slate-700">
                            Szukaj po osobie lub drużynie
                            <input
                                type="search"
                                value={query}
                                onChange={(event) => setQuery(event.target.value)}
                                placeholder="np. Anna lub Drużyna Audytowa"
                                className="mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                            />
                        </label>
                        <label className="block text-sm font-medium text-slate-700">
                            Status
                            <select
                                value={statusFilter}
                                onChange={(event) =>
                                    setStatusFilter(event.target.value as StatusFilter)
                                }
                                className="app-select mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                            >
                                <option value="ALL">Wszystkie</option>
                                <option value="UNVERIFIED">Do weryfikacji</option>
                                <option value="ACCEPTED">Zatwierdzone</option>
                                <option value="REJECTED">Odrzucone</option>
                            </select>
                        </label>
                    </div>
                </section>

                <section className="mt-6" aria-label="Lista zgód RODO">
                    {loading ? (
                        <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center text-sm text-slate-500 shadow-sm">
                            Ładowanie zgód…
                        </div>
                    ) : visibleApplications.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-12 text-center">
                            <p className="font-semibold text-slate-800">
                                Brak zgód do wyświetlenia
                            </p>
                            <p className="mt-2 text-sm text-slate-500">
                                Zmień filtry albo poczekaj, aż opiekunowie prześlą dokumenty.
                            </p>
                        </div>
                    ) : (
                        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                            <div className="overflow-x-auto">
                                <table className="w-full min-w-[820px] text-left text-sm">
                                    <thead className="bg-slate-100 text-slate-700">
                                        <tr>
                                            <th className="px-4 py-3">Osoba</th>
                                            <th className="px-4 py-3">Drużyna</th>
                                            <th className="px-4 py-3">Status</th>
                                            <th className="px-4 py-3">Aktualizacja</th>
                                            <th className="px-4 py-3 text-right">Akcje</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {visibleApplications.map((application) => {
                                            const status = statusFor(application.rodo.state);
                                            return (
                                                <tr
                                                    key={application.key}
                                                    className="hover:bg-slate-50"
                                                >
                                                    <td className="px-4 py-4">
                                                        <p className="font-semibold text-slate-900">
                                                            {application.personName}
                                                        </p>
                                                        <p className="mt-0.5 text-xs text-slate-500">
                                                            {application.personRole}
                                                        </p>
                                                    </td>
                                                    <td className="px-4 py-4 text-slate-700">
                                                        {application.team.name}
                                                    </td>
                                                    <td className="px-4 py-4">
                                                        <span
                                                            className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${status.className}`}
                                                        >
                                                            {status.label}
                                                        </span>
                                                    </td>
                                                    <td className="px-4 py-4 text-slate-600">
                                                        {new Date(
                                                            application.rodo.updatedAt
                                                        ).toLocaleString("pl-PL")}
                                                    </td>
                                                    <td className="px-4 py-4 text-right">
                                                        <Button
                                                            text={
                                                                <>
                                                                    <FaEye aria-hidden="true" />
                                                                    Szczegóły
                                                                </>
                                                            }
                                                            onClick={() =>
                                                                setSelectedKey(application.key)
                                                            }
                                                            className="px-3"
                                                        />
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    )}
                </section>
            </main>

            <Modal
                isOpen={selectedApplication !== null}
                onClose={() => setSelectedKey(null)}
                title="Weryfikacja zgody RODO"
                maxWidth="lg"
            >
                {selectedApplication && (
                    <div className="space-y-5">
                        <dl className="grid gap-4 rounded-xl bg-slate-50 p-4 sm:grid-cols-2">
                            <div>
                                <dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                    Osoba
                                </dt>
                                <dd className="mt-1 font-semibold text-slate-900">
                                    {selectedApplication.personName}
                                </dd>
                                <dd className="text-sm text-slate-500">
                                    {selectedApplication.personRole}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                    Drużyna
                                </dt>
                                <dd className="mt-1 font-semibold text-slate-900">
                                    {selectedApplication.team.name}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                    Przesłano
                                </dt>
                                <dd className="mt-1 text-sm text-slate-700">
                                    {new Date(selectedApplication.rodo.createdAt).toLocaleString(
                                        "pl-PL"
                                    )}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                    Status
                                </dt>
                                <dd className="mt-1">
                                    <span
                                        className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${
                                            statusFor(selectedApplication.rodo.state).className
                                        }`}
                                    >
                                        {statusFor(selectedApplication.rodo.state).label}
                                    </span>
                                </dd>
                            </div>
                        </dl>

                        <a
                            href={fileUrlFor(selectedApplication)}
                            target="_blank"
                            rel="noreferrer"
                            className="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                        >
                            <FaEye aria-hidden="true" />
                            Otwórz przesłany dokument
                        </a>

                        <div className="flex flex-col gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:flex-wrap sm:justify-end">
                            {selectedApplication.rodo.state !== "UNVERIFIED" && (
                                <Button
                                    text={
                                        <>
                                            <FaUndo aria-hidden="true" />
                                            Cofnij do weryfikacji
                                        </>
                                    }
                                    onClick={() =>
                                        void setRodoState(selectedApplication, "UNVERIFIED")
                                    }
                                    disabled={savingKey === selectedApplication.key}
                                    className="bg-slate-600 hover:bg-slate-700"
                                />
                            )}
                            {selectedApplication.rodo.state !== "REJECTED" && (
                                <Button
                                    text={
                                        <>
                                            <FaTimes aria-hidden="true" />
                                            Odrzuć
                                        </>
                                    }
                                    onClick={() =>
                                        void setRodoState(selectedApplication, "REJECTED")
                                    }
                                    disabled={savingKey === selectedApplication.key}
                                    className="bg-red-600 hover:bg-red-700 focus:ring-red-500"
                                />
                            )}
                            {selectedApplication.rodo.state !== "ACCEPTED" && (
                                <Button
                                    text={
                                        <>
                                            <FaCheck aria-hidden="true" />
                                            Zatwierdź
                                        </>
                                    }
                                    onClick={() =>
                                        void setRodoState(selectedApplication, "ACCEPTED")
                                    }
                                    disabled={savingKey === selectedApplication.key}
                                />
                            )}
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    );
};

export default AdminRodoScreen;
