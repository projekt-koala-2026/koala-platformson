import { screen, waitFor } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { expect, it, vi } from "vitest";
import EditPostScreen from "../../../src/screens/admin/EditPostScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

vi.mock("../../../src/components/MarkdownEditor", () => ({
    default: () => <textarea aria-label="Treść Markdown" />,
}));

const edition = {
    id: "edition-1",
    name: "Edycja",
    state: "ACTIVE",
    teamRegisterOpen: "2026-01-01T00:00:00Z",
    teamRegisterClose: "2027-01-01T00:00:00Z",
    version: 1,
};
const post = (i: number) => ({
    id: `post-${i}`,
    name: `Wpis ${i}`,
    editionId: edition.id,
    contentJson: JSON.stringify({ version: "1", title_pl: `Tytuł ${i}`, body_pl: `Treść ${i}` }),
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    isVisible: true,
    version: 1,
});

it("pobiera tylko wybraną stronę wpisów i zachowuje formularz przy zmianie strony", async () => {
    const requests: number[] = [];
    server.use(
        http.get(`${apiUrl}${apiEndpoints.editions}`, ({ request }) =>
            HttpResponse.json({
                data: new URL(request.url).searchParams.getAll("States").includes("ACTIVE")
                    ? [edition]
                    : [],
            })
        ),
        http.get(`${apiUrl}${apiEndpoints.posts}`, ({ request }) => {
            const params = new URL(request.url).searchParams;
            const page = Number(params.get("PageNumber"));
            expect(params.get("PageSize")).toBe("10");
            requests.push(page);
            return HttpResponse.json({
                data: Array.from({ length: page === 0 ? 10 : 1 }, (_, i) => post(page * 10 + i)),
                pagination: { pageNumber: page, pageSize: 10, totalNumber: 11 },
            });
        })
    );
    const { user } = renderWithRouter(<EditPostScreen />);
    const title = await screen.findByPlaceholderText("Tytuł wpisu");
    await user.type(title, "Niezapisany szkic");
    await user.click(await screen.findByRole("button", { name: "Wpisy (11)" }));
    await screen.findByRole("heading", { name: "Tytuł 0" });
    expect(requests).toEqual([0]);
    expect(screen.queryByRole("heading", { name: "Tytuł 10" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Następna" }));
    await screen.findByRole("heading", { name: "Tytuł 10" });
    expect(screen.queryByRole("heading", { name: "Tytuł 0" })).not.toBeInTheDocument();
    expect(title).toHaveValue("Niezapisany szkic");
    expect(screen.getByRole("button", { name: "Następna" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Poprzednia" }));
    await screen.findByRole("heading", { name: "Tytuł 0" });
    await waitFor(() => expect(requests).toEqual([0, 1, 0]));
});

it("po błędzie pobrania pozwala ponowić wybraną stronę", async () => {
    let fail = true;
    server.use(
        http.get(`${apiUrl}${apiEndpoints.editions}`, () => HttpResponse.json({ data: [] })),
        http.get(`${apiUrl}${apiEndpoints.posts}`, () =>
            fail
                ? new HttpResponse(null, { status: 500 })
                : HttpResponse.json({
                      data: [post(0)],
                      pagination: { pageNumber: 0, pageSize: 10, totalNumber: 1 },
                  })
        )
    );
    const { user } = renderWithRouter(<EditPostScreen />);
    await user.click(await screen.findByRole("button", { name: "Wpisy (0)" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Nie udało się pobrać wpisów");
    fail = false;
    await user.click(screen.getByRole("button", { name: "Spróbuj ponownie" }));
    await screen.findByRole("heading", { name: "Tytuł 0" });
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
});
