import { beforeEach, describe, expect, it } from "vitest";
import type { UserRoles } from "../../../src/types/models";
import {
    clearStoredSession,
    getAuthArea,
    getAuthenticatedHomePath,
    getLoginPath,
    getStoredUserId,
    getUserRoles,
    hasAnyRole,
    rolesToFlags,
    storeSession,
} from "../../../src/utils/authService";

const noRoles: UserRoles = {
    isAdmin: false,
    isEditor: false,
    isReviewer: false,
    isGuardian: false,
    isCaptain: false,
};

describe("authService", () => {
    beforeEach(() => localStorage.clear());

    it("returns safe defaults when role storage is missing or malformed", () => {
        expect(getUserRoles()).toEqual(noRoles);

        localStorage.setItem("userRoles", "not-json");
        expect(getUserRoles()).toEqual(noRoles);

        localStorage.setItem("userRoles", JSON.stringify({ isAdmin: "true", isCaptain: true }));
        expect(getUserRoles()).toEqual({ ...noRoles, isCaptain: true });
    });

    it("maps current and legacy API roles to the same frontend flags", () => {
        expect(
            rolesToFlags([
                "ORGANIZATION_ADMIN",
                "ORGANIZATION_EDITOR",
                "ORGANIZATION_REVIEWER",
                "TEAM_ADMIN",
                "TEAM_PLAYER",
            ])
        ).toEqual({
            isAdmin: true,
            isEditor: true,
            isReviewer: true,
            isGuardian: true,
            isCaptain: true,
        });

        expect(rolesToFlags(["ADMIN", "EDITOR", "REVIEWER", "GUARDIAN", "CAPTAIN"])).toEqual({
            isAdmin: true,
            isEditor: true,
            isReviewer: true,
            isGuardian: true,
            isCaptain: true,
        });
    });

    it("stores and clears the authenticated user session", () => {
        storeSession({ id: "user-1", roles: ["TEAM_PLAYER"] });

        expect(getStoredUserId()).toBe("user-1");
        expect(getUserRoles()).toEqual({ ...noRoles, isCaptain: true });

        clearStoredSession();
        expect(getStoredUserId()).toBeNull();
        expect(getUserRoles()).toEqual(noRoles);
    });

    it("selects the correct application area and landing page", () => {
        expect(getAuthArea({ ...noRoles, isAdmin: true })).toBe("organization");
        expect(getAuthenticatedHomePath({ ...noRoles, isAdmin: true })).toBe("/admin");
        expect(getAuthenticatedHomePath({ ...noRoles, isEditor: true })).toBe("/admin/posts");
        expect(getAuthenticatedHomePath({ ...noRoles, isReviewer: true })).toBe("/");
        expect(getAuthenticatedHomePath({ ...noRoles, isGuardian: true })).toBe("/captain");
        expect(getAuthenticatedHomePath(noRoles)).toBeNull();
    });

    it("keeps organization and team login routes separate", () => {
        expect(getLoginPath({ ...noRoles, isEditor: true })).toBe("/admin/login");
        expect(getLoginPath({ ...noRoles, isCaptain: true })).toBe("/login");
        expect(getLoginPath(noRoles)).toBe("/login");
    });

    it("checks access against every allowed role alias", () => {
        localStorage.setItem(
            "userRoles",
            JSON.stringify({ ...noRoles, isReviewer: true, isGuardian: true })
        );

        expect(hasAnyRole(["ORGANIZATION_REVIEWER"])).toBe(true);
        expect(hasAnyRole(["TEAM_ADMIN"])).toBe(true);
        expect(hasAnyRole(["ORGANIZATION_ADMIN", "TEAM_PLAYER"])).toBe(false);
    });
});
