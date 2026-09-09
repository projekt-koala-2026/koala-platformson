import { useEffect, useId, useRef } from "react";
import { FaTimes } from "react-icons/fa";
import type { ModalProps } from "../types/ui";

const widths = { md: "max-w-md", lg: "max-w-2xl", xl: "max-w-5xl" };
const focusableSelector =
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

const Modal = ({ children, isOpen, onClose, title, maxWidth = "lg" }: ModalProps) => {
    const dialogRef = useRef<HTMLElement>(null);
    const closeRef = useRef(onClose);
    const titleId = useId();

    useEffect(() => {
        closeRef.current = onClose;
    }, [onClose]);

    useEffect(() => {
        if (!isOpen) return;
        const previouslyFocused = document.activeElement as HTMLElement | null;
        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        const focusFrame = window.requestAnimationFrame(() => dialogRef.current?.focus());

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                event.preventDefault();
                closeRef.current();
                return;
            }
            if (event.key !== "Tab" || !dialogRef.current) return;
            const focusable = Array.from(
                dialogRef.current.querySelectorAll<HTMLElement>(focusableSelector)
            );
            if (focusable.length === 0) {
                event.preventDefault();
                dialogRef.current.focus();
                return;
            }
            const first = focusable[0];
            const last = focusable[focusable.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        };

        document.addEventListener("keydown", onKeyDown);
        return () => {
            window.cancelAnimationFrame(focusFrame);
            document.removeEventListener("keydown", onKeyDown);
            document.body.style.overflow = previousOverflow;
            previouslyFocused?.focus();
        };
    }, [isOpen]);

    if (!isOpen) return null;

    return (
        <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/45 p-4 backdrop-blur-sm"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) onClose();
            }}
        >
            <section
                ref={dialogRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
                tabIndex={-1}
                className={`max-h-[90vh] w-full ${widths[maxWidth]} animate-[fadeIn_.18s_ease-out] overflow-y-auto rounded-2xl border border-slate-100 bg-white p-5 shadow-2xl outline-none sm:p-6`}
            >
                <header className="mb-5 flex items-center justify-between gap-4">
                    <h2 id={titleId} className="text-lg font-semibold text-slate-900">
                        {title}
                    </h2>
                    <button
                        type="button"
                        aria-label="Zamknij"
                        onClick={onClose}
                        className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                    >
                        <FaTimes aria-hidden="true" />
                    </button>
                </header>
                {children}
            </section>
        </div>
    );
};

export default Modal;
