import { useEffect, useState } from "react";
import { FaEdit, FaExclamationTriangle } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import Modal from "../../components/Modal";
import type {
    ApiCompetitionTask,
    ApiEdition,
    ApiSubedition,
    ApiTaskSubmission,
} from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";
import { getStoredUserId } from "../../utils/authService";
import { decodeMultipleChoice } from "../../utils/competitionAnswers";

type TaskWithContext = { task: ApiCompetitionTask; subedition: ApiSubedition; edition: ApiEdition };
const bodyOf = (json: string) => {
    try {
        const value: unknown = JSON.parse(json);
        return value &&
            typeof value === "object" &&
            "body_pl" in value &&
            typeof value.body_pl === "string"
            ? value.body_pl
            : "";
    } catch {
        return "";
    }
};
const optionsOf = (json: string): { id: string; text: string }[] => {
    try {
        const value: unknown = JSON.parse(json);
        if (
            !value ||
            typeof value !== "object" ||
            !("options" in value) ||
            !Array.isArray(value.options)
        )
            return [];
        return value.options.filter(
            (option): option is { id: string; text: string } =>
                !!option &&
                typeof option === "object" &&
                typeof option.id === "string" &&
                typeof option.text === "string"
        );
    } catch {
        return [];
    }
};
const scoreLabel = (score: boolean | null) =>
    score === null ? "Brak" : score ? "Poprawna" : "Niepoprawna";
const displaySubmission = (submission: string, json: string) => {
    try {
        const content = JSON.parse(json) as { answer_type?: string };
        if (content.answer_type !== "multiple_choice") return submission;
        const selected = decodeMultipleChoice(submission);
        if (selected === null) return "Nieprawidłowy format odpowiedzi";
        if (selected.length === 0) return "Nie zaznaczono żadnego wariantu";
        const options = optionsOf(json);
        return selected
            .map((id) => {
                const option = options.find((item) => item.id === id);
                return option ? `${id}. ${option.text}` : `${id} (wariant niedostępny)`;
            })
            .join("\n");
    } catch {
        return submission;
    }
};

