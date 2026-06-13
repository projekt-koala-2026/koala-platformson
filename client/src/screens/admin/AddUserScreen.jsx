import { useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import { apiRequest } from "../../utils/apiFetcher";

const AddUserScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [roles, setRoles] = useState([]);
    const rolesList = ["ADMIN", "EDITOR", "REVIEWER"];

    const handleAddUser = async () => {
        startLoading();

        const data = await apiRequest(
            "/api/admin/user/user",
            { email: email, password: password, roles: roles },
            "POST",
            navigate
        );
        await new Promise((resolve) => setTimeout(resolve, 500));
        if (data) {
            navigate("/admin");
        }
        stopLoading();
    };

    const handleBack = () => {
        navigate("/admin");
    };

    return (
        <div className="container-near">
            <div className="container">
                <h1>Dodaj Użytkownika</h1>
                <input
                    type="text"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                />
                <input
                    type="password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                />
                <div className="roles-container">
                    {rolesList.map((role) => (
                        <label key={role} className="role-checkbox">
                            <input
                                type="checkbox"
                                checked={roles.includes(role)}
                                onChange={() => {
                                    if (roles.includes(role)) {
                                        setRoles(roles.filter((r) => r !== role));
                                    } else {
                                        setRoles([...roles, role]);
                                    }
                                }}
                            />
                            {role}
                        </label>
                    ))}
                </div>
                <p>
                    Wybrane role dla {email}: {roles.join(", ")}
                </p>
                <Button text={"Dodaj Użytkownika"} onClick={handleAddUser} />
                <Button text={"Wróć do panelu"} onClick={handleBack} />
            </div>
            <div className="container">
                <h1>Wymagania hasła</h1>
                <ul>
                    <li>Minimum 8 znaków</li>
                    <li>Przynajmniej jedna wielka litera</li>
                    <li>Przynajmniej jedna mała litera</li>
                    <li>Przynajmniej jedna cyfra</li>
                    <li>Przynajmniej jeden znak specjalny (!@#$%^&*)</li>
                </ul>
            </div>
        </div>
    );
};

export default AddUserScreen;
