import { useNavigate } from "react-router-dom";
import Hamburger from "../../components/Hamburger";

const HomeScreen = () => {
    const navigate = useNavigate();
    return (
        <>
            <header>
                <h1>Koala</h1>
                <Hamburger options={[["Zadania", () => navigate("/problems")]]} />
            </header>

            <div className="container" style={{ minWidth: "50%" }}></div>
        </>
    );
};

export default HomeScreen;
