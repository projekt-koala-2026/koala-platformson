import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HttpResponse, http } from "msw";
import { describe, expect, it, vi } from "vitest";
import ChangePasswordModal from "../../../src/components/ChangePasswordModal";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

const userUrl = `${apiUrl}${apiEndpoints.users}/user-1`;
const resetUrl = `${apiUrl}${apiEndpoints.links}/reset-password`;

const openModal = (onClose = vi.fn()) => {
    renderWithRouter(<ChangePasswordModal isOpen onClose={onClose} />);
    return { onClose, user: userEvent.setup() };
};

describe("ChangePasswordModal", () => {
    it("reports an expired session without making an API request", async () => {
        const { user } = openModal();

        await user.click(screen.getByRole("button", { name: "Wyślij link" }));

        expect(screen.getByRole("alert")).toHaveTextContent("Sesja wygasła. Zaloguj się ponownie.");
    });

    it("loads the account email and sends a reset link", async () => {
        localStorage.setItem("userId", "user-1");
        const { user } = openModal();
        server.use(
            http.get(userUrl, () =>
                HttpResponse.json({
                    data: {
                        id: "user-1",
                        email: "kapitan@example.com",
                        roles: ["TEAM_PLAYER"],
                    },
                })
            ),
            http.post(resetUrl, async ({ request }) => {
                expect(await request.json()).toEqual({ email: "kapitan@example.com" });
                return HttpResponse.json({ data: null }, { status: 201 });
            })
        );

        await user.click(screen.getByRole("button", { name: "Wyślij link" }));

        expect(await screen.findByRole("status")).toHaveTextContent(
            "Wysłaliśmy wiadomość z bezpiecznym linkiem"
        );
        expect(screen.queryByRole("button", { name: "Wyślij link" })).not.toBeInTheDocument();
        expect(screen.getAllByRole("button", { name: "Zamknij" })).toHaveLength(2);
        expect(screen.getAllByRole("button", { name: "Zamknij" })[1]).toBeEnabled();
    });

    it("reports missing account data and a failed reset request", async () => {
        localStorage.setItem("userId", "user-1");
        let userRequestCount = 0;
        const { user } = openModal();
        server.use(
            http.get(userUrl, () => {
                userRequestCount += 1;
                return userRequestCount === 1
                    ? HttpResponse.json({ data: { id: "user-1", roles: ["TEAM_PLAYER"] } })
                    : HttpResponse.json({
                          data: {
                              id: "user-1",
                              email: "kapitan@example.com",
                              roles: ["TEAM_PLAYER"],
                          },
                      });
            }),
            http.post(resetUrl, () => new HttpResponse(null, { status: 500 }))
        );

        await user.click(screen.getByRole("button", { name: "Wyślij link" }));
        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Nie udało się odczytać adresu e-mail konta."
        );

        await user.click(screen.getByRole("button", { name: "Wyślij link" }));
        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Nie udało się wysłać wiadomości resetującej hasło."
        );
        expect(userRequestCount).toBe(2);
    });

    it("disables dismissal and duplicate submission while a request is pending", async () => {
        localStorage.setItem("userId", "user-1");
        let releaseRequest!: () => void;
        const requestGate = new Promise<void>((resolve) => {
            releaseRequest = resolve;
        });
        const { onClose, user } = openModal();
        server.use(
            http.get(userUrl, async () => {
                await requestGate;
                return HttpResponse.json({
                    data: {
                        id: "user-1",
                        email: "kapitan@example.com",
                        roles: ["TEAM_PLAYER"],
                    },
                });
            }),
            http.post(resetUrl, () => HttpResponse.json({ data: null }, { status: 201 }))
        );

        await user.click(screen.getByRole("button", { name: "Wyślij link" }));

        expect(await screen.findByRole("button", { name: "Wysyłanie…" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Anuluj" })).toBeDisabled();
        await user.keyboard("{Escape}");
        expect(onClose).not.toHaveBeenCalled();

        await act(async () => releaseRequest());
        expect(await screen.findByRole("status")).toHaveTextContent("Wysłaliśmy wiadomość");
    });
});
