import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import Hamburger from "../../components/Hamburger";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import ProfileButton from "../../components/ProfileButton";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const PanelScreen = () => {
    const navigate = useNavigate();
    const [users, setUsers] = useState(null);
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);
    const [posts, setPosts] = useState([]);

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
            <Hamburger options={[["Dodaj plik", () => navigate("/admin/images")]]} />
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

            {posts.map((text, idx) => (
                <MarkdownRenderer key={idx} content={text} />
            ))}
            <MarkdownEditor onSave={(text) => setPosts((prev) => [...prev, text])} />
        </div>
    );
};

export default PanelScreen;
