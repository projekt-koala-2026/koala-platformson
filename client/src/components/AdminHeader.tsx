import { useMemo, useState } from "react";
import type { NavigateFunction } from "react-router-dom";
import { useLocation } from "react-router-dom";
import { apiRequestResult } from "../utils/apiFetcher";
import { clearStoredSession, isAdmin } from "../utils/authService";
import ChangePasswordModal from "./ChangePasswordModal";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";

interface AdminHeaderProps {
    navigate: NavigateFunction;
}

const editorLinks = [
    { label: "Wpisy", path: "/admin/posts" },
    { label: "Historia", path: "/admin/history" },
    { label: "Regulamin", path: "/admin/rules" },
    { label: "Sponsorzy", path: "/admin/sponsors" },
    { label: "Koalicjanci", path: "/admin/koalicjants" },
    { label: "Zadania", path: "/admin/problems" },
    { label: "Pliki", path: "/admin/images" },
];

const administratorLinks = [
    { label: "Start", path: "/admin" },
    { label: "Edycje", path: "/admin/editions" },
    ...editorLinks,
    { label: "Szkoły", path: "/admin/schools" },
    { label: "Drużyny", path: "/admin/teams" },
];

const desktopPaths = new Set([
    "/admin",
    "/admin/editions",
    "/admin/posts",
    "/admin/history",
    "/admin/rules",
    "/admin/problems",
]);

const AdminHeader = ({ navigate }: AdminHeaderProps) => {
    const location = useLocation();
    const [isPasswordOpen, setIsPasswordOpen] = useState(false);
    const [loggingOut, setLoggingOut] = useState(false);
    const isAdminUser = useMemo(() => isAdmin(), []);
    const homePath = isAdminUser ? "/admin" : "/admin/posts";
    const links = isAdminUser ? administratorLinks : editorLinks;
    const desktopLinks = links.filter((link) => desktopPaths.has(link.path));

    const logout = async () => {
        if (loggingOut) return;
        setLoggingOut(true);
        await apiRequestResult<boolean>("/api/admin/auth/session", null, "DELETE", navigate);
        clearStoredSession();
        navigate("/admin/login");
        setLoggingOut(false);
    };

    return (
        <>
            <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 px-4 py-3 backdrop-blur-md sm:px-6">
                <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
                    <button
                        type="button"
                        onClick={() => navigate(homePath)}
                        aria-label="Przejdź na stronę główną panelu"
                        className="rounded-lg text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-500"
                    >
                        <span className="block text-xl font-bold tracking-tight text-slate-950">
                            KOALA
                        </span>
                        <span className="block text-xs font-semibold text-emerald-700">
                            Panel administracyjny
                        </span>
                    </button>

                    <nav
                        aria-label="Nawigacja panelu"
                        className="hidden items-center gap-1 xl:flex"
                    >
                        {desktopLinks.map((link) => {
                            const isActive = location.pathname === link.path;
                            return (
                                <button
                                    type="button"
                                    key={link.path}
                                    aria-current={isActive ? "page" : undefined}
                                    onClick={() => navigate(link.path)}
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
                        <ProfileButton
                            options={[
                                {
                                    label: "Zmień hasło",
                                    onClick: () => setIsPasswordOpen(true),
                                },
                                {
                                    label: loggingOut ? "Wylogowywanie…" : "Wyloguj",
                                    onClick: () => void logout(),
                                    disabled: loggingOut,
                                },
                            ]}
                        />
                        <Hamburger
                            options={links.map((link) => ({
                                label: link.label,
                                onClick: () => navigate(link.path),
                            }))}
                        />
                    </div>
                </div>
            </header>
            <ChangePasswordModal isOpen={isPasswordOpen} onClose={() => setIsPasswordOpen(false)} />
        </>
    );
};

export default AdminHeader;
