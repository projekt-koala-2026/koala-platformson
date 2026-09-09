import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { FaEdit, FaExternalLinkAlt, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { Sponsor } from "../../types/models";
import { apiRequest, apiRequestResult } from "../../utils/apiFetcher";

interface SponsorForm {
    name: string;
    websiteUrl: string;
    logoUrl: string;
    description: string;
}
const emptyForm: SponsorForm = { name: "", websiteUrl: "", logoUrl: "", description: "" };
const inputClass =
    "mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";
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
        void apiRequestResult<Sponsor[]>("/api/admin/sponsors", null, "GET", navigate).then(
            (result) => {
                if (!active) return;
                if (result.data) setSponsors(result.data);
                else
                    setFeedback({
                        tone: "error",
                        message: "Nie udało się pobrać listy sponsorów.",
                    });
                setLoading(false);
            }
        );
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
        const payload: SponsorForm = {
            name: form.name.trim(),
            websiteUrl: form.websiteUrl.trim(),
            logoUrl: form.logoUrl.trim(),
            description: form.description.trim(),
        };
        if (!payload.name) {
            setFeedback({ tone: "error", message: "Nazwa sponsora jest wymagana." });
            return;
        }
        if (payload.name.length > 100) {
            setFeedback({
                tone: "error",
                message: "Nazwa sponsora może mieć maksymalnie 100 znaków.",
            });
            return;
        }
        if (!isHttpUrl(payload.websiteUrl)) {
            setFeedback({
                tone: "error",
                message: "Adres strony musi być poprawnym URL-em HTTP lub HTTPS.",
            });
            return;
        }
        if (!isHttpUrl(payload.logoUrl)) {
            setFeedback({
                tone: "error",
                message: "Adres logo musi być poprawnym URL-em HTTP lub HTTPS.",
            });
            return;
        }
        setSaving(true);
        if (modal === "create") {
            const created = await apiRequest<Sponsor>(
                "/api/admin/sponsors",
                payload,
                "POST",
                navigate
            );
            if (created) {
                setSponsors((items) => [...items, created]);
                setModal(null);
                setForm(emptyForm);
                setFeedback({ tone: "success", message: `Dodano sponsora „${created.name}”.` });
            } else setFeedback({ tone: "error", message: "Nie udało się dodać sponsora." });
        } else if (modal) {
            const updated = await apiRequest<boolean>(
                `/api/admin/sponsors/${modal.id}`,
                payload,
                "PUT",
                navigate
            );
            if (updated) {
                setSponsors((items) =>
                    items.map((item) => (item.id === modal.id ? { ...item, ...payload } : item))
                );
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
        const deleted = await apiRequest<boolean>(
            `/api/admin/sponsors/${sponsor.id}`,
            null,
            "DELETE",
            navigate
        );
        if (deleted) {
            setSponsors((items) => items.filter((item) => item.id !== sponsor.id));
            setFeedback({ tone: "success", message: "Sponsor został usunięty." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć sponsora." });
        setPendingId(null);
    };

    return (
        <>
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl space-y-7 px-4 py-8 sm:px-6 lg:px-8">
                <header className="flex flex-col gap-5 border-b border-slate-100 pb-6 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                            Administracja
                        </p>
                        <h1 className="mt-2 text-3xl font-bold text-slate-900">Sponsorzy</h1>
                        <p className="mt-3 max-w-2xl text-slate-600">
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
                        className={`rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-700"}`}
                    >
                        {feedback.message}
                    </div>
                )}
                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center text-sm text-slate-500">
                        Ładowanie sponsorów…
                    </div>
                ) : sponsors.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-200 bg-white p-12 text-center">
                        <h2 className="font-semibold text-slate-800">Brak sponsorów</h2>
                        <p className="mt-2 text-sm text-slate-500">
                            Dodaj pierwszy wpis, aby pojawił się w stopce strony.
                        </p>
                    </div>
                ) : (
                    <section className="grid gap-5 md:grid-cols-2">
                        {sponsors.map((sponsor) => (
                            <article
                                key={sponsor.id}
                                className="flex min-h-52 flex-col rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                            >
                                <div className="flex items-start gap-4">
                                    <div className="relative grid h-20 w-28 shrink-0 place-items-center overflow-hidden rounded-xl bg-slate-100 text-xl font-bold text-slate-400">
                                        <span>{sponsor.name.charAt(0).toUpperCase()}</span>
                                        {sponsor.logoUrl && (
                                            <img
                                                src={sponsor.logoUrl}
                                                alt={`Logo ${sponsor.name}`}
                                                className="absolute inset-0 h-full w-full bg-white object-contain p-2"
                                                onError={(event) => {
                                                    event.currentTarget.hidden = true;
                                                }}
                                            />
                                        )}
                                    </div>
                                    <div className="min-w-0">
                                        <h2 className="text-lg font-semibold text-slate-900">
                                            {sponsor.name}
                                        </h2>
                                        <a
                                            href={sponsor.websiteUrl}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="mt-1 inline-flex max-w-full items-center gap-1 truncate text-sm font-medium text-emerald-700 hover:underline"
                                        >
                                            <span className="truncate">{sponsor.websiteUrl}</span>
                                            <FaExternalLinkAlt className="shrink-0 text-xs" />
                                        </a>
                                    </div>
                                </div>
                                <p className="mt-4 flex-1 text-sm leading-6 text-slate-600">
                                    {sponsor.description || "Brak opisu."}
                                </p>
                                <div className="mt-5 flex gap-2 border-t border-slate-100 pt-4">
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
                                        className="bg-red-600 px-3 hover:bg-red-700 focus:ring-red-500"
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
                    <label className="block text-sm font-medium text-slate-700">
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
                    <label className="block text-sm font-medium text-slate-700">
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
                    <label className="block text-sm font-medium text-slate-700">
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
                        <div className="grid min-h-28 place-items-center rounded-xl border border-slate-200 bg-slate-50 p-4">
                            <img
                                src={form.logoUrl}
                                alt="Podgląd logo"
                                className="max-h-20 max-w-full object-contain"
                            />
                        </div>
                    )}
                    <label className="block text-sm font-medium text-slate-700">
                        Opis
                        <textarea
                            value={form.description}
                            onChange={(event) =>
                                setForm((value) => ({ ...value, description: event.target.value }))
                            }
                            className={`${inputClass} min-h-24 resize-y`}
                        />
                    </label>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            onClick={closeModal}
                            disabled={saving}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
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
