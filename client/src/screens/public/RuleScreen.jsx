import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Hamburger from "../../components/Hamburger";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest } from "../../utils/apiFetcher";

const RuleScreen = () => {
    const navigate = useNavigate();
    const [rules, setRules] = useState([]);

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/content/rules/rules.json", null, "GET", navigate);
            if (data) setRules(data);
        };

        getData();
    }, [navigate]);
    return (
        <>
            <header style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
                <h1>Koala</h1>
                <h2>
                    <span style={{ color: "#458756" }}>KO</span>mbinatoryka{" "}
                    <span style={{ color: "#458756" }}>A</span>lgorytmika{" "}
                    <span style={{ color: "#458756" }}>L</span>ogik
                    <span style={{ color: "#458756" }}>A</span>
                </h2>
                <h5>Wielkopolski konkurs grup szkolnych</h5>
                <Hamburger
                    options={[
                        ["Aktualności", () => navigate("/")],
                        ["Zadania", () => navigate("/problems")],
                        ["Regulamin", () => navigate("/rules")],
                        ["Historia", () => navigate("/history")],
                        ["KOALicjA", () => navigate("/koalicja")],
                    ]}
                />
            </header>

            <div className="container" style={{ minWidth: "50%" }}>
                <h1>Regulamin</h1>
                <MarkdownRenderer content={rules.markdownBody} />
            </div>
        </>
    );
};

export default RuleScreen;
