import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

const EditRule = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [markdownBody, setMarkdownBody] = useState("");
    const [loadingData, setLoadingData] = useState(true);

    const AddNewRules = async (rules) => {
        const apiData = { markdownBody: rules };

        const data = await apiRequest(
            "/api/static-pages/rules",
            { markdownBody: JSON.stringify(apiData) },
            "PUT",
            navigate
        );

        if (data) alert("Zapisano nowy regulamin");

        await new Promise((resolve) => setTimeout(resolve, 500));
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/static-pages/rules", null, "GET", navigate);
            if (data) {
                const dataJson = JSON.parse(data);
                setMarkdownBody(dataJson.markdownBody || "");
            }
            setLoadingData(false);
        };

        if (isAdminUser) getData();
    }, [navigate]);

    return (
        <div className="container">
            <AdminHeader navigate={navigate} />
            {isAdminUser && (
                <div className="container-near">
                    <div className="container">
                        <h1>Regulamin</h1>
                        <div
                            style={{
                                whiteSpace: "pre-wrap",
                                wordBreak: "break-word",
                                maxWidth: "600px",
                            }}
                        >
                            {!loadingData && (
                                <MarkdownEditor
                                    initialValue={markdownBody}
                                    onChange={setMarkdownBody}
                                    onSave={(text) => {
                                        AddNewRules(text);
                                    }}
                                />
                            )}
                            {loadingData && <p>Ładowanie danych...</p>}
                        </div>
                    </div>
                    <div className="container">
                        <MarkdownRenderer content={markdownBody} />
                    </div>
                </div>
            )}
        </div>
    );
};

export default EditRule;
