import { useEffect, useState } from "react";
import { FaEdit } from "react-icons/fa";
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

type TaskWithContext = { task: ApiCompetitionTask; subedition: ApiSubedition };
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
            const allSubs = await Promise.all(
                editions.data.map(async (edition) => {
                    const result = await apiListRequestResult<ApiSubedition>(
                        `${apiEndpoints.subeditions(edition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    );
                    if (!result.data) throw new Error();
                    return result.data;
                })
            );
            const allTasks = await Promise.all(
                allSubs.flat().map(async (subedition) => {
                    const result = await apiListRequestResult<ApiCompetitionTask>(
                        `${apiEndpoints.competitionTasks(subedition.editionId, subedition.id)}?${firstPage}`,
                        null,
                        "GET",
                        navigate
                    );
                    if (!result.data) throw new Error();
                    return result.data.map((task) => ({ task, subedition }));
                })
            );
            if (!active) return;
            setTaskMap(Object.fromEntries(allTasks.flat().map((item) => [item.task.id, item])));
            setSubmissions(assignments.data);
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
                    <div className="mt-8 space-y-3">
                        {submissions.map((item, index) => (
                            <article
                                key={`${item.taskId}-${item.teamId}`}
                                className="rounded-2xl border border-line bg-surface p-5"
                            >
                                <div className="flex flex-wrap items-center justify-between gap-4">
                                    <div>
                                        <h2 className="font-semibold text-ink-heading">
                                            {taskMap[item.taskId]?.task.name ??
                                                "Treść zadania niedostępna"}
                                        </h2>
                                        <p className="mt-1 text-sm text-ink-secondary">
                                            Odpowiedź {index + 1} ·{" "}
                                            {conflict(item)
                                                ? "Konflikt ocen"
                                                : ownScore(item) === null
                                                  ? "Do oceny"
                                                  : "Oceniona"}
                                        </p>
                                    </div>
                                    <Button
                                        text={ownScore(item) === null ? "Oceń" : "Zmień ocenę"}
                                        icon={<FaEdit />}
                                        disabled={!taskMap[item.taskId]}
                                        onClick={() => open(item)}
                                    />
                                </div>
                            </article>
                        ))}
                    </div>
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
