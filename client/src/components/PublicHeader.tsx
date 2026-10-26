import { useMemo, useState } from "react";
import type { NavigateFunction } from "react-router-dom";
import { useLocation } from "react-router-dom";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import { clearStoredSession, getOrganizationHomePath, getUserRoles } from "../utils/authService";
import ChangePasswordModal from "./ChangePasswordModal";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";
import ThemeToggle from "./ThemeToggle";

interface PublicHeaderProps {
    navigate: NavigateFunction;
}

const baseLinks = [
    { label: "Aktualności", path: "/" },
    { label: "Zadania", path: "/problems" },
    { label: "Regulamin", path: "/rules" },
    { label: "Historia", path: "/history" },
    { label: "KOALicjA", path: "/koalicja" },
];

const PublicHeader = ({ navigate }: PublicHeaderProps) => {
    const location = useLocation();
    const [isPasswordOpen, setIsPasswordOpen] = useState(false);
    const [loggingOut, setLoggingOut] = useState(false);
    const roles = useMemo(() => getUserRoles(), []);
    const isOrganizationUser = roles.isAdmin || roles.isEditor || roles.isReviewer;
    const isTeamAccount = roles.isCaptain || roles.isGuardian;
    const isLoggedIn = isOrganizationUser || isTeamAccount;
    const organizationHomePath = getOrganizationHomePath(roles);
    const links = isTeamAccount
        ? [...baseLinks, { label: "Drużyny", path: "/captain" }]
        : baseLinks;
    const navigationOptions = links.map((link) => ({
        label: link.label,
        onClick: () => navigate(link.path),
    }));

    const logout = async () => {
        if (loggingOut) return;
        setLoggingOut(true);
        await apiRequestResult<boolean>(apiEndpoints.sessions, null, "DELETE", navigate);
        clearStoredSession();
        navigate(isOrganizationUser ? "/admin/login" : "/login");
        setLoggingOut(false);
    };

    return (
        <>
            <header className="sticky top-0 z-40 border-b border-line bg-surface/90 px-4 py-3 backdrop-blur-md sm:px-6">
                <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
                    <button
                        type="button"
                        onClick={() => navigate("/")}
                        aria-label="Przejdź na stronę główną"
                        className="rounded-lg text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus"
                    >
                        <span className="block text-xl font-bold tracking-tight text-ink-heading">
                            KOALA
                        </span>
                        <span className="hidden text-xs font-semibold text-brand-text sm:block">
                            Kombinatoryka · Algorytmika · Logika
                        </span>
                    </button>

                    <nav
                        aria-label="Główna nawigacja"
                        className="hidden items-center gap-1 lg:flex"
                    >
                        {links.map((link) => {
                            const isActive = location.pathname === link.path;
                            return (
                                <button
                                    type="button"
                                    key={link.path}
                                    onClick={() => navigate(link.path)}
                                    aria-current={isActive ? "page" : undefined}
                                    className={`rounded-lg px-3 py-2 text-sm font-medium transition focus:outline-none focus:ring-2 focus:ring-brand-focus ${
                                        isActive
                                            ? "bg-brand-soft text-brand-text-strong"
                                            : "text-ink hover:bg-surface-muted hover:text-ink-heading"
                                    }`}
                                >
                                    {link.label}
                                </button>
                            );
                        })}
                    </nav>

                    <div className="flex items-center gap-3">
                        <ThemeToggle />
                        {isLoggedIn ? (
                            <ProfileButton
                                options={[
                                    ...(isTeamAccount
                                        ? [
                                              {
                                                  label: "Panel drużyn",
                                                  onClick: () => navigate("/captain"),
                                              },
                                          ]
                                        : []),
                                    ...(isOrganizationUser && organizationHomePath !== "/"
                                        ? [
                                              {
                                                  label: "Panel administracyjny",
                                                  onClick: () => navigate(organizationHomePath),
                                              },
                                          ]
                                        : []),
                                    ...(isLoggedIn
                                        ? [
                                              {
                                                  label: "Zmień hasło",
                                                  onClick: () => setIsPasswordOpen(true),
                                              },
                                          ]
                                        : []),
                                    {
                                        label: loggingOut ? "Wylogowywanie…" : "Wyloguj",
                                        onClick: () => void logout(),
                                        disabled: loggingOut,
                                    },
                                ]}
                            />
                        ) : (
                            <button
                                type="button"
                                onClick={() => navigate("/login")}
                                className="hidden min-h-10 appearance-none items-center overflow-hidden rounded-xl border-0 bg-brand bg-clip-border px-4 py-2 text-sm font-medium text-ink-inverse shadow-sm transition hover:bg-brand-hover active:bg-brand-active focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-border-strong lg:inline-flex"
                            >
                                Zaloguj
                            </button>
                        )}
                        <div className="lg:hidden">
                            <Hamburger
                                options={[
                                    ...navigationOptions,
                                    ...(!isLoggedIn
                                        ? [
                                              {
                                                  label: "Zaloguj",
                                                  onClick: () => navigate("/login"),
                                              },
                                          ]
                                        : []),
                                ]}
                            />
                        </div>
                    </div>
                </div>
            </header>
            <ChangePasswordModal isOpen={isPasswordOpen} onClose={() => setIsPasswordOpen(false)} />
        </>
    );
};

export default PublicHeader;
