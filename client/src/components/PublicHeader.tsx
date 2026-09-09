import { useMemo, useState } from "react";
import type { NavigateFunction } from "react-router-dom";
import { useLocation } from "react-router-dom";
import { apiRequestResult } from "../utils/apiFetcher";
import { clearStoredSession, isAdmin, isCaptain, isEditor } from "../utils/authService";
import ChangePasswordModal from "./ChangePasswordModal";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";

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
    const isCaptainUser = useMemo(() => isCaptain(), []);
    const canChangePassword = useMemo(() => isAdmin() || isEditor() || isCaptain(), []);
    const isLoggedIn = useMemo(() => Boolean(localStorage.getItem("userId")), []);
    const links = isCaptainUser
        ? [...baseLinks, { label: "Dla kapitana", path: "/captain" }]
        : baseLinks;
    const navigationOptions = links.map((link) => ({
        label: link.label,
        onClick: () => navigate(link.path),
    }));

    const logout = async () => {
        if (loggingOut) return;
        setLoggingOut(true);
        await apiRequestResult<boolean>("/api/admin/auth/session", null, "DELETE", navigate);
        clearStoredSession();
        navigate("/login");
        setLoggingOut(false);
    };

    return (
        <>
            <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 px-4 py-3 backdrop-blur-md sm:px-6">
                <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
                    <button
                        type="button"
                        onClick={() => navigate("/")}
                        aria-label="Przejdź na stronę główną"
                        className="rounded-lg text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-500"
                    >
                        <span className="block text-xl font-bold tracking-tight text-slate-950">
                            KOALA
                        </span>
                        <span className="hidden text-xs font-semibold text-emerald-700 sm:block">
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
                                    className={`rounded-lg px-3 py-2 text-sm font-medium transition focus:outline-none focus:ring-2 focus:ring-emerald-500 ${
                                        isActive
                                            ? "bg-emerald-50 text-emerald-900"
                                            : "text-slate-700 hover:bg-slate-100 hover:text-slate-950"
                                    }`}
                                >
                                    {link.label}
                                </button>
                            );
                        })}
                    </nav>

                    <div className="flex items-center gap-3">
                        {isLoggedIn ? (
                            <ProfileButton
                                options={[
                                    ...(isCaptainUser
                                        ? [
                                              {
                                                  label: "Panel kapitana",
                                                  onClick: () => navigate("/captain"),
                                              },
                                          ]
                                        : []),
                                    ...(canChangePassword
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
                                className="hidden appearance-none overflow-hidden rounded-xl border-0 bg-emerald-600 bg-clip-border px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-emerald-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-300 sm:inline-flex"
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
