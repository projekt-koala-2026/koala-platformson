import type { Role } from "../types/models";
import type { NavigationLink } from "../types/ui";

type OrganizationRole = Extract<Role, `ORGANIZATION_${string}`>;

// Matches the eight shortcuts currently available to an administrator.
export const adminHeaderShortcutLimit = 8;

// null: show every accessible link if it fits, otherwise the first eight.
// An array overrides the automatic selection; its order is the header order.
// [] hides shortcuts. Links the user cannot access are always discarded.
export const adminHeaderShortcuts: Record<OrganizationRole, readonly string[] | null> = {
    ORGANIZATION_ADMIN: [
        "/admin",
        "/admin/users",
        "/admin/editions",
        "/admin/rankings",
        "/admin/posts",
        "/admin/history",
        "/admin/rules",
        "/admin/problems",
    ],
    ORGANIZATION_EDITOR: null,
    ORGANIZATION_REVIEWER: null,
    ORGANIZATION_RODO_MODERATOR: null,
};

// For combined roles, use the first matching configuration in this order.
const rolePriority: OrganizationRole[] = [
    "ORGANIZATION_ADMIN",
    "ORGANIZATION_EDITOR",
    "ORGANIZATION_REVIEWER",
    "ORGANIZATION_RODO_MODERATOR",
];

export function selectAdminHeaderLinks(
    activeRoles: Partial<Record<OrganizationRole, boolean>>,
    accessibleLinks: NavigationLink[]
): NavigationLink[] {
    const role = rolePriority.find((item) => activeRoles[item]);
    if (!role) return [];
    const configured = adminHeaderShortcuts[role];
    const paths = configured ?? accessibleLinks.map((link) => link.path);
    const uniquePaths = [...new Set(paths)];
    return uniquePaths
        .flatMap((path) => accessibleLinks.filter((link) => link.path === path))
        .slice(0, adminHeaderShortcutLimit);
}
