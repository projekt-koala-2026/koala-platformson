import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import type { ApiCompetitionTask, ApiTaskAnswer } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";
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
    let answerType = "";
    try {
        const content: unknown = JSON.parse(task?.contentJson ?? "{}");
        if (
            content &&
            typeof content === "object" &&
            "answer_type" in content &&
            typeof content.answer_type === "string"
        )
            answerType = content.answer_type;
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
            if (data) setAnswers(data);
            else setError("Nie udało się pobrać poprawnych odpowiedzi.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [task, navigate]);
    const save = async (event: FormEvent) => {
        event.preventDefault();
        if (!task || !value.trim()) {
            setError("Podaj poprawną odpowiedź.");
            return;
        }
        const answer =
            answerType === "single_choice"
                ? value.trim().toUpperCase()
                : answerType === "true_false"
                  ? value.trim().toLowerCase()
                  : value.trim();
        if (answerType === "single_choice" && !["A", "B", "C", "D"].includes(answer)) {
            setError("Dla zadania jednokrotnego wyboru podaj A, B, C albo D.");
            return;
        }
        if (answerType === "true_false" && !["true", "false"].includes(answer)) {
            setError("Dla zadania prawda / fałsz podaj true albo false.");
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
                Zadanie: <strong className="text-ink-heading">{task?.name}</strong>. Wystarczy jedna
                z podanych odpowiedzi.
            </p>
            <p className="mt-2 text-sm text-ink-secondary">
                Zmiana odpowiedzi wycofuje ranking i wymaga ponownego automatycznego sprawdzenia
                odpowiedzi bez ocen recenzentów.
            </p>
            {answerType === "single_choice" && (
                <p className="mt-2 text-sm text-ink-secondary">
                    Wpisz literę poprawnego wariantu: A, B, C lub D.
                </p>
            )}
            {answerType === "true_false" && (
                <p className="mt-2 text-sm text-ink-secondary">
                    Wpisz true dla prawdy albo false dla fałszu.
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
                                {answer.answer}
                            </span>
                            <div className="flex gap-2">
                                <Button
                                    text="Edytuj"
                                    icon={<FaEdit />}
                                    disabled={busy}
                                    onClick={() => {
                                        setEditing(answer);
                                        setValue(answer.answer);
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
                <label className="block text-sm font-medium text-ink">
                    {editing ? "Edytuj odpowiedź" : "Nowa poprawna odpowiedź"}
                    <input
                        className="mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading focus:border-brand-focus focus:outline-none focus:ring-2 focus:ring-brand-muted"
                        value={value}
                        onChange={(event) => setValue(event.target.value)}
                        disabled={busy || loading}
                        required
                    />
                </label>
                <div className="flex flex-wrap justify-end gap-2">
                    {editing && (
                        <Button
                            text="Anuluj edycję"
                            disabled={busy}
                            className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                            onClick={() => {
                                setEditing(null);
                                setValue("");
                            }}
                        />
                    )}
                    <Button
                        type="submit"
                        text={editing ? "Zapisz odpowiedź" : "Dodaj odpowiedź"}
                        icon={editing ? <FaEdit /> : <FaPlus />}
                        disabled={busy || loading}
                    />
                </div>
            </form>
        </Modal>
    );
}
