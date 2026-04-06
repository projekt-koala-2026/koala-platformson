import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";

export default function LoginScreen({}) {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();

    const handleLogin = async () => {
        startLoading();
        await new Promise((resolve) => setTimeout(resolve, 800));
        navigate("/admin");
        stopLoading();
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
