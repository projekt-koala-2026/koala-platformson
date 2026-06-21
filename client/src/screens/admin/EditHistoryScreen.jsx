import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const EditHistory = () => {
    const navigate = useNavigate();
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);
    const [markdownBody, setMarkdownBody] = useState("");
    const [loadingData, setLoadingData] = useState(true);

    const UpdateHistory = async (history) => {
        const apiData = { markdownBody: history };

        const data = await apiRequest(
            "/api/static-pages/history",
            { markdownBody: JSON.stringify(apiData) },
            "PUT",
            navigate
        );

        if (data) alert("Zapisano edycję historii");

        await new Promise((resolve) => setTimeout(resolve, 500));
    };

    useEffect(() => {
        if (!isAdminEditor) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/static-pages/history", null, "GET", navigate);
            if (data) {
                const dataJson = JSON.parse(data);
                setMarkdownBody(dataJson.markdownBody || "");
            }
            setLoadingData(false);
        };

        if (isAdminEditor) getData();
    }, [navigate]);

    return (
        <div className="container">
            <AdminHeader navigate={navigate} />
            {isAdminEditor && (
                <div className="container-near">
                    <div className="container">
                        <>
                            <h1>Historia konkursu</h1>
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
                                            UpdateHistory(text);
                                        }}
                                    />
                                )}
                                {loadingData && <p>Ładowanie danych...</p>}
                            </div>
                        </>
                    </div>
                    <div className="container">
                        <MarkdownRenderer content={markdownBody} />
                    </div>
                </div>
            )}
        </div>
    );
};

export default EditHistory;
