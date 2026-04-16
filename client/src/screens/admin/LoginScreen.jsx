import { useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import { apiRequest } from "../../utils/apiFetcher";

export default function LoginScreen({}) {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [password, setPassword] = useState("");
    const [email, setEmail] = useState("");

    const handleLogin = async () => {
        const data = await apiRequest(
            "/api/admin/auth/session",
            { email: email, password: password, roles: null },
            "POST",
            navigate
        );
        startLoading();
        await new Promise((resolve) => setTimeout(resolve, 500));
        if (data) {
            navigate("/admin");
        }
        stopLoading();
    };

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
}
