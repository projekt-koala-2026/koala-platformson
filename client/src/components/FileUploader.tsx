import { useId, useRef, useState, type ChangeEvent, type DragEvent } from "react";

interface FileUploaderProps {
    onFileSelect: (file: File) => void | Promise<void>;
    onValidationError?: (message: string) => void;
    validateFile?: (file: File) => string | null;
    accept?: string;
    disabled?: boolean;
    label?: string;
    hint?: string;
}

const FileUploader = ({
    onFileSelect,
    onValidationError,
    validateFile,
    accept,
    disabled = false,
    label = "Wybierz plik",
    hint,
}: FileUploaderProps) => {
    const inputId = useId();
    const dragDepth = useRef(0);
    const [isDragging, setIsDragging] = useState(false);

    const processFile = async (file: File | undefined) => {
        if (!file || disabled) return;
        const validationError = validateFile?.(file);
        if (validationError) {
            onValidationError?.(validationError);
            return;
        }
        await onFileSelect(file);
    };

    const handleChange = async (event: ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        event.target.value = "";
        await processFile(file);
    };

    const handleDragEnter = (event: DragEvent<HTMLLabelElement>) => {
        event.preventDefault();
        event.stopPropagation();
        if (disabled || !Array.from(event.dataTransfer.types).includes("Files")) return;
        dragDepth.current += 1;
        setIsDragging(true);
    };

    const handleDragOver = (event: DragEvent<HTMLLabelElement>) => {
        event.preventDefault();
        event.stopPropagation();
        event.dataTransfer.dropEffect = disabled ? "none" : "copy";
    };

    const handleDragLeave = (event: DragEvent<HTMLLabelElement>) => {
        event.preventDefault();
        event.stopPropagation();
        dragDepth.current = Math.max(0, dragDepth.current - 1);
        if (dragDepth.current === 0) setIsDragging(false);
    };

    const handleDrop = async (event: DragEvent<HTMLLabelElement>) => {
        event.preventDefault();
        event.stopPropagation();
        dragDepth.current = 0;
        setIsDragging(false);
        await processFile(event.dataTransfer.files?.[0]);
    };

    return (
        <div>
            <label
                htmlFor={inputId}
                aria-disabled={disabled}
                onDragEnter={handleDragEnter}
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={(event) => void handleDrop(event)}
                className={`flex min-h-32 flex-col items-center justify-center rounded-2xl border-2 border-dashed px-5 py-6 text-center transition ${
                    disabled
                        ? "cursor-not-allowed border-line bg-surface-muted text-ink-subtle"
                        : isDragging
                          ? "cursor-copy border-brand-focus bg-brand-muted text-brand-text-deep ring-2 ring-brand-border"
                          : "cursor-pointer border-brand-border bg-brand-soft/50 text-brand-text-strong hover:border-brand-emphasis hover:bg-brand-soft focus-within:ring-2 focus-within:ring-brand-focus"
                }`}
            >
                <span className="font-semibold">
                    {disabled ? "Przesyłanie pliku…" : isDragging ? "Upuść plik tutaj" : label}
                </span>
                {hint && <span className="mt-2 text-xs text-ink-muted">{hint}</span>}
                <input
                    id={inputId}
                    type="file"
                    accept={accept}
                    disabled={disabled}
                    onChange={(event) => void handleChange(event)}
                    className="sr-only"
                />
            </label>
        </div>
    );
};

export default FileUploader;
