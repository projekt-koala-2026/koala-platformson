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
                    ["Edycje", () => navigate("/admin/editions")],
                    ["Wpisy", () => navigate("/admin/posts")],
                    ["Historia", () => navigate("/admin/history")],
                    ["Regulamin", () => navigate("/admin/rules")],
                    ["Sponsorzy", () => navigate("/admin/sponsors")],
                    ["Koalicjanci", () => navigate("/admin/koalicjants")],
                    ["Zadania", () => navigate("/admin/problems")],
                    ["Szkoły", () => navigate("/admin/schools")],
                    ["Pliki", () => navigate("/admin/images")],
                    ["Drużyny", () => navigate("/admin/teams")],
                ]}
            />
        </header>
    );
};

export default AdminHeader;
