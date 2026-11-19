import { screen } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import ProtectedRoute from "../../../src/components/ProtectedRoute";
import { renderWithRouter } from "../../helpers/renderWithRouter";

const renderProtectedPage = () =>
    renderWithRouter(
        <Routes>
            <Route
                path="/admin"
                element={
                    <ProtectedRoute allowedRoles={["ORGANIZATION_ADMIN"]} redirectTo="/admin/login">
                        <h1>Panel administratora</h1>
                    </ProtectedRoute>
                }
            />
            <Route path="/admin/login" element={<h1>Logowanie administratora</h1>} />
        </Routes>,
        { initialEntries: ["/admin"] }
    );

describe("ProtectedRoute", () => {
    it("renders protected content for a user with an allowed role", () => {
        localStorage.setItem("userRoles", JSON.stringify({ isAdmin: true }));

        renderProtectedPage();

        expect(screen.getByRole("heading", { name: "Panel administratora" })).toBeInTheDocument();
    });

    it("redirects a user without an allowed role", () => {
        localStorage.setItem("userRoles", JSON.stringify({ isCaptain: true }));

        renderProtectedPage();

        expect(
            screen.getByRole("heading", { name: "Logowanie administratora" })
        ).toBeInTheDocument();
        expect(screen.queryByRole("heading", { name: "Panel administratora" })).toBeNull();
    });
});
