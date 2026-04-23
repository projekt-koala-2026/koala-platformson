import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import useKeyboardShortcuts from "../../hooks/UseKeyboardShortcuts";
import { apiRequest } from "../../utils/apiFetcher";

const LoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [password, setPassword] = useState("");
    const [email, setEmail] = useState("");

    const handleLogin = async () => {
        const data = await apiRequest(
            "/api/admin/auth/session",
            { email: email, password: password },
            "POST",
            navigate
        );
        startLoading();
        await new Promise((resolve) => setTimeout(resolve, 500));
        if (data) {
            const state = {
                isAdmin: data.roles.includes("ADMIN"),
                isEditor: data.roles.includes("EDITOR"),
                isReviewer: data.roles.includes("REVIEWER"),
                isGuardian: data.roles.includes("GUARDIAN"),
                isCaptain: data.roles.includes("CAPTAIN"),
            };
            localStorage.setItem("userRoles", JSON.stringify(state));
            navigate("/admin");
        }
        stopLoading();
    };

    const shortcuts = useMemo(() => [
        { Enter: handleLogin },
        {
            ArrowUp: () => {
                setEmail("admin");
                setPassword("admin");
            },
        },
    ]);

    useKeyboardShortcuts(shortcuts);

    return (
        <div className="container">
            <h1>Logowanie</h1>
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
            <Button text={"Zaloguj"} onClick={handleLogin} />
        </div>
    );
};

export default LoginScreen;
