import { useCallback, useEffect, useId, useRef, useState } from "react";
import { createPortal } from "react-dom";
import type { NavigationOption } from "../types/ui";

interface HamburgerProps {
    options: (NavigationOption & { active?: boolean })[];
}

const desktopMenuKey = "koala-desktop-navigation-open";
const desktopQuery = "(min-width: 1280px)";

const desktopMenuIsOpen = () => {
    try {
        return sessionStorage.getItem(desktopMenuKey) === "true";
    } catch {
        return false;
    }
};

const Hamburger = ({ options }: HamburgerProps) => {
    const [isWide, setIsWide] = useState(() => window.matchMedia?.(desktopQuery).matches ?? false);
    const [isOpen, setIsOpen] = useState(() => isWide && desktopMenuIsOpen());
    const triggerRef = useRef<HTMLButtonElement>(null);
    const panelRef = useRef<HTMLElement>(null);
    const menuId = useId();
    const titleId = useId();

    useEffect(() => {
        const query = window.matchMedia?.(desktopQuery);
        if (!query) return;
        const update = () => {
            setIsWide(query.matches);
            setIsOpen(query.matches && desktopMenuIsOpen());
        };
        query.addEventListener("change", update);
        return () => query.removeEventListener("change", update);
    }, []);

    const changeOpen = useCallback(
        (open: boolean) => {
            if (isWide) {
                try {
                    sessionStorage.setItem(desktopMenuKey, String(open));
                } catch {
                    // The navigation still works if browser storage is unavailable.
                }
            }
            setIsOpen(open);
        },
        [isWide]
    );

    useEffect(() => {
        if (!isOpen) return;
        const trigger = triggerRef.current;
        const previousOverflow = document.body.style.overflow;
        if (isWide) document.body.classList.add("navigation-panel-open");
        else {
            document.body.style.overflow = "hidden";
            panelRef.current?.focus();
        }

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                event.preventDefault();
                changeOpen(false);
            } else if (!isWide && event.key === "Tab" && panelRef.current) {
                const buttons =
                    panelRef.current.querySelectorAll<HTMLButtonElement>("button:not([disabled])");
                const first = buttons[0];
                const last = buttons[buttons.length - 1];
                if (
                    event.shiftKey &&
                    (document.activeElement === first ||
                        document.activeElement === panelRef.current)
                ) {
                    event.preventDefault();
                    last?.focus();
                } else if (!event.shiftKey && document.activeElement === last) {
                    event.preventDefault();
                    first?.focus();
                }
            }
        };
        document.addEventListener("keydown", onKeyDown);
        return () => {
            document.removeEventListener("keydown", onKeyDown);
            document.body.style.overflow = previousOverflow;
            document.body.classList.remove("navigation-panel-open");
            trigger?.focus();
        };
    }, [isOpen, isWide, changeOpen]);

    return (
        <>
            <span className="block h-10 w-10 shrink-0" aria-hidden="true" />
            <button
                ref={triggerRef}
                type="button"
                aria-label="Otwórz nawigację"
                aria-expanded={isOpen}
                aria-controls={isOpen ? menuId : undefined}
                aria-haspopup={isWide ? undefined : "dialog"}
                onClick={() => changeOpen(!isOpen)}
                tabIndex={isOpen ? -1 : undefined}
                className={`fixed left-4 top-3 z-40 grid h-10 w-10 place-items-center rounded-xl border border-line bg-surface shadow-sm transition hover:bg-brand-soft active:bg-brand-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus ${isOpen ? "invisible" : ""}`}
            >
                <span aria-hidden="true" className="space-y-1">
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                    <span className="block h-0.5 w-5 bg-ink-strong" />
                </span>
            </button>
            {isOpen &&
                createPortal(
                    <div
                        className={
                            isWide
                                ? "fixed inset-y-0 left-0 z-50"
                                : "fixed inset-0 z-50 flex justify-start bg-backdrop/45"
                        }
                        onMouseDown={(event) => {
                            if (event.target === event.currentTarget) changeOpen(false);
                        }}
                    >
                        <section
                            ref={panelRef}
                            id={menuId}
                            role={isWide ? "region" : "dialog"}
                            aria-modal={isWide ? undefined : true}
                            aria-labelledby={titleId}
                            tabIndex={-1}
                            className="flex h-dvh w-[min(16rem,85vw)] flex-col border-r border-line bg-surface shadow-2xl outline-none"
                        >
                            <div className="flex shrink-0 items-center gap-3 border-line px-4 py-3.5">
                                <button
                                    type="button"
                                    aria-label="Zamknij nawigację"
                                    aria-expanded="true"
                                    aria-controls={menuId}
                                    onClick={() => changeOpen(false)}
                                    className="grid h-10 w-10 shrink-0 place-items-center rounded-xl border border-line bg-surface shadow-sm transition hover:bg-brand-soft active:bg-brand-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
                                >
                                    <span aria-hidden="true" className="space-y-1">
                                        <span className="block h-0.5 w-5 bg-ink-strong" />
                                        <span className="block h-0.5 w-5 bg-ink-strong" />
                                        <span className="block h-0.5 w-5 bg-ink-strong" />
                                    </span>
                                </button>
                                <h2 id={titleId} className="text-lg font-semibold text-ink-heading">
                                    Nawigacja
                                </h2>
                            </div>
                            <nav
                                aria-label="Nawigacja"
                                className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-3"
                            >
                                {options.map((option) => (
                                    <button
                                        type="button"
                                        key={option.label}
                                        disabled={option.disabled}
                                        aria-current={option.active ? "page" : undefined}
                                        onClick={() => {
                                            if (!isWide) changeOpen(false);
                                            void option.onClick();
                                        }}
                                        className={`block w-full rounded-lg px-4 py-3 text-left text-sm font-medium transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus disabled:cursor-not-allowed disabled:opacity-50 ${option.active ? "bg-brand-soft text-brand-text-strong" : "text-ink-strong hover:bg-brand-soft hover:text-brand-text-strong active:bg-brand-muted"}`}
                                    >
                                        {option.label}
                                    </button>
                                ))}
                            </nav>
                        </section>
                    </div>,
                    document.body
                )}
        </>
    );
};

export default Hamburger;
