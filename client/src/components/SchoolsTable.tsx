import type { KeyboardEvent } from "react";
import { useMemo, useState } from "react";
import type { School } from "../types/models";

const ITEMS_PER_PAGE = 50;

const columns = [
    { key: "rspo", label: "RSPO", width: "w-28" },
    { key: "name", label: "Pełna nazwa szkoły", width: "min-w-72" },
    { key: "nameShort", label: "Nazwa skrócona", width: "min-w-48" },
    { key: "state", label: "Województwo", width: "min-w-40" },
    { key: "city", label: "Miejscowość", width: "min-w-40" },
    { key: "type", label: "Typ placówki", width: "min-w-44" },
    { key: "addres", label: "Adres", width: "min-w-56" },
] as const;

type SchoolColumn = (typeof columns)[number]["key"];
type SchoolFilters = Record<SchoolColumn, string>;

const emptyFilters: SchoolFilters = {
    rspo: "",
    name: "",
    nameShort: "",
    state: "",
    city: "",
    type: "",
    addres: "",
};

interface SchoolsTableProps {
    schools?: School[];
    onRowClick?: (school: School) => void;
    selectedRspo?: number | null;
}

const SchoolsTable = ({ schools = [], onRowClick, selectedRspo = null }: SchoolsTableProps) => {
    const [filters, setFilters] = useState<SchoolFilters>(emptyFilters);
    const [currentPage, setCurrentPage] = useState(1);

    const filteredSchools = useMemo(
        () =>
            schools.filter((school) =>
                columns.every(({ key }) =>
                    String(school[key] ?? "")
                        .toLocaleLowerCase("pl-PL")
                        .includes(filters[key].trim().toLocaleLowerCase("pl-PL"))
                )
            ),
        [filters, schools]
    );

    const totalPages = Math.max(1, Math.ceil(filteredSchools.length / ITEMS_PER_PAGE));
    const visiblePage = Math.min(currentPage, totalPages);
    const paginatedSchools = useMemo(() => {
        const start = (visiblePage - 1) * ITEMS_PER_PAGE;
        return filteredSchools.slice(start, start + ITEMS_PER_PAGE);
    }, [filteredSchools, visiblePage]);

    const setFilter = (column: SchoolColumn, value: string) => {
        setFilters((previous) => ({ ...previous, [column]: value }));
        setCurrentPage(1);
    };

    const clearFilters = () => {
        setFilters(emptyFilters);
        setCurrentPage(1);
    };

    const activateRow = (event: KeyboardEvent<HTMLTableRowElement>, school: School) => {
        if (!onRowClick || (event.key !== "Enter" && event.key !== " ")) return;
        event.preventDefault();
        onRowClick(school);
    };

    const pageButton =
        "rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-emerald-300 hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-40";
    const filterInput =
        "mt-2 w-full rounded-lg border border-slate-200 bg-white px-2.5 py-2 text-sm font-normal text-slate-800 outline-none placeholder:text-slate-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";

    return (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
            <div className="flex flex-col gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm text-slate-600">
                    Strona <strong className="text-slate-900">{visiblePage}</strong> z{" "}
                    <strong className="text-slate-900">{totalPages}</strong>
                    <span className="ml-2 text-slate-500">({filteredSchools.length} wyników)</span>
                </p>
                <div className="flex flex-wrap items-center gap-2">
                    <button
                        type="button"
                        onClick={clearFilters}
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
                        onClick={() => setCurrentPage(Math.min(totalPages, visiblePage + 1))}
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

            <div className="max-h-[38rem] overflow-auto">
                <table className="w-full min-w-[1180px] border-collapse text-left text-sm">
                    <thead className="sticky top-0 z-10 bg-slate-100 shadow-[0_1px_0_rgb(203_213_225)]">
                        <tr>
                            {columns.map(({ key, label, width }) => (
                                <th
                                    key={key}
                                    scope="col"
                                    className={`${width} px-3 py-3 align-top font-semibold text-slate-800`}
                                >
                                    <label>
                                        <span>{label}</span>
                                        <input
                                            type="search"
                                            value={filters[key]}
                                            onChange={(event) => setFilter(key, event.target.value)}
                                            className={filterInput}
                                            placeholder="Filtruj…"
                                            aria-label={`Filtruj: ${label}`}
                                        />
                                    </label>
                                </th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                        {paginatedSchools.length === 0 ? (
                            <tr>
                                <td
                                    colSpan={columns.length}
                                    className="px-6 py-14 text-center text-slate-500"
                                >
                                    Brak szkół spełniających kryteria wyszukiwania.
                                </td>
                            </tr>
                        ) : (
                            paginatedSchools.map((school) => {
                                const selected = school.rspo === selectedRspo;
                                return (
                                    <tr
                                        key={school.rspo}
                                        tabIndex={onRowClick ? 0 : undefined}
                                        aria-selected={selected}
                                        onClick={() => onRowClick?.(school)}
                                        onKeyDown={(event) => activateRow(event, school)}
                                        className={`${onRowClick ? "cursor-pointer focus:outline-none focus:ring-2 focus:ring-inset focus:ring-emerald-500" : ""} ${selected ? "bg-emerald-50" : "bg-white hover:bg-slate-50"}`}
                                    >
                                        <td className="whitespace-nowrap px-3 py-3 font-mono text-xs font-semibold text-slate-700">
                                            {school.rspo}
                                        </td>
                                        <td className="px-3 py-3 font-medium text-slate-900">
                                            {school.name}
                                        </td>
                                        <td className="px-3 py-3 text-slate-600">
                                            {school.nameShort || "—"}
                                        </td>
                                        <td className="px-3 py-3 text-slate-600">{school.state}</td>
                                        <td className="px-3 py-3 text-slate-600">{school.city}</td>
                                        <td className="px-3 py-3 text-slate-600">{school.type}</td>
                                        <td className="px-3 py-3 text-slate-600">
                                            {school.addres}
                                        </td>
                                    </tr>
                                );
                            })
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

export default SchoolsTable;
