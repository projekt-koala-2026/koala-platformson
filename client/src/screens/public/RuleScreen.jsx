import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
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
            <PublicHeader navigate={navigate} />

            <div className="container" style={{ minWidth: "50%" }}>
                <h1>Regulamin</h1>
                <MarkdownRenderer content={rules.markdownBody} />
            </div>

            <PublicFooter />
        </>
    );
};

export default RuleScreen;
