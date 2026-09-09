import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import type { Team } from "../../types/models";
import { apiRequestResult } from "../../utils/apiFetcher";

const ITEMS_PER_PAGE = 50;
const filterColumns = ["teamName", "name1", "name2", "name3", "name4", "schoolRSPO"] as const;
type FilterColumn = (typeof filterColumns)[number];
type Filters = Record<FilterColumn, string>;
type EditField = "teamName" | "name1" | "name2" | "name3" | "name4";
type EditForm = Record<EditField, string>;

const emptyFilters: Filters = {
    teamName: "",
    name1: "",
    name2: "",
    name3: "",
    name4: "",
    schoolRSPO: "",
};
const headers: Array<{ key: FilterColumn; label: string; width: string }> = [
    { key: "teamName", label: "Nazwa zespołu", width: "min-w-56" },
    { key: "name1", label: "Członek 1", width: "min-w-44" },
    { key: "name2", label: "Członek 2", width: "min-w-44" },
    { key: "name3", label: "Członek 3", width: "min-w-44" },
    { key: "name4", label: "Członek 4", width: "min-w-44" },
    { key: "schoolRSPO", label: "RSPO szkoły", width: "w-36" },
];
const emptyEditForm: EditForm = { teamName: "", name1: "", name2: "", name3: "", name4: "" };

