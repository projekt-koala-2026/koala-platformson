import { useEffect, useState } from "react";
import { FaChevronRight, FaFilePdf } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import { apiRequest, apiUrl } from "../../utils/apiFetcher";

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
            } catch (error) {
                setProblemsData({});
            }
        };

        loadPublicData();
    }, [navigate]);

    const currentEditionProblems = problemsData[selectedEditionId] || {};

    const styles = {
        container: {
            padding: "2rem",
            maxWidth: "900px",
            margin: "0 auto",
            fontFamily: "inherit",
        },
        selectBox: {
            marginBottom: "30px",
            padding: "15px",
            backgroundColor: "#f9f9f9",
            borderRadius: "8px",
            border: "1px solid #eee",
        },
        label: {
            display: "block",
            fontWeight: "bold",
            marginBottom: "8px",
            color: "#333",
        },
        select: {
            width: "100%",
            padding: "10px",
            borderRadius: "4px",
            border: "1px solid #ccc",
            fontSize: "1rem",
            backgroundColor: "#fff",
        },
        subpointSection: {
            marginBottom: "25px",
            backgroundColor: "#fff",
            border: "1px solid #e0e0e0",
            borderRadius: "8px",
            overflow: "hidden",
            boxShadow: "0 2px 4px rgba(0,0,0,0.02)",
        },
        subpointHeader: {
            backgroundColor: "#f5f5f5",
            padding: "12px 20px",
            margin: 0,
            fontSize: "1.2rem",
            color: "#2D8A4E",
            borderBottom: "1px solid #e0e0e0",
            fontWeight: "bold",
        },
        fileList: {
            listStyleType: "none",
            padding: 0,
            margin: 0,
        },
        fileItem: {
            borderBottom: "1px solid #f0f0f0",
        },
        fileLink: {
            display: "flex",
            alignItems: "center",
            padding: "14px 20px",
            textDecoration: "none",
            color: "#333",
            transition: "background-color 0.2s",
        },
        iconPdf: {
            color: "#de1414",
            fontSize: "1.3rem",
            marginRight: "12px",
            flexShrink: 0,
        },
        fileName: {
            fontWeight: "500",
            flexGrow: 1,
        },
        iconArrow: {
            color: "#ccc",
            fontSize: "0.9rem",
        },
        noData: {
            textAlign: "center",
            padding: "40px",
            color: "#666",
            backgroundColor: "#fafafa",
            borderRadius: "8px",
            border: "1px dashed #ccc",
        },
    };

    return (
        <div style={styles.container}>
            <h1 style={{ color: "#2D8A4E", marginBottom: "5px" }}>Archiwum Zadań</h1>
            <p style={{ color: "#666", marginBottom: "25px" }}>
                Wybierz edycję olimpiady, aby uzyskać dostęp do bazy zadań i materiałów
                dokumentacyjnych.
            </p>

            {editions.length > 0 && (
                <div style={styles.selectBox}>
                    <label style={styles.label}>Edycja Olimpiady:</label>
                    <select
                        value={selectedEditionId}
                        onChange={(e) => setSelectedEditionId(e.target.value)}
                        style={styles.select}
                    >
                        {editions.map((e) => (
                            <option key={e.id} value={e.id}>
                                {e.title}
                            </option>
                        ))}
                    </select>
                </div>
            )}

            <div style={{ marginTop: "20px" }}>
                {Object.keys(currentEditionProblems).length === 0 ? (
                    <div style={styles.noData}>
                        <h3>Brak materiałów</h3>
                        <p>Dla wybranej edycji nie dodano jeszcze żadnych zadań ani podpunktów.</p>
                    </div>
                ) : (
                    Object.keys(currentEditionProblems).map((subpoint) => (
                        <div key={subpoint} style={styles.subpointSection}>
                            <h3 style={styles.subpointHeader}>{subpoint}</h3>
                            <ul style={styles.fileList}>
                                {currentEditionProblems[subpoint].map((file) => (
                                    <li key={file.id} style={styles.fileItem}>
                                        <a
                                            href={`${apiUrl}${file.url}`}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            style={styles.fileLink}
                                            onMouseEnter={(e) =>
                                                (e.currentTarget.style.backgroundColor = "#f9f9f9")
                                            }
                                            onMouseLeave={(e) =>
                                                (e.currentTarget.style.backgroundColor =
                                                    "transparent")
                                            }
                                        >
                                            <FaFilePdf style={styles.iconPdf} />
                                            <span style={styles.fileName}>{file.title}</span>
                                            <FaChevronRight style={styles.iconArrow} />
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
