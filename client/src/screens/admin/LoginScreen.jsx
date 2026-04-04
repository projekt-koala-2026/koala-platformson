import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";

export default function LoginScreen({}) {
    const navigate = useNavigate();

    const handleLogin = () => {
        navigate("/admin");
    };

    return (
        <div className="container">
            <h1>Logowanie</h1>
            <input type="text" placeholder="Email" required />
            <input type="password" placeholder="Hasło" required />
            <Button text={"Zaloguj"} onClick={handleLogin} />
        </div>
    );
}
