import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import ProfileButton from "../../components/ProfileButton";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const PanelScreen = () => {
    const navigate = useNavigate();
    const [users, setUsers] = useState(null);
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);

    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        if (data) {
            localStorage.removeItem("userRoles");
            navigate("/admin/login");
        }
    };

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/api/admin/user/users", null, "GET", navigate);
            setUsers(data);
        };

        if (isAdminEditor) getData();
    }, [navigate]);

    return (
        <div className="container" style={{ minWidth: "50%" }}>
            <ProfileButton options={[["Logout", handleLogout]]} />
            <h1>Panel administracyjny</h1>
            {users && isAdminEditor && (
                <>
                    <h1>Lista użytkowników</h1>
                    <ContentsListBox>
                        {users.map((item, idx) => (
                            <ContentsListTile key={"users-list" + idx}>
                                <span>{item.email}</span>
                            </ContentsListTile>
                        ))}
                    </ContentsListBox>
                </>
            )}
        </div>
    );
};

export default PanelScreen;
