import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaFlagCheckered, FaPlay, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import AutoCheckNotice from "../../components/AutoCheckNotice";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import SelectListbox from "../../components/SelectListbox";
import type { ApiEdition, ApiSubedition } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading focus:border-brand-focus focus:outline-none focus:ring-2 focus:ring-brand-muted";
const statusLabels = { INACTIVE: "Nieaktywna", ACTIVE: "Aktywna", CLOSED: "Zakończona" };
const localDate = (value: string) => {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
        ? ""
        : new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};

export default function SubeditionsScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [items, setItems] = useState<ApiSubedition[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingItems, setLoadingItems] = useState(true);
    const [feedback, setFeedback] = useState("");
    const [error, setError] = useState("");
    const [editing, setEditing] = useState<ApiSubedition | null>(null);
    const [modal, setModal] = useState(false);
    const [busy, setBusy] = useState(false);
    const [now, setNow] = useState(() => Date.now());
    const [form, setForm] = useState({ name: "", answerOpen: "", answerClose: "" });
    const edition = editions.find((item) => item.id === editionId);

    useEffect(() => {
        const timer = window.setInterval(() => setNow(Date.now()), 30000);
        return () => window.clearInterval(timer);
    }, []);

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
                setEditionId(data.find((e) => e.state === "ACTIVE")?.id ?? data[0]?.id ?? "");
            } else setError("Nie udało się pobrać edycji.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    useEffect(() => {
        if (!editionId) return;
        let active = true;
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) setItems(data);
            else setError("Nie udało się pobrać podedycji.");
            setLoadingItems(false);
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate]);

    const openForm = (item?: ApiSubedition) => {
        setEditing(item ?? null);
        setForm(
            item
                ? {
                      name: item.name,
                      answerOpen: localDate(item.answerOpen),
                      answerClose: localDate(item.answerClose),
                  }
                : { name: "", answerOpen: "", answerClose: "" }
        );
        setError("");
        setModal(true);
    };
    const save = async (event: FormEvent) => {
        event.preventDefault();
        const start = new Date(form.answerOpen);
        const end = new Date(form.answerClose);
        if (
            !form.name.trim() ||
            Number.isNaN(start.getTime()) ||
            Number.isNaN(end.getTime()) ||
            end <= start
        ) {
            setError("Podaj nazwę i poprawny okres odpowiadania.");
            return;
        }
        setBusy(true);
        setError("");
        const body = {
            name: form.name.trim(),
            answerOpen: start.toISOString(),
            answerClose: end.toISOString(),
            ...(editing ? { version: editing.version } : {}),
        };
        const result = await apiRequestResult<ApiSubedition>(
            editing
                ? apiEndpoints.subedition(editionId, editing.id)
                : apiEndpoints.subeditions(editionId),
            body,
            editing ? "PUT" : "POST",
            navigate
        );
        if (result.data) {
            setItems((previous) =>
                editing
                    ? previous.map((item) => (item.id === editing.id ? result.data! : item))
                    : [...previous, result.data!]
            );
            setModal(false);
            setFeedback(editing ? "Zapisano zmiany." : "Dodano podedycję.");
        } else
            setError(
                result.status === 409
                    ? "Dane zostały zmienione. Odśwież stronę i spróbuj ponownie."
                    : "Nie udało się zapisać podedycji. Sprawdź nazwę i terminy."
            );
        setBusy(false);
    };
    const action = async (item: ApiSubedition, operation: "activate" | "close" | "delete") => {
        const label =
            operation === "delete"
                ? "usunąć podedycję wraz z jej zadaniami"
                : operation === "close"
                  ? "zakończyć podedycję"
                  : "aktywować podedycję";
        if (!window.confirm(`Czy na pewno chcesz ${label} „${item.name}”?`)) return;
        setBusy(true);
        setError("");
        setFeedback("");
        const result = await apiRequestResult<ApiSubedition>(
            operation === "delete"
                ? apiEndpoints.subedition(editionId, item.id)
                : apiEndpoints.subeditionAction(editionId, item.id, operation),
            null,
            operation === "delete" ? "DELETE" : "PUT",
            navigate
        );
        if (result.status && result.status >= 200 && result.status < 300) {
            setItems((previous) =>
                operation === "delete"
                    ? previous.filter((entry) => entry.id !== item.id)
                    : previous.map((entry) => (entry.id === item.id ? result.data! : entry))
            );
            setFeedback("Zaktualizowano podedycję.");
        } else if (
            operation === "close" &&
            result.error?.message.includes("before the answer deadline")
        )
            setError("Nie można zakończyć podedycji przed upływem terminu odpowiedzi.");
        else if (operation === "close" && result.error?.message.includes("two matching reviews"))
            setError(
                "Przed zakończeniem wszystkie nadesłane odpowiedzi muszą mieć dwie zgodne oceny recenzentów."
            );
        else setError("Nie udało się wykonać operacji. Odśwież dane i spróbuj ponownie.");
        setBusy(false);
    };

    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
                <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                    Konkurs
                </p>
                <h1 className="mt-2 text-3xl font-bold text-ink-heading">Podedycje</h1>
                <p className="mt-3 text-ink-secondary">
                    Zarządzaj etapami konkursu i terminami odpowiadania.
                </p>
                <p className="mt-2 text-sm text-ink-secondary">
                    Zakończenie podedycji wymaga upływu terminu i dwóch zgodnych ocen każdej
                    nadesłanej odpowiedzi.
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
                        <div className="mt-8 rounded-2xl border border-line bg-surface p-5">
                            <span
                                id="subedition-edition"
                                className="mb-2 block text-sm font-medium text-ink"
                            >
                                Edycja konkursu
                            </span>
                            <SelectListbox
                                ariaLabelledBy="subedition-edition"
                                value={editionId}
                                onChange={(id) => {
                                    if (id !== editionId) {
                                        setLoadingItems(true);
                                        setItems([]);
                                    }
                                    setEditionId(id);
                                    setError("");
                                    setFeedback("");
                                }}
                                options={editions.map((e) => ({ value: e.id, label: e.name }))}
                            />
                        </div>
                        {!loadingItems &&
                            items
                                .filter(
                                    (item) =>
                                        item.state === "INACTIVE" &&
                                        Date.parse(item.answerOpen) <= now
                                )
                                .map((item) => (
                                    <div
                                        key={`activation-warning-${item.id}`}
                                        role="status"
                                        className="app-status-warning mt-6 rounded-2xl border p-5"
                                    >
                                        <p className="font-semibold">
                                            Podedycja „{item.name}” nie została aktywowana.
                                        </p>
                                        <p className="mt-2 text-sm">
                                            {Date.parse(item.answerClose) < now
                                                ? "Termin nadsyłania odpowiedzi już minął. Sprawdź i w razie potrzeby zmień terminy przed aktywacją."
                                                : "Termin nadsyłania odpowiedzi już się rozpoczął, ale drużyny nie mają dostępu do zadań. Aktywuj podedycję, aby mogły odpowiadać."}
                                            {edition?.state !== "ACTIVE" &&
                                                " Dostęp do zadań wymaga również aktywnej edycji."}
                                        </p>
                                    </div>
                                ))}
                        {!loadingItems &&
                            items.some(
                                (item) =>
                                    item.state !== "CLOSED" && Date.parse(item.answerClose) < now
                            ) && (
                                <div className="mt-6 space-y-3">
                                    {items
                                        .filter(
                                            (item) =>
                                                item.state !== "CLOSED" &&
                                                Date.parse(item.answerClose) < now
                                        )
                                        .map((item) => (
                                            <AutoCheckNotice
                                                key={item.id}
                                                id={item.id}
                                                name={item.name}
                                            />
                                        ))}
                                </div>
                            )}
                        <div className="mt-8 flex flex-wrap items-center justify-between gap-4">
                            <h2 className="text-xl font-semibold text-ink-heading">
                                Podedycje wybranej edycji
                            </h2>
                            <Button
                                text="Dodaj podedycję"
                                icon={<FaPlus />}
                                disabled={busy || edition?.state === "CLOSED"}
                                onClick={() => openForm()}
                            />
                        </div>
                        {edition?.state === "CLOSED" && (
                            <p className="mt-2 text-sm text-ink-secondary">
                                Nie można dodawać podedycji do zakończonej edycji.
                            </p>
                        )}
                        {loadingItems ? (
                            <p className="mt-5 text-ink-secondary">Ładowanie podedycji…</p>
                        ) : items.length === 0 ? (
                            <p className="mt-5 text-ink-secondary">Brak podedycji w tej edycji.</p>
                        ) : (
                            <div className="mt-5 space-y-3">
                                {items.map((item) => (
                                    <article
                                        key={item.id}
                                        className="rounded-2xl border border-line bg-surface p-5"
                                    >
                                        <div className="flex flex-wrap items-start justify-between gap-4">
                                            <div className="min-w-0">
                                                <h3 className="break-words text-lg font-semibold text-ink-heading">
                                                    {item.name}
                                                </h3>
                                                <p className="mt-1 text-sm text-ink-secondary">
                                                    {statusLabels[item.state] ?? item.state} ·{" "}
                                                    {new Date(item.answerOpen).toLocaleString(
                                                        "pl-PL"
                                                    )}{" "}
                                                    –{" "}
                                                    {new Date(item.answerClose).toLocaleString(
                                                        "pl-PL"
                                                    )}
                                                </p>
                                            </div>
                                            <div className="flex flex-wrap gap-2">
                                                {item.state !== "CLOSED" &&
                                                    edition?.state !== "CLOSED" && (
                                                        <Button
                                                            text="Zadania"
                                                            onClick={() =>
                                                                navigate(
                                                                    `/admin/competition-tasks?edition=${editionId}&subedition=${item.id}`
                                                                )
                                                            }
                                                        />
                                                    )}
                                                <Button
                                                    text="Edytuj"
                                                    icon={<FaEdit />}
                                                    disabled={busy || item.state === "CLOSED"}
                                                    onClick={() => openForm(item)}
                                                />
                                                {item.state === "INACTIVE" && (
                                                    <>
                                                        <Button
                                                            text="Aktywuj"
                                                            icon={<FaPlay />}
                                                            disabled={busy}
                                                            onClick={() =>
                                                                void action(item, "activate")
                                                            }
                                                        />
                                                        <Button
                                                            text="Usuń"
                                                            icon={<FaTrash />}
                                                            disabled={busy}
                                                            className="bg-danger hover:bg-danger-hover"
                                                            onClick={() =>
                                                                void action(item, "delete")
                                                            }
                                                        />
                                                    </>
                                                )}
                                                {item.state === "ACTIVE" && (
                                                    <Button
                                                        text="Zakończ"
                                                        icon={<FaFlagCheckered />}
                                                        disabled={
                                                            busy ||
                                                            Date.parse(item.answerClose) >= now
                                                        }
                                                        onClick={() => void action(item, "close")}
                                                    />
                                                )}
                                            </div>
                                        </div>
                                    </article>
                                ))}
                            </div>
                        )}
                    </>
                )}
            </main>
            <Modal
                isOpen={modal}
                onClose={() => {
                    if (!busy) {
                        setModal(false);
                        setError("");
                    }
                }}
                title={editing ? "Edytuj podedycję" : "Dodaj podedycję"}
                maxWidth="md"
            >
                <form onSubmit={(event) => void save(event)} className="space-y-4">
                    {error && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3">
                            {error}
                        </p>
                    )}
                    <label className="block text-sm text-ink">
                        Nazwa
                        <input
                            className={inputClass}
                            value={form.name}
                            onChange={(e) => setForm({ ...form, name: e.target.value })}
                            required
                        />
                    </label>
                    <label className="block text-sm text-ink">
                        Początek odpowiadania
                        <input
                            className={inputClass}
                            type="datetime-local"
                            value={form.answerOpen}
                            onChange={(e) => setForm({ ...form, answerOpen: e.target.value })}
                            required
                        />
                    </label>
                    <label className="block text-sm text-ink">
                        Koniec odpowiadania
                        <input
                            className={inputClass}
                            type="datetime-local"
                            value={form.answerClose}
                            onChange={(e) => setForm({ ...form, answerClose: e.target.value })}
                            required
                        />
                    </label>
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
