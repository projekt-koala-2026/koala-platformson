import { useEffect, useState } from "react";
import { FaChevronRight, FaFilePdf } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import { apiRequest, apiUrl } from "../../utils/apiFetcher";
import styles from "./ProblemsPublicScreen.module.css";

const ProblemsPublicScreen = () => {
    const navigate = useNavigate();
    const [editions, setEditions] = useState([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [problemsData, setProblemsData] = useState({});

    useEffect(() => {
        const loadPublicData = async () => {
            const editionsData = await apiRequest("/api/edition", null, "GET", navigate);
            if (editionsData && editionsData.length > 0) {
                setEditions(editionsData);
                setSelectedEditionId(editionsData[0].id);
            }

            try {
                const response = await fetch(apiUrl + "/content/problems/problems.json");
                if (response.ok) {
                    const json = await response.json();
                    setProblemsData(json);
                }
            } catch {
                setProblemsData({});
            }
        };

        loadPublicData();
    }, [navigate]);

    const currentEditionProblems = problemsData[selectedEditionId] || {};

    return (
        <div className={styles.container}>
            <h1 className={styles.title}>Archiwum Zadań</h1>
            <p className={styles.description}>
                Wybierz edycję olimpiady, aby uzyskać dostęp do bazy zadań i materiałów
                dokumentacyjnych.
            </p>

            {editions.length > 0 && (
                <div className={styles.selectBox}>
                    <label className={styles.label}>Edycja Olimpiady:</label>
                    <select
                        value={selectedEditionId}
                        onChange={(e) => setSelectedEditionId(e.target.value)}
                        className={styles.select}
                    >
                        {editions.map((e) => (
                            <option key={e.id} value={e.id}>
                                {e.title}
                            </option>
                        ))}
                    </select>
                </div>
            )}

            <div className={styles.problemsList}>
                {Object.keys(currentEditionProblems).length === 0 ? (
                    <div className={styles.noData}>
                        <h3>Brak materiałów</h3>
                        <p>Dla wybranej edycji nie dodano jeszcze żadnych zadań ani podpunktów.</p>
                    </div>
                ) : (
                    Object.keys(currentEditionProblems).map((subpoint) => (
                        <div key={subpoint} className={styles.subpointSection}>
                            <h3 className={styles.subpointHeader}>{subpoint}</h3>
                            <ul className={styles.fileList}>
                                {currentEditionProblems[subpoint].map((file) => (
                                    <li key={file.id} className={styles.fileItem}>
                                        <a
                                            href={`${apiUrl}${file.url}`}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className={styles.fileLink}
                                        >
                                            <FaFilePdf className={styles.iconPdf} />
                                            <span className={styles.fileName}>{file.title}</span>
                                            <FaChevronRight className={styles.iconArrow} />
                                        </a>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    ))
                )}
            </div>
        </div>
    );
};

export default ProblemsPublicScreen;
