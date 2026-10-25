import { useEffect, useId, useRef, useState } from "react";
import { FaUser } from "react-icons/fa";
import type { NavigationOption } from "../types/ui";

interface ProfileButtonProps {
    options: NavigationOption[];
}

const ProfileButton = ({ options }: ProfileButtonProps) => {
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
                aria-label={isOpen ? "Zamknij menu użytkownika" : "Otwórz menu użytkownika"}
                aria-expanded={isOpen}
                aria-controls={menuId}
                aria-haspopup="menu"
                onClick={() => setIsOpen((value) => !value)}
                className="grid h-10 w-10 appearance-none place-items-center overflow-hidden rounded-full border-0 bg-emerald-600 bg-clip-border text-sm text-white shadow-sm transition hover:bg-emerald-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-300"
            >
                <FaUser aria-hidden="true" />
            </button>
            {isOpen && (
                <div
                    id={menuId}
                    role="menu"
                    className="absolute right-0 top-12 z-50 w-52 rounded-2xl border border-slate-200 bg-white p-2 shadow-xl"
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
                            className="block w-full rounded-lg p-3 text-left text-sm font-medium text-slate-800 transition hover:bg-emerald-50 hover:text-emerald-900 focus:bg-emerald-50 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            {option.label}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
};

export default ProfileButton;
