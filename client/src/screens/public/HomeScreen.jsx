import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import Hamburger from "../../components/Hamburger";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import styles from "./HomeScreen.module.css";

const HomeScreen = () => {
    const navigate = useNavigate();
    const [posts, setPosts] = useState([]);
    const [editions, setEditions] = useState([]);

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
            <header className={styles.header}>
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
                    <span className={styles.brandAccent}>KO</span>mbinatoryka{" "}
                    <span className={styles.brandAccent}>A</span>lgorytmika{" "}
                    <span className={styles.brandAccent}>L</span>ogik
                    <span className={styles.brandAccent}>A</span>
                </h2>
                <h5>Wielkopolski konkurs grup szkolnych</h5>
            </header>

            <div className={`container ${styles.container}`}>
                <h1>Aktualności</h1>
                <ContentsListBox>
                    {posts.map((item, idx) => {
                        const linkedEdition = editions.find((e) => e.id === item.editionId);

                        return (
                            <ContentsListTile key={item.id}>
                                <div className={styles.postHeader}>
                                    <h3>Tytuł: {item.title}</h3>
                                    <div className={styles.postMeta}>
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
                                        <h6 className={styles.editionMeta}>
                                            Edycja:{" "}
                                            {linkedEdition ? linkedEdition.title : "Nieprzypisana"}
                                        </h6>
                                    </div>
                                    <hr className={styles.divider} />
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
