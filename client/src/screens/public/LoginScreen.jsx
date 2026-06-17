import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import PublicHeader from "../../components/PublicHeader";
import { useLoading } from "../../contexts/LoadingContext";
import useKeyboardShortcuts from "../../hooks/UseKeyboardShortcuts";
import { apiRequest } from "../../utils/apiFetcher";

const LoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [password, setPassword] = useState("");
    const [passwordRegister, setPasswordRegister] = useState("");
    const [email, setEmail] = useState("");
    const [emailRegister, setEmailRegister] = useState("");
    const [register, setRegister] = useState(false);
    const [roles, setRoles] = useState([]);
    const rolesList = ["CAPTAIN"];

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
            console.log(data);
            const state = {
                isAdmin: data.roles.includes("ADMIN"),
                isEditor: data.roles.includes("EDITOR"),
                isReviewer: data.roles.includes("REVIEWER"),
                isGuardian: data.roles.includes("GUARDIAN"),
                isCaptain: data.roles.includes("CAPTAIN"),
            };
            const id = data.id;
            localStorage.setItem("userRoles", JSON.stringify(state));
            localStorage.setItem("userId", id);
            if (
                data.roles.includes("ADMIN") ||
                data.roles.includes("EDITOR") ||
                data.roles.includes("REVIEWER")
            ) {
                navigate("/admin");
            }
            if (data.roles.includes("GUARDIAN") || data.roles.includes("CAPTAIN")) {
                navigate("/");
            }
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

    const handleRegister = async () => {
        startLoading();

        const data = await apiRequest(
            "/api/admin/user/create-account",
            { email: emailRegister, password: passwordRegister, roles: roles },
            "POST",
            navigate
        );
        await new Promise((resolve) => setTimeout(resolve, 500));
        if (data) {
            alert("Zarejestrowno :)\nZapraszamy do zalogowania");
            setEmailRegister("");
            setPasswordRegister("");
            setRoles([]);
            setRegister(false);
            navigate("/login");
        }
        stopLoading();
    };

    useKeyboardShortcuts(shortcuts);

    return (
        <>
            <PublicHeader navigate={navigate} />
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
                <Button text={"Nie masz konta?"} onClick={() => setRegister(true)} />
                {register === true && (
                    <>
                        <h1>Rejestracja</h1>

                        <input
                            type="text"
                            placeholder="Email"
                            value={emailRegister}
                            onChange={(e) => setEmailRegister(e.target.value)}
                            required
                        />
                        <input
                            type="password"
                            placeholder="Hasło"
                            value={passwordRegister}
                            onChange={(e) => setPasswordRegister(e.target.value)}
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
                        <h3>Wymagania hasła</h3>

                        <ul>
                            <li>Minimum 8 znaków</li>
                            <li>Przynajmniej jedna wielka litera</li>
                            <li>Przynajmniej jedna mała litera</li>
                            <li>Przynajmniej jedna cyfra</li>
                            <li>Przynajmniej jeden znak specjalny (!@#$%^&*)</li>
                        </ul>
                        <Button text={"Zarejestruj"} onClick={handleRegister} />
                    </>
                )}
            </div>
        </>
    );
};

export default LoginScreen;
