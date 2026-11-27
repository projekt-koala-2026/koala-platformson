import { screen, within } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { expect, it } from "vitest";
import UsersScreen from "../../../src/screens/admin/UsersScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

it("wyświetla dane i podpisane akcje oraz szuka po nazwisku i mailu bez polskich znaków", async () => {
    const users = [
        { id: "u1", nameFirst: "Łucja", nameLast: "Żółć", email: "lucja@koala.test" },
        { id: "u2", nameFirst: null, nameLast: null, email: "zaproszona@koala.test" },
    ].map((user) => ({ ...user, roles: ["ORGANIZATION_EDITOR"], version: 1 }));
    let requests = 0;
    server.use(
        http.get(`${apiUrl}${apiEndpoints.users}`, () => {
            requests++;
            return HttpResponse.json({ data: users });
        })
    );
    const { user } = renderWithRouter(<UsersScreen />);
    const heading = await screen.findByRole("heading", { name: "Łucja Żółć" });
    const card = heading.closest("article")!;
    expect(within(card).getByText("lucja@koala.test")).toBeInTheDocument();
    expect(within(card).getByRole("button", { name: "Edytuj" })).toBeVisible();
    expect(within(card).getByRole("button", { name: "Usuń" })).toBeVisible();
    const search = screen.getByRole("searchbox", { name: "Szukaj użytkownika" });
    await user.type(search, "  ZOLC lucja  ");
    expect(heading).toBeInTheDocument();
    expect(
        screen.queryByRole("heading", { name: "zaproszona@koala.test" })
    ).not.toBeInTheDocument();
    await user.clear(search);
    await user.type(search, "zaproszona@");
    expect(screen.getByRole("heading", { name: "zaproszona@koala.test" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Łucja Żółć" })).not.toBeInTheDocument();
    await user.clear(search);
    await user.type(search, "nieistniejaca");
    expect(screen.getByText("Brak użytkowników pasujących do wyszukiwania.")).toBeInTheDocument();
    await user.clear(search);
    expect(screen.getAllByRole("article")).toHaveLength(2);
    expect(requests).toBe(1);
});
