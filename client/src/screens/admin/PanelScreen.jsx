import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { apiRequest } from "../../utils/apiFetcher";

export default function PanelScreen() {
    const navigate = useNavigate();

    const handleLogout = async () => {
        const data = await apiRequest("/api/admin/auth/session", {}, "DELETE", navigate);
        navigate("/admin/login");
    };

    useEffect(() => {
        const getData = async () => {
            const result = await apiRequest("/api/admin/auth/users", null, "GET", navigate);
            console.log(result);
        };

        getData();
    }, []);

    return (
        <div className="container">
            <h1>Panel administracyjny</h1>
            <Button text={"Logout"} onClick={handleLogout} />
        </div>
    );
}
