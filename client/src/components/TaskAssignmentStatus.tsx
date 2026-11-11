import { useCallback, useEffect, useState } from "react";
import { FaEdit, FaPlus } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import type { User } from "../types/models";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import Button from "./Button";
import Modal from "./Modal";
import SelectListbox from "./SelectListbox";

type PairProgress = {
    reviewer1Id: string;
    reviewer2Id: string;
    total: number;
    reviewer1Reviewed: number;
    reviewer2Reviewed: number;
    pendingSubmissions: number;
};
type TaskProgress = {
    totalSubmissions: number;
    unassigned: number;
    incomplete: number;
    pairs: PairProgress[];
};
type PendingReviewer = { reviewerId: string; pending: number; unavailableReplacements: string[] };

export default function TaskAssignmentStatus({
    taskId,
    subeditionId,
    reviewers,
}: {
    taskId: string;
    subeditionId: string;
    reviewers: User[];
}) {
    const navigate = useNavigate();
    const [progress, setProgress] = useState<TaskProgress | null>(null);
    const [pendingReviewers, setPendingReviewers] = useState<PendingReviewer[]>([]);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [modal, setModal] = useState<"assign" | "replace" | null>(null);
    const [first, setFirst] = useState("");
    const [second, setSecond] = useState("");
    const [busy, setBusy] = useState(false);
    const [revision, setRevision] = useState(0);
    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequestResult<TaskProgress>(
                apiEndpoints.taskReviewProgress(taskId),
                null,
                "GET",
                navigate
            ),
            apiRequestResult<PendingReviewer[]>(
                apiEndpoints.taskPendingReviewers(taskId),
                null,
                "GET",
                navigate
            ),
        ]).then(([summary, pending]) => {
            if (!active) return;
            if (summary.data && pending.data) {
                setProgress(summary.data);
                setPendingReviewers(pending.data);
            } else setError("Nie udało się pobrać przydziałów. Odśwież stronę i spróbuj ponownie.");
        });
        return () => {
            active = false;
        };
    }, [taskId, navigate, revision]);

    const close = useCallback(() => {
        if (!busy) {
            setModal(null);
            setError("");
        }
    }, [busy]);
    const names = new Map(
        reviewers.map((u) => [u.id, [u.nameFirst, u.nameLast].filter(Boolean).join(" ") || u.email])
    );
    const label = (id: string) => names.get(id) ?? `Niedostępny recenzent (${id})`;
    const options = [
        { value: "", label: "Wybierz recenzenta" },
        ...reviewers.map((u) => ({
            value: u.id,
            label: label(u.id) === u.email ? u.email : `${label(u.id)} (${u.email})`,
        })),
    ];
    const pendingOptions = [
        ...options.filter(
            (option) =>
                !option.value ||
                pendingReviewers.some((reviewer) => reviewer.reviewerId === option.value)
        ),
        ...pendingReviewers
            .filter((reviewer) => !reviewers.some((user) => user.id === reviewer.reviewerId))
            .map((reviewer) => ({ value: reviewer.reviewerId, label: label(reviewer.reviewerId) })),
    ];
    const unassigned = progress?.unassigned ?? 0;
    const incomplete = progress?.incomplete ?? 0;
    const pairs = new Map<
        string,
        { ids: string[]; count: number; scores: Record<string, number>; pending: number }
    >();
    const pending = new Map(
        pendingReviewers.map((reviewer) => [reviewer.reviewerId, reviewer.pending])
    );
    for (const s of progress?.pairs ?? []) {
        const ids = [s.reviewer1Id, s.reviewer2Id].sort();
        const key = ids.join(":");
        const pair = pairs.get(key) ?? { ids, count: 0, scores: {}, pending: 0 };
        pair.count += s.total;
        pair.scores[s.reviewer1Id] = (pair.scores[s.reviewer1Id] ?? 0) + s.reviewer1Reviewed;
        pair.scores[s.reviewer2Id] = (pair.scores[s.reviewer2Id] ?? 0) + s.reviewer2Reviewed;
        pair.pending += s.pendingSubmissions;
        pairs.set(key, pair);
    }
    // A replacement must not already be the other reviewer on any affected submission.
    const forbidden = new Set<string>([
        first,
        ...(pendingReviewers.find((reviewer) => reviewer.reviewerId === first)
            ?.unavailableReplacements ?? []),
    ]);
    const existingPair = pairs.size === 1 ? [...pairs.values()][0].ids : null;
    const canAssign = unassigned > 0 && incomplete === 0 && pairs.size <= 1;
    const currentPairs = [...pairs.values()].filter((pair) => pair.pending > 0);
    const currentPair = currentPairs.length === 1 ? currentPairs[0] : null;
    const historicalPairs = [...pairs.values()].filter((pair) => pair.pending === 0);
    const save = async () => {
        if (
            !first ||
            !second ||
            first === second ||
            (modal === "replace" && forbidden.has(second))
        ) {
            setError(
                "Wybierz dwie różne osoby, które nie tworzą już pary przy zmienianych odpowiedziach."
            );
            return;
        }
        setBusy(true);
        setError("");
        const result = await apiRequestResult(
            modal === "assign"
                ? apiEndpoints.assignReviewers(subeditionId, taskId)
                : apiEndpoints.updateAssignedReviewer(subeditionId, taskId),
            modal === "assign"
                ? { reviewer1Id: first, reviewer2Id: second }
                : { oldReviewer: first, newReviewer: second },
            "PUT",
            navigate
        );
        if (result.status && result.status >= 200 && result.status < 300) {
            setModal(null);
            setNotice(
                modal === "assign"
                    ? "Przydzielono parę recenzentów."
                    : "Przeniesiono nieocenione odpowiedzi do nowego recenzenta."
            );
            setProgress(null);
            setRevision((current) => current + 1);
        } else
            setError(
                "Nie udało się zapisać przydziału. Odśwież dane i sprawdź konta recenzentów oraz termin odpowiedzi."
            );
        setBusy(false);
    };
    return (
        <div className="mt-4 space-y-3">
            {error && !modal && (
                <p role="alert" className="app-status-danger rounded-xl border p-3">
                    {error}
                </p>
            )}
            {notice && (
                <p role="status" className="app-status-success rounded-xl border p-3">
                    {notice}
                </p>
            )}
            {!progress ? (
                !error && (
                    <p role="status" className="text-sm text-ink-secondary">
                        Ładowanie przydziałów…
                    </p>
                )
            ) : (
                <>
                    <p className="text-sm text-ink-secondary">
                        Odpowiedzi: {progress.totalSubmissions} · Bez przydziału: {unassigned}
                    </p>
                    {progress.totalSubmissions === 0 && (
                        <p className="text-sm text-ink-secondary">
                            Brak odpowiedzi dla tego zadania — nie ma jeszcze czego przydzielać.
                        </p>
                    )}
                    {currentPair && (
                        <div className="rounded-xl border border-brand-border bg-brand-muted p-4">
                            <p className="font-semibold text-ink-heading">
                                Para przy nieocenionych odpowiedziach
                            </p>
                            <p className="mt-1 text-sm text-ink-secondary">
                                Odpowiedzi w tym zestawie: {currentPair.count}
                            </p>
                            <div className="mt-3 space-y-3">
                                {currentPair.ids.map((id) => (
                                    <div
                                        key={id}
                                        className="flex flex-wrap items-center justify-between gap-2"
                                    >
                                        <span className="text-sm text-ink">
                                            <strong>{label(id)}</strong> · oceniono:{" "}
                                            {currentPair.scores[id] ?? 0} / {currentPair.count}
                                        </span>
                                        {(pending.get(id) ?? 0) > 0 && (
                                            <Button
                                                text={`Zmień: ${label(id)}`}
                                                icon={<FaEdit />}
                                                onClick={() => {
                                                    setFirst(id);
                                                    setSecond("");
                                                    setError("");
                                                    setModal("replace");
                                                }}
                                            />
                                        )}
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                    {currentPairs.length > 1 && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3 text-sm">
                            Nie można jednoznacznie ustalić aktualnej pary: nieocenione odpowiedzi
                            mają różne przydziały. Sprawdź dane na serwerze.
                        </p>
                    )}
                    {historicalPairs.length > 0 && (
                        <div>
                            <p className="text-sm text-ink-secondary">
                                Zakończone zestawy odpowiedzi:
                            </p>
                            <ul className="mt-2 space-y-2">
                                {historicalPairs.map((pair) => (
                                    <li
                                        key={pair.ids.join(":")}
                                        className="rounded-xl border border-line bg-surface-muted px-4 py-3 text-sm text-ink-secondary"
                                    >
                                        <span>
                                            {pair.ids
                                                .map(
                                                    (id) =>
                                                        `${label(id)}: ${pair.scores[id] ?? 0} / ${pair.count}`
                                                )
                                                .join(" · ")}
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                    {pairs.size > 0 && currentPairs.length === 0 && (
                        <p className="text-sm text-ink-secondary">
                            Wszystkie przydzielone odpowiedzi zostały ocenione. Backend nie
                            przechowuje osobnej informacji o aktualnej parze.
                        </p>
                    )}
                    {incomplete > 0 && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3 text-sm">
                            Niepełny przydział przy {incomplete} odpowiedziach — sprawdź dane na
                            serwerze.
                        </p>
                    )}
                    {unassigned > 0 && pairs.size > 1 && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3 text-sm">
                            Nie można ustalić jednej pary dla nieprzydzielonych odpowiedzi. Sprawdź
                            przydziały na serwerze.
                        </p>
                    )}
                    <p className="text-sm text-ink-secondary">
                        Przy zmianie recenzenta jego dotychczasowe oceny pozostają bez zmian.
                        Przenoszone są tylko odpowiedzi, których jeszcze nie ocenił.
                    </p>
                    <div className="flex flex-wrap gap-3">
                        {canAssign && (
                            <Button
                                text="Przydziel parę"
                                icon={<FaPlus />}
                                disabled={reviewers.length < 2}
                                onClick={() => {
                                    setFirst(existingPair?.[0] ?? "");
                                    setSecond(existingPair?.[1] ?? "");
                                    setError("");
                                    setModal("assign");
                                }}
                            />
                        )}
                    </div>
                    {reviewers.length < 2 && (
                        <p className="text-sm text-ink-secondary">
                            Potrzebne są co najmniej dwa konta recenzentów.
                        </p>
                    )}
                </>
            )}
            <Modal
                isOpen={modal !== null}
                onClose={close}
                title={modal === "assign" ? "Przydziel parę recenzentów" : "Zmień recenzenta"}
                maxWidth="lg"
                footer={
                    <div className="flex flex-wrap justify-end gap-3">
                        <Button
                            text="Anuluj"
                            disabled={busy}
                            className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                            onClick={close}
                        />
                        <Button
                            text={busy ? "Zapisywanie…" : "Zapisz"}
                            disabled={busy || !first || !second}
                            onClick={() => void save()}
                        />
                    </div>
                }
            >
                <p className="text-sm text-ink-secondary">
                    {modal === "assign"
                        ? `Para zostanie przydzielona do ${unassigned} nieprzydzielonych odpowiedzi.`
                        : "Wybierz osobę do zastąpienia oraz nowego recenzenta. Ocenione przez poprzednią osobę odpowiedzi pozostaną przy niej."}
                </p>
                {existingPair && modal === "assign" && (
                    <p className="mt-2 text-sm text-ink-secondary">
                        Zachowaj istniejącą parę, aby odpowiedzi nie były rozdzielone między różne
                        pary.
                    </p>
                )}
                {error && (
                    <p role="alert" className="app-status-danger mt-4 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                <div className="mt-4 grid gap-4 sm:grid-cols-2">
                    <div>
                        <span id="reviewer-first" className="mb-2 block text-sm text-ink">
                            {modal === "assign" ? "Recenzent 1" : "Zastępowany recenzent"}
                        </span>
                        {existingPair && modal === "assign" ? (
                            <p className="py-3 text-ink">{label(first)}</p>
                        ) : (
                            <SelectListbox
                                inline
                                ariaLabelledBy="reviewer-first"
                                value={first}
                                options={
                                    modal === "replace"
                                        ? pendingOptions
                                        : options.filter((o) => !o.value || o.value !== second)
                                }
                                onChange={(id) => {
                                    setFirst(id);
                                    if (modal === "replace") setSecond("");
                                }}
                            />
                        )}
                    </div>
                    <div>
                        <span id="reviewer-second" className="mb-2 block text-sm text-ink">
                            {modal === "assign" ? "Recenzent 2" : "Nowy recenzent"}
                        </span>
                        {existingPair && modal === "assign" ? (
                            <p className="py-3 text-ink">{label(second)}</p>
                        ) : (
                            <SelectListbox
                                inline
                                ariaLabelledBy="reviewer-second"
                                value={second}
                                options={options.filter(
                                    (o) =>
                                        !o.value ||
                                        (modal === "replace"
                                            ? !forbidden.has(o.value)
                                            : o.value !== first)
                                )}
                                onChange={setSecond}
                            />
                        )}
                    </div>
                </div>
                {modal === "replace" && first && (
                    <p className="mt-4 text-sm text-ink-secondary">
                        Oczekujące odpowiedzi do przeniesienia: {pending.get(first) ?? 0}.
                    </p>
                )}
            </Modal>
        </div>
    );
}
