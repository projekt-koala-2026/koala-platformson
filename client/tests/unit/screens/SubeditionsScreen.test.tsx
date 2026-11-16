import { screen, waitFor } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import SubeditionsScreen from "../../../src/screens/admin/SubeditionsScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

vi.mock("../../../src/components/AdminHeader", () => ({ default: () => null }));

const edition = { id: "edition-1", name: "Edycja testowa", state: "ACTIVE" };
const subedition = {
    id: "sub-1",
    editionId: edition.id,
    name: "Etap szkolny",
    state: "INACTIVE",
    answerOpen: "2026-03-01T10:00:00Z",
    answerClose: "2026-03-03T10:00:00Z",
    version: 3,
};
const editionsUrl = `${apiUrl}${apiEndpoints.editions}`;
const subsUrl = `${apiUrl}${apiEndpoints.subeditions(edition.id)}`;

describe("zarządzanie podedycjami", () => {
    it("tworzy podedycję z terminami i pokazuje wynik", async () => {
        const requests: unknown[] = [];
        server.use(
            http.get(editionsUrl, () => HttpResponse.json({ data: [edition] })),
            http.get(subsUrl, () => HttpResponse.json({ data: [] })),
            http.post(subsUrl, async ({ request }) => {
                const body = await request.json();
                requests.push(body);
                return HttpResponse.json({ data: { ...subedition, ...(body as object) } });
            })
        );
        const { user } = renderWithRouter(<SubeditionsScreen />);
        await user.click(await screen.findByRole("button", { name: "Dodaj podedycję" }));
        await user.type(screen.getByLabelText("Nazwa"), "Etap szkolny");
        await user.type(screen.getByLabelText("Początek odpowiadania"), "2026-03-01T10:00");
        await user.type(screen.getByLabelText("Koniec odpowiadania"), "2026-03-03T10:00");
        await user.click(screen.getByRole("button", { name: "Zapisz" }));
        await waitFor(() => expect(requests).toHaveLength(1));
        expect(requests[0]).toEqual({
            name: "Etap szkolny",
            answerOpen: new Date("2026-03-01T10:00").toISOString(),
            answerClose: new Date("2026-03-03T10:00").toISOString(),
        });
        expect(await screen.findByText("Etap szkolny")).toBeInTheDocument();
    });

    it("nie wysyła niepoprawnego okresu", async () => {
        const post = vi.fn();
        server.use(
            http.get(editionsUrl, () => HttpResponse.json({ data: [edition] })),
            http.get(subsUrl, () => HttpResponse.json({ data: [] })),
            http.post(subsUrl, post)
        );
        const { user } = renderWithRouter(<SubeditionsScreen />);
        await user.click(await screen.findByRole("button", { name: "Dodaj podedycję" }));
        await user.type(screen.getByLabelText("Nazwa"), "Test");
        await user.type(screen.getByLabelText("Początek odpowiadania"), "2026-03-03T10:00");
        await user.type(screen.getByLabelText("Koniec odpowiadania"), "2026-03-01T10:00");
        await user.click(screen.getByRole("button", { name: "Zapisz" }));
        expect(screen.getByRole("alert")).toHaveTextContent("poprawny okres");
        expect(post).not.toHaveBeenCalled();
    });

    it("aktualizuje wersję edytowanej podedycji", async () => {
        const put = vi.fn(async ({ request }: { request: Request }) => {
            const body = await request.json();
            return HttpResponse.json({ data: { ...subedition, ...(body as object), version: 4 } });
        });
        server.use(
            http.get(editionsUrl, () => HttpResponse.json({ data: [edition] })),
            http.get(subsUrl, () => HttpResponse.json({ data: [subedition] })),
            http.put(`${subsUrl}/${subedition.id}`, put)
        );
        const { user } = renderWithRouter(<SubeditionsScreen />);
        await user.click(await screen.findByRole("button", { name: "Edytuj" }));
        const name = screen.getByLabelText("Nazwa");
        await user.clear(name);
        await user.type(name, "Nowa nazwa");
        await user.click(screen.getByRole("button", { name: "Zapisz" }));
        expect(await screen.findByText("Nowa nazwa")).toBeInTheDocument();
        expect(put).toHaveBeenCalledOnce();
    });
});
