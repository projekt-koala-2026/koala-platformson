import { useMemo, useState } from "react";
import type { NavigateFunction } from "react-router-dom";
import { useLocation } from "react-router-dom";
import { selectAdminHeaderLinks } from "../config/adminHeaderShortcuts";
import type { NavigationLink } from "../types/ui";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import {
    clearStoredSession,
    getOrganizationHomePath,
    isAdmin,
    isEditor,
    isReviewer,
    isRodoModerator,
} from "../utils/authService";
import ChangePasswordModal from "./ChangePasswordModal";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";
import ThemeToggle from "./ThemeToggle";

interface AdminHeaderProps {
    navigate: NavigateFunction;
}

// Sidebar order; separatorBefore adds a subtle line above a link.
const editorLinks: NavigationLink[] = [
    { label: "Wpisy", path: "/admin/posts", separatorBefore: true },
    { label: "Historia", path: "/admin/history" },
    { label: "Regulamin", path: "/admin/rules" },
    { label: "Sponsorzy", path: "/admin/sponsors" },
    { label: "Koalicjanci", path: "/admin/koalicjants" },
    { label: "Poprzednie zadania", path: "/admin/problems", separatorBefore: true },
    { label: "Zdjęcia", path: "/admin/images" },
];

const administratorLinks: NavigationLink[] = [
    { label: "Start", path: "/admin" },
    { label: "Użytkownicy", path: "/admin/users" },
    { label: "Wiadomości", path: "/admin/emails" },
    { label: "Edycje", path: "/admin/editions", separatorBefore: true },
    { label: "Etapy", path: "/admin/subeditions", indentLevel: 1 },
    {
        label: "Zadania konkursowe",
        path: "/admin/competition-tasks",
        indentLevel: 2,
    },
    { label: "Przydzielanie recenzentów", path: "/admin/review-assignments" },
    { label: "Rankingi", path: "/admin/rankings", indentLevel: 1 },
    ...editorLinks,
    { label: "Szkoły", path: "/admin/schools", separatorBefore: true },
    { label: "Drużyny", path: "/admin/teams" },
];

const AdminHeader = ({ navigate }: AdminHeaderProps) => {
    const location = useLocation();
    const [isPasswordOpen, setIsPasswordOpen] = useState(false);
    const [loggingOut, setLoggingOut] = useState(false);
    const isAdminUser = useMemo(() => isAdmin(), []);
    const isEditorUser = useMemo(() => isEditor(), []);
    const isReviewerUser = useMemo(() => isReviewer(), []);
    const isModeratorUser = useMemo(() => isRodoModerator(), []);
    const homePath = getOrganizationHomePath();
    const links = [
        ...(isAdminUser ? administratorLinks : isEditorUser ? editorLinks : []),
        ...(isReviewerUser ? [{ label: "Moje recenzje", path: "/admin/reviews" }] : []),
        ...(isModeratorUser
            ? [
                  { label: "Drużyny", path: "/admin/teams" },
                  { label: "Opiekunowie", path: "/admin/guardians" },
                  { label: "RODO", path: "/admin/rodos" },
              ]
            : []),
    ].filter((link, index, links) => links.findIndex((item) => item.path === link.path) === index);
    const desktopLinks = selectAdminHeaderLinks(
        {
            ORGANIZATION_ADMIN: isAdminUser,
            ORGANIZATION_EDITOR: isEditorUser,
            ORGANIZATION_REVIEWER: isReviewerUser,
            ORGANIZATION_RODO_MODERATOR: isModeratorUser,
        },
        links
    );

    const logout = async () => {
        if (loggingOut) return;
        setLoggingOut(true);
        await apiRequestResult<boolean>(apiEndpoints.sessions, null, "DELETE", navigate);
        clearStoredSession();
        navigate("/admin/login");
        setLoggingOut(false);
    };

    return (
        <>
            <header className="sticky top-0 z-40 border-b border-line bg-surface/90 px-4 py-3 backdrop-blur-md sm:px-6">
                <div className="grid w-full grid-cols-[minmax(0,1fr)_auto] items-center gap-4 xl:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)]">
                    <div className="flex min-w-0 items-center gap-3">
                        <Hamburger
                            options={links.map((link) => ({
                                label: link.label,
                                separatorBefore: link.separatorBefore,
                                indentLevel: link.indentLevel,
                                active: location.pathname === link.path,
                                onClick: () => navigate(link.path),
                            }))}
                        />
                        <button
                            type="button"
                            onClick={() => navigate(homePath)}
                            aria-label="Przejdź na stronę główną panelu"
                            className="rounded-lg text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus"
                        >
                            <span className="block text-xl font-bold tracking-tight text-ink-heading">
                                KOALA
                            </span>
                            <span className="block text-xs font-semibold text-brand-text">
                                Panel administracyjny
                            </span>
                        </button>
                    </div>

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

                    <div className="flex shrink-0 items-center justify-self-end gap-3">
                        <ThemeToggle />
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
                    </div>
                </div>
            </header>
            <ChangePasswordModal isOpen={isPasswordOpen} onClose={() => setIsPasswordOpen(false)} />
        </>
    );
};

export default AdminHeader;
