import { useEffect, useId, useRef, useState } from "react";
import { FaUser } from "react-icons/fa";
import type { Role, User } from "../types/models";
import type { NavigationOption } from "../types/ui";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import { getStoredUserId } from "../utils/authService";

interface ProfileButtonProps {
    options: NavigationOption[];
}

const ProfileButton = ({ options }: ProfileButtonProps) => {
    const [isOpen, setIsOpen] = useState(false);
    const [user, setUser] = useState<User | null>(null);
    const [loadingUser, setLoadingUser] = useState(false);
    const [userError, setUserError] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const menuId = useId();

    const toggleMenu = async () => {
        if (isOpen) {
            setIsOpen(false);
            return;
        }
        setIsOpen(true);
        setUser(null);
        setUserError(false);
        const userId = getStoredUserId();
        if (!userId) {
            setUserError(true);
            return;
        }
        setLoadingUser(true);
        const result = await apiRequestResult<User>(
            `${apiEndpoints.users}/${encodeURIComponent(userId)}`,
            null,
            "GET"
        );
        if (result.data) setUser(result.data);
        else setUserError(true);
        setLoadingUser(false);
    };
    const roleLabels: Partial<Record<Role, string>> = {
        ORGANIZATION_ADMIN: "Administrator",
        ORGANIZATION_EDITOR: "Redaktor",
        ORGANIZATION_REVIEWER: "Recenzent",
        TEAM_CAPTAIN: "Kapitan",
        TEAM_GUARDIAN: "Opiekun",
    };

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
                onClick={() => void toggleMenu()}
                className="grid h-10 w-10 appearance-none place-items-center overflow-hidden rounded-full border-0 bg-brand bg-clip-border text-sm text-ink-inverse shadow-sm transition hover:bg-brand-hover active:bg-brand-active focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-border-strong"
            >
                <FaUser aria-hidden="true" />
            </button>
            {isOpen && (
                <div
                    id={menuId}
                    role="menu"
                    className="absolute right-0 top-12 z-50 max-h-[min(70dvh,28rem)] w-64 max-w-[calc(100vw-2rem)] origin-top-right overflow-y-auto overscroll-contain rounded-2xl border border-line bg-surface p-2 shadow-xl"
                >
                    <div className="mb-1 border-b border-line-subtle px-3 py-3 text-sm" role="none">
                        <p className="text-xs font-medium text-ink-muted">Zalogowano jako</p>
                        {loadingUser ? (
                            <p className="mt-1 text-ink-secondary">Ładowanie danych…</p>
                        ) : user ? (
                            <>
                                {(user.nameFirst || user.nameLast) && (
                                    <p className="mt-1 break-words font-semibold text-ink-heading">
                                        {[user.nameFirst, user.nameLast].filter(Boolean).join(" ")}
                                    </p>
                                )}
                                <p className="mt-1 break-all text-ink-heading">{user.email}</p>
                                {user.roles?.length > 0 && (
                                    <p className="mt-1 text-xs text-ink-secondary">
                                        {user.roles
                                            .map((role) => roleLabels[role] ?? role)
                                            .join(", ")}
                                    </p>
                                )}
                            </>
                        ) : (
                            <p className="mt-1 text-ink-secondary">
                                {userError
                                    ? "Nie udało się pobrać danych konta."
                                    : "Brak danych konta."}
                            </p>
                        )}
                    </div>
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

export default ProfileButton;
