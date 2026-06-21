import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import { apiRequest } from "../../utils/apiFetcher";

const HistoryScreen = () => {
    const navigate = useNavigate();
    const [history, setHistory] = useState([]);
    const [editions, setEditions] = useState([]);
    const [show, setShow] = useState(false);
    const [posts, setPosts] = useState([]);

    const setShowPosts = async (edition) => {
        setShow(show === edition ? false : edition);
        const data = await apiRequest("/api/admin/post", null, "GET", navigate);
        const results = data.filter((p) => p.editionId === edition.id);
        console.log(results);
        setPosts(results);
    };

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/content/history/history.json", null, "GET", navigate);
            const editionsData = await apiRequest("/api/edition", null, "GET", navigate);
            if (data) setHistory(data);
            if (editionsData) {
                const now = new Date();
                const results = editionsData.filter((e) => new Date(e.endDate) < now);
                setEditions(results);
            }
        };

        getData();
    }, [navigate]);
    return (
        <>
            <PublicHeader navigate={navigate} />

            <div className="container" style={{ minWidth: "50%" }}>
                <h1>Historia</h1>
                <MarkdownRenderer content={history.markdownBody} />
                <h1>Wpisy z poprzednich edycji</h1>

                <div className="container-near">
                    {editions.map((item, idx) => (
                        <Button text={item.title} onClick={() => setShowPosts(item)} />
                    ))}
                </div>
                {show !== false && (
                    <ContentsListBox>
                        {posts.map((item, idx) => {
                            const linkedEdition = editions.find((e) => e.id === item.editionId);

                            return (
                                <ContentsListTile key={item.id}>
                                    <div style={{ display: "flex", flexDirection: "column" }}>
                                        <h3>Tytuł: {item.title}</h3>
                                        <div style={{ display: "flex" }}>
                                            <h6>
                                                Data:{" "}
                                                {new Date(item.createdAt).toLocaleString("pl-PL", {
                                                    year: "numeric",
                                                    month: "long",
                                                    day: "numeric",
                                                    hour: "2-digit",
                                                    minute: "2-digit",
                                                })}
                                            </h6>
                                            <h6 style={{ marginLeft: "auto" }}>
                                                Edycja:{" "}
                                                {linkedEdition
                                                    ? linkedEdition.title
                                                    : "Nieprzypisana"}
                                            </h6>
                                        </div>
                                        <hr
                                            style={{
                                                border: "none",
                                                height: "2px",
                                                backgroundColor: "#054e0b",
                                                margin: "3px 0",
                                            }}
                                        />
                                    </div>
                                    <MarkdownRenderer key={idx} content={item.markdownBody} />
                                </ContentsListTile>
                            );
                        })}
                    </ContentsListBox>
                )}
            </div>
            <PublicFooter />
        </>
    );
};

export default HistoryScreen;
