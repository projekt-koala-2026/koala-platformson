import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";
import styles from "./PanelScreen.module.css";

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

        await apiRequest("/api/admin/user/user", { id: user.id }, "DELETE", navigate);
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
            <AdminHeader navigate={navigate} />
            <div className={`container ${styles.container}`}>
                {users && isAdminEditor && (
                    <>
                        <h1>Panel administracyjny</h1>
                        <h1>Lista użytkowników</h1>
                        <ContentsListBox>
                            {users.map((item, idx) => (
                                <ContentsListTile key={"users-list" + idx}>
                                    <div className={styles.userRow}>
                                        <div className={styles.userInfo}>
                                            <span>{item.email}</span>
                                            <p>
                                                Role to:{" "}
                                                <span className={styles.rolesText}>
                                                    {item.roles.join(", ")}
                                                </span>
                                            </p>
                                        </div>

                                        <div className={styles.actions}>
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
                                                <label key={role} className={styles.roleLabel}>
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedRoles.includes(role)}
                                                        onChange={() => toggleRole(role)}
                                                    />
                                                    {role.toLowerCase()}
                                                </label>
                                            ))}

                                            <div className={styles.editActions}>
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
