import { useEffect, useId, useRef, useState } from "react";
import type { NavigationOption } from "../types/ui";

interface HamburgerProps {
    options: NavigationOption[];
}

const Hamburger = ({ options }: HamburgerProps) => {
    const [isOpen, setIsOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const menuId = useId();

    useEffect(() => {
        if (!isOpen) return;
        const closeOnOutsideClick = (event: PointerEvent) => {
            if (!containerRef.current?.contains(event.target as Node)) setIsOpen(false);
        };
        const closeOnEscape = (event: KeyboardEvent) => {
            if (event.key === "Escape") setIsOpen(false);
        };
        document.addEventListener("pointerdown", closeOnOutsideClick);
        document.addEventListener("keydown", closeOnEscape);
        return () => {
            document.removeEventListener("pointerdown", closeOnOutsideClick);
            document.removeEventListener("keydown", closeOnEscape);
        };
    }, [isOpen]);

    return (
        <div ref={containerRef} className="relative">
            <button
                type="button"
                aria-label={isOpen ? "Zamknij nawigację" : "Otwórz nawigację"}
                aria-expanded={isOpen}
                aria-controls={menuId}
                aria-haspopup="menu"
                onClick={() => setIsOpen((value) => !value)}
                className="grid h-10 w-10 place-items-center rounded-xl border border-line bg-surface shadow-sm transition hover:bg-brand-soft active:bg-brand-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
            >
                <span aria-hidden="true" className="space-y-1">
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                </span>
            </button>
            {isOpen && (
                <div
                    id={menuId}
                    role="menu"
                    className="absolute right-0 top-12 z-50 max-h-[min(70dvh,28rem)] w-60 origin-top-right overflow-y-auto overscroll-contain rounded-2xl border border-line bg-surface p-2 shadow-xl"
                >
                    {options.map((option) => (
                        <button
                            type="button"
                            role="menuitem"
                            key={option.label}
                            disabled={option.disabled}
                            onClick={() => {
                                setIsOpen(false);
                                void option.onClick();
                            }}
                            className="block w-full rounded-lg p-3 text-left text-sm font-medium text-ink-strong transition hover:bg-brand-soft hover:text-brand-text-strong active:bg-brand-muted focus-visible:bg-brand-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            {option.label}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
};

export default Hamburger;
