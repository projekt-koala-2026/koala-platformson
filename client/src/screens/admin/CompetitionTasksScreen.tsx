import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate, useSearchParams } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import Modal from "../../components/Modal";
import SelectListbox from "../../components/SelectListbox";
import TaskAnswersModal from "../../components/TaskAnswersModal";
import type {
    ApiCompetitionTask,
    ApiEdition,
    ApiSubedition,
    ApiTaskAnswer,
} from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";
import { decodeMultipleChoice } from "../../utils/competitionAnswers";

const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading focus:border-brand-focus focus:outline-none focus:ring-2 focus:ring-brand-muted";
type TaskForm = { name: string; tier: "TIER_1" | "TIER_2"; contentJson: string };
type ChoiceOption = { id: string; text: string };
const optionNumber = (id: string) =>
    /^[A-Z]+$/.test(id)
        ? [...id].reduce((value, letter) => value * 26 + letter.charCodeAt(0) - 64, 0)
        : 0;
const optionId = (number: number) => {
    let result = "";
    while (number > 0) {
        number--;
        result = String.fromCharCode(65 + (number % 26)) + result;
        number = Math.floor(number / 26);
    }
    return result;
};
const readOptions = (json: string): ChoiceOption[] => {
    try {
        const content: unknown = JSON.parse(json);
        if (
            !content ||
            typeof content !== "object" ||
            !("options" in content) ||
            !Array.isArray(content.options)
        )
            return [];
        return content.options.filter(
            (option): option is ChoiceOption =>
                !!option &&
                typeof option === "object" &&
                typeof option.id === "string" &&
                typeof option.text === "string"
        );
    } catch {
        return [];
    }
};
const emptyForm: TaskForm = {
    name: "",
    tier: "TIER_1",
    contentJson: '{\n  "version": 1,\n  "body_pl": "",\n  "answer_type": "open"\n}',
};

