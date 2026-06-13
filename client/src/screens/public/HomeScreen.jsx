import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import Hamburger from "../../components/Hamburger";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest } from "../../utils/apiFetcher";

const HomeScreen = () => {
    const navigate = useNavigate();
    const [posts, setPosts] = useState([]);
    const [editions, setEditions] = useState([]);
    const [editionId, setEditionId] = useState("");

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/api/admin/post", null, "GET", navigate);
            const editionsData = await apiRequest("/api/edition", null, "GET", navigate);
            if (data) setPosts(data);
            if (editionsData) {
                setEditions(editionsData);
                if (editionsData.length > 0) {
                    setEditionId(editionsData[0].id);
                }
            }
        };

        getData();
    }, [navigate]);
    return (
        <>
            <header style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
                <h1>Koala</h1>
                <Hamburger
                    options={[
                        ["Aktualności", () => navigate("/")],
                        ["Zadania", () => navigate("/problems")],
                        ["Regulamin", () => navigate("/rules")],
                        ["Historia", () => navigate("/history")],
                        ["KOALicjA", () => navigate("/koalicja")],
                    ]}
                />
                <h2>
                    <span style={{ color: "#458756" }}>KO</span>mbinatoryka{" "}
                    <span style={{ color: "#458756" }}>A</span>lgorytmika{" "}
                    <span style={{ color: "#458756" }}>L</span>ogik
                    <span style={{ color: "#458756" }}>A</span>
                </h2>
                <h5>Wielkopolski konkurs grup szkolnych</h5>
            </header>

            <div className="container" style={{ minWidth: "50%" }}>
                <h1>Aktualności</h1>
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
                                            {linkedEdition ? linkedEdition.title : "Nieprzypisana"}
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
            </div>
        </>
    );
};

export default HomeScreen;
