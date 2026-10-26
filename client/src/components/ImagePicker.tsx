import { forwardRef, useEffect, useImperativeHandle, useState } from "react";
import { FaCheck, FaTrash } from "react-icons/fa";
import type { NavigateFunction } from "react-router-dom";
import type { ApiPublicFile, ManagedFile } from "../types/models";
import { adaptPublicFile } from "../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiRequest, resolveApiAssetUrl } from "../utils/apiFetcher";

export interface ImagePickerHandle {
    refresh: () => void;
}

interface ImagePickerProps {
    onSelect: (image: ManagedFile) => void | Promise<void>;
    navigate?: NavigateFunction;
    mode?: "select" | "manage";
    busyId?: string | null;
}

const ImagePicker = forwardRef<ImagePickerHandle, ImagePickerProps>(
    ({ onSelect, navigate, mode = "select", busyId = null }, ref) => {
        const [images, setImages] = useState<ManagedFile[]>([]);
        const [isLoading, setIsLoading] = useState(true);
        const [hasError, setHasError] = useState(false);
        const [refreshVersion, setRefreshVersion] = useState(0);

        const refresh = () => {
            setIsLoading(true);
            setHasError(false);
            setRefreshVersion((version) => version + 1);
        };

        useImperativeHandle(ref, () => ({ refresh }));

        useEffect(() => {
            let active = true;
            void apiRequest<ApiPublicFile[]>(
                `${apiEndpoints.publicFiles}?${firstPage}`,
                null,
                "GET",
                navigate
            ).then((data) => {
                if (!active) return;
                if (data) {
                    const images = data
                        .filter((file) =>
                            [".jpg", ".jpeg", ".png", ".webp", ".gif"].includes(
                                file.type.toLowerCase()
                            )
                        )
                        .map(adaptPublicFile);
                    setImages(
                        images.sort((first, second) =>
                            first.title.localeCompare(second.title, "pl")
                        )
                    );
                    setHasError(false);
                } else {
                    setImages([]);
                    setHasError(true);
                }
                setIsLoading(false);
            });
            return () => {
                active = false;
            };
        }, [navigate, refreshVersion]);

        if (isLoading) {
            return (
                <p role="status" className="py-10 text-center text-sm text-ink-muted">
                    Ładowanie obrazów…
                </p>
            );
        }

        if (hasError) {
            return (
                <div role="alert" className="rounded-xl border p-6 text-center app-status-danger">
                    <p className="text-sm">Nie udało się pobrać biblioteki obrazów.</p>
                    <button
                        type="button"
                        onClick={refresh}
                        className="mt-3 rounded-lg px-3 py-2 text-sm font-medium text-danger-text hover:bg-danger-muted focus:outline-none focus:ring-2 focus:ring-danger-emphasis"
                    >
                        Spróbuj ponownie
                    </button>
                </div>
            );
        }

        if (images.length === 0) {
            return (
                <p className="rounded-xl border border-dashed border-line-strong p-8 text-center text-sm text-ink-muted">
                    Brak obrazów w bibliotece.
                </p>
            );
        }

        return (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                {images.map((image) => {
                    const isBusy = busyId === image.id;
                    return (
                        <button
                            type="button"
                            key={image.id}
                            disabled={busyId !== null}
                            aria-label={`${mode === "manage" ? "Usuń" : "Wybierz"} obraz ${image.title}`}
                            onClick={() => void onSelect(image)}
                            className={`group overflow-hidden rounded-xl border bg-surface text-left shadow-sm transition focus:outline-none focus:ring-2 disabled:cursor-not-allowed disabled:opacity-60 ${
                                mode === "manage"
                                    ? "border-line hover:border-danger-border-strong hover:shadow-md focus:ring-danger-emphasis"
                                    : "border-line hover:-translate-y-0.5 hover:border-brand-emphasis hover:shadow-md focus:ring-brand-focus"
                            }`}
                        >
                            <span className="relative block overflow-hidden bg-surface-muted">
                                <img
                                    src={resolveApiAssetUrl(image.filePath)}
                                    alt=""
                                    loading="lazy"
                                    draggable={false}
                                    className="aspect-square w-full object-cover transition group-hover:scale-[1.02]"
                                />
                                <span
                                    aria-hidden="true"
                                    className={`absolute right-2 top-2 grid h-8 w-8 place-items-center rounded-full text-ink-inverse shadow ${
                                        mode === "manage" ? "bg-danger" : "bg-brand"
                                    }`}
                                >
                                    {mode === "manage" ? <FaTrash /> : <FaCheck />}
                                </span>
                            </span>
                            <span className="block truncate p-2 text-xs font-medium text-ink">
                                {isBusy ? "Usuwanie…" : image.title}
                            </span>
                        </button>
                    );
                })}
            </div>
        );
    }
);

ImagePicker.displayName = "ImagePicker";

export default ImagePicker;
