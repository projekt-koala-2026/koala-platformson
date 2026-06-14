import { apiRequest } from "../utils/apiFetcher";
import Hamburger from "./Hamburger";
import ProfileButton from "./ProfileButton";

const AdminHeader = ({ navigate }) => {
    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        if (data) {
            localStorage.removeItem("userRoles");
            localStorage.removeItem("userId");
            navigate("/admin/login");
        }
    };

    const ChangePassword = async () => {
        navigate("/admin/changepass");
    };

    return (
        <header>
            <ProfileButton
                options={[
                    ["Logout", handleLogout],
                    ["Zmień Hasło", ChangePassword],
                ]}
            />
            <Hamburger
                options={[
                    ["Strona główna", () => navigate("/admin")],
                    ["Posty", () => navigate("/admin/posts")],
                    ["Historia", () => navigate("/admin/history")],
                    ["Regulamin", () => navigate("/admin/rules")],
                    ["Zarządzanie Sponsorami", () => navigate("/admin/sponsors")],
                    ["Zarządzanie Koalicjantami", () => navigate("/admin/koalicjants")],
                    ["Zarządzanie Edycjami", () => navigate("/admin/editions")],
                    ["Zarządzanie Zadaniami", () => navigate("/admin/problems")],
                    ["Zarządzanie Szkołami", () => navigate("/admin/schools")],
                    ["Zarządzanie plikami", () => navigate("/admin/images")],
                    ["Zarządzanie drużynami", () => navigate("/admin/teams")],
                ]}
            />
        </header>
    );
};

export default AdminHeader;
