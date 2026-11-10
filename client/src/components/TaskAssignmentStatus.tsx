import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiTaskSubmission, User } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult } from "../utils/apiFetcher";

export default function TaskAssignmentStatus({
    taskId,
    reviewers,
}: {
    taskId: string;
    reviewers: User[];
}) {
    const navigate = useNavigate();
    const [submissions, setSubmissions] = useState<ApiTaskSubmission[] | null>(null);
    const [error, setError] = useState(false);

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiTaskSubmission>(
            `${apiEndpoints.taskSubmissions(taskId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) setSubmissions(data);
            else setError(true);
        });
        return () => {
            active = false;
        };
    }, [taskId, navigate]);

    if (error)
        return (
            <p role="alert" className="app-status-danger mt-4 rounded-xl border p-3">
                Nie udało się pobrać przydziałów. Odśwież stronę i spróbuj ponownie.
            </p>
        );
    if (!submissions)
        return (
            <p role="status" className="mt-4 text-sm text-ink-secondary">
                Ładowanie przydziałów…
            </p>
        );
    if (submissions.length === 0)
        return (
            <p className="mt-4 text-sm text-ink-secondary">
                Brak odpowiedzi dla tego zadania — nie ma jeszcze czego przydzielać.
            </p>
        );

    const names = new Map(
        reviewers.map((user) => [
            user.id,
            [user.nameFirst, user.nameLast].filter(Boolean).join(" ") || user.email,
        ])
    );
    const pairs = new Map<string, { ids: string[]; count: number }>();
    let unassigned = 0;
    let incomplete = 0;
    for (const item of submissions) {
        if (!item.reviewer1Id && !item.reviewer2Id) {
            unassigned++;
            continue;
        }
        if (!item.reviewer1Id || !item.reviewer2Id) {
            incomplete++;
            continue;
        }
        const ids = [item.reviewer1Id, item.reviewer2Id].sort();
        const key = ids.join(":");
        const pair = pairs.get(key) ?? { ids, count: 0 };
        pair.count++;
        pairs.set(key, pair);
    }

    return (
        <div className="mt-4 space-y-3">
            <p className="text-sm text-ink-secondary">
                Odpowiedzi: {submissions.length} · Bez przydziału: {unassigned}
            </p>
            {pairs.size === 0 ? (
                <p className="text-sm text-ink-secondary">Nie przydzielono jeszcze żadnej pary.</p>
            ) : (
                <ul className="space-y-2">
                    {Array.from(pairs.entries()).map(([key, pair]) => (
                        <li
                            key={key}
                            className="rounded-xl border border-line bg-surface-muted px-4 py-3 text-sm text-ink"
                        >
                            <span className="font-medium text-ink-heading">
                                {pair.ids
                                    .map((id) => names.get(id) ?? "Recenzent niedostępny")
                                    .join(" i ")}
                            </span>
                            <span className="ml-2 text-ink-secondary">
                                · odpowiedzi: {pair.count}
                            </span>
                        </li>
                    ))}
                </ul>
            )}
            {incomplete > 0 && (
                <p role="alert" className="app-status-danger rounded-xl border p-3 text-sm">
                    Niepełny przydział przy {incomplete} odpowiedziach — sprawdź dane na serwerze.
                </p>
            )}
        </div>
    );
}