export default function ReviewerTasksScreen() {
    const navigate = useNavigate();
    const userId = getStoredUserId();
    const [submissions, setSubmissions] = useState<ApiTaskSubmission[]>([]);
    const [taskMap, setTaskMap] = useState<Record<string, TaskWithContext>>({});
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditionId, setSubeditionId] = useState("");
    const [selected, setSelected] = useState<ApiTaskSubmission | null>(null);
    const [score, setScore] = useState<"correct" | "incorrect" | "">("");
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    useEffect(() => {
        let active = true;
        const load = async () => {
            const [assignments, editions] = await Promise.all([
                apiListRequestResult<ApiTaskSubmission>(
                    `${apiEndpoints.reviewerSubmissions}?${firstPage}`,
                    null,
                    "GET",
                    navigate
                ),
                apiListRequestResult<ApiEdition>(
                    `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
                    null,
                    "GET",
                    navigate
                ),
            ]);
            if (!assignments.data || !editions.data) throw new Error();
            const allSubsByEdition = await Promise.all(
                editions.data.map(async (edition) => {
                    const result = await apiListRequestResult<ApiSubedition>(
                        `${apiEndpoints.subeditions(edition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    );
                    if (!result.data) return [];
                    return result.data.map((subedition) => ({ subedition, edition }));
                })
            );
            const allSubs = allSubsByEdition.flat();
            const allTasks = await Promise.all(
                allSubs.map(async ({ subedition, edition }) => {
                    const result = await apiListRequestResult<ApiCompetitionTask>(
                        `${apiEndpoints.competitionTasks(subedition.editionId, subedition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    );
                    if (!result.data) return [];
                    return result.data.map((task) => ({ task, subedition, edition }));
                })
            );
            const flatTasks = allTasks.flat();
            if (!active) return;
            const loadedTaskIds = new Set(flatTasks.map(({ task }) => task.id));
            const missingAssignments = assignments.data.some(
                (assignment) => !loadedTaskIds.has(assignment.taskId)
            );
            if (missingAssignments)
                setError("Nie udało się pobrać treści części przydzielonych zadań.");
            setTaskMap(Object.fromEntries(flatTasks.map((item) => [item.task.id, item])));
            setEditions(editions.data);
            setSubeditions(allSubs.map(({ subedition }) => subedition));
            setSubmissions(assignments.data);
            const assignedTaskIds = new Set(assignments.data.map((item) => item.taskId));
            const latestClosed = allSubs
                .filter(({ subedition }) => Date.parse(subedition.answerClose) <= Date.now())
                .filter(({ subedition }) =>
                    flatTasks.some(
                        ({ task }) =>
                            task.subEditionId === subedition.id && assignedTaskIds.has(task.id)
                    )
                )
                .sort(
                    (a, b) =>
                        Date.parse(b.subedition.answerClose) - Date.parse(a.subedition.answerClose)
                )[0];
            const initial =
                latestClosed ??
                allSubs.find(({ subedition }) =>
                    flatTasks.some(
                        ({ task }) =>
                            task.subEditionId === subedition.id && assignedTaskIds.has(task.id)
                    )
                );
            if (initial) {
                setEditionId(initial.edition.id);
                setSubeditionId(initial.subedition.id);
            }
        };
        void load()
            .catch(() => {
                if (active)
                    setError("Nie udało się pobrać przydzielonych odpowiedzi lub treści zadań.");
            })
            .finally(() => {
                if (active) setLoading(false);
            });
        return () => {
            active = false;
        };
    }, [navigate]);
    const ownScore = (item: ApiTaskSubmission) =>
        item.reviewer1Id === userId
            ? item.review1Score
            : item.reviewer2Id === userId
              ? item.review2Score
              : null;
    const conflict = (item: ApiTaskSubmission) =>
        item.review1Score !== null &&
        item.review2Score !== null &&
        item.review1Score !== item.review2Score;
    const visibleSubeditions = subeditions
        .filter((item) => item.editionId === editionId)
        .sort((a, b) => Date.parse(b.answerClose) - Date.parse(a.answerClose));
    const visibleSubmissions = submissions
        .filter((item) => {
            const context = taskMap[item.taskId];
            return context?.edition.id === editionId && context.subedition.id === subeditionId;
        })
        .sort((a, b) => {
            const first = taskMap[a.taskId]?.task.name ?? "";
            const second = taskMap[b.taskId]?.task.name ?? "";
            return first.localeCompare(second);
        });
    const open = (item: ApiTaskSubmission) => {
        const current = ownScore(item);
        setSelected(item);
        setScore(current === null ? "" : current ? "correct" : "incorrect");
        setError("");
        setNotice("");
    };
    const save = async () => {
        if (!selected || !score) {
            setError("Wybierz ocenę odpowiedzi.");
            return;
        }
        const context = taskMap[selected.taskId];
        if (!context) {
            setError("Nie udało się odnaleźć zadania. Odśwież stronę.");
            return;
        }
        setBusy(true);
        setError("");
        const result = await apiRequestResult<ApiTaskSubmission>(
            apiEndpoints.reviewerScore(selected.taskId, selected.teamId, context.subedition.id),
            { score: score === "correct" },
            "PUT",
            navigate
        );
        if (result.data) {
            setSubmissions((current) =>
                current.map((item) =>
                    item.taskId === selected.taskId && item.teamId === selected.teamId
                        ? result.data!
                        : item
                )
            );
            setSelected(null);
            setNotice("Zapisano ocenę odpowiedzi.");
        } else
            setError(
                result.status === 409
                    ? "Odpowiedź została zmieniona. Odśwież stronę i spróbuj ponownie."
                    : "Nie udało się zapisać oceny."
            );
        setBusy(false);
    };
    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                    Panel recenzenta
                </p>
                <h1 className="mt-2 text-3xl font-bold text-ink-heading">Odpowiedzi do oceny</h1>
                <p className="mt-3 text-ink-secondary">
                    Sprawdź przydzielone odpowiedzi. W razie konfliktu uzgodnij ocenę z drugim
                    recenzentem.
                </p>
                {notice && (
                    <p role="status" className="app-status-success mt-6 rounded-xl border p-3">
                        {notice}
                    </p>
                )}
                {error && !selected && (
                    <p role="alert" className="app-status-danger mt-6 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                {loading ? (
                    <p className="mt-8 text-ink-secondary">Ładowanie odpowiedzi…</p>
                ) : submissions.length === 0 ? (
                    <p className="mt-8 text-ink-secondary">Nie masz przydzielonych odpowiedzi.</p>
                ) : (
                    <>
                        <div className="mt-8 grid gap-4 sm:grid-cols-2">
                            <label className="text-sm font-medium text-ink">
                                Edycja
                                <select
                                    className="mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5"
                                    value={editionId}
                                    onChange={(event) => {
                                        const nextEdition = event.target.value;
                                        const nextSubedition = subeditions
                                            .filter((item) => item.editionId === nextEdition)
                                            .sort(
                                                (a, b) =>
                                                    Date.parse(b.answerClose) -
                                                    Date.parse(a.answerClose)
                                            )[0];
                                        setEditionId(nextEdition);
                                        setSubeditionId(nextSubedition?.id ?? "");
                                    }}
                                >
                                    {editions.map((item) => (
                                        <option key={item.id} value={item.id}>
                                            {item.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                            <label className="text-sm font-medium text-ink">
                                Podedycja
                                <select
                                    className="mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5"
                                    value={subeditionId}
                                    onChange={(event) => setSubeditionId(event.target.value)}
                                >
                                    {visibleSubeditions.map((item) => (
                                        <option key={item.id} value={item.id}>
                                            {item.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>
                        {visibleSubmissions.length === 0 ? (
                            <p className="mt-6 text-ink-secondary">
                                Brak przydzielonych odpowiedzi w tej podedycji.
                            </p>
                        ) : (
                            <div className="mt-4 space-y-3">
                                {visibleSubmissions.map((item, index) => (
                                    <article
                                        key={`${item.taskId}-${item.teamId}`}
                                        className={`rounded-2xl border p-5 ${conflict(item) ? "border-danger-border bg-danger-soft" : "border-line bg-surface"}`}
                                    >
                                        <div className="flex flex-wrap items-center justify-between gap-4">
                                            <div>
                                                <h2 className="font-semibold text-ink-heading">
                                                    {taskMap[item.taskId]?.task.name ??
                                                        "Treść zadania niedostępna"}
                                                </h2>
                                                <p className="mt-1 text-sm text-ink-secondary">
                                                    Odpowiedź {index + 1} ·{" "}
                                                    {conflict(item) ? (
                                                        <span className="inline-flex items-center gap-1 text-danger-text">
                                                            <FaExclamationTriangle aria-hidden="true" />
                                                            Konflikt opinii
                                                        </span>
                                                    ) : ownScore(item) === null ? (
                                                        "Do oceny"
                                                    ) : (
                                                        "Oceniona"
                                                    )}
                                                </p>
                                            </div>
                                            <Button
                                                text={
                                                    ownScore(item) === null ? "Oceń" : "Zmień ocenę"
                                                }
                                                icon={<FaEdit />}
                                                disabled={!taskMap[item.taskId]}
                                                onClick={() => open(item)}
                                            />
                                        </div>
                                    </article>
                                ))}
                            </div>
                        )}
                    </>
                )}
            </main>
            <Modal
                isOpen={selected !== null}
                onClose={() => {
                    if (!busy) {
                        setSelected(null);
                        setError("");
                    }
                }}
                title="Ocena odpowiedzi"
                maxWidth="lg"
            >
                {selected && (
                    <div className="space-y-5">
                        <h2 className="text-lg font-semibold text-ink-heading">
                            {taskMap[selected.taskId]?.task.name}
                        </h2>
                        <section>
                            <h3 className="mb-2 font-semibold text-ink-heading">Treść zadania</h3>
                            <MarkdownRenderer
                                content={bodyOf(taskMap[selected.taskId]?.task.contentJson ?? "")}
                            />
                            <ul className="mt-3 space-y-1 text-ink">
                                {optionsOf(taskMap[selected.taskId]?.task.contentJson ?? "").map(
                                    (option) => (
                                        <li key={option.id}>
                                            <strong>{option.id}.</strong> {option.text}
                                        </li>
                                    )
                                )}
                            </ul>
                        </section>
                        <section>
                            <h3 className="mb-2 font-semibold text-ink-heading">Odpowiedź</h3>
                            <p className="whitespace-pre-wrap break-words text-ink">
                                {displaySubmission(
                                    selected.submission,
                                    taskMap[selected.taskId]?.task.contentJson ?? ""
                                )}
                            </p>
                        </section>
                        <p className="text-sm text-ink-secondary">
                            {selected.review1Score !== null && selected.review2Score !== null
                                ? "Wynik sprawdzarki jest niedostępny po ocenie obu recenzentów."
                                : `Wynik sprawdzarki: ${scoreLabel(selected.autoReviewState)}`}
                        </p>
                        {conflict(selected) && (
                            <p className="app-status-danger rounded-xl border p-3">
                                Oceny recenzentów są różne. Uzgodnijcie ocenę, a następnie zmień
                                swoją, jeśli to potrzebne.
                            </p>
                        )}
                        {error && (
                            <p role="alert" className="app-status-danger rounded-xl border p-3">
                                {error}
                            </p>
                        )}
                        <fieldset>
                            <legend className="font-semibold text-ink-heading">Twoja ocena</legend>
                            <div className="mt-3 flex flex-wrap gap-6 text-ink">
                                <label className="flex items-center gap-2">
                                    <input
                                        type="radio"
                                        name="review-score"
                                        checked={score === "correct"}
                                        onChange={() => setScore("correct")}
                                    />{" "}
                                    Poprawna
                                </label>
                                <label className="flex items-center gap-2">
                                    <input
                                        type="radio"
                                        name="review-score"
                                        checked={score === "incorrect"}
                                        onChange={() => setScore("incorrect")}
                                    />{" "}
                                    Niepoprawna
                                </label>
                            </div>
                        </fieldset>
                        <div className="flex justify-end gap-2">
                            <Button
                                text="Anuluj"
                                className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                                disabled={busy}
                                onClick={() => setSelected(null)}
                            />
                            <Button
                                text={busy ? "Zapisywanie…" : "Zapisz ocenę"}
                                disabled={busy || !score}
                                onClick={() => void save()}
                            />
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    );
}
