import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import ProfileButton from "../../components/ProfileButton";
import { apiRequest } from "../../utils/apiFetcher";

const PanelScreen = () => {
    const navigate = useNavigate();
    const [users, setUsers] = useState(null);

    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        navigate("/admin/login");
    };

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/api/admin/auth/users", null, "GET", navigate);
            console.log(data);
            setUsers(data);
        };

        getData();
    }, [navigate]);

    return (
        <div className="container" style={{ minWidth: "50%" }}>
            <ProfileButton options={[["Logout", handleLogout]]} />
            <h1>Panel administracyjny</h1>
            <h1>Lista użytkowników</h1>
            {users && (
                <ContentsListBox>
                    {users.map((item, idx) => (
                        <ContentsListTile key={"users-list" + idx}>
                            <span>{item.email}</span>
                        </ContentsListTile>
                    ))}
                </ContentsListBox>
            )}
        </div>
    );
};

export default PanelScreen;
