import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import Hamburger from "../../components/Hamburger";
import ProfileButton from "../../components/ProfileButton";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const PanelScreen = () => {
    const navigate = useNavigate();
    const [users, setUsers] = useState(null);
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [editingUser, setEditingUser] = useState(null);
    const [selectedRoles, setSelectedRoles] = useState([]);

    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        if (data) {
            localStorage.removeItem("userRoles");
            localStorage.removeItem("userId");
            navigate("/admin/login");
        }
    };

    const AddUser = async () => {
        navigate("/admin/adduser");
    };

    const DeleteUser = async (user) => {
        const confirmed = window.confirm(`Czy na pewno chcesz usunąć użytkownika ${user.email}?`);

        if (!confirmed) return;

        const data = await apiRequest("/api/admin/user/user", { id: user.id }, "DELETE", navigate);
        setUsers((prev) => prev.filter((u) => u.id !== user.id));
    };

    const EditUser = (user) => {
        setEditingUser(editingUser === user ? null : user);
        setSelectedRoles(user.roles || []);
    };

    const toggleRole = (role) => {
        setSelectedRoles((prev) =>
            prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
        );
    };

    const saveRoles = async () => {
        await apiRequest(
            "/api/admin/user/roles",
            { id: editingUser.id, newRoles: selectedRoles },
            "PUT",
            navigate
        );

        setUsers((prev) =>
            prev.map((u) => (u.id === editingUser.id ? { ...u, roles: selectedRoles } : u))
        );

        setEditingUser(null);
    };

    const ChangePassword = async () => {
        navigate("/admin/changepass");
    };

    useEffect(() => {
        if (!isAdminEditor) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/admin/user/users", null, "GET", navigate);
            setUsers(data);
        };

        if (isAdminEditor) getData();
    }, [navigate]);

    return (
        <>
            <header>
                <h1>Panel administracyjny</h1>
                <ProfileButton
                    options={[
                        ["Logout", handleLogout],
                        ["Zmień Hasło", ChangePassword],
                    ]}
                />
                <Hamburger
                    options={[
                        ["Zarządzanie plikami", () => navigate("/admin/images")],

                        ...(isAdminUser
                            ? [
                                  ["Zarządzanie Sponsorami", () => navigate("/admin/sponsors")],
                                  [
                                      "Zarządzanie Koalicjantami",
                                      () => navigate("/admin/koalicjants"),
                                  ],
                                  ["Posty", () => navigate("/admin/posts")],
                                  ["Regulamin", () => navigate("/admin/rules")],
                                  ["Zarządzanie Edycjami", () => navigate("/admin/editions")],
                              ]
                            : []),
                    ]}
                />
            </header>

            <div className="container" style={{ minWidth: "50%" }}>
                {users && isAdminEditor && (
                    <>
                        <h1>Lista użytkowników</h1>
                        <ContentsListBox>
                            {users.map((item, idx) => (
                                <ContentsListTile key={"users-list" + idx}>
                                    <div style={{ display: "flex" }}>
                                        <div style={{ display: "flex", flexDirection: "column" }}>
                                            <span>{item.email}</span>
                                            <p>
                                                Role to:{" "}
                                                <span style={{ textTransform: "lowercase" }}>
                                                    {item.roles.join(", ")}
                                                </span>
                                            </p>
                                        </div>

                                        <div
                                            style={{
                                                display: "flex",
                                                gap: "8px",
                                                marginLeft: "auto",
                                            }}
                                        >
                                            <Button
                                                text={<FaEdit />}
                                                onClick={() => EditUser(item)}
                                            />
                                            <Button
                                                text={<FaTrash />}
                                                onClick={() => DeleteUser(item)}
                                            />
                                        </div>
                                    </div>
                                    {editingUser?.id === item.id && (
                                        <div>
                                            <h3>Edytuj Rolę dla {editingUser.email}</h3>

                                            {["ADMIN", "EDITOR", "REVIEWER"].map((role) => (
                                                <label key={role} style={{ display: "block" }}>
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedRoles.includes(role)}
                                                        onChange={() => toggleRole(role)}
                                                    />
                                                    {role.toLowerCase()}
                                                </label>
                                            ))}

                                            <div
                                                style={{
                                                    display: "flex",
                                                    gap: "10px",
                                                    marginTop: "10px",
                                                }}
                                            >
                                                <Button text={"Zapisz"} onClick={saveRoles} />
                                                <Button
                                                    text={"Anuluj"}
                                                    onClick={() => setEditingUser(null)}
                                                />
                                            </div>
                                        </div>
                                    )}
                                </ContentsListTile>
                            ))}
                        </ContentsListBox>
                        {users && isAdminUser && (
                            <Button text={"Dodaj nowego użytkownika"} onClick={AddUser} />
                        )}
                    </>
                )}
            </div>
        </>
    );
};

export default PanelScreen;
