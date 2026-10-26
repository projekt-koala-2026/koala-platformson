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
            className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-backdrop/45 p-4 backdrop-blur-sm"
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
                className={`flex max-h-[calc(100dvh-2rem)] w-full ${widths[maxWidth]} animate-[fadeIn_.18s_ease-out] flex-col overflow-hidden rounded-2xl border border-line-subtle bg-surface shadow-2xl outline-none`}
            >
                <header className="flex shrink-0 items-center justify-between gap-4 border-b border-line-subtle px-5 py-4 sm:px-6">
                    <h2 id={titleId} className="text-lg font-semibold text-ink-heading">
                        {title}
                    </h2>
                    <button
                        type="button"
                        aria-label="Zamknij"
                        onClick={onClose}
                        className="shrink-0 rounded-lg p-2 text-ink-muted transition hover:bg-surface-muted hover:text-ink-heading focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
                    >
                        <FaTimes aria-hidden="true" />
                    </button>
                </header>
                <div className="min-h-0 overflow-y-auto overscroll-contain px-5 py-5 sm:px-6 sm:py-6">
                    {children}
                </div>
            </section>
        </div>
    );
};

export default Modal;
