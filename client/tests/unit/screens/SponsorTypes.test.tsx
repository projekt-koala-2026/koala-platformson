import { screen, waitFor, within } from "@testing-library/react";
import { HttpResponse, http } from "msw";
import { expect, it } from "vitest";
import PublicFooter from "../../../src/components/PublicFooter";
import EditSponsorInfo from "../../../src/screens/admin/EditSponsorInfoScreen";
import { adaptSponsor } from "../../../src/utils/apiAdapters";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

const sponsor = (id: string, type?: string, isVisible = true) => ({
    id,
    name: `Firma ${id}`,
    isVisible,
    version: 2,
    contentJson: JSON.stringify({
        version: "1",
        sponsor_type: type,
        sponsor_web_url: "https://example.org",
        sponsor_logo_image_url: "https://example.org/logo.svg",
        body_pl: "Opis firmy",
    }),
});

it("klasyfikuje stare wpisy i nieznany typ jako sponsorów", () => {
    expect(adaptSponsor(sponsor("a")).type).toBe("sponsor");
    expect(adaptSponsor(sponsor("a", "unknown")).type).toBe("sponsor");
    expect(adaptSponsor(sponsor("a", "organizer")).type).toBe("organizer");
});

it("dzieli stopkę na trzy grupy i pomija ukryte wpisy", async () => {
    server.use(
        http.get(`${apiUrl}${apiEndpoints.sponsors}`, () =>
            HttpResponse.json({
                data: [
                    sponsor("Organizator", "organizer"),
                    sponsor("Sponsor"),
                    sponsor("Patron", "patron"),
                    sponsor("Ukryty", "patron", false),
                ],
            })
        )
    );
    renderWithRouter(<PublicFooter />);
    for (const [title, name] of [
        ["Organizatorzy", "Firma Organizator"],
        ["Sponsorzy", "Firma Sponsor"],
        ["Patroni", "Firma Patron"],
    ]) {
        const region = await screen.findByRole("region", { name: title });
        expect(within(region).getByRole("img", { name })).toBeInTheDocument();
        expect(within(region).getAllByRole("link")).toHaveLength(1);
    }
    expect(screen.queryByRole("img", { name: "Firma Ukryty" })).not.toBeInTheDocument();
});

it("wczytuje i zapisuje zmianę typu w formularzu edycji", async () => {
    const existing = sponsor("Organizator", "organizer");
    let payload: Record<string, unknown> | undefined;
    server.use(
        http.get(`${apiUrl}${apiEndpoints.sponsors}`, () =>
            HttpResponse.json({ data: [existing] })
        ),
        http.put(`${apiUrl}${apiEndpoints.sponsors}/${existing.id}`, async ({ request }) => {
            payload = (await request.json()) as Record<string, unknown>;
            return HttpResponse.json({ data: { ...existing, ...payload, version: 3 } });
        })
    );
    const { user } = renderWithRouter(<EditSponsorInfo />);
    await user.click(await screen.findByRole("button", { name: /Edytuj/ }));
    const select = screen.getByRole("button", { name: "Typ wpisu" });
    expect(select).toHaveTextContent("Organizator");
    await user.click(select);
    await user.click(screen.getByRole("option", { name: "Patron" }));
    await user.click(screen.getByRole("button", { name: "Zapisz" }));
    await waitFor(() => expect(payload).toBeDefined());
    expect(JSON.parse(payload!.contentJson as string).sponsor_type).toBe("patron");
    expect(payload!.version).toBe(2);
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(screen.getByText("Patron")).toBeInTheDocument();
});
