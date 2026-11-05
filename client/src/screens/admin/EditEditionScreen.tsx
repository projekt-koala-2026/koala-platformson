import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaFlagCheckered, FaPlay, FaPlus } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { ApiEdition, Edition } from "../../types/models";
import { adaptEdition } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

const EDITIONS_ENDPOINT = apiEndpoints.editions;

interface EditionForm {
    title: string;
    teamRegisterOpen: string;
    teamRegisterClose: string;
}

type Feedback = { tone: "success" | "error"; message: string } | null;

const emptyForm: EditionForm = { title: "", teamRegisterOpen: "", teamRegisterClose: "" };
const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted";

const sortEditions = (editions: Edition[]) =>
    [...editions].sort(
        (first, second) =>
            new Date(second.startDate).getTime() - new Date(first.startDate).getTime()
    );

const toDateTimeLocal = (value: string) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    const offset = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
};

const EditEditionScreen = () => {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<Edition[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingEdition, setEditingEdition] = useState<Edition | null>(null);
    const [form, setForm] = useState<EditionForm>(emptyForm);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [modalError, setModalError] = useState("");
    const [saving, setSaving] = useState(false);
    const [actionId, setActionId] = useState<string | null>(null);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiListRequestResult<ApiEdition>(
                `${EDITIONS_ENDPOINT}?${firstPage}&States=CLOSED`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiEdition>(
                `${EDITIONS_ENDPOINT}?${firstPage}&States=ACTIVE&States=INACTIVE`,
                null,
                "GET",
                navigate
            ),
        ]).then(([pastResult, activeResult]) => {
            if (!active) return;
            const data = [...(activeResult.data ?? []), ...(pastResult.data ?? [])].map(
                adaptEdition
            );
            setEditions(sortEditions(data));
            if (!pastResult.data || !activeResult.data)
                setFeedback({ tone: "error", message: "Nie udało się pobrać edycji." });
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
            teamRegisterOpen: toDateTimeLocal(edition.startDate),
            teamRegisterClose: toDateTimeLocal(edition.endDate),
        });
        setModalError("");
        setFeedback(null);
        setIsModalOpen(true);
    };

    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const title = form.title.trim();
        const registerOpen = new Date(form.teamRegisterOpen);
        const registerClose = new Date(form.teamRegisterClose);
        setModalError("");
        if (!title || title.length > 200) {
            setModalError("Tytuł jest wymagany i może mieć maksymalnie 200 znaków.");
            return;
        }
        if (
            Number.isNaN(registerOpen.getTime()) ||
            Number.isNaN(registerClose.getTime()) ||
            registerClose <= registerOpen
        ) {
            setModalError(
                "Podaj prawidłowy okres rejestracji; data zakończenia musi być późniejsza od daty otwarcia."
            );
            return;
        }
        const datesPayload = {
            teamRegisterOpen: registerOpen.toISOString(),
            teamRegisterClose: registerClose.toISOString(),
        };
        setSaving(true);

        if (!editingEdition) {
            const { data: created } = await apiRequestResult<ApiEdition>(
                EDITIONS_ENDPOINT,
                { name: title, ...datesPayload },
                "POST",
                navigate
            );
            if (!created) {
                setModalError("Nie udało się utworzyć edycji. Formularz nie został wyczyszczony.");
                setSaving(false);
                return;
            }
            const adapted = adaptEdition(created);
            setEditions((current) => sortEditions([...current, adapted]));
            setFeedback({ tone: "success", message: `Utworzono edycję „${adapted.title}”.` });
            setSaving(false);
            resetModal();
            return;
        }

        if (
            title === editingEdition.title &&
            registerOpen.getTime() === new Date(editingEdition.startDate).getTime() &&
            registerClose.getTime() === new Date(editingEdition.endDate).getTime()
        ) {
            setModalError("Nie wprowadzono żadnych zmian.");
            setSaving(false);
            return;
        }

        const { data } = await apiRequestResult<ApiEdition>(
            `${EDITIONS_ENDPOINT}/${editingEdition.id}`,
            {
                name: title,
                teamRegisterOpen: datesPayload.teamRegisterOpen,
                teamRegisterClose: datesPayload.teamRegisterClose,
                version: editingEdition.version,
            },
            "PUT",
            navigate
        );
        if (!data) {
            setModalError("Nie udało się zmienić nazwy edycji.");
            setSaving(false);
            return;
        }
        const latest = adaptEdition(data);
        setEditions((current) =>
            sortEditions(current.map((edition) => (edition.id === latest.id ? latest : edition)))
        );
        setFeedback({ tone: "success", message: `Zapisano edycję „${latest.title}”.` });
        setSaving(false);
        resetModal();
    };

    const endEdition = async (edition: Edition) => {
        if (
            !window.confirm(
                `Czy na pewno chcesz zakończyć edycję „${edition.title}”? Tej operacji nie można cofnąć z poziomu panelu.`
            )
        )
            return;

        setActionId(edition.id);
        setFeedback(null);
        const { data } = await apiRequestResult<ApiEdition>(
            `${EDITIONS_ENDPOINT}/${edition.id}/close`,
            null,
            "PUT",
            navigate
        );
        if (data === null) {
            setFeedback({
                tone: "error",
                message: "Nie udało się zakończyć edycji.",
            });
        } else {
            const ended = adaptEdition(data);
            setEditions((current) =>
                sortEditions(current.map((item) => (item.id === edition.id ? ended : item)))
            );
            setFeedback({ tone: "success", message: `Zakończono edycję „${edition.title}”.` });
        }
        setActionId(null);
    };

    const activateEdition = async (edition: Edition) => {
        setActionId(edition.id);
        setFeedback(null);
        const { data } = await apiRequestResult<ApiEdition>(
            `${EDITIONS_ENDPOINT}/${edition.id}/activate`,
            null,
            "PUT",
            navigate
        );
        if (!data) {
            setFeedback({ tone: "error", message: "Nie udało się aktywować edycji." });
        } else {
            const activated = adaptEdition(data);
            setEditions((current) =>
                sortEditions(current.map((item) => (item.id === edition.id ? activated : item)))
            );
            setFeedback({ tone: "success", message: `Aktywowano edycję „${edition.title}”.` });
        }
        setActionId(null);
    };

    const statusFor = (edition: Edition) => {
        if (edition.state === "INACTIVE")
            return { label: "Nieaktywna", className: "bg-warning-muted text-warning-text" };
        if (edition.state === "ACTIVE")
            return { label: "Aktywna", className: "bg-brand-soft text-brand-text" };
        return { label: "Zakończona", className: "bg-surface-muted text-ink-secondary" };
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">Edycje konkursu</h1>
                        <p className="mt-2 max-w-2xl text-sm text-ink-secondary">
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
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${feedback.tone === "success" ? "app-status-success" : "app-status-danger"}`}
                    >
                        {feedback.message}
                    </p>
                )}

                <section className="mt-8 space-y-4" aria-label="Lista edycji konkursu">
                    {loading ? (
                        <div className="rounded-2xl border border-line bg-surface p-10 text-center text-sm text-ink-muted shadow-sm">
                            Ładowanie edycji…
                        </div>
                    ) : editions.length === 0 ? (
                        <p className="rounded-2xl border border-dashed border-line-strong bg-surface p-10 text-center text-sm text-ink-muted">
                            Brak utworzonych edycji.
                        </p>
                    ) : (
                        editions.map((edition) => {
                            const status = statusFor(edition);
                            return (
                                <article
                                    key={edition.id}
                                    className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6"
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
                                            <p className="mt-2 text-sm text-ink-muted">
                                                <time dateTime={edition.startDate}>
                                                    {new Date(edition.startDate).toLocaleString(
                                                        "pl-PL"
                                                    )}
                                                </time>
                                                {status.label === "Zakończona" && (
                                                    <>
                                                        {" — "}
                                                        <time dateTime={edition.endDate}>
                                                            {new Date(
                                                                edition.endDate
                                                            ).toLocaleString("pl-PL")}
                                                        </time>
                                                    </>
                                                )}
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
                                            {edition.state === "INACTIVE" && (
                                                <Button
                                                    text={
                                                        <>
                                                            <FaPlay />
                                                            <span className="sr-only">
                                                                Aktywuj {edition.title}
                                                            </span>
                                                        </>
                                                    }
                                                    disabled={actionId === edition.id}
                                                    onClick={() => void activateEdition(edition)}
                                                    className="px-3"
                                                />
                                            )}
                                            {status.label === "Aktywna" && (
                                                <Button
                                                    text={
                                                        <>
                                                            <FaFlagCheckered />
                                                            <span className="sr-only">
                                                                Zakończ {edition.title}
                                                            </span>
                                                        </>
                                                    }
                                                    disabled={actionId === edition.id}
                                                    onClick={() => void endEdition(edition)}
                                                    className="bg-warning px-3 hover:bg-warning-hover focus:ring-warning"
                                                />
                                            )}
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
                        <p role="alert" className="rounded-xl border p-3 text-sm app-status-danger">
                            {modalError}
                        </p>
                    )}
                    <label className="block text-sm font-medium text-ink">
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
                    <label className="block text-sm font-medium text-ink">
                        Otwarcie rejestracji drużyn
                        <input
                            type="datetime-local"
                            value={form.teamRegisterOpen}
                            disabled={saving}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    teamRegisterOpen: event.target.value,
                                }))
                            }
                            className={inputClass}
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-ink">
                        Zamknięcie rejestracji drużyn
                        <input
                            type="datetime-local"
                            value={form.teamRegisterClose}
                            disabled={saving}
                            onChange={(event) =>
                                setForm((current) => ({
                                    ...current,
                                    teamRegisterClose: event.target.value,
                                }))
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
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:cursor-not-allowed disabled:opacity-50"
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
