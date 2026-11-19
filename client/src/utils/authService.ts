import type { Role, SessionUser, UserRoles } from "../types/models";

const emptyRoles: UserRoles = {
    isAdmin: false,
    isEditor: false,
    isReviewer: false,
    isGuardian: false,
    isCaptain: false,
};

export const getUserRoles = (): UserRoles => {
    try {
        const raw = localStorage.getItem("userRoles");
        if (!raw) return emptyRoles;
        const parsed = JSON.parse(raw) as Partial<UserRoles>;
        return {
            isAdmin: parsed.isAdmin === true,
            isEditor: parsed.isEditor === true,
            isReviewer: parsed.isReviewer === true,
            isGuardian: parsed.isGuardian === true,
            isCaptain: parsed.isCaptain === true,
        };
    } catch {
        return emptyRoles;
    }
};

export const rolesToFlags = (roles: Role[]): UserRoles => ({
    isAdmin: roles.includes("ORGANIZATION_ADMIN"),
    isEditor: roles.includes("ORGANIZATION_EDITOR"),
    isReviewer: roles.includes("ORGANIZATION_REVIEWER"),
    isGuardian: roles.includes("TEAM_GUARDIAN"),
    isCaptain: roles.includes("TEAM_CAPTAIN"),
});

export const storeSession = (user: SessionUser) => {
    localStorage.setItem("userRoles", JSON.stringify(rolesToFlags(user.roles)));
    localStorage.setItem("userId", user.id);
};

export const clearStoredSession = () => {
    localStorage.removeItem("userRoles");
    localStorage.removeItem("userId");
};

export const getStoredUserId = () => localStorage.getItem("userId");

export const isAdmin = () => getUserRoles().isAdmin || false;
export const isEditor = () => getUserRoles().isEditor || false;
export const isReviewer = () => getUserRoles().isReviewer || false;
export const isGuardian = () => getUserRoles().isGuardian || false;
export const isCaptain = () => getUserRoles().isCaptain || false;

export type AuthArea = "organization" | "team";

export const getAuthArea = (roles = getUserRoles()): AuthArea | null => {
    if (roles.isAdmin || roles.isEditor || roles.isReviewer) return "organization";
    if (roles.isCaptain || roles.isGuardian) return "team";
    return null;
};

export const isOrganizationUser = () => getAuthArea() === "organization";
export const isTeamUser = () => getAuthArea() === "team";

export const getOrganizationHomePath = (roles = getUserRoles()) => {
    if (roles.isAdmin) return "/admin";
    if (roles.isEditor) return "/admin/posts";
    if (roles.isReviewer) return "/admin/reviews";
    return "/";
};

export const getAuthenticatedHomePath = (roles = getUserRoles()): string | null => {
    const area = getAuthArea(roles);
    if (area === "organization") return getOrganizationHomePath(roles);
    if (area === "team") return "/captain";
    return null;
};

export const getLoginPath = (roles = getUserRoles()) =>
    getAuthArea(roles) === "organization" ? "/admin/login" : "/login";

const roleFlags: Record<Role, keyof UserRoles> = {
    ORGANIZATION_ADMIN: "isAdmin",
    ORGANIZATION_EDITOR: "isEditor",
    ORGANIZATION_REVIEWER: "isReviewer",
    TEAM_GUARDIAN: "isGuardian",
    TEAM_CAPTAIN: "isCaptain",
};

export const hasAnyRole = (roles: Role[]) => {
    const userRoles = getUserRoles();
    return roles.some((role) => Boolean(userRoles[roleFlags[role]]));
};
