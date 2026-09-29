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
    isAdmin: roles.includes("ORGANIZATION_ADMIN") || roles.includes("ADMIN"),
    isEditor: roles.includes("ORGANIZATION_EDITOR") || roles.includes("EDITOR"),
    isReviewer: roles.includes("ORGANIZATION_REVIEWER") || roles.includes("REVIEWER"),
    isGuardian: roles.includes("TEAM_ADMIN") || roles.includes("GUARDIAN"),
    isCaptain: roles.includes("TEAM_PLAYER") || roles.includes("CAPTAIN"),
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

const roleFlags: Record<Role, keyof UserRoles> = {
    ADMIN: "isAdmin",
    EDITOR: "isEditor",
    REVIEWER: "isReviewer",
    GUARDIAN: "isGuardian",
    CAPTAIN: "isCaptain",
    ORGANIZATION_ADMIN: "isAdmin",
    ORGANIZATION_EDITOR: "isEditor",
    ORGANIZATION_REVIEWER: "isReviewer",
    TEAM_ADMIN: "isGuardian",
    TEAM_PLAYER: "isCaptain",
};

export const hasAnyRole = (roles: Role[]) => {
    const userRoles = getUserRoles();
    return roles.some((role) => Boolean(userRoles[roleFlags[role]]));
};
