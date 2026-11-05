import { screen, waitFor } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { describe, expect, it, vi } from "vitest";
import StaticPageEditor from "../../../src/components/StaticPageEditor";
import EditPostScreen from "../../../src/screens/admin/EditPostScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

vi.mock("../../../src/components/MarkdownEditor", () => ({
    default: ({
        initialValue,
        onChange,
    }: {
        initialValue: string;
        onChange: (value: string) => void;
    }) => (
        <textarea
            aria-label="Treść Markdown"
            defaultValue={initialValue}
            onChange={(event) => onChange(event.target.value)}
        />
    ),
}));

describe("zapisywanie treści Markdown", () => {
    it("zapisuje aktualną treść strony statycznej przyciskiem w nagłówku edytora", async () => {
        const page = {
            id: "page-1",
            name: "RULES_PAGE",
            contentJson: JSON.stringify({ markdownBody: "Początkowa treść" }),
            version: 3,
        };
        const requests: unknown[] = [];
        server.use(
            http.get(`${apiUrl}${apiEndpoints.staticPages}`, () =>
                HttpResponse.json({ data: [page] })
            ),
            http.put(`${apiUrl}${apiEndpoints.staticPages}/${page.id}`, async ({ request }) => {
                requests.push(await request.json());
                return HttpResponse.json({
                    data: {
                        ...page,
                        contentJson: JSON.stringify({ markdownBody: "Nowa treść" }),
                        version: 4,
                    },
                });
            })
        );
        const { user } = renderWithRouter(
            <StaticPageEditor title="Regulamin" description="Opis" pageName="RULES_PAGE" />
        );
        const input = await screen.findByRole("textbox", { name: "Treść Markdown" });
        const save = screen.getByRole("button", { name: "Zapisz zmiany" });
        expect(save.compareDocumentPosition(input) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
        await user.clear(input);
        await user.type(input, "Nowa treść");
        await user.click(save);
        await waitFor(() =>
            expect(requests).toEqual([
                { contentJson: JSON.stringify({ markdownBody: "Nowa treść" }), version: 3 },
            ])
        );
        expect(await screen.findByRole("status")).toHaveTextContent("Treść została zapisana");
    });

    it("tworzy wpis z aktualnym tytułem i treścią przez górny przycisk", async () => {
        const edition = {
            id: "edition-1",
            name: "Test",
            state: "ACTIVE",
            teamRegisterOpen: "2026-01-01T00:00:00Z",
            teamRegisterClose: "2027-01-01T00:00:00Z",
            version: 1,
        };
        const requests: unknown[] = [];
        server.use(
            http.get(`${apiUrl}${apiEndpoints.posts}`, () => HttpResponse.json({ data: [] })),
            http.get(`${apiUrl}${apiEndpoints.editions}`, ({ request }) =>
                HttpResponse.json({
                    data: new URL(request.url).searchParams.getAll("States").includes("ACTIVE")
                        ? [edition]
                        : [],
                })
            ),
            http.post(`${apiUrl}${apiEndpoints.posts}`, async ({ request }) => {
                const body = (await request.json()) as Record<string, unknown>;
                requests.push(body);
                return HttpResponse.json({
                    data: {
                        ...body,
                        id: "post-1",
                        createdAt: "2026-01-01T00:00:00Z",
                        updatedAt: "2026-01-01T00:00:00Z",
                        version: 1,
                    },
                });
            })
        );
        const { user } = renderWithRouter(<EditPostScreen />);
        const input = await screen.findByRole("textbox", { name: "Treść Markdown" });
        const save = screen.getByRole("button", { name: "Utwórz wpis" });
        expect(save.compareDocumentPosition(input) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
        await user.type(screen.getByPlaceholderText("Tytuł wpisu"), "Przykładowy wpis");
        await user.type(input, "Treść testowa");
        await user.click(save);
        await waitFor(() =>
            expect(requests).toEqual([
                {
                    name: "Przykładowy wpis",
                    contentJson: JSON.stringify({ markdownBody: "Treść testowa" }),
                    editionId: "edition-1",
                    isVisible: true,
                    version: 0,
                },
            ])
        );
        expect(await screen.findByRole("status")).toHaveTextContent("Wpis został utworzony");
    });
});
