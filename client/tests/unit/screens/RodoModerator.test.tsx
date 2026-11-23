import { screen, waitFor, within } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { expect, it, vi } from "vitest";
import AdminHeader from "../../../src/components/AdminHeader";
import AdminRodoScreen from "../../../src/screens/admin/AdminRodoScreen";
import AdminTeamsScreen from "../../../src/screens/admin/AdminTeamsScreen";
import PanelScreen from "../../../src/screens/admin/PanelScreen";
import UsersScreen from "../../../src/screens/admin/UsersScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { hasAnyRole, storeSession } from "../../../src/utils/authService";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

const moderator = "ORGANIZATION_RODO_MODERATOR" as const;
const session = (roles: ("ORGANIZATION_ADMIN" | typeof moderator)[]) =>
    storeSession({
        id: "current",
        roles,
        version: 1,
        createdAt: "2026-01-01T00:00:00Z",
        updatedAt: "2026-01-01T00:00:00Z",
    });

it("udostępnia moderatorowi tylko drużyny i RODO, bez uprawnień administratora", async () => {
    session([moderator]);
    const navigate = vi.fn();
    const { user } = renderWithRouter(<AdminHeader navigate={navigate} />);
    await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
    const sidebar = screen.getByRole("navigation", { name: "Nawigacja" });
    expect(within(sidebar).getByRole("button", { name: "Drużyny" })).toBeInTheDocument();
    expect(within(sidebar).getByRole("button", { name: "RODO" })).toBeInTheDocument();
    expect(within(sidebar).queryByRole("button", { name: "Użytkownicy" })).not.toBeInTheDocument();
    expect(hasAnyRole([moderator])).toBe(true);
    expect(hasAnyRole(["ORGANIZATION_ADMIN"])).toBe(false);
});

it("administrator bez roli moderatora nie otrzymuje odnośnika do RODO", async () => {
    session(["ORGANIZATION_ADMIN"]);
    const { user } = renderWithRouter(<AdminHeader navigate={vi.fn()} />);
    await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
    const sidebar = screen.getByRole("navigation", { name: "Nawigacja" });
    expect(within(sidebar).queryByRole("button", { name: "RODO" })).not.toBeInTheDocument();
    expect(within(sidebar).getByRole("button", { name: "Użytkownicy" })).toBeInTheDocument();
});

it("zaprasza moderatora i dołącza jego rolę do filtra listy użytkowników", async () => {
    session(["ORGANIZATION_ADMIN"]);
    let payload: unknown;
    server.use(
        http.get(`${apiUrl}${apiEndpoints.users}`, ({ request }) => {
            expect(new URL(request.url).searchParams.getAll("UserRoles")).toContain(moderator);
            return HttpResponse.json({ data: [] });
        }),
        http.post(`${apiUrl}${apiEndpoints.users}`, async ({ request }) => {
            payload = await request.json();
            return HttpResponse.json({ data: null }, { status: 201 });
        })
    );
    const { user } = renderWithRouter(<UsersScreen />);
    await user.click(screen.getByRole("button", { name: "Zaproś użytkownika" }));
    const dialog = screen.getByRole("dialog");
    await user.type(
        within(dialog).getByRole("textbox", { name: "E-mail" }),
        "moderator@koala.test"
    );
    await user.click(within(dialog).getByRole("checkbox", { name: "moderator RODO" }));
    await user.click(within(dialog).getByRole("button", { name: "Wyślij zaproszenie" }));
    await waitFor(() =>
        expect(payload).toEqual({ email: "moderator@koala.test", userRoles: [moderator] })
    );
});

it("administrator pobiera ograniczony widok drużyn bez danych członków", async () => {
    session(["ORGANIZATION_ADMIN"]);
    server.use(
        http.get(`${apiUrl}${apiEndpoints.adminTeams}`, () =>
            HttpResponse.json({
                data: [
                    {
                        id: "team",
                        editionId: "ed",
                        schoolId: "school",
                        name: "Drużyna próbna",
                        tier: "TIER_1",
                        isCensored: false,
                        version: 1,
                        createdAt: "2026-01-01T00:00:00Z",
                    },
                ],
            })
        ),
        http.get(`${apiUrl}${apiEndpoints.schools}`, () =>
            HttpResponse.json({ data: [{ id: "school", nameFull: "Szkoła próbna" }] })
        ),
        http.get(`${apiUrl}${apiEndpoints.editions}`, () =>
            HttpResponse.json({ data: [{ id: "ed", name: "Edycja próbna" }] })
        )
    );
    renderWithRouter(<AdminTeamsScreen />);
    expect(await screen.findByRole("cell", { name: "Drużyna próbna" })).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Członkowie" })).not.toBeInTheDocument();
});

it("podsumowanie admina nie odpytuje endpointów danych osobowych ani RODO", async () => {
    session(["ORGANIZATION_ADMIN"]);
    server.use(
        http.get(`${apiUrl}${apiEndpoints.editions}`, () =>
            HttpResponse.json({ data: [{ id: "ed", name: "Bieżąca edycja", state: "ACTIVE" }] })
        ),
        http.get(`${apiUrl}${apiEndpoints.adminTeams}`, () =>
            HttpResponse.json({ data: [{ id: "team", tier: "TIER_1" }] })
        ),
        http.get(`${apiUrl}${apiEndpoints.subeditions("ed")}`, () =>
            HttpResponse.json({ data: [] })
        )
    );
    renderWithRouter(<PanelScreen />);
    expect(await screen.findByRole("heading", { name: "Bieżąca edycja" })).toBeInTheDocument();
    expect(screen.getByRole("img", { name: "Podstawowa: 1, Średnia: 0" })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Zgody RODO" })).not.toBeInTheDocument();
});

it("moderator przegląda dokument i zmienia status zgody z obiema wersjami", async () => {
    session([moderator]);
    const member = {
        teamMemberId: "member",
        teamId: "team",
        nameFist: "Jan",
        nameLast: "Testowy",
        rodoState: "RODO_UNVERIFIED",
        version: 3,
        rodoVersion: 4,
        updatedAt: "2026-01-01T00:00:00Z",
    };
    let payload: unknown;
    server.use(
        http.get(`${apiUrl}${apiEndpoints.teams}`, () =>
            HttpResponse.json({
                data: [
                    {
                        id: "team",
                        name: "Koale",
                        teamCaptain: null,
                        teamGuardian: null,
                        teamMembers: [member],
                    },
                ],
            })
        ),
        http.get(`${apiUrl}${apiEndpoints.teamRodos("team")}`, () =>
            HttpResponse.json({ data: [member] })
        ),
        http.put(`${apiUrl}${apiEndpoints.teamRodos("team")}/member/state`, async ({ request }) => {
            payload = await request.json();
            return HttpResponse.json({
                data: { ...member, version: 4, rodoVersion: 5, rodoState: "RODO_ACCEPTED" },
            });
        })
    );
    const { user } = renderWithRouter(<AdminRodoScreen />);
    await user.click(await screen.findByRole("button", { name: "Szczegóły" }));
    expect(screen.getByRole("link", { name: "Otwórz przesłany dokument" })).toHaveAttribute(
        "href",
        expect.stringContaining("/teams/team/rodos/member/file")
    );
    await user.click(screen.getByRole("button", { name: "Zatwierdź" }));
    await waitFor(() =>
        expect(payload).toEqual({ state: "RODO_ACCEPTED", version: 3, rodoVersion: 4 })
    );
    await waitFor(() =>
        expect(
            within(screen.getByRole("dialog")).queryByRole("button", { name: "Zatwierdź" })
        ).not.toBeInTheDocument()
    );
});
