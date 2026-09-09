import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { FaEdit, FaImage, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import ImagePicker from "../../components/ImagePicker";
import Modal from "../../components/Modal";
import type { Koalicjant, ManagedFile } from "../../types/models";
import { apiRequest, apiRequestResult, resolveApiAssetUrl } from "../../utils/apiFetcher";

interface KoalicjantForm {
    name: string;
    profilePicture: string;
    description: string;
}
const emptyForm: KoalicjantForm = { name: "", profilePicture: "", description: "" };
const inputClass =
    "mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";

const EditKoalicjantInfo = () => {
    const navigate = useNavigate();
    const [koalicjants, setKoalicjants] = useState<Koalicjant[]>([]);
    const [loading, setLoading] = useState(true);
    const [modal, setModal] = useState<"create" | Koalicjant | null>(null);
    const [form, setForm] = useState<KoalicjantForm>(emptyForm);
    const [showImages, setShowImages] = useState(false);
    const [saving, setSaving] = useState(false);
    const [pendingId, setPendingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<{ tone: "success" | "error"; message: string } | null>(
        null
    );

    useEffect(() => {
        let active = true;
        void apiRequestResult<Koalicjant[]>("/api/admin/koalicjants", null, "GET", navigate).then(
            (result) => {
                if (!active) return;
                if (result.data) setKoalicjants(result.data);
                else if (result.status !== 404)
                    setFeedback({
                        tone: "error",
                        message: "Nie udało się pobrać listy koalicjantów.",
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
        setShowImages(false);
        setModal("create");
        setFeedback(null);
    };
    const openEdit = (person: Koalicjant) => {
        setForm({
            name: person.name,
            profilePicture: person.profilePicture,
            description: person.description ?? "",
        });
        setShowImages(false);
        setModal(person);
        setFeedback(null);
    };
    const closeModal = () => {
        if (!saving) {
            setModal(null);
            setForm(emptyForm);
            setShowImages(false);
        }
    };
    const chooseImage = (file: ManagedFile) => {
        setForm((value) => ({ ...value, profilePicture: file.filePath }));
        setShowImages(false);
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFeedback(null);
        const payload: KoalicjantForm = {
            name: form.name.trim(),
            profilePicture: form.profilePicture.trim(),
            description: form.description.trim(),
        };
        if (!payload.name) {
            setFeedback({ tone: "error", message: "Imię i nazwisko są wymagane." });
            return;
        }
        if (!payload.profilePicture) {
            setFeedback({ tone: "error", message: "Wybierz zdjęcie lub podaj jego adres." });
            return;
        }
        setSaving(true);
        if (modal === "create") {
            const created = await apiRequest<Koalicjant>(
                "/api/admin/koalicjants",
                payload,
                "POST",
                navigate
            );
            if (created) {
                setKoalicjants((items) => [...items, created]);
                setModal(null);
                setForm(emptyForm);
                setFeedback({ tone: "success", message: `Dodano osobę „${created.name}”.` });
            } else setFeedback({ tone: "error", message: "Nie udało się dodać koalicjanta." });
        } else if (modal) {
            const updated = await apiRequest<boolean>(
                `/api/admin/koalicjants/${modal.id}`,
                { id: modal.id, ...payload },
                "PUT",
                navigate
            );
            if (updated) {
                setKoalicjants((items) =>
                    items.map((item) => (item.id === modal.id ? { ...item, ...payload } : item))
                );
                setModal(null);
                setForm(emptyForm);
                setFeedback({ tone: "success", message: "Dane koalicjanta zostały zapisane." });
            } else
                setFeedback({
                    tone: "error",
                    message: "Nie udało się zapisać danych koalicjanta.",
                });
        }
        setSaving(false);
    };

    const remove = async (person: Koalicjant) => {
        if (!window.confirm(`Usunąć osobę „${person.name}” z listy koalicjantów?`)) return;
        setPendingId(person.id);
        setFeedback(null);
        const deleted = await apiRequest<boolean>(
            `/api/admin/koalicjants/${person.id}`,
            null,
            "DELETE",
            navigate
        );
        if (deleted) {
            setKoalicjants((items) => items.filter((item) => item.id !== person.id));
            setFeedback({ tone: "success", message: "Koalicjant został usunięty." });
        } else setFeedback({ tone: "error", message: "Nie udało się usunąć koalicjanta." });
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
                        <h1 className="mt-2 text-3xl font-bold text-slate-900">Koalicjanci</h1>
                        <p className="mt-3 max-w-2xl text-slate-600">
                            Zarządzaj osobami prezentowanymi na publicznej stronie KOALicjA.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Dodaj osobę</span>
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
                        Ładowanie koalicjantów…
                    </div>
                ) : koalicjants.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-200 bg-white p-12 text-center">
                        <h2 className="font-semibold text-slate-800">Brak koalicjantów</h2>
                        <p className="mt-2 text-sm text-slate-500">
                            Dodaj pierwszą osobę do publicznej prezentacji.
                        </p>
                    </div>
                ) : (
                    <section className="grid gap-5 md:grid-cols-2">
                        {koalicjants.map((person) => (
                            <article
                                key={person.id}
                                className="flex min-h-56 flex-col rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                            >
                                <div className="flex items-start gap-4">
                                    <div className="relative grid h-24 w-24 shrink-0 place-items-center overflow-hidden rounded-2xl bg-emerald-50 text-2xl font-bold text-emerald-700">
                                        <span>{person.name.charAt(0).toUpperCase()}</span>
                                        {person.profilePicture && (
                                            <img
                                                src={resolveApiAssetUrl(person.profilePicture)}
                                                alt={person.name}
                                                className="absolute inset-0 h-full w-full object-cover"
                                                onError={(event) => {
                                                    event.currentTarget.hidden = true;
                                                }}
                                            />
                                        )}
                                    </div>
                                    <div className="min-w-0">
                                        <h2 className="text-lg font-semibold text-slate-900">
                                            {person.name}
                                        </h2>
                                        <p className="mt-2 line-clamp-4 text-sm leading-6 text-slate-600">
                                            {person.description || "Brak opisu."}
                                        </p>
                                    </div>
                                </div>
                                <div className="mt-auto flex gap-2 border-t border-slate-100 pt-4">
                                    <Button
                                        text={
                                            <>
                                                <FaEdit />
                                                <span className="ml-2">Edytuj</span>
                                            </>
                                        }
                                        onClick={() => openEdit(person)}
                                        className="px-3"
                                    />
                                    <Button
                                        text={
                                            <>
                                                <FaTrash />
                                                <span className="ml-2">Usuń</span>
                                            </>
                                        }
                                        onClick={() => void remove(person)}
                                        disabled={pendingId === person.id}
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
                title={modal === "create" ? "Dodaj koalicjanta" : "Edytuj koalicjanta"}
                maxWidth="xl"
            >
                <form onSubmit={submit} className="space-y-4">
                    <div className="grid gap-5 md:grid-cols-[1fr_12rem]">
                        <div className="space-y-4">
                            <label className="block text-sm font-medium text-slate-700">
                                Imię i nazwisko *
                                <input
                                    value={form.name}
                                    onChange={(event) =>
                                        setForm((value) => ({ ...value, name: event.target.value }))
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="block text-sm font-medium text-slate-700">
                                Adres zdjęcia *
                                <input
                                    value={form.profilePicture}
                                    onChange={(event) =>
                                        setForm((value) => ({
                                            ...value,
                                            profilePicture: event.target.value,
                                        }))
                                    }
                                    className={inputClass}
                                    placeholder="https://… lub ścieżka z biblioteki"
                                    required
                                />
                            </label>
                            <Button
                                text={
                                    <>
                                        <FaImage />
                                        <span className="ml-2">
                                            {showImages
                                                ? "Ukryj bibliotekę"
                                                : "Wybierz z biblioteki"}
                                        </span>
                                    </>
                                }
                                onClick={() => setShowImages((visible) => !visible)}
                                className="bg-slate-700 hover:bg-slate-800 focus:ring-slate-500"
                            />
                            <label className="block text-sm font-medium text-slate-700">
                                Opis
                                <textarea
                                    value={form.description}
                                    onChange={(event) =>
                                        setForm((value) => ({
                                            ...value,
                                            description: event.target.value,
                                        }))
                                    }
                                    className={`${inputClass} min-h-28 resize-y`}
                                />
                            </label>
                        </div>
                        <div className="grid min-h-48 place-items-center overflow-hidden rounded-2xl border border-slate-200 bg-slate-50">
                            {form.profilePicture ? (
                                <img
                                    src={resolveApiAssetUrl(form.profilePicture)}
                                    alt="Podgląd zdjęcia"
                                    className="h-48 w-full object-cover"
                                />
                            ) : (
                                <span className="px-4 text-center text-sm text-slate-400">
                                    Podgląd zdjęcia
                                </span>
                            )}
                        </div>
                    </div>
                    {showImages && (
                        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                            <ImagePicker navigate={navigate} onSelect={chooseImage} />
                        </div>
                    )}
                    <div className="flex justify-end gap-3 border-t border-slate-100 pt-4">
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

export default EditKoalicjantInfo;