const AdminTeamsScreen = () => {
    const navigate = useNavigate();
    const [teams, setTeams] = useState<Team[]>([]);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<Filters>(emptyFilters);
    const [currentPage, setCurrentPage] = useState(1);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editForm, setEditForm] = useState<EditForm>(emptyEditForm);
    const [pendingId, setPendingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<{ tone: "success" | "error"; message: string } | null>(
        null
    );

    const loadTeams = useCallback(async () => {
        const result = await apiRequestResult<Team[]>("/api/teams", null, "GET", navigate);
        if (result.data) setTeams(result.data);
        return result;
    }, [navigate]);

    useEffect(() => {
        let active = true;
        void apiRequestResult<Team[]>("/api/teams", null, "GET", navigate).then((result) => {
            if (active) {
                if (result.data) setTeams(result.data);
                else
                    setFeedback({
                        tone: "error",
                        message: "Nie udało się pobrać listy zespołów.",
                    });
                setLoading(false);
            }
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const filteredTeams = useMemo(
        () =>
            teams.filter((team) =>
                filterColumns.every((column) =>
                    String(team[column] ?? "")
                        .toLocaleLowerCase("pl-PL")
                        .includes(filters[column].trim().toLocaleLowerCase("pl-PL"))
                )
            ),
        [filters, teams]
    );
    const totalPages = Math.max(1, Math.ceil(filteredTeams.length / ITEMS_PER_PAGE));
    const visiblePage = Math.min(currentPage, totalPages);
    const visibleTeams = useMemo(
        () => filteredTeams.slice((visiblePage - 1) * ITEMS_PER_PAGE, visiblePage * ITEMS_PER_PAGE),
        [filteredTeams, visiblePage]
    );

    const setFilter = (column: FilterColumn, value: string) => {
        setFilters((previous) => ({ ...previous, [column]: value }));
        setCurrentPage(1);
    };
    const beginEdit = (team: Team) => {
        setEditingId(team.id);
        setFeedback(null);
        setEditForm({
            teamName: team.teamName ?? "",
            name1: team.name1 ?? "",
            name2: team.name2 ?? "",
            name3: team.name3 ?? "",
            name4: team.name4 ?? "",
        });
    };

    const updateTeam = async (team: Team) => {
        if (!editForm.teamName.trim()) {
            setFeedback({ tone: "error", message: "Nazwa zespołu nie może być pusta." });
            return;
        }
        setPendingId(team.id);
        setFeedback(null);
        const { data: updated } = await apiRequestResult<boolean>(
            `/api/admin/teams/${team.id}`,
            {
                id: team.id,
                teamName: editForm.teamName.trim(),
                name1: editForm.name1.trim(),
                name2: editForm.name2.trim(),
                name3: editForm.name3.trim(),
                name4: editForm.name4.trim(),
                schoolRSPO: team.schoolRSPO ?? null,
            },
            "PUT",
            navigate
        );
        if (updated) {
            setTeams((previous) =>
                previous.map((current) =>
                    current.id === team.id ? { ...current, ...editForm } : current
                )
            );
            setEditingId(null);
            setFeedback({ tone: "success", message: "Dane zespołu zostały zapisane." });
            const refreshed = await loadTeams();
            if (!refreshed.data)
                setFeedback({
                    tone: "success",
                    message: "Zmiany zapisano. Nie udało się jedynie odświeżyć listy z serwera.",
                });
        } else setFeedback({ tone: "error", message: "Nie udało się zapisać zmian zespołu." });
        setPendingId(null);
    };

    const deleteTeam = async (team: Team) => {
        if (!window.confirm(`Usunąć zespół „${team.teamName || "bez nazwy"}”?`)) return;
        setPendingId(team.id);
        setFeedback(null);
        const { data: deleted } = await apiRequestResult<boolean>(
            `/api/admin/teams/${team.id}`,
            null,
            "DELETE",
            navigate
        );
        if (deleted) {
            if (editingId === team.id) setEditingId(null);
            setTeams((previous) => previous.filter((current) => current.id !== team.id));
            setFeedback({ tone: "success", message: "Zespół został usunięty." });
            const refreshed = await loadTeams();
            if (!refreshed.data)
                setFeedback({
                    tone: "success",
                    message: "Zespół usunięto. Nie udało się jedynie odświeżyć listy z serwera.",
                });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć zespołu." });
        setPendingId(null);
    };

    const pageButton =
        "rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-emerald-300 hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-40";
    const filterInput =
        "mt-2 w-full rounded-lg border border-slate-200 bg-white px-2.5 py-2 text-sm font-normal text-slate-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";
    const editInput =
        "w-full rounded-lg border border-emerald-200 bg-white px-2.5 py-2 text-sm text-slate-900 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";

    return (
        <>
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
                <header className="border-b border-slate-100 pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Administracja
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">
                        Zarejestrowane drużyny
                    </h1>
                    <p className="mt-3 text-slate-600">
                        Przeglądaj składy, poprawiaj dane uczestników i usuwaj błędne zgłoszenia.
                    </p>
                </header>
                {feedback && (
                    <div
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-700"}`}
                    >
                        {feedback.message}
                    </div>
                )}
                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center text-sm text-slate-500">
                        Ładowanie listy zespołów…
                    </div>
                ) : (
                    <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                        <div className="flex flex-col gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                            <p className="text-sm text-slate-600">
                                Strona <strong className="text-slate-900">{visiblePage}</strong> z{" "}
                                <strong className="text-slate-900">{totalPages}</strong>
                                <span className="ml-2 text-slate-500">
                                    ({filteredTeams.length} wyników)
                                </span>
                            </p>
                            <div className="flex flex-wrap gap-2">
                                <button
                                    type="button"
                                    onClick={() => {
                                        setFilters(emptyFilters);
                                        setCurrentPage(1);
                                    }}
                                    disabled={!Object.values(filters).some(Boolean)}
                                    className={pageButton}
                                >
                                    Wyczyść filtry
                                </button>
                                <button
                                    type="button"
                                    aria-label="Pierwsza strona"
                                    onClick={() => setCurrentPage(1)}
                                    disabled={visiblePage === 1}
                                    className={pageButton}
                                >
                                    «
                                </button>
                                <button
                                    type="button"
                                    onClick={() => setCurrentPage(Math.max(1, visiblePage - 1))}
                                    disabled={visiblePage === 1}
                                    className={pageButton}
                                >
                                    Poprzednia
                                </button>
                                <button
                                    type="button"
                                    onClick={() =>
                                        setCurrentPage(Math.min(totalPages, visiblePage + 1))
                                    }
                                    disabled={visiblePage === totalPages}
                                    className={pageButton}
                                >
                                    Następna
                                </button>
                                <button
                                    type="button"
                                    aria-label="Ostatnia strona"
                                    onClick={() => setCurrentPage(totalPages)}
                                    disabled={visiblePage === totalPages}
                                    className={pageButton}
                                >
                                    »
                                </button>
                            </div>
                        </div>
                        <div className="max-h-[42rem] overflow-auto">
                            <table className="w-full min-w-[1220px] border-collapse text-left text-sm">
                                <thead className="sticky top-0 z-10 bg-slate-100 shadow-[0_1px_0_rgb(203_213_225)]">
                                    <tr>
                                        {headers.map(({ key, label, width }) => (
                                            <th
                                                key={key}
                                                className={`${width} px-3 py-3 align-top font-semibold text-slate-800`}
                                            >
                                                <label>
                                                    <span>{label}</span>
                                                    <input
                                                        type="search"
                                                        value={filters[key]}
                                                        onChange={(event) =>
                                                            setFilter(key, event.target.value)
                                                        }
                                                        className={filterInput}
                                                        aria-label={`Filtruj: ${label}`}
                                                    />
                                                </label>
                                            </th>
                                        ))}
                                        <th className="sticky right-0 min-w-44 bg-slate-100 px-3 py-3 align-top font-semibold text-slate-800">
                                            Akcje
                                        </th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {visibleTeams.length === 0 ? (
                                        <tr>
                                            <td
                                                colSpan={7}
                                                className="px-6 py-14 text-center text-slate-500"
                                            >
                                                Brak zespołów spełniających kryteria wyszukiwania.
                                            </td>
                                        </tr>
                                    ) : (
                                        visibleTeams.map((team) => {
                                            const editing = editingId === team.id;
                                            const pending = pendingId === team.id;
                                            const values: EditField[] = [
                                                "teamName",
                                                "name1",
                                                "name2",
                                                "name3",
                                                "name4",
                                            ];
                                            return (
                                                <tr
                                                    key={team.id}
                                                    className={
                                                        editing
                                                            ? "bg-emerald-50"
                                                            : "bg-white hover:bg-slate-50"
                                                    }
                                                >
                                                    {values.map((field) => (
                                                        <td
                                                            key={field}
                                                            className={`px-3 py-3 ${field === "teamName" ? "font-medium text-slate-900" : "text-slate-600"}`}
                                                        >
                                                            {editing ? (
                                                                <input
                                                                    value={editForm[field]}
                                                                    onChange={(event) =>
                                                                        setEditForm((previous) => ({
                                                                            ...previous,
                                                                            [field]:
                                                                                event.target.value,
                                                                        }))
                                                                    }
                                                                    className={editInput}
                                                                    aria-label={
                                                                        headers.find(
                                                                            (header) =>
                                                                                header.key === field
                                                                        )?.label
                                                                    }
                                                                />
                                                            ) : (
                                                                team[field] || "—"
                                                            )}
                                                        </td>
                                                    ))}
                                                    <td className="whitespace-nowrap px-3 py-3 font-mono text-xs text-slate-600">
                                                        {team.schoolRSPO ?? "—"}
                                                    </td>
                                                    <td
                                                        className={`sticky right-0 px-3 py-3 ${editing ? "bg-emerald-50" : "bg-white"}`}
                                                    >
                                                        <div className="flex gap-2">
                                                            {editing ? (
                                                                <>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() =>
                                                                            void updateTeam(team)
                                                                        }
                                                                        disabled={pending}
                                                                        className="rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                                                                    >
                                                                        {pending
                                                                            ? "Zapisywanie…"
                                                                            : "Zapisz"}
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() =>
                                                                            setEditingId(null)
                                                                        }
                                                                        disabled={pending}
                                                                        className={pageButton}
                                                                    >
                                                                        Anuluj
                                                                    </button>
                                                                </>
                                                            ) : (
                                                                <>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() =>
                                                                            beginEdit(team)
                                                                        }
                                                                        className="rounded-lg bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-800 hover:bg-emerald-100"
                                                                    >
                                                                        Edytuj
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        onClick={() =>
                                                                            void deleteTeam(team)
                                                                        }
                                                                        disabled={pending}
                                                                        className="rounded-lg bg-red-50 px-3 py-2 text-xs font-semibold text-red-700 hover:bg-red-100 disabled:opacity-50"
                                                                    >
                                                                        Usuń
                                                                    </button>
                                                                </>
                                                            )}
                                                        </div>
                                                    </td>
                                                </tr>
                                            );
                                        })
                                    )}
                                </tbody>
                            </table>
                        </div>
                        <p className="border-t border-slate-100 px-4 py-3 text-xs text-slate-500">
                            Zmiana szkoły zespołu nie jest dostępna w endpointzie administracyjnym.
                        </p>
                    </section>
                )}
            </main>
        </>
    );
};

export default AdminTeamsScreen;
