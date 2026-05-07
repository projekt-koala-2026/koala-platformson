import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import useKeyboardShortcuts from "../../hooks/UseKeyboardShortcuts";
import { apiRequest } from "../../utils/apiFetcher";

const ChangePassScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [id, setId] = useState("");
    const [oldPassword, setOldPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");

    const handleSubmit = async () => {
        startLoading();

        const user = JSON.parse(localStorage.getItem("user"));

        const data = await apiRequest(
            "/api/admin/user/password",
            { id: localStorage.getItem("userId"), password: oldPassword, newPassword: newPassword },
            "PUT",
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
                <h1>Zmiana Hasła</h1>

                <input
                    type="password"
                    placeholder="Stare Hasło"
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    required
                />

                <input
                    type="password"
                    placeholder="Nowe Hasło"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                />

                <Button text={"Potwierdź zmianę Hasła"} onClick={handleSubmit} />
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

export default ChangePassScreen;