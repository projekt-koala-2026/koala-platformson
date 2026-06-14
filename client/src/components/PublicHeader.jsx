import { useMemo } from "react";
import { useLoading } from "../contexts/LoadingContext";
import { apiRequest } from "../utils/apiFetcher";
import { isCaptain } from "../utils/authService";
import Hamburger from "./Hamburger";

const PublicHeader = ({ navigate }) => {
    const isCaptainUser = useMemo(() => isCaptain(), []);
    const { startLoading, stopLoading } = useLoading();
    const handleLogin = async () => {
        startLoading();

        await apiRequest(
            "/api/admin/user/create-account",
            {
                email: "captain@example.com",
                password: "Aaaaa11#",
                roles: ["CAPTAIN"],
            },
            "POST",
            navigate
        );

        await new Promise((resolve) => setTimeout(resolve, 500));

        const data = await apiRequest(
            "/api/admin/auth/session",
            { email: "captain@example.com", password: "Aaaaa11#" },
            "POST",
            navigate
        );

        if (data) {
            const state = {
                isAdmin: data.roles.includes("ADMIN"),
                isEditor: data.roles.includes("EDITOR"),
                isReviewer: data.roles.includes("REVIEWER"),
                isGuardian: data.roles.includes("GUARDIAN"),
                isCaptain: data.roles.includes("CAPTAIN"),
            };
            const id = data.id;
            localStorage.setItem("userRoles", JSON.stringify(state));
            localStorage.setItem("userId", id);
            navigate("/");
        }

        stopLoading();
    };

    return (
        <div
            style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                padding: "16px",
            }}
        >
            <h1>Koala</h1>
            <h2>
                <span style={{ color: "#458756" }}>KO</span>mbinatoryka{" "}
                <span style={{ color: "#458756" }}>A</span>lgorytmika{" "}
                <span style={{ color: "#458756" }}>L</span>ogik
                <span style={{ color: "#458756" }}>A</span>
            </h2>
            <h5>Wielkopolski konkurs grup szkolnych</h5>
            <Hamburger
                options={[
                    ["Aktualności", () => navigate("/")],
                    ["Zadania", () => navigate("/problems")],
                    ["Regulamin", () => navigate("/rules")],
                    ["Historia", () => navigate("/history")],
                    ["KOALicjA", () => navigate("/koalicja")],
                    ["Login tymczasowy", handleLogin],
                    ...(isCaptainUser ? [["Dla kapitana", () => navigate("/captain")]] : []),
                ]}
            />
        </div>
    );
};

export default PublicHeader;