export default function CompetitionTasksScreen() {
    const navigate = useNavigate();
    const [params] = useSearchParams();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [tasks, setTasks] = useState<ApiCompetitionTask[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingTasks, setLoadingTasks] = useState(false);
    const [error, setError] = useState("");
    const [feedback, setFeedback] = useState("");
    const [editing, setEditing] = useState<ApiCompetitionTask | null>(null);
    const [answersTask, setAnswersTask] = useState<ApiCompetitionTask | null>(null);
    const [modal, setModal] = useState(false);
    const [busy, setBusy] = useState(false);
    const [form, setForm] = useState<TaskForm>(emptyForm);
    const [rawMode, setRawMode] = useState(false);
    const selectedEdition = editions.find((e) => e.id === editionId);
    const selectedSubedition = subeditions.find((s) => s.id === subeditionId);

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) {
                setEditions(data);
                setEditionId(
                    data.find((e) => e.id === params.get("edition"))?.id ??
                        data.find((e) => e.state === "ACTIVE")?.id ??
                        data[0]?.id ??
                        ""
                );
            } else setError("Nie udało się pobrać edycji.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate, params]);
    useEffect(() => {
        if (!editionId) return;
        let active = true;
        setSubeditions([]);
        setSubeditionId("");
        setTasks([]);
        setLoadingTasks(true);
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) {
                const eligible =
                    selectedEdition?.state === "CLOSED"
                        ? []
                        : data.filter((subedition) => subedition.state !== "CLOSED");
                setSubeditions(eligible);
                setSubeditionId(
                    eligible.find((s) => s.id === params.get("subedition"))?.id ??
                        eligible[0]?.id ??
                        ""
                );
            } else setError("Nie udało się pobrać podedycji.");
            setLoadingTasks(false);
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate, params, selectedEdition?.state]);
    useEffect(() => {
        if (!editionId || !subeditionId) return;
        let active = true;
        setTasks([]);
        setLoadingTasks(true);
        void apiListRequestResult<ApiCompetitionTask>(
            `${apiEndpoints.competitionTasks(editionId, subeditionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) setTasks(data);
            else setError("Nie udało się pobrać zadań.");
            setLoadingTasks(false);
        });
        return () => {
            active = false;
        };
    }, [editionId, subeditionId, navigate]);
    const openForm = (task?: ApiCompetitionTask) => {
        setEditing(task ?? null);
        setForm(
            task ? { name: task.name, tier: task.tier, contentJson: task.contentJson } : emptyForm
        );
        setRawMode(false);
        setError("");
        setModal(true);
    };
    let body: string | null = null;
    let answerType = "unspecified";
    const options = readOptions(form.contentJson);
    try {
        const content: unknown = JSON.parse(form.contentJson);
        if (
            content &&
            typeof content === "object" &&
            !Array.isArray(content) &&
            "body_pl" in content &&
            typeof content.body_pl === "string"
        )
            body = content.body_pl;
        if (
            content &&
            typeof content === "object" &&
            !Array.isArray(content) &&
            "answer_type" in content &&
            typeof content.answer_type === "string"
        )
            answerType = content.answer_type;
    } catch {
        /* The advanced editor retains invalid input until it is corrected. */
    }
    const updateBody = (value: string) =>
        setForm((current) => {
            try {
                const content = JSON.parse(current.contentJson) as Record<string, unknown>;
                return {
                    ...current,
                    contentJson: JSON.stringify({ ...content, body_pl: value }, null, 2),
                };
            } catch {
                return current;
            }
        });
    const updateAnswerType = (value: string) =>
        setForm((current) => {
            try {
                const content = JSON.parse(current.contentJson) as Record<string, unknown>;
                return {
                    ...current,
                    contentJson: JSON.stringify(
                        {
                            ...content,
                            answer_type: value,
                            ...(["single_choice", "multiple_choice"].includes(value) &&
                            !Array.isArray(content.options)
                                ? {
                                      options: [
                                          { id: "A", text: "" },
                                          { id: "B", text: "" },
                                      ],
                                  }
                                : {}),
                        },
                        null,
                        2
                    ),
                };
            } catch {
                return current;
            }
        });
    const updateOptions = (change: (previous: ChoiceOption[]) => ChoiceOption[]) =>
        setForm((current) => {
            try {
                const content = JSON.parse(current.contentJson) as Record<string, unknown>;
                return {
                    ...current,
                    contentJson: JSON.stringify(
                        { ...content, options: change(readOptions(current.contentJson)) },
                        null,
                        2
                    ),
                };
            } catch {
                return current;
            }
        });
    const save = async (event: FormEvent) => {
        event.preventDefault();
        try {
            const content: unknown = JSON.parse(form.contentJson);
            if (!content || typeof content !== "object" || Array.isArray(content))
                throw new Error();
        } catch {
            setError("Treść musi być poprawnym obiektem JSON.");
            return;
        }
        if (!form.name.trim()) {
            setError("Podaj nazwę zadania.");
            return;
        }
        if (
            ["single_choice", "multiple_choice"].includes(answerType) &&
            (options.length < 2 ||
                options.some((option) => !option.id.trim() || !option.text.trim()) ||
                new Set(options.map((option) => option.id.toUpperCase())).size !== options.length)
        ) {
            setError(
                "Dodaj co najmniej dwa warianty i uzupełnij ich treść. Oznaczenia wariantów muszą być różne."
            );
            return;
        }
        setBusy(true);
        setError("");
        if (editing) {
            const originalOptions = readOptions(editing.contentJson);
            let originalType = "";
            try {
                const original = JSON.parse(editing.contentJson) as { answer_type?: unknown };
                if (typeof original.answer_type === "string") originalType = original.answer_type;
            } catch {
                /* An existing unknown format may be corrected. */
            }
            const retainedIds = new Set(
                (["single_choice", "multiple_choice"].includes(answerType) ? options : []).map(
                    (option) => option.id.toLowerCase()
                )
            );
            const removed = originalOptions.filter(
                (option) => !retainedIds.has(option.id.toLowerCase())
            );
            if (removed.length > 0 || originalType !== answerType) {
                const result = await apiListRequestResult<ApiTaskAnswer>(
                    `${apiEndpoints.taskAnswers(editing.id)}?${firstPage}`,
                    null,
                    "GET",
                    navigate
                );
                if (!result.data) {
                    setError("Nie udało się sprawdzić poprawnych odpowiedzi. Spróbuj ponownie.");
                    setBusy(false);
                    return;
                }
                if (originalType !== answerType && result.data.length > 0) {
                    setError(
                        "Przed zmianą typu usuń poprawne odpowiedzi zadania i ustaw je ponownie dla nowego typu."
                    );
                    setBusy(false);
                    return;
                }
                if (
                    result.data.some((answer) => {
                        const values =
                            originalType === "multiple_choice"
                                ? decodeMultipleChoice(answer.answer)
                                : [answer.answer.trim()];
                        return (
                            values === null ||
                            values.some((value) =>
                                removed.some(
                                    (option) => option.id.toLowerCase() === value.toLowerCase()
                                )
                            )
                        );
                    })
                ) {
                    setError(
                        "Nie można usunąć wariantu oznaczonego jako poprawny. Najpierw zmień poprawne odpowiedzi zadania."
                    );
                    setBusy(false);
                    return;
                }
            }
        }
        const url = apiEndpoints.competitionTasks(editionId, subeditionId);
        const result = await apiRequestResult<ApiCompetitionTask>(
            editing ? `${url}/${editing.id}` : url,
            {
                name: form.name.trim(),
                tier: form.tier,
                contentJson: form.contentJson,
                ...(editing ? { version: editing.version } : {}),
            },
            editing ? "PUT" : "POST",
            navigate
        );
        if (result.data) {
            setTasks((previous) =>
                editing
                    ? previous.map((t) => (t.id === editing.id ? result.data! : t))
                    : [...previous, result.data!]
            );
            setModal(false);
            setFeedback(editing ? "Zapisano zadanie." : "Dodano zadanie.");
        } else
            setError(
                result.status === 409
                    ? "Zadanie zostało zmienione, jest zamknięte lub ma już odpowiedzi. Odśwież dane."
                    : "Nie udało się zapisać zadania. Sprawdź dane i spróbuj ponownie."
            );
        setBusy(false);
    };
    const remove = async (task: ApiCompetitionTask) => {
        if (
            !window.confirm(
                `Czy na pewno usunąć zadanie „${task.name}” wraz z poprawnymi odpowiedziami?`
            )
        )
            return;
        setBusy(true);
        setError("");
        const result = await apiRequestResult(
            `${apiEndpoints.competitionTasks(editionId, subeditionId)}/${task.id}`,
            null,
            "DELETE",
            navigate
        );
        if (result.status && result.status >= 200 && result.status < 300) {
            setTasks((previous) => previous.filter((t) => t.id !== task.id));
            setFeedback("Usunięto zadanie.");
        } else
            setError(
                "Nie udało się usunąć zadania. Zadanie z odpowiedziami nie może zostać usunięte."
            );
        setBusy(false);
    };
    const locked = selectedEdition?.state === "CLOSED" || selectedSubedition?.state === "CLOSED";
    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
                <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                    Konkurs
                </p>
                <h1 className="mt-2 text-3xl font-bold text-ink-heading">Zadania konkursowe</h1>
                <p className="mt-3 text-ink-secondary">
                    Zarządzaj zadaniami w podedycjach konkursu.
                </p>
                {feedback && (
                    <p role="status" className="app-status-success mt-6 rounded-xl border p-3">
                        {feedback}
                    </p>
                )}
                {error && !modal && (
                    <p role="alert" className="app-status-danger mt-6 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                {loading ? (
                    <p className="mt-8 text-ink-secondary">Ładowanie edycji…</p>
                ) : editions.length === 0 ? (
                    <p className="mt-8 text-ink-secondary">Brak edycji konkursu.</p>
                ) : (
                    <>
                        <div className="mt-8 grid gap-4 rounded-2xl border border-line bg-surface p-5 sm:grid-cols-2">
                            <div>
                                <span
                                    id="task-edition"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Edycja konkursu
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="task-edition"
                                    value={editionId}
                                    options={editions.map((e) => ({ value: e.id, label: e.name }))}
                                    onChange={(id) => {
                                        setEditionId(id);
                                        setError("");
                                        setFeedback("");
                                    }}
                                />
                            </div>
                            <div>
                                <span
                                    id="task-subedition"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Podedycja
                                </span>
                                {subeditions.length ? (
                                    <SelectListbox
                                        ariaLabelledBy="task-subedition"
                                        value={subeditionId}
                                        options={subeditions.map((s) => ({
                                            value: s.id,
                                            label: s.name,
                                        }))}
                                        onChange={(id) => {
                                            setSubeditionId(id);
                                            setError("");
                                            setFeedback("");
                                        }}
                                    />
                                ) : (
                                    <p className="py-2 text-sm text-ink-secondary">
                                        Brak podedycji, do których można dodawać zadania.
                                    </p>
                                )}
                            </div>
                        </div>
                        {subeditionId && (
                            <>
                                <div className="mt-8 flex flex-wrap items-center justify-between gap-4">
                                    <h2 className="text-xl font-semibold text-ink-heading">
                                        Zadania: {selectedSubedition?.name}
                                    </h2>
                                    <Button
                                        text="Dodaj zadanie"
                                        icon={<FaPlus />}
                                        disabled={busy || locked}
                                        onClick={() => openForm()}
                                    />
                                </div>
                                {locked && (
                                    <p className="mt-2 text-sm text-ink-secondary">
                                        Zakończona edycja lub podedycja nie przyjmuje nowych zadań.
                                    </p>
                                )}
                            </>
                        )}
                        {loadingTasks ? (
                            <p className="mt-5 text-ink-secondary">Ładowanie zadań…</p>
                        ) : (
                            subeditionId &&
                            (tasks.length === 0 ? (
                                <p className="mt-5 text-ink-secondary">
                                    Brak zadań w tej podedycji.
                                </p>
                            ) : (
                                <div className="mt-5 space-y-3">
                                    {tasks.map((task) => (
                                        <article
                                            key={task.id}
                                            className="rounded-2xl border border-line bg-surface p-5"
                                        >
                                            <div className="flex flex-wrap items-center justify-between gap-4">
                                                <div className="min-w-0">
                                                    <h3 className="break-words text-lg font-semibold text-ink-heading">
                                                        {task.name}
                                                    </h3>
                                                    <p className="mt-1 text-sm text-ink-secondary">
                                                        {task.tier === "TIER_1"
                                                            ? "Kategoria I"
                                                            : "Kategoria II"}
                                                    </p>
                                                </div>
                                                <div className="flex flex-wrap gap-2">
                                                    <Button
                                                        text="Poprawne odpowiedzi"
                                                        onClick={() => setAnswersTask(task)}
                                                        disabled={busy}
                                                    />
                                                    <Button
                                                        text="Edytuj"
                                                        icon={<FaEdit />}
                                                        disabled={busy || locked}
                                                        onClick={() => openForm(task)}
                                                    />
                                                    <Button
                                                        text="Usuń"
                                                        icon={<FaTrash />}
                                                        disabled={busy || locked}
                                                        className="bg-danger hover:bg-danger-hover"
                                                        onClick={() => void remove(task)}
                                                    />
                                                </div>
                                            </div>
                                        </article>
                                    ))}
                                </div>
                            ))
                        )}
                    </>
                )}
            </main>
            {answersTask && (
                <TaskAnswersModal
                    key={answersTask.id}
                    task={answersTask}
                    onClose={() => setAnswersTask(null)}
                />
            )}
            <Modal
                isOpen={modal}
                onClose={() => {
                    if (!busy) {
                        setModal(false);
                        setError("");
                    }
                }}
                title={editing ? "Edytuj zadanie" : "Dodaj zadanie"}
                maxWidth="lg"
            >
                <form onSubmit={(event) => void save(event)} className="space-y-4">
                    {error && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3">
                            {error}
                        </p>
                    )}
                    <label className="block text-sm text-ink">
                        Nazwa zadania
                        <input
                            className={inputClass}
                            value={form.name}
                            onChange={(e) => setForm({ ...form, name: e.target.value })}
                            required
                        />
                    </label>
                    <div>
                        <span id="task-tier" className="mb-2 block text-sm text-ink">
                            Kategoria
                        </span>
                        <SelectListbox
                            ariaLabelledBy="task-tier"
                            value={form.tier}
                            options={[
                                { value: "TIER_1", label: "Kategoria I" },
                                { value: "TIER_2", label: "Kategoria II" },
                            ]}
                            onChange={(tier) => setForm({ ...form, tier })}
                        />
                    </div>
                    {body !== null && (
                        <div>
                            <span id="answer-type" className="mb-2 block text-sm text-ink">
                                Typ odpowiedzi
                            </span>
                            <SelectListbox
                                ariaLabelledBy="answer-type"
                                value={answerType}
                                options={[
                                    ...(![
                                        "open",
                                        "single_choice",
                                        "multiple_choice",
                                        "true_false",
                                    ].includes(answerType)
                                        ? [
                                              {
                                                  value: answerType,
                                                  label:
                                                      answerType === "unspecified"
                                                          ? "Nieokreślony"
                                                          : `Nieznany: ${answerType}`,
                                              },
                                          ]
                                        : []),
                                    { value: "open", label: "Odpowiedź otwarta" },
                                    { value: "single_choice", label: "Jednokrotny wybór" },
                                    { value: "multiple_choice", label: "Wielokrotny wybór" },
                                    { value: "true_false", label: "Prawda / fałsz" },
                                ]}
                                onChange={updateAnswerType}
                            />
                            {["single_choice", "multiple_choice"].includes(answerType) && (
                                <div className="mt-4 space-y-3">
                                    <p className="text-sm text-ink-secondary">
                                        Wpisz warianty odpowiedzi osobno. Treść zadania powinna
                                        zawierać samo pytanie.
                                    </p>
                                    {options.map((option) => (
                                        <div key={option.id} className="flex items-end gap-3">
                                            <label className="min-w-0 flex-1 text-sm text-ink">
                                                Wariant {option.id}
                                                <input
                                                    className={inputClass}
                                                    value={option.text}
                                                    onChange={(event) => {
                                                        const text = event.target.value;
                                                        updateOptions((previous) =>
                                                            previous.map((entry) =>
                                                                entry.id === option.id
                                                                    ? { ...entry, text }
                                                                    : entry
                                                            )
                                                        );
                                                    }}
                                                    required
                                                />
                                            </label>
                                            <Button
                                                text={
                                                    <span className="sr-only">
                                                        Usuń wariant {option.id}
                                                    </span>
                                                }
                                                icon={<FaTrash />}
                                                className="bg-danger px-3 hover:bg-danger-hover"
                                                disabled={busy}
                                                onClick={() =>
                                                    updateOptions((previous) =>
                                                        previous.filter(
                                                            (entry) => entry.id !== option.id
                                                        )
                                                    )
                                                }
                                            />
                                        </div>
                                    ))}
                                    <Button
                                        text="Dodaj wariant"
                                        icon={<FaPlus />}
                                        disabled={busy}
                                        onClick={() =>
                                            updateOptions((previous) => {
                                                const used = [
                                                    ...previous,
                                                    ...(editing
                                                        ? readOptions(editing.contentJson)
                                                        : []),
                                                ];
                                                const next =
                                                    Math.max(
                                                        0,
                                                        ...used.map((option) =>
                                                            optionNumber(option.id.toUpperCase())
                                                        )
                                                    ) + 1;
                                                return [
                                                    ...previous,
                                                    { id: optionId(next), text: "" },
                                                ];
                                            })
                                        }
                                    />
                                </div>
                            )}
                        </div>
                    )}
                    <div>
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <span className="text-sm font-medium text-ink">Treść zadania</span>
                            <button
                                type="button"
                                className="text-sm font-medium text-brand-text underline"
                                onClick={() => setRawMode(!rawMode)}
                            >
                                {rawMode ? "Wróć do edytora" : "Dane zaawansowane"}
                            </button>
                        </div>
                        {rawMode || body === null ? (
                            <label className="block text-sm text-ink">
                                Treść zadania (JSON)
                                <textarea
                                    className={`${inputClass} min-h-48 font-mono`}
                                    value={form.contentJson}
                                    onChange={(e) =>
                                        setForm({ ...form, contentJson: e.target.value })
                                    }
                                    spellCheck={false}
                                    required
                                />
                            </label>
                        ) : (
                            <div className="mt-3">
                                <MarkdownEditor
                                    key={editing?.id ?? "new"}
                                    initialValue={body}
                                    disabled={busy}
                                    onChange={updateBody}
                                />
                            </div>
                        )}
                        {body === null && !rawMode && (
                            <p className="mt-2 text-sm text-ink-secondary">
                                Nieznany format treści. Możesz sprawdzić dane przed ich edycją.
                            </p>
                        )}
                    </div>
                    <div className="flex justify-end gap-2">
                        <Button
                            text="Anuluj"
                            disabled={busy}
                            onClick={() => setModal(false)}
                            className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                        />
                        <Button
                            type="submit"
                            text={busy ? "Zapisywanie…" : "Zapisz"}
                            disabled={busy}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
}
