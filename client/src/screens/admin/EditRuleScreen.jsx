import { useEffect, useMemo, useState } from "react";
import { useRef } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest, uploadFile } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

const EditRule = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [posts, setPosts] = useState([]);
    const [rules, setRules] = useState([]);


    const handleBack = () => {
        navigate("/admin");
    };

    const AddNewRules = async (rules) => {
        navigate("/admin");
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/regulamin/edit", null, "GET", navigate);
            setRules(data);
        };

        if (isAdminUser);
    }, [navigate]);

    return (
        <div className="container">
            {isAdminUser && (
                <>
                    <h1>Regulamin</h1>
                    <div>
                        <Button text={"Wróć do panelu"} onClick={handleBack} />
                        {posts.map((text, idx) => (
                            <MarkdownRenderer key={idx} content={text} />
                        ))}
                        <MarkdownEditor onSave={(text) => {AddNewRules(text)}} />
                    </div>
                </>
            )}
            
        </div>
    );
};

export default EditRule;
