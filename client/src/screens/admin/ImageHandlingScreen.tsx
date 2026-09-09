import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import FileUploader from "../../components/FileUploader";
import ImagePicker, { type ImagePickerHandle } from "../../components/ImagePicker";
import type { ManagedFile } from "../../types/models";
import { apiRequestResult, uploadFile } from "../../utils/apiFetcher";

const FILES_ENDPOINT = "/api/admin/file/public/files";
const MAX_IMAGE_SIZE = 16 * 1024 * 1024;
const allowedImageTypes = new Set(["image/jpeg", "image/png", "image/webp", "image/gif"]);

type Feedback = { tone: "success" | "error"; message: string } | null;

const validateImage = (file: File) => {
    if (file.size === 0) return "Wybrany plik jest pusty.";
    if (file.size > MAX_IMAGE_SIZE) return "Obraz przekracza maksymalny rozmiar 16 MB.";
    if (!allowedImageTypes.has(file.type)) {
        return "Dozwolone formaty to JPG, PNG, WebP i GIF.";
    }
    return null;
};

const ImageHandlingScreen = () => {
    const navigate = useNavigate();
    const pickerRef = useRef<ImagePickerHandle>(null);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [uploading, setUploading] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    const addFile = async (file: File) => {
        setFeedback(null);
        setUploading(true);
        const uploaded = await uploadFile(file, file.name, "images", navigate);
        if (!uploaded) {
            setFeedback({ tone: "error", message: "Nie udało się przesłać obrazu." });
        } else {
            setFeedback({ tone: "success", message: `Dodano obraz ${uploaded.title}.` });
            pickerRef.current?.refresh();
        }
        setUploading(false);
    };

    const deleteFile = async (file: ManagedFile) => {
        if (!window.confirm(`Czy na pewno chcesz usunąć obraz „${file.title}”?`)) return;
        setFeedback(null);
        setDeletingId(file.id);
        const { data } = await apiRequestResult(
            FILES_ENDPOINT,
            { id: file.id },
            "DELETE",
            navigate
        );
        if (data === null) {
            setFeedback({ tone: "error", message: "Nie udało się usunąć obrazu." });
        } else {
            setFeedback({ tone: "success", message: `Usunięto obraz ${file.title}.` });
            pickerRef.current?.refresh();
        }
        setDeletingId(null);
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Biblioteka plików
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">Zarządzanie obrazami</h1>
                    <p className="mt-2 max-w-3xl text-sm text-slate-600">
                        Dodawaj obrazy używane we wpisach, historii, regulaminie i pozostałych
                        treściach.
                    </p>
                </header>

                {feedback && (
                    <p
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                                : "border-red-200 bg-red-50 text-red-700"
                        }`}
                    >
                        {feedback.message}
                    </p>
                )}

                <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                    <h2 className="text-xl font-semibold">Dodaj obraz</h2>
                    <p className="mb-5 mt-1 text-sm text-slate-500">
                        Plik zostanie zapisany w publicznej bibliotece obrazów.
                    </p>
                    <FileUploader
                        accept="image/jpeg,image/png,image/webp,image/gif"
                        label="Kliknij, aby wybrać obraz"
                        hint="JPG, PNG, WebP lub GIF · maksymalnie 16 MB"
                        disabled={uploading}
                        validateFile={validateImage}
                        onValidationError={(message) => setFeedback({ tone: "error", message })}
                        onFileSelect={addFile}
                    />
                </section>

                <section className="mt-8" aria-labelledby="images-heading">
                    <div className="mb-4">
                        <h2 id="images-heading" className="text-xl font-semibold">
                            Biblioteka obrazów
                        </h2>
                        <p className="mt-1 text-sm text-slate-500">
                            Użyj czerwonego przycisku na miniaturze, aby usunąć obraz.
                        </p>
                    </div>
                    <ImagePicker
                        ref={pickerRef}
                        navigate={navigate}
                        mode="manage"
                        busyId={deletingId}
                        onSelect={deleteFile}
                    />
                </section>
            </main>
        </div>
    );
};

export default ImageHandlingScreen;
