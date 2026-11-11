import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiCompetitionTask, ApiSubedition, ApiTeam } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";
import MarkdownRenderer from "./MarkdownRenderer";

type Submission = { submission: string; version: number };
type TaskContent = { body_pl?: string; answer_type?: string; [key: string]: unknown };
const inputClass =
    "mt-2 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";

export default function CaptainTasksPanel({ team }: { team: ApiTeam }) {
    const navigate = useNavigate();
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [tasks, setTasks] = useState<ApiCompetitionTask[]>([]);
    const [task, setTask] = useState<ApiCompetitionTask | null>(null);
    const [submission, setSubmission] = useState<Submission | null>(null);
    const [answer, setAnswer] = useState("");
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [message, setMessage] = useState("");

    useEffect(() => {
        let active = true;
        setLoading(true);
        setSubeditions([]);
        setSubeditionId("");
        setTasks([]);
        setTask(null);
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(team.editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const available = (data ?? []).filter((item) => item.state === "ACTIVE");
            setSubeditions(available);
            setSubeditionId(available[0]?.id ?? "");
            if (!data) setMessage("Nie udało się pobrać podedycji.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [team.editionId, navigate]);

    useEffect(() => {
        if (!subeditionId) {
            setTasks([]);
            setTask(null);
            return;
        }
        let active = true;
        setLoading(true);
        setTasks([]);
        setTask(null);
        setMessage("");
        void apiListRequestResult<ApiCompetitionTask>(
            `${apiEndpoints.activeTeamTasks(team.editionId, subeditionId, team.id)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data, error }) => {
            if (!active) return;
            setTasks(data ?? []);
            if (!data)
                setMessage(error?.message ?? "Zadania są dostępne tylko w wyznaczonym czasie.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [team.editionId, team.id, subeditionId, navigate]);

    useEffect(() => {
        if (!task || !subeditionId) {
            setSubmission(null);
            setAnswer("");
            return;
        }
        let active = true;
        setSubmission(null);
        setAnswer("");
        setMessage("");
        void Promise.all([
            apiRequestResult<ApiCompetitionTask>(
                apiEndpoints.competitionTask(team.editionId, subeditionId, task.id),
                null,
                "GET",
                navigate
            ),
            apiRequestResult<Submission>(
                apiEndpoints.teamTaskSubmission(task.id, team.id, subeditionId),
                null,
                "GET",
                navigate
            ),
        ]).then(([taskResult, answerResult]) => {
            if (!active) return;
            if (taskResult.data) setTask(taskResult.data);
            else setMessage("Nie udało się otworzyć zadania.");
            if (answerResult.data) {
                setSubmission(answerResult.data);
                setAnswer(answerResult.data.submission ?? "");
            }
        });
        return () => {
            active = false;
        };
    }, [task?.id, subeditionId, team.editionId, team.id, navigate]);

    const content = useMemo<TaskContent>(() => {
        try {
            return JSON.parse(task?.contentJson ?? "{}") as TaskContent;
        } catch {
            return {};
        }
    }, [task?.contentJson]);
    const answerType = (content.answer_type ?? "open").toLowerCase();
    const options = useMemo(() => {
        const raw = content.options ?? content.choices ?? content.answers;
        if (Array.isArray(raw))
            return raw.map((item, index) =>
                typeof item === "string"
                    ? { value: item, label: item }
                    : {
                          value: String(
                              (item as Record<string, unknown>).value ??
                                  (item as Record<string, unknown>).id ??
                                  String.fromCharCode(65 + index)
                          ),
                          label: String(
                              (item as Record<string, unknown>).label ??
                                  (item as Record<string, unknown>).text ??
                                  (item as Record<string, unknown>).value ??
                                  String.fromCharCode(65 + index)
                          ),
                      }
            );
        if (answerType === "true_false" || answerType === "boolean" || answerType === "tf")
            return [
                { value: "true", label: "Prawda" },
                { value: "false", label: "Fałsz" },
            ];
        if (["abcd", "single_choice", "choice", "multiple_choice"].includes(answerType))
            return ["A", "B", "C", "D"].map((x) => ({ value: x, label: x }));
        return [];
    }, [content, answerType]);

    const save = async (event: FormEvent) => {
        event.preventDefault();
        if (!task || !subeditionId || !answer.trim()) return;
        setBusy(true);
        setMessage("");
        const result = await apiRequestResult<Submission>(
            apiEndpoints.teamTaskSubmission(task.id, team.id, subeditionId),
            { submission: answer.trim(), version: submission?.version ?? 0 },
            "PUT",
            navigate
        );
        if (result.data) {
            setSubmission(result.data);
            setAnswer(result.data.submission);
            setMessage("Odpowiedź została zapisana.");
        } else setMessage(result.error?.message ?? "Nie udało się zapisać odpowiedzi.");
        setBusy(false);
    };

    return (
        <section
            className="mb-8 rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-7"
            aria-labelledby="captain-tasks-heading"
        >
            <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Panel gracza
                    </p>
                    <h2
                        id="captain-tasks-heading"
                        className="mt-1 text-2xl font-bold text-ink-heading"
                    >
                        Zadania drużyny
                    </h2>
                    <p className="mt-2 text-sm text-ink-secondary">
                        Wybierz podedycję, aby zobaczyć zadania dostępne dla drużyny {team.name}.
                    </p>
                </div>
                <label className="min-w-56 text-sm font-medium text-ink">
                    Podedycja
                    <select
                        className={inputClass}
                        value={subeditionId}
                        onChange={(e) => setSubeditionId(e.target.value)}
                        disabled={loading || !subeditions.length}
                    >
                        <option value="">{loading ? "Ładowanie…" : "Wybierz podedycję"}</option>
                        {subeditions.map((s) => (
                            <option value={s.id} key={s.id}>
                                {s.name}
                            </option>
                        ))}
                    </select>
                </label>
            </div>
            {message && (
                <p className="app-status-info mt-4 rounded-xl border p-3 text-sm" role="status">
                    {message}
                </p>
            )}
            <div className="mt-5 grid gap-5 lg:grid-cols-[minmax(13rem,0.7fr)_minmax(0,1.3fr)]">
                <div>
                    <h3 className="mb-2 text-sm font-semibold text-ink-heading">Lista zadań</h3>
                    {loading ? (
                        <p className="text-sm text-ink-muted">Ładowanie zadań…</p>
                    ) : tasks.length ? (
                        <ul className="space-y-2">
                            {tasks.map((item) => (
                                <li key={item.id}>
                                    <button
                                        type="button"
                                        onClick={() => setTask(item)}
                                        className={`w-full rounded-xl border px-4 py-3 text-left text-sm transition ${task?.id === item.id ? "border-brand-focus bg-brand-muted" : "border-line hover:bg-surface-muted"}`}
                                    >
                                        <span className="block font-semibold text-ink-heading">
                                            {item.name}
                                        </span>
                                        <span className="mt-1 block text-xs text-ink-muted">
                                            {item.tier === "TIER_2" ? "Poziom II" : "Poziom I"}
                                        </span>
                                    </button>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p className="text-sm text-ink-muted">Brak dostępnych zadań.</p>
                    )}
                </div>
                <div className="min-h-48 rounded-xl bg-canvas p-4 sm:p-5">
                    {!task ? (
                        <p className="text-sm text-ink-muted">Wybierz zadanie z listy.</p>
                    ) : (
                        <>
                            <h3 className="text-xl font-bold text-ink-heading">{task.name}</h3>
                            <div className="mt-4">
                                <MarkdownRenderer content={content.body_pl ?? ""} />
                            </div>
                            <form
                                className="mt-5 border-t border-line pt-4"
                                onSubmit={(e) => void save(e)}
                            >
                                <label
                                    className="block text-sm font-semibold text-ink-heading"
                                    htmlFor="team-task-answer"
                                >
                                    Twoja odpowiedź
                                </label>
                                {options.length ? (
                                    <div className="mt-3 grid gap-2 sm:grid-cols-2">
                                        {options.map((option) => (
                                            <label
                                                key={option.value}
                                                className={`flex cursor-pointer items-center gap-3 rounded-xl border bg-surface p-3 text-sm ${answer === option.value ? "border-brand-focus" : "border-line"}`}
                                            >
                                                <input
                                                    type="radio"
                                                    name="team-task-answer"
                                                    value={option.value}
                                                    checked={answer === option.value}
                                                    onChange={() => setAnswer(option.value)}
                                                />
                                                {option.label}
                                            </label>
                                        ))}
                                    </div>
                                ) : (
                                    <textarea
                                        id="team-task-answer"
                                        className={inputClass}
                                        rows={answerType === "open" ? 4 : 2}
                                        value={answer}
                                        onChange={(e) => setAnswer(e.target.value)}
                                        placeholder="Wpisz odpowiedź…"
                                    />
                                )}
                                <div className="mt-4 flex flex-wrap items-center gap-3">
                                    <button
                                        disabled={busy || !answer.trim()}
                                        className="rounded-xl bg-brand px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
                                    >
                                        {busy
                                            ? "Zapisywanie…"
                                            : submission
                                              ? "Zapisz odpowiedź"
                                              : "Wyślij odpowiedź"}
                                    </button>
                                    {submission && (
                                        <span className="text-xs text-ink-muted">
                                            Odpowiedź drużyny jest już zapisana; możesz ją
                                            zaktualizować.
                                        </span>
                                    )}
                                </div>
                            </form>
                        </>
                    )}
                </div>
            </div>
        </section>
    );
}
