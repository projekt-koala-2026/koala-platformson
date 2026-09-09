import { forwardRef, useEffect, useImperativeHandle, useState } from "react";
import { FaCheck, FaTrash } from "react-icons/fa";
import type { NavigateFunction } from "react-router-dom";
import type { ManagedFile } from "../types/models";
import { apiRequest, resolveApiAssetUrl } from "../utils/apiFetcher";

const IMAGES_ENDPOINT = "/api/admin/file/public/files?Folder=images";

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
            void apiRequest<ManagedFile[]>(IMAGES_ENDPOINT, null, "GET", navigate).then((data) => {
                if (!active) return;
                if (data) {
                    setImages(
                        [...data].sort((first, second) =>
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
                <p role="status" className="py-10 text-center text-sm text-slate-500">
                    Ładowanie obrazów…
                </p>
            );
        }

        if (hasError) {
            return (
                <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
                    <p className="text-sm text-red-800">Nie udało się pobrać biblioteki obrazów.</p>
                    <button
                        type="button"
                        onClick={refresh}
                        className="mt-3 rounded-lg px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 focus:outline-none focus:ring-2 focus:ring-red-500"
                    >
                        Spróbuj ponownie
                    </button>
                </div>
            );
        }

        if (images.length === 0) {
            return (
                <p className="rounded-xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500">
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
                            className={`group overflow-hidden rounded-xl border bg-white text-left shadow-sm transition focus:outline-none focus:ring-2 disabled:cursor-not-allowed disabled:opacity-60 ${
                                mode === "manage"
                                    ? "border-slate-200 hover:border-red-400 hover:shadow-md focus:ring-red-500"
                                    : "border-slate-200 hover:-translate-y-0.5 hover:border-emerald-400 hover:shadow-md focus:ring-emerald-500"
                            }`}
                        >
                            <span className="relative block overflow-hidden bg-slate-100">
                                <img
                                    src={resolveApiAssetUrl(image.filePath)}
                                    alt=""
                                    loading="lazy"
                                    draggable={false}
                                    className="aspect-square w-full object-cover transition group-hover:scale-[1.02]"
                                />
                                <span
                                    aria-hidden="true"
                                    className={`absolute right-2 top-2 grid h-8 w-8 place-items-center rounded-full text-white shadow ${
                                        mode === "manage" ? "bg-red-600" : "bg-emerald-600"
                                    }`}
                                >
                                    {mode === "manage" ? <FaTrash /> : <FaCheck />}
                                </span>
                            </span>
                            <span className="block truncate p-2 text-xs font-medium text-slate-700">
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
