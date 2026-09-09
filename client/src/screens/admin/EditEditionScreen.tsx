import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { Edition } from "../../types/models";
import { apiRequest, apiRequestResult } from "../../utils/apiFetcher";

const EDITIONS_ENDPOINT = "/api/admin/edition";
const TITLE_ENDPOINT = `${EDITIONS_ENDPOINT}/title`;
const START_DATE_ENDPOINT = `${EDITIONS_ENDPOINT}/start-date`;
const END_DATE_ENDPOINT = `${EDITIONS_ENDPOINT}/end-date`;

interface EditionForm {
    title: string;
    startDate: string;
    endDate: string;
}

type Feedback = { tone: "success" | "error"; message: string } | null;

const emptyForm: EditionForm = { title: "", startDate: "", endDate: "" };
const inputClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:cursor-not-allowed disabled:bg-slate-100";

const sortEditions = (editions: Edition[]) =>
    [...editions].sort(
        (first, second) =>
            new Date(second.startDate).getTime() - new Date(first.startDate).getTime()
    );

const toLocalDateTime = (value: string) => {
    if (!value) return "";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
};

const sameInstant = (first: string, second: string) =>
    new Date(first).getTime() === new Date(second).getTime();

const EditEditionScreen = () => {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<Edition[]>([]);
    const [loading, setLoading] = useState(true);
    const [referenceTime, setReferenceTime] = useState(0);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingEdition, setEditingEdition] = useState<Edition | null>(null);
    const [form, setForm] = useState<EditionForm>(emptyForm);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [modalError, setModalError] = useState("");
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    useEffect(() => {
        let active = true;
        void apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate).then((data) => {
            if (!active) return;
            if (data) setEditions(sortEditions(data));
            else setFeedback({ tone: "error", message: "Nie udało się pobrać edycji." });
            setReferenceTime(Date.now());
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const resetModal = () => {
        setIsModalOpen(false);
        setEditingEdition(null);
        setForm(emptyForm);
        setModalError("");
    };

    const closeModal = () => {
        if (saving) return;
        resetModal();
    };

    const openCreate = () => {
        setEditingEdition(null);
        setForm(emptyForm);
        setModalError("");
        setFeedback(null);
        setIsModalOpen(true);
    };

    const openEdit = (edition: Edition) => {
        setEditingEdition(edition);
        setForm({
            title: edition.title,
            startDate: toLocalDateTime(edition.startDate),
            endDate: toLocalDateTime(edition.endDate),
        });
        setModalError("");
        setFeedback(null);
        setIsModalOpen(true);
    };

    const refreshAfterPartialSave = async (editionId: string) => {
        const data = await apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate);
        if (!data) return;
        setEditions(sortEditions(data));
        const refreshedEdition = data.find((edition) => edition.id === editionId);
        if (refreshedEdition) setEditingEdition(refreshedEdition);
    };

    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const title = form.title.trim();
        const startTimestamp = new Date(form.startDate).getTime();
        const endTimestamp = new Date(form.endDate).getTime();

        setModalError("");
        if (!title || title.length > 200) {
            setModalError("Tytuł jest wymagany i może mieć maksymalnie 200 znaków.");
            return;
        }
        if (
            !form.startDate ||
            !form.endDate ||
            Number.isNaN(startTimestamp) ||
            Number.isNaN(endTimestamp)
        ) {
            setModalError("Podaj prawidłową datę rozpoczęcia i zakończenia.");
            return;
        }
        if (startTimestamp >= endTimestamp) {
            setModalError("Data zakończenia musi być późniejsza niż data rozpoczęcia.");
            return;
        }

        const startDate = new Date(startTimestamp).toISOString();
        const endDate = new Date(endTimestamp).toISOString();
        setSaving(true);

        if (!editingEdition) {
            const { data: created } = await apiRequestResult<Edition>(
                EDITIONS_ENDPOINT,
                { title, startDate, endDate },
                "POST",
                navigate
            );
            if (!created) {
                setModalError("Nie udało się utworzyć edycji. Formularz nie został wyczyszczony.");
                setSaving(false);
                return;
            }
            setEditions((current) => sortEditions([...current, created]));
            setReferenceTime(Date.now());
            setFeedback({ tone: "success", message: `Utworzono edycję „${created.title}”.` });
            setSaving(false);
            resetModal();
            return;
        }

        const titleChanged = title !== editingEdition.title;
        const startChanged = !sameInstant(startDate, editingEdition.startDate);
        const endChanged = !sameInstant(endDate, editingEdition.endDate);
        if (!titleChanged && !startChanged && !endChanged) {
            setModalError("Nie wprowadzono żadnych zmian.");
            setSaving(false);
            return;
        }

        let latest = editingEdition;
        let failed = false;
        if (titleChanged) {
            const { data } = await apiRequestResult<Edition>(
                TITLE_ENDPOINT,
                { id: editingEdition.id, title },
                "PUT",
                navigate
            );
            if (data) latest = data;
            else failed = true;
        }
        if (startChanged) {
            const { data } = await apiRequestResult<Edition>(
                START_DATE_ENDPOINT,
                { id: editingEdition.id, startDate },
                "PUT",
                navigate
            );
            if (data) latest = data;
            else failed = true;
        }
        if (endChanged) {
            const { data } = await apiRequestResult<Edition>(
                END_DATE_ENDPOINT,
                { id: editingEdition.id, endDate },
                "PUT",
                navigate
            );
            if (data) latest = data;
            else failed = true;
        }

        if (failed) {
            await refreshAfterPartialSave(editingEdition.id);
            setModalError(
                "Nie udało się zapisać wszystkich zmian. Dane serwera zostały odświeżone; możesz ponowić zapis."
            );
            setSaving(false);
            return;
        }

        setEditions((current) =>
            sortEditions(current.map((edition) => (edition.id === latest.id ? latest : edition)))
        );
        setReferenceTime(Date.now());
        setFeedback({ tone: "success", message: `Zapisano edycję „${latest.title}”.` });
        setSaving(false);
        resetModal();
    };

    const remove = async (edition: Edition) => {
        if (
            !window.confirm(
                `Czy na pewno chcesz usunąć edycję „${edition.title}”? Powiązane wpisy lub dane mogą uniemożliwić tę operację.`
            )
        )
            return;

        setDeletingId(edition.id);
        setFeedback(null);
        const { data } = await apiRequestResult(
            `${EDITIONS_ENDPOINT}/${edition.id}`,
            null,
            "DELETE",
            navigate
        );
        if (data === null) {
            setFeedback({
                tone: "error",
                message: "Nie udało się usunąć edycji. Może być powiązana z innymi danymi.",
            });
        } else {
            setEditions((current) => current.filter((item) => item.id !== edition.id));
            setFeedback({ tone: "success", message: `Usunięto edycję „${edition.title}”.` });
        }
        setDeletingId(null);
    };

    const statusFor = (edition: Edition) => {
        const start = new Date(edition.startDate).getTime();
        const end = new Date(edition.endDate).getTime();
        if (referenceTime < start)
            return { label: "Nadchodząca", className: "bg-sky-50 text-sky-700" };
        if (referenceTime <= end)
            return { label: "Aktywna", className: "bg-emerald-50 text-emerald-700" };
        return { label: "Zakończona", className: "bg-slate-100 text-slate-600" };
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">Edycje konkursu</h1>
                        <p className="mt-2 max-w-2xl text-sm text-slate-600">
                            Zarządzaj okresami publikacji aktualności i materiałów konkursowych.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Dodaj edycję</span>
                            </>
                        }
                        onClick={openCreate}
                    />
                </header>

                {feedback && (
                    <p
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-700"}`}
                    >
                        {feedback.message}
                    </p>
                )}

                <section className="mt-8 space-y-4" aria-label="Lista edycji konkursu">
                    {loading ? (
                        <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center text-sm text-slate-500 shadow-sm">
                            Ładowanie edycji…
                        </div>
                    ) : editions.length === 0 ? (
                        <p className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">
                            Brak utworzonych edycji.
                        </p>
                    ) : (
                        editions.map((edition) => {
                            const status = statusFor(edition);
                            return (
                                <article
                                    key={edition.id}
                                    className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6"
                                >
                                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                        <div className="min-w-0">
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h2 className="break-words text-lg font-semibold">
                                                    {edition.title}
                                                </h2>
                                                <span
                                                    className={`rounded-full px-2.5 py-1 text-xs font-semibold ${status.className}`}
                                                >
                                                    {status.label}
                                                </span>
                                            </div>
                                            <p className="mt-2 text-sm text-slate-500">
                                                <time dateTime={edition.startDate}>
                                                    {new Date(edition.startDate).toLocaleString(
                                                        "pl-PL"
                                                    )}
                                                </time>
                                                {" — "}
                                                <time dateTime={edition.endDate}>
                                                    {new Date(edition.endDate).toLocaleString(
                                                        "pl-PL"
                                                    )}
                                                </time>
                                            </p>
                                        </div>
                                        <div className="flex shrink-0 gap-2">
                                            <Button
                                                text={
                                                    <>
                                                        <FaEdit />
                                                        <span className="sr-only">
                                                            Edytuj {edition.title}
                                                        </span>
                                                    </>
                                                }
                                                onClick={() => openEdit(edition)}
                                                className="px-3"
                                            />
                                            <Button
                                                text={
                                                    <>
                                                        <FaTrash />
                                                        <span className="sr-only">
                                                            Usuń {edition.title}
                                                        </span>
                                                    </>
                                                }
                                                disabled={deletingId === edition.id}
                                                onClick={() => void remove(edition)}
                                                className="bg-red-600 px-3 hover:bg-red-700 focus:ring-red-500"
                                            />
                                        </div>
                                    </div>
                                </article>
                            );
                        })
                    )}
                </section>
            </main>

            <Modal
                isOpen={isModalOpen}
                onClose={closeModal}
                title={editingEdition ? "Edytuj edycję" : "Dodaj nową edycję"}
                maxWidth="md"
            >
                <form onSubmit={save} className="space-y-4">
                    {modalError && (
                        <p
                            role="alert"
                            className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                        >
                            {modalError}
                        </p>
                    )}
                    <label className="block text-sm font-medium text-slate-700">
                        Tytuł edycji
                        <input
                            value={form.title}
                            maxLength={200}
                            disabled={saving}
                            onChange={(event) =>
                                setForm((current) => ({ ...current, title: event.target.value }))
                            }
                            className={inputClass}
                            placeholder="np. Edycja V"
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-slate-700">
                        Data rozpoczęcia
                        <input
                            type="datetime-local"
                            value={form.startDate}
                            disabled={saving}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    startDate: event.target.value,
                                }))
                            }
                            className={inputClass}
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-slate-700">
                        Data zakończenia
                        <input
                            type="datetime-local"
                            value={form.endDate}
                            min={form.startDate || undefined}
                            disabled={saving}
                            onChange={(event) =>
                                setForm((current) => ({ ...current, endDate: event.target.value }))
                            }
                            className={inputClass}
                            required
                        />
                    </label>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={saving}
                            onClick={closeModal}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={saving ? "Zapisywanie…" : "Zapisz"}
                            disabled={saving}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
};

export default EditEditionScreen;
