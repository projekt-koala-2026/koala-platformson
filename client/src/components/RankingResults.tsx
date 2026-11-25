import { FaEdit, FaTrash } from "react-icons/fa";
import { rankingPageSize } from "../hooks/useRankingBrowser";
import type { ApiRankingData } from "../types/models";
import Button from "./Button";

export default function RankingResults({
    rows,
    total,
    page,
    onPage,
    onEdit,
    onDelete,
    disabled = false,
}: {
    rows: ApiRankingData[];
    total: number;
    page: number;
    onPage: (page: number) => void;
    onEdit?: (row: ApiRankingData) => void;
    onDelete?: (row: ApiRankingData) => void;
    disabled?: boolean;
}) {
    return (
        <>
            {!rows.length ? (
                <p className="rounded-xl border border-dashed border-line-strong p-6 text-center text-ink-muted">
                    Brak pozycji w rankingu.
                </p>
            ) : (
                <div className="overflow-x-auto rounded-xl border border-line bg-surface">
                    <table className="w-full text-left text-sm">
                        <thead className="bg-surface-muted text-ink-secondary">
                            <tr>
                                <th className="px-4 py-3">Lp.</th>
                                <th className="px-4 py-3">Drużyna</th>
                                <th className="px-4 py-3 text-right">Punkty</th>
                                <th className="px-4 py-3">Awans do kolejnego etapu</th>
                                {onEdit && <th className="px-4 py-3">Akcje</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {rows.map((row, i) => (
                                <tr key={row.id} className="border-t border-line">
                                    <td className="px-4 py-3">{page * rankingPageSize + i + 1}</td>
                                    <td className="break-words px-4 py-3 font-medium text-ink-heading">
                                        {row.teamName}
                                    </td>
                                    <td className="whitespace-nowrap px-4 py-3 text-right">
                                        {row.pointsAchived} / {row.pointsMax}
                                    </td>
                                    <td className="px-4 py-3">
                                        {row.hasQualifiedToNextEtap ? (
                                            <span className="font-semibold text-brand-text">
                                                Tak
                                            </span>
                                        ) : (
                                            "—"
                                        )}
                                    </td>
                                    {onEdit && (
                                        <td className="px-4 py-3">
                                            <div className="flex gap-2">
                                                <Button
                                                    text="Edytuj"
                                                    icon={<FaEdit />}
                                                    disabled={disabled}
                                                    onClick={() => onEdit(row)}
                                                />
                                                <Button
                                                    text="Usuń"
                                                    icon={<FaTrash />}
                                                    disabled={disabled}
                                                    onClick={() => onDelete?.(row)}
                                                    className="bg-danger hover:bg-danger-hover"
                                                />
                                            </div>
                                        </td>
                                    )}
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            <nav
                aria-label="Strony wyników"
                className="mt-4 flex flex-wrap items-center justify-between gap-3"
            >
                <Button
                    text="Poprzednia"
                    disabled={disabled || page === 0}
                    onClick={() => onPage(page - 1)}
                />
                <p className="text-sm text-ink-secondary" aria-live="polite">
                    Strona {page + 1} z {Math.max(1, Math.ceil(total / rankingPageSize))} · {total}{" "}
                    drużyn
                </p>
                <Button
                    text="Następna"
                    disabled={disabled || (page + 1) * rankingPageSize >= total}
                    onClick={() => onPage(page + 1)}
                />
            </nav>
        </>
    );
}
