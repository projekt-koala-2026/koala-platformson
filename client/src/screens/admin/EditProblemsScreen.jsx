import { useEffect, useMemo, useState } from "react";
import { FaFilePdf, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import { apiRequest, apiUrl, uploadFile } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const EditProblemsScreen = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin() || isEditor(), []);

    const [editions, setEditions] = useState([]);
    const [selectedEditionId, setSelectedEditionId] = useState("");
    const [allProblemsData, setAllProblemsData] = useState({});

    const [subpointInputValue, setSubpointInputValue] = useState("");
    const [selectedFile, setSelectedFile] = useState(null);

    const handleBack = () => {
        navigate("/admin");
    };

    const saveToServer = async (data) => {
        const cleanData = { ...data };
        delete cleanData.markdownBody;

        const fileContent = JSON.stringify(cleanData);
        await apiRequest(
            "/api/static-pages/problems",
            { markdownBody: fileContent },
            "PUT",
            navigate
        );
    };

    const handleAddEntry = async () => {
        if (!selectedEditionId) return alert("Wybierz edycję!");
        if (!subpointInputValue.trim()) return alert("Wpisz nazwę podpunktu!");
        if (!selectedFile) return alert("Wybierz plik PDF!");

        const maxSizeBytes = 64 * 1024 * 1024;
        if (selectedFile.size > maxSizeBytes) {
            return alert("Wybrany plik jest za duży! Maksymalny rozmiar to 64 MB.");
        }

        const serverFileData = await uploadFile(
            selectedFile,
            selectedFile.name,
            "problems",
            navigate
        );

        if (serverFileData) {
            const fileGuid = serverFileData.id || Date.now().toString();
            const fileUrl =
                serverFileData.filePath ||
                serverFileData.url ||
                `/content/problems/${fileGuid}.pdf`;
            const finalFileName = fileUrl.split("/").pop();

            const updatedData = { ...allProblemsData };
            const subpoint = subpointInputValue.trim();

            if (!updatedData[selectedEditionId]) {
                updatedData[selectedEditionId] = {};
            }

            if (!updatedData[selectedEditionId][subpoint]) {
                updatedData[selectedEditionId][subpoint] = [];
            }

            updatedData[selectedEditionId][subpoint].push({
                id: fileGuid,
                title: selectedFile.name,
                fileName: finalFileName,
                url: fileUrl,
            });

            setAllProblemsData(updatedData);
            setSubpointInputValue("");
            setSelectedFile(null);
            document.getElementById("pdfFileInput").value = "";

            await saveToServer(updatedData);
        } else {
            alert("Błąd podczas wysyłania pliku na serwer.");
        }
    };

    const handleDeletePdf = async (subpoint, pdfId) => {
        const confirmed = window.confirm("Czy na pewno chcesz usunąć ten plik PDF?");
        if (!confirmed) return;

        try {
            await apiRequest("/api/admin/file/public/files", { id: pdfId }, "DELETE", navigate);
        } catch (err) {
            console.error("Błąd podczas usuwania pliku z bazy danych:", err);
        }

        const updatedData = { ...allProblemsData };
        updatedData[selectedEditionId][subpoint] = updatedData[selectedEditionId][subpoint].filter(
            (p) => p.id !== pdfId
        );

        if (updatedData[selectedEditionId][subpoint].length === 0) {
            delete updatedData[selectedEditionId][subpoint];
        }

        setAllProblemsData(updatedData);
        await saveToServer(updatedData);
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const loadData = async () => {
            const editionsData = await apiRequest("/api/edition", null, "GET", navigate);
            let fileJson = {};

            try {
                const res = await apiRequest("/api/static-pages/problems", null, "GET", navigate);

                if (res) {
                    if (typeof res === "string" && res.trim().startsWith("{")) {
                        fileJson = JSON.parse(res);
                    } else if (typeof res === "object") {
                        if (
                            typeof res.markdownBody === "string" &&
                            res.markdownBody.trim().startsWith("{")
                        ) {
                            fileJson = JSON.parse(res.markdownBody);
                        } else {
                            fileJson = { ...res };
                        }
                    }
                }
            } catch (err) {}

            if (fileJson.markdownBody !== undefined) {
                delete fileJson.markdownBody;
            }
            if (fileJson.id !== undefined) {
                delete fileJson.id;
            }

            if (editionsData && editionsData.length > 0) {
                editionsData.forEach((edition) => {
                    if (!fileJson[edition.id]) {
                        fileJson[edition.id] = {};
                    }
                });
                setEditions(editionsData);
                setSelectedEditionId(editionsData[0].id);
            }

            setAllProblemsData(fileJson);
        };

        loadData();
    }, [navigate, isAdminUser]);

    const currentEditionData = allProblemsData[selectedEditionId] || {};

    const inlineStyles = {
        selectSection: { marginTop: "20px", marginBottom: "20px" },
        selectInput: { display: "block", marginTop: "5px", padding: "8px", width: "100%" },
        formBox: {
            marginTop: "20px",
            padding: "15px",
            border: "1px solid #ccc",
            borderRadius: "8px",
            background: "#fff",
        },
        limitNotice: {
            fontSize: "0.85rem",
            color: "#b35c00",
            fontWeight: "500",
            margin: "2px 0 10px 0",
        },
        flexGroup: { display: "flex", gap: "10px", marginTop: "10px", alignItems: "center" },
        inputField: { flexGrow: 1, padding: "8px", border: "1px solid #ccc", borderRadius: "4px" },
        fileInput: { padding: "5px" },
        card: {
            marginBottom: "25px",
            padding: "15px",
            border: "1px solid #ddd",
            borderRadius: "8px",
            background: "#fff",
        },
        cardHeader: {
            display: "flex",
            alignItems: "center",
            paddingBottom: "8px",
            borderBottom: "1px solid #ccc",
            marginBottom: "15px",
        },
        pdfRow: { display: "flex", alignItems: "center", width: "100%" },
        pdfIconGroup: { display: "flex", alignItems: "center", gap: "10px" },
        link: { color: "#de1414", textDecoration: "none", fontWeight: "bold" },
    };

    return (
        <div className="container" style={{ padding: "2rem" }}>
            <h1>Zarządzanie Zadaniami (PDF)</h1>
            <Button text={"Wróć do panelu"} onClick={handleBack} />

            <div style={inlineStyles.selectSection}>
                <label style={{ fontWeight: "bold" }}>Wybierz Edycję:</label>
                <select
                    value={selectedEditionId}
                    onChange={(e) => setSelectedEditionId(e.target.value)}
                    style={inlineStyles.selectInput}
                >
                    {editions.map((e) => (
                        <option key={e.id} value={e.id}>
                            {e.title}
                        </option>
                    ))}
                </select>
            </div>

            <div style={inlineStyles.formBox}>
                <h3 style={{ margin: "0 0 2px 0" }}>Utwórz nowy podpunkt i dodaj PDF</h3>
                <p style={inlineStyles.limitNotice}>
                    Maksymalny rozmiar przesyłanego pliku to 64 MB.
                </p>
                <div style={inlineStyles.flexGroup}>
                    <input
                        type="text"
                        placeholder="Nazwa podpunktu (np. Etap Szkolny)"
                        value={subpointInputValue}
                        onChange={(e) => setSubpointInputValue(e.target.value)}
                        style={inlineStyles.inputField}
                    />
                    <input
                        id="pdfFileInput"
                        type="file"
                        accept=".pdf"
                        onChange={(e) => setSelectedFile(e.target.files[0])}
                        style={inlineStyles.fileInput}
                    />
                    <Button text={"Zapisz"} onClick={handleAddEntry} />
                </div>
            </div>

            <div style={{ marginTop: "30px" }}>
                <h2>Struktura dokumentacji</h2>
                {Object.keys(currentEditionData).length === 0 ? (
                    <p style={{ color: "#666" }}>Brak podpunktów dla tej edycji.</p>
                ) : (
                    Object.keys(currentEditionData).map((subpoint) => (
                        <div key={subpoint} style={inlineStyles.card}>
                            <div style={inlineStyles.cardHeader}>
                                <h3 style={{ margin: 0 }}>{subpoint}</h3>
                            </div>
                            <ContentsListBox>
                                {currentEditionData[subpoint].map((pdf) => (
                                    <ContentsListTile key={pdf.id}>
                                        <div style={inlineStyles.pdfRow}>
                                            <div style={inlineStyles.pdfIconGroup}>
                                                <FaFilePdf
                                                    style={{ color: "#de1414", fontSize: "1.4rem" }}
                                                />
                                                <a
                                                    href={
                                                        pdf.url.startsWith("http")
                                                            ? pdf.url
                                                            : `${apiUrl}${pdf.url}`
                                                    }
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    style={inlineStyles.link}
                                                >
                                                    {pdf.title}
                                                </a>
                                            </div>
                                            <div style={{ marginLeft: "auto" }}>
                                                <Button
                                                    text={<FaTrash />}
                                                    onClick={() =>
                                                        handleDeletePdf(subpoint, pdf.id)
                                                    }
                                                />
                                            </div>
                                        </div>
                                    </ContentsListTile>
                                ))}
                            </ContentsListBox>
                        </div>
                    ))
                )}
            </div>
        </div>
    );
};

export default EditProblemsScreen;
