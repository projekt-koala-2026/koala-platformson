import { useId, type ChangeEvent } from "react";

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

    const handleChange = async (event: ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        event.target.value = "";
        if (!file) return;
        const validationError = validateFile?.(file);
        if (validationError) {
            onValidationError?.(validationError);
            return;
        }
        await onFileSelect(file);
    };

    return (
        <div>
            <label
                htmlFor={inputId}
                className={`flex min-h-32 flex-col items-center justify-center rounded-2xl border-2 border-dashed px-5 py-6 text-center transition ${
                    disabled
                        ? "cursor-not-allowed border-slate-200 bg-slate-100 text-slate-400"
                        : "cursor-pointer border-emerald-200 bg-emerald-50/50 text-emerald-900 hover:border-emerald-400 hover:bg-emerald-50 focus-within:ring-2 focus-within:ring-emerald-500"
                }`}
            >
                <span className="font-semibold">{disabled ? "Przesyłanie pliku…" : label}</span>
                {hint && <span className="mt-2 text-xs text-slate-500">{hint}</span>}
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
