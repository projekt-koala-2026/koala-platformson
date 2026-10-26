import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { FaEdit, FaExternalLinkAlt, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { ApiSponsor, Sponsor } from "../../types/models";
import { adaptSponsor } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

interface SponsorForm {
    name: string;
    websiteUrl: string;
    logoUrl: string;
    description: string;
    isVisible: boolean;
}
const emptyForm: SponsorForm = {
    name: "",
    websiteUrl: "",
    logoUrl: "",
    description: "",
    isVisible: true,
};
const inputClass =
    "mt-1.5 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading outline-none transition placeholder:text-ink-subtle focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";
const isHttpUrl = (value: string) => {
    try {
        const url = new URL(value);
        return url.protocol === "http:" || url.protocol === "https:";
    } catch {
        return false;
    }
};

const EditSponsorInfo = () => {
    const navigate = useNavigate();
    const [sponsors, setSponsors] = useState<Sponsor[]>([]);
    const [versions, setVersions] = useState<Record<string, number>>({});
    const [loading, setLoading] = useState(true);
    const [modal, setModal] = useState<"create" | Sponsor | null>(null);
    const [form, setForm] = useState<SponsorForm>(emptyForm);
    const [saving, setSaving] = useState(false);
    const [pendingId, setPendingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<{ tone: "success" | "error"; message: string } | null>(
        null
    );

    useEffect(() => {
        let active = true;
        void apiRequestResult<ApiSponsor[]>(
            `${apiEndpoints.sponsors}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            if (result.data) {
                setSponsors(result.data.map(adaptSponsor));
                setVersions(Object.fromEntries(result.data.map((item) => [item.id, item.version])));
            } else
                setFeedback({
                    tone: "error",
                    message: "Nie udało się pobrać listy sponsorów.",
                });
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const openCreate = () => {
        setForm(emptyForm);
        setModal("create");
        setFeedback(null);
    };
    const openEdit = (sponsor: Sponsor) => {
        setForm({
            name: sponsor.name,
            websiteUrl: sponsor.websiteUrl,
            logoUrl: sponsor.logoUrl ?? "",
            description: sponsor.description ?? "",
            isVisible: sponsor.isVisible,
        });
        setModal(sponsor);
        setFeedback(null);
    };
    const closeModal = () => {
        if (!saving) {
            setModal(null);
            setForm(emptyForm);
        }
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFeedback(null);
        const normalized: SponsorForm = {
            name: form.name.trim(),
            websiteUrl: form.websiteUrl.trim(),
            logoUrl: form.logoUrl.trim(),
            description: form.description.trim(),
            isVisible: form.isVisible,
        };
        if (!normalized.name) {
            setFeedback({ tone: "error", message: "Nazwa sponsora jest wymagana." });
            return;
        }
        if (normalized.name.length > 100) {
            setFeedback({
                tone: "error",
                message: "Nazwa sponsora może mieć maksymalnie 100 znaków.",
            });
            return;
        }
        if (!isHttpUrl(normalized.websiteUrl)) {
            setFeedback({
                tone: "error",
                message: "Adres strony musi być poprawnym URL-em HTTP lub HTTPS.",
            });
            return;
        }
        if (!isHttpUrl(normalized.logoUrl)) {
            setFeedback({
                tone: "error",
                message: "Adres logo musi być poprawnym URL-em HTTP lub HTTPS.",
            });
            return;
        }
        const payload = {
            name: normalized.name,
            contentJson: JSON.stringify({
                websiteUrl: normalized.websiteUrl,
                logoUrl: normalized.logoUrl,
                description: normalized.description,
            }),
            isVisible: normalized.isVisible,
            version: modal !== "create" && modal ? (versions[modal.id] ?? 0) : 0,
        };
        setSaving(true);
        if (modal === "create") {
            const { data: created } = await apiRequestResult<ApiSponsor>(
                apiEndpoints.sponsors,
                payload,
                "POST",
                navigate
            );
            if (created) {
                const adapted = adaptSponsor(created);
                setSponsors((items) => [...items, adapted]);
                setVersions((items) => ({ ...items, [created.id]: created.version }));
                setModal(null);
                setForm(emptyForm);
                setFeedback({ tone: "success", message: `Dodano sponsora „${adapted.name}”.` });
            } else setFeedback({ tone: "error", message: "Nie udało się dodać sponsora." });
        } else if (modal) {
            const { data: updated } = await apiRequestResult<ApiSponsor>(
                `${apiEndpoints.sponsors}/${modal.id}`,
                payload,
                "PUT",
                navigate
            );
            if (updated) {
                const adapted = adaptSponsor(updated);
                setSponsors((items) =>
                    items.map((item) => (item.id === modal.id ? adapted : item))
                );
                setVersions((items) => ({ ...items, [updated.id]: updated.version }));
                setModal(null);
                setForm(emptyForm);
                setFeedback({ tone: "success", message: "Dane sponsora zostały zapisane." });
            } else
                setFeedback({ tone: "error", message: "Nie udało się zapisać danych sponsora." });
        }
        setSaving(false);
    };

    const remove = async (sponsor: Sponsor) => {
        if (!window.confirm(`Usunąć sponsora „${sponsor.name}”?`)) return;
        setPendingId(sponsor.id);
        setFeedback(null);
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.sponsors}/${sponsor.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status === 200 || status === 204) {
            setSponsors((items) => items.filter((item) => item.id !== sponsor.id));
            setFeedback({ tone: "success", message: "Sponsor został usunięty." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć sponsora." });
        setPendingId(null);
    };

    return (
        <>
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl space-y-7 px-4 py-8 sm:px-6 lg:px-8">
                <header className="flex flex-col gap-5 border-b border-line-subtle pb-6 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Administracja
                        </p>
                        <h1 className="mt-2 text-3xl font-bold text-ink-heading">Sponsorzy</h1>
                        <p className="mt-3 max-w-2xl text-ink-secondary">
                            Zarządzaj logotypami i odnośnikami wyświetlanymi w stopce strony.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Dodaj sponsora</span>
                            </>
                        }
                        onClick={openCreate}
                    />
                </header>
                {feedback && (
                    <div
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "app-status-success" : "app-status-danger"}`}
                    >
                        {feedback.message}
                    </div>
                )}
                {loading ? (
                    <div className="rounded-2xl border border-line bg-surface p-12 text-center text-sm text-ink-muted">
                        Ładowanie sponsorów…
                    </div>
                ) : sponsors.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-line bg-surface p-12 text-center">
                        <h2 className="font-semibold text-ink-strong">Brak sponsorów</h2>
                        <p className="mt-2 text-sm text-ink-muted">
                            Dodaj pierwszy wpis, aby pojawił się w stopce strony.
                        </p>
                    </div>
                ) : (
                    <section className="grid gap-5 md:grid-cols-2">
                        {sponsors.map((sponsor) => (
                            <article
                                key={sponsor.id}
                                className="flex min-h-52 flex-col rounded-2xl border border-line bg-surface p-5 shadow-sm"
                            >
                                <div className="flex items-start gap-4">
                                    <div className="relative grid h-20 w-28 shrink-0 place-items-center overflow-hidden rounded-xl bg-surface-muted text-xl font-bold text-ink-subtle">
                                        <span>{sponsor.name.charAt(0).toUpperCase()}</span>
                                        {sponsor.logoUrl && (
                                            <img
                                                src={sponsor.logoUrl}
                                                alt={`Logo ${sponsor.name}`}
                                                className="absolute inset-0 h-full w-full bg-surface object-contain p-2"
                                                onError={(event) => {
                                                    event.currentTarget.hidden = true;
                                                }}
                                            />
                                        )}
                                    </div>
                                    <div className="min-w-0">
                                        <h2 className="text-lg font-semibold text-ink-heading">
                                            {sponsor.name}
                                        </h2>
                                        <span
                                            className={`mt-2 inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${sponsor.isVisible ? "bg-brand-soft text-brand-text" : "bg-surface-muted text-ink-secondary"}`}
                                        >
                                            {sponsor.isVisible ? "Widoczny" : "Ukryty"}
                                        </span>
                                        <a
                                            href={sponsor.websiteUrl}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="mt-1 inline-flex max-w-full items-center gap-1 truncate text-sm font-medium text-brand-text hover:underline"
                                        >
                                            <span className="truncate">{sponsor.websiteUrl}</span>
                                            <FaExternalLinkAlt className="shrink-0 text-xs" />
                                        </a>
                                    </div>
                                </div>
                                <p className="mt-4 flex-1 text-sm leading-6 text-ink-secondary">
                                    {sponsor.description || "Brak opisu."}
                                </p>
                                <div className="mt-5 flex gap-2 border-t border-line-subtle pt-4">
                                    <Button
                                        text={
                                            <>
                                                <FaEdit />
                                                <span className="ml-2">Edytuj</span>
                                            </>
                                        }
                                        onClick={() => openEdit(sponsor)}
                                        className="px-3"
                                    />
                                    <Button
                                        text={
                                            <>
                                                <FaTrash />
                                                <span className="ml-2">Usuń</span>
                                            </>
                                        }
                                        onClick={() => void remove(sponsor)}
                                        disabled={pendingId === sponsor.id}
                                        className="bg-danger px-3 hover:bg-danger-hover focus:ring-danger-emphasis"
                                    />
                                </div>
                            </article>
                        ))}
                    </section>
                )}
            </main>
            <Modal
                isOpen={modal !== null}
                onClose={closeModal}
                title={modal === "create" ? "Dodaj sponsora" : "Edytuj sponsora"}
                maxWidth="lg"
            >
                <form onSubmit={submit} className="space-y-4">
                    <label className="block text-sm font-medium text-ink">
                        Nazwa *
                        <input
                            value={form.name}
                            maxLength={100}
                            onChange={(event) =>
                                setForm((value) => ({ ...value, name: event.target.value }))
                            }
                            className={inputClass}
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-ink">
                        Adres strony *
                        <input
                            type="url"
                            value={form.websiteUrl}
                            onChange={(event) =>
                                setForm((value) => ({ ...value, websiteUrl: event.target.value }))
                            }
                            className={inputClass}
                            placeholder="https://…"
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-ink">
                        Adres logo *
                        <input
                            type="url"
                            value={form.logoUrl}
                            onChange={(event) =>
                                setForm((value) => ({ ...value, logoUrl: event.target.value }))
                            }
                            className={inputClass}
                            placeholder="https://…"
                            required
                        />
                    </label>
                    {form.logoUrl && isHttpUrl(form.logoUrl) && (
                        <div className="grid min-h-28 place-items-center rounded-xl border border-line bg-canvas p-4">
                            <img
                                src={form.logoUrl}
                                alt="Podgląd logo"
                                className="max-h-20 max-w-full object-contain"
                            />
                        </div>
                    )}
                    <label className="block text-sm font-medium text-ink">
                        Opis
                        <textarea
                            value={form.description}
                            onChange={(event) =>
                                setForm((value) => ({ ...value, description: event.target.value }))
                            }
                            className={`${inputClass} min-h-24 resize-y`}
                        />
                    </label>
                    <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-line bg-canvas p-4">
                        <input
                            type="checkbox"
                            checked={form.isVisible}
                            onChange={(event) =>
                                setForm((value) => ({
                                    ...value,
                                    isVisible: event.target.checked,
                                }))
                            }
                            className="mt-0.5 h-4 w-4 rounded border-line-strong text-brand-text focus:ring-brand-focus"
                        />
                        <span>
                            <span className="block text-sm font-medium text-ink">
                                Widoczny na stronie publicznej
                            </span>
                            <span className="mt-1 block text-xs leading-5 text-ink-muted">
                                Wyłącz, aby ukryć sponsora bez usuwania jego danych.
                            </span>
                        </span>
                    </label>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            onClick={closeModal}
                            disabled={saving}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:opacity-50"
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
        </>
    );
};

export default EditSponsorInfo;
