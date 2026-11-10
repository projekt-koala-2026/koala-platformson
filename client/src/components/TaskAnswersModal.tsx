import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import type { ApiCompetitionTask, ApiTaskAnswer } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";
import { decodeMultipleChoice, encodeMultipleChoice } from "../utils/competitionAnswers";
import Button from "./Button";
import Modal from "./Modal";

export default function TaskAnswersModal({
    task,
    onClose,
}: {
    task: ApiCompetitionTask | null;
    onClose: () => void;
}) {
    const navigate = useNavigate();
    const [answers, setAnswers] = useState<ApiTaskAnswer[]>([]);
    const [value, setValue] = useState("");
    const [editing, setEditing] = useState<ApiTaskAnswer | null>(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [answersLoaded, setAnswersLoaded] = useState(false);
    const [selectedOptions, setSelectedOptions] = useState<string[]>([]);
    let answerType = "";
    let choiceOptions: { id: string; text: string }[] = [];
    try {
        const content: unknown = JSON.parse(task?.contentJson ?? "{}");
        if (
            content &&
            typeof content === "object" &&
            "answer_type" in content &&
            typeof content.answer_type === "string"
        )
            answerType = content.answer_type;
        if (
            content &&
            typeof content === "object" &&
            "options" in content &&
            Array.isArray(content.options)
        ) {
            choiceOptions = content.options.filter(
                (option): option is { id: string; text: string } =>
                    !!option &&
                    typeof option === "object" &&
                    typeof option.id === "string" &&
                    typeof option.text === "string"
            );
        }
    } catch {
        /* Existing content can use an unknown format. */
    }
    useEffect(() => {
        if (!task) return;
        let active = true;
        void apiListRequestResult<ApiTaskAnswer>(
            `${apiEndpoints.taskAnswers(task.id)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) {
                setAnswers(data);
                setAnswersLoaded(true);
            } else setError("Nie udało się pobrać poprawnych odpowiedzi.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [task, navigate]);
    const save = async (event: FormEvent) => {
        event.preventDefault();
        if (!task || (answerType !== "multiple_choice" && !value.trim())) {
            setError("Podaj poprawną odpowiedź.");
            return;
        }
        const answer =
            answerType === "multiple_choice"
                ? encodeMultipleChoice(selectedOptions)
                : answerType === "single_choice"
                  ? value.trim()
                  : answerType === "true_false"
                    ? value.trim().toLowerCase()
                    : value.trim();
        if (
            answerType === "single_choice" &&
            !choiceOptions.some((option) => option.id === answer)
        ) {
            setError("Wybierz jeden z istniejących wariantów zadania.");
            return;
        }
        if (answerType === "true_false" && !["true", "false"].includes(answer)) {
            setError("Dla zadania prawda / fałsz podaj true albo false.");
            return;
        }
        if (
            answerType === "multiple_choice" &&
            (choiceOptions.length < 2 ||
                selectedOptions.some((id) => !choiceOptions.some((option) => option.id === id)))
        ) {
            setError("Wybierz wyłącznie istniejące warianty zadania.");
            return;
        }
        if (
            ["single_choice", "true_false", "multiple_choice"].includes(answerType) &&
            answers.some((item) => item.id !== editing?.id)
        ) {
            setError(
                "To zadanie może mieć tylko jeden klucz odpowiedzi. Usuń pozostałe lub edytuj istniejący."
            );
            return;
        }
        if (
            answers.some(
                (item) =>
                    item.id !== editing?.id &&
                    item.answer.trim().toLowerCase() === answer.toLowerCase()
            )
        ) {
            setError("Ta odpowiedź już znajduje się na liście.");
            return;
        }
        setBusy(true);
        setError("");
        const base = apiEndpoints.taskAnswers(task.id);
        const result = await apiRequestResult<ApiTaskAnswer>(
            editing ? `${base}/${editing.id}` : base,
            { answer, ...(editing ? { version: editing.version } : {}) },
            editing ? "PUT" : "POST",
            navigate
        );
        if (result.data) {
            setAnswers((current) =>
                editing
                    ? current.map((a) => (a.id === editing.id ? result.data! : a))
                    : [...current, result.data!]
            );
            setValue("");
            setSelectedOptions([]);
            setEditing(null);
        } else
            setError(
                result.status === 409
                    ? "Odpowiedź została zmieniona. Zamknij okno i odśwież dane."
                    : "Nie udało się zapisać odpowiedzi. Sprawdź, czy nie jest już na liście."
            );
        setBusy(false);
    };
    const remove = async (answer: ApiTaskAnswer) => {
        if (!task || !window.confirm(`Czy na pewno usunąć odpowiedź „${answer.answer}”?`)) return;
        setBusy(true);
        setError("");
        const result = await apiRequestResult(
            `${apiEndpoints.taskAnswers(task.id)}/${answer.id}`,
            null,
            "DELETE",
            navigate
        );
        if (result.status && result.status >= 200 && result.status < 300) {
            setAnswers((current) => current.filter((a) => a.id !== answer.id));
            if (editing?.id === answer.id) {
                setEditing(null);
                setValue("");
                setSelectedOptions([]);
            }
        } else setError("Nie udało się usunąć odpowiedzi.");
        setBusy(false);
    };
    return (
        <Modal
            isOpen={task !== null}
            onClose={() => {
                if (!busy) onClose();
            }}
            title="Poprawne odpowiedzi"
            maxWidth="lg"
        >
            <p className="text-sm text-ink-secondary">
                Zadanie: <strong className="text-ink-heading">{task?.name}</strong>.
                {answerType === "open" && " Wystarczy jedna z podanych odpowiedzi."}
            </p>
            <p className="mt-2 text-sm text-ink-secondary">
                Zmiana odpowiedzi wycofuje ranking i wymaga ponownego automatycznego sprawdzenia
                odpowiedzi bez ocen recenzentów.
            </p>
            {answerType === "single_choice" && (
                <p className="mt-2 text-sm text-ink-secondary">
                    {choiceOptions.length
                        ? "Wybierz jeden poprawny wariant."
                        : "Najpierw dodaj warianty w edytorze zadania."}
                </p>
            )}
            {answerType === "true_false" && (
                <p className="mt-2 text-sm text-ink-secondary">
                    Wybierz, czy poprawna odpowiedź to prawda, czy fałsz.
                </p>
            )}
            {answerType === "multiple_choice" && (
                <p className="mt-2 text-sm text-ink-secondary">
                    Zaznacz wszystkie poprawne warianty. Jeżeli żaden nie jest poprawny, zapisz bez
                    zaznaczania.
                </p>
            )}
            {["single_choice", "true_false", "multiple_choice"].includes(answerType) &&
                answers.length > 1 && (
                    <p role="alert" className="app-status-danger mt-4 rounded-xl border p-3">
                        Zadanie ma kilka zapisanych kluczy. Usuń nadmiarowe, aby pozostał jeden.
                    </p>
                )}
            {error && (
                <p role="alert" className="app-status-danger mt-4 rounded-xl border p-3">
                    {error}
                </p>
            )}
            {loading ? (
                <p className="mt-4 text-ink-secondary">Ładowanie odpowiedzi…</p>
            ) : answers.length === 0 ? (
                <p className="mt-4 text-ink-secondary">Brak poprawnych odpowiedzi.</p>
            ) : (
                <ul className="mt-4 space-y-2">
                    {answers.map((answer) => (
                        <li
                            key={answer.id}
                            className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-line p-3"
                        >
                            <span className="min-w-0 break-all text-ink-heading">
                                {answerType === "multiple_choice"
                                    ? (() => {
                                          const ids = decodeMultipleChoice(answer.answer);
                                          return ids === null
                                              ? "Nieprawidłowy klucz odpowiedzi"
                                              : ids.length === 0
                                                ? "Żaden wariant"
                                                : ids
                                                      .map((id) => {
                                                          const text = choiceOptions.find(
                                                              (option) => option.id === id
                                                          )?.text;
                                                          return text
                                                              ? `${id} — ${text}`
                                                              : `${id} (wariant niedostępny)`;
                                                      })
                                                      .join(", ");
                                      })()
                                    : answerType === "single_choice"
                                      ? `${answer.answer}${choiceOptions.find((option) => option.id.toLowerCase() === answer.answer.toLowerCase())?.text ? ` — ${choiceOptions.find((option) => option.id.toLowerCase() === answer.answer.toLowerCase())?.text}` : ""}`
                                      : answerType === "true_false" &&
                                          ["true", "false"].includes(answer.answer.toLowerCase())
                                        ? answer.answer.toLowerCase() === "true"
                                            ? "Prawda"
                                            : "Fałsz"
                                        : answer.answer}
                            </span>
                            <div className="flex gap-2">
                                <Button
                                    text="Edytuj"
                                    icon={<FaEdit />}
                                    disabled={busy}
                                    onClick={() => {
                                        if (answerType === "multiple_choice") {
                                            const decoded = decodeMultipleChoice(answer.answer);
                                            if (
                                                decoded === null ||
                                                decoded.some(
                                                    (id) =>
                                                        !choiceOptions.some(
                                                            (option) => option.id === id
                                                        )
                                                )
                                            ) {
                                                setError(
                                                    "Klucz odpowiedzi jest nieprawidłowy lub zawiera usunięte warianty. Usuń go i dodaj ponownie."
                                                );
                                                return;
                                            }
                                            setSelectedOptions(decoded);
                                        }
                                        setEditing(answer);
                                        setValue(
                                            answerType === "single_choice"
                                                ? (choiceOptions.find(
                                                      (option) =>
                                                          option.id.toLowerCase() ===
                                                          answer.answer.toLowerCase()
                                                  )?.id ?? "")
                                                : answerType === "true_false"
                                                  ? answer.answer.toLowerCase()
                                                  : answer.answer
                                        );
                                        setError("");
                                    }}
                                />
                                <Button
                                    text="Usuń"
                                    icon={<FaTrash />}
                                    className="bg-danger hover:bg-danger-hover"
                                    disabled={busy}
                                    onClick={() => void remove(answer)}
                                />
                            </div>
                        </li>
                    ))}
                </ul>
            )}
            <form onSubmit={(event) => void save(event)} className="mt-5 space-y-3">
                {["single_choice", "true_false", "multiple_choice"].includes(answerType) ? (
                    <fieldset
                        disabled={
                            busy || loading || !answersLoaded || (!editing && answers.length > 0)
                        }
                    >
                        <legend className="text-sm font-medium text-ink">
                            {editing ? "Edytuj odpowiedź" : "Nowa poprawna odpowiedź"}
                        </legend>
                        <div className="mt-2 space-y-2">
                            {(["single_choice", "multiple_choice"].includes(answerType)
                                ? choiceOptions
                                : [
                                      { id: "true", text: "Prawda" },
                                      { id: "false", text: "Fałsz" },
                                  ]
                            ).map((option) => (
                                <label
                                    key={option.id}
                                    className="flex items-start gap-3 rounded-xl border border-line px-4 py-3 text-sm text-ink"
                                >
                                    <input
                                        type={
                                            answerType === "multiple_choice" ? "checkbox" : "radio"
                                        }
                                        name="correct-answer"
                                        value={option.id}
                                        checked={
                                            answerType === "multiple_choice"
                                                ? selectedOptions.includes(option.id)
                                                : value === option.id
                                        }
                                        onChange={() =>
                                            answerType === "multiple_choice"
                                                ? setSelectedOptions((current) =>
                                                      current.includes(option.id)
                                                          ? current.filter((id) => id !== option.id)
                                                          : [...current, option.id]
                                                  )
                                                : setValue(option.id)
                                        }
                                        required={answerType !== "multiple_choice"}
                                    />
                                    <span>
                                        {["single_choice", "multiple_choice"].includes(
                                            answerType
                                        ) && (
                                            <strong>
                                                {option.id}
                                                {option.text ? ". " : ""}
                                            </strong>
                                        )}
                                        {option.text}
                                    </span>
                                </label>
                            ))}
                        </div>
                    </fieldset>
                ) : (
                    <label className="block text-sm font-medium text-ink">
                        {editing ? "Edytuj odpowiedź" : "Nowa poprawna odpowiedź"}
                        <input
                            className="mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading focus:border-brand-focus focus:outline-none focus:ring-2 focus:ring-brand-muted"
                            value={value}
                            onChange={(event) => setValue(event.target.value)}
                            disabled={busy || loading || !answersLoaded}
                            required
                        />
                    </label>
                )}
                <div className="flex flex-wrap justify-end gap-2">
                    {editing && (
                        <Button
                            text="Anuluj edycję"
                            disabled={busy}
                            className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                            onClick={() => {
                                setEditing(null);
                                setValue("");
                                setSelectedOptions([]);
                            }}
                        />
                    )}
                    <Button
                        type="submit"
                        text={editing ? "Zapisz odpowiedź" : "Dodaj odpowiedź"}
                        icon={editing ? <FaEdit /> : <FaPlus />}
                        disabled={
                            busy ||
                            loading ||
                            !answersLoaded ||
                            (["single_choice", "true_false", "multiple_choice"].includes(
                                answerType
                            ) &&
                                ((!editing && answers.length > 0) ||
                                    (editing !== null && answers.length > 1)))
                        }
                    />
                </div>
            </form>
        </Modal>
    );
}
