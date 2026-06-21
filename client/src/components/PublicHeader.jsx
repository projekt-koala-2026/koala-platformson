import { useMemo } from "react";
import { useLoading } from "../contexts/LoadingContext";
import { apiRequest } from "../utils/apiFetcher";
import { isCaptain } from "../utils/authService";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";

const PublicHeader = ({ navigate }) => {
    const isCaptainUser = useMemo(() => isCaptain(), []);
    const { startLoading, stopLoading } = useLoading();
    const isLoggedIn = useMemo(() => !!localStorage.getItem("userId"), []);

    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        if (data) {
            localStorage.removeItem("userRoles");
            localStorage.removeItem("userId");
            navigate("/login");
        }
    };

    const ChangePassword = async () => {
        navigate("/changepass");
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
            {isLoggedIn && (
                <ProfileButton
                    options={[
                        ["Logout", handleLogout],
                        ["Zmień Hasło", ChangePassword],
                    ]}
                />
            )}
            <Hamburger
                options={[
                    ["Aktualności", () => navigate("/")],
                    ["Zadania", () => navigate("/problems")],
                    ["Regulamin", () => navigate("/rules")],
                    ["Historia", () => navigate("/history")],
                    ["KOALicjA", () => navigate("/koalicja")],
                    ...(!isLoggedIn ? [["Zaloguj", () => navigate("/login")]] : []),
                    ...(isCaptainUser ? [["Dla kapitana", () => navigate("/captain")]] : []),
                ]}
            />
        </div>
    );
};

export default PublicHeader;
