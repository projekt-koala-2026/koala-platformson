import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

const EditRule = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [markdownBody, setMarkdownBody] = useState("");

    const handleBack = () => {
        navigate("/admin");
    };

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
            const data_json = JSON.parse(data);

            setMarkdownBody(data_json.markdownBody);
        };

        if (isAdminUser) getData();
    }, [navigate]);

    return (
        <div className="container">
            {isAdminUser && (
                <>
                    <h1>Regulamin</h1>
                    <div>
                        <Button text={"Wróć do panelu"} onClick={handleBack} />
                        <MarkdownEditor
                            key={markdownBody}
                            initialValue={markdownBody}
                            onChange={setMarkdownBody}
                            onSave={(text) => {
                                AddNewRules(text);
                            }}
                        />
                    </div>
                </>
            )}
        </div>
    );
};

export default EditRule;
