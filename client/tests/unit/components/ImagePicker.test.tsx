import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HttpResponse, http } from "msw";
import { describe, expect, it, vi } from "vitest";
import ImagePicker from "../../../src/components/ImagePicker";
import type { ApiPublicFile } from "../../../src/types/models";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { server } from "../../mocks/server";

const filesUrl = `${apiUrl}${apiEndpoints.publicFiles}`;
const file = (id: string, name: string, type: string, path = `images/${id}`): ApiPublicFile => ({
    id,
    name,
    type,
    path,
    createdAt: "2026-01-01T12:00:00Z",
    version: 1,
});

describe("ImagePicker", () => {
    it("loads, filters and sorts images before returning the selected file", async () => {
        const onSelect = vi.fn();
        const user = userEvent.setup();
        server.use(
            http.get(filesUrl, ({ request }) => {
                const url = new URL(request.url);
                expect(url.searchParams.get("PageNumber")).toBe("0");
                expect(url.searchParams.get("PageSize")).toBe("100");
                return HttpResponse.json({
                    data: [
                        file("z", "Zebra", ".PNG"),
                        file("pdf", "Regulamin", ".pdf"),
                        file("a", "Alfa", ".jpg"),
                    ],
                });
            })
        );

        render(<ImagePicker onSelect={onSelect} />);

        expect(screen.getByRole("status")).toHaveTextContent("Ładowanie obrazów");
        const imageButtons = await screen.findAllByRole("button", { name: /Wybierz obraz/ });
        expect(imageButtons.map((button) => button.getAttribute("aria-label"))).toEqual([
            "Wybierz obraz Alfa",
            "Wybierz obraz Zebra",
        ]);
        expect(screen.queryByRole("button", { name: /Regulamin/ })).not.toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Wybierz obraz Alfa" }));
        expect(onSelect).toHaveBeenCalledWith({
            id: "a",
            title: "Alfa",
            filePath: "/api/koala/content/images/a",
        });
    });

    it("uses delete semantics and blocks every option while an image is busy", async () => {
        server.use(
            http.get(filesUrl, () =>
                HttpResponse.json({
                    data: [file("a", "Alfa", ".jpg"), file("b", "Beta", ".webp")],
                })
            )
        );

        render(<ImagePicker mode="manage" busyId="a" onSelect={vi.fn()} />);

        const alfa = await screen.findByRole("button", { name: "Usuń obraz Alfa" });
        const beta = screen.getByRole("button", { name: "Usuń obraz Beta" });
        expect(alfa).toBeDisabled();
        expect(beta).toBeDisabled();
        expect(alfa).toHaveTextContent("Usuwanie…");
        expect(beta).toHaveTextContent("Beta");
    });

    it("shows a recoverable error and retries the failed request", async () => {
        let requestCount = 0;
        const user = userEvent.setup();
        server.use(
            http.get(filesUrl, () => {
                requestCount += 1;
                if (requestCount === 1) return new HttpResponse(null, { status: 500 });
                return HttpResponse.json({ data: [file("a", "Alfa", ".gif")] });
            })
        );

        render(<ImagePicker onSelect={vi.fn()} />);

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Nie udało się pobrać biblioteki obrazów."
        );
        await user.click(screen.getByRole("button", { name: "Spróbuj ponownie" }));

        expect(await screen.findByRole("button", { name: "Wybierz obraz Alfa" })).toBeEnabled();
        await waitFor(() => expect(requestCount).toBe(2));
    });

    it("renders an empty state when the response contains no supported images", async () => {
        server.use(
            http.get(filesUrl, () =>
                HttpResponse.json({ data: [file("pdf", "Regulamin", ".pdf")] })
            )
        );

        render(<ImagePicker onSelect={vi.fn()} />);

        expect(await screen.findByText("Brak obrazów w bibliotece.")).toBeInTheDocument();
    });
});
