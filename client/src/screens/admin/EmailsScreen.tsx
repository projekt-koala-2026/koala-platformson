import { useState, type FormEvent } from "react";
import { FaPaperPlane } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import SelectListbox from "../../components/SelectListbox";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const groups = [
    {
        value: "captains",
        label: "Kapitanowie",
        description: "Wszystkie zweryfikowane konta kapitanów, również bez przypisanej drużyny.",
    },
    {
        value: "guardians",
        label: "Opiekunowie",
        description: "Wszystkie zweryfikowane konta opiekunów, również bez przypisanej drużyny.",
    },
    {
        value: "schools",
        label: "Szkoły",
        description: "Wszystkie adresy e-mail szkół zapisane w bazie.",
    },
] as const;
type Group = (typeof groups)[number]["value"];
const fieldClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";

export default function EmailsScreen() {
    const navigate = useNavigate();
    const [group, setGroup] = useState<Group>("captains");
    const [subject, setSubject] = useState("");
    const [body, setBody] = useState("");
    const [sending, setSending] = useState(false);
    const [feedback, setFeedback] = useState<{ error: boolean; message: string } | null>(null);

    const send = async (event: FormEvent) => {
        event.preventDefault();
        if (sending || !subject.trim() || !body.trim()) return;
        setSending(true);
        setFeedback(null);
        const result = await apiRequestResult(
            apiEndpoints.bulkEmails(group),
            {
                messageSubject: subject.trim(),
                messageBody: body.trim(),
            },
            "POST",
            navigate
        );
        if (result.status !== null && result.status >= 200 && result.status < 300) {
            setFeedback({
                error: false,
                message: `Zlecono wysyłkę do grupy „${groups.find((item) => item.value === group)!.label}”. Wiadomości zostaną wysłane w tle; przyjęcie zlecenia nie oznacza potwierdzenia dostarczenia.`,
            });
            setSubject("");
            setBody("");
        } else
            setFeedback({
                error: true,
                message:
                    result.error?.message ??
                    "Nie udało się zlecić wysyłki. Treść formularza została zachowana.",
            });
        setSending(false);
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Komunikacja
                    </p>
                    <h1 className="mt-1 text-3xl font-bold">Wiadomości zbiorcze</h1>
                    <p className="mt-3 text-ink-secondary">
                        Wyślij wiadomość e-mail do wybranej grupy odbiorców konkursu.
                    </p>
                </header>
                <section className="max-w-4xl rounded-2xl border border-line bg-surface p-5 sm:p-6">
                    <form onSubmit={(event) => void send(event)} className="space-y-5">
                        {feedback && (
                            <p
                                role={feedback.error ? "alert" : "status"}
                                className={`rounded-xl border p-4 ${feedback.error ? "app-status-danger" : "app-status-success"}`}
                            >
                                {feedback.message}
                            </p>
                        )}
                        <fieldset disabled={sending} className="space-y-5">
                            <div>
                                <p id="email-group-label" className="mb-2 text-sm font-semibold">
                                    Grupa odbiorców
                                </p>
                                <SelectListbox
                                    value={group}
                                    options={groups.map(({ value, label }) => ({ value, label }))}
                                    onChange={(value) => {
                                        if (groups.some((item) => item.value === value)) {
                                            setGroup(value as Group);
                                            setFeedback(null);
                                        }
                                    }}
                                    ariaLabelledBy="email-group-label"
                                />
                                <p className="mt-2 text-sm text-ink-secondary">
                                    {groups.find((item) => item.value === group)!.description}{" "}
                                    Wysyłka obejmuje całą grupę, bez ograniczenia do wybranej
                                    edycji.
                                </p>
                            </div>
                            <label className="block text-sm font-semibold">
                                Temat wiadomości *
                                <input
                                    required
                                    value={subject}
                                    onChange={(event) => setSubject(event.target.value)}
                                    className={fieldClass}
                                />
                            </label>
                            <label className="block text-sm font-semibold">
                                Wiadomość *
                                <textarea
                                    required
                                    rows={12}
                                    value={body}
                                    onChange={(event) => setBody(event.target.value)}
                                    className={`${fieldClass} resize-y font-normal`}
                                />
                            </label>
                            <p className="text-sm text-ink-secondary">
                                Wpisz zwykły tekst. Wiadomość zostanie umieszczona w szablonie
                                e-mail konkursu KOALA.
                            </p>
                            <Button
                                text={sending ? "Zlecanie wysyłki…" : "Wyślij wiadomość"}
                                icon={<FaPaperPlane />}
                                type="submit"
                                disabled={sending || !subject.trim() || !body.trim()}
                            />
                        </fieldset>
                    </form>
                </section>
            </main>
        </div>
    );
}
