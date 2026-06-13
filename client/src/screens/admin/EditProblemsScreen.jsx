import { useEffect, useMemo, useState } from "react";
import { FaFilePdf, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import { apiRequest, apiUrl, uploadFile } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";
import styles from "./EditProblemsScreen.module.css";

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

        await apiRequest(
            "/api/static-pages/problems",
            { markdownBody: JSON.stringify(cleanData) },
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

        const serverFileData = await uploadFile(selectedFile, selectedFile.name, "problems", navigate);

        if (serverFileData) {
            const fileGuid = serverFileData.id || Date.now().toString();
            const fileUrl =
                serverFileData.filePath || serverFileData.url || `/content/problems/${fileGuid}.pdf`;
            const finalFileName = fileUrl.split("/").pop();
            const updatedData = { ...allProblemsData };
            const subpoint = subpointInputValue.trim();

            if (!updatedData[selectedEditionId]) updatedData[selectedEditionId] = {};
            if (!updatedData[selectedEditionId][subpoint]) updatedData[selectedEditionId][subpoint] = [];

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
            } catch {
                // Missing or invalid problem metadata should fall back to an empty structure.
            }

            delete fileJson.markdownBody;
            delete fileJson.id;

            if (editionsData && editionsData.length > 0) {
                editionsData.forEach((edition) => {
                    if (!fileJson[edition.id]) fileJson[edition.id] = {};
                });
                setEditions(editionsData);
                setSelectedEditionId(editionsData[0].id);
            }

            setAllProblemsData(fileJson);
        };

        loadData();
    }, [navigate, isAdminUser]);

    const currentEditionData = allProblemsData[selectedEditionId] || {};

    return (
        <div className={`container ${styles.container}`}>
            <h1>Zarządzanie Zadaniami (PDF)</h1>
            <Button text={"Wróć do panelu"} onClick={handleBack} />

            <div className={styles.selectSection}>
                <label className={styles.label}>Wybierz Edycję:</label>
                <select
                    value={selectedEditionId}
                    onChange={(e) => setSelectedEditionId(e.target.value)}
                    className={styles.selectInput}
                >
                    {editions.map((e) => (
                        <option key={e.id} value={e.id}>
                            {e.title}
                        </option>
                    ))}
                </select>
            </div>

            <div className={styles.formBox}>
                <h3 className={styles.formTitle}>Utwórz nowy podpunkt i dodaj PDF</h3>
                <p className={styles.limitNotice}>Maksymalny rozmiar przesyłanego pliku to 64 MB.</p>
                <div className={styles.flexGroup}>
                    <input
                        type="text"
                        placeholder="Nazwa podpunktu (np. Etap Szkolny)"
                        value={subpointInputValue}
                        onChange={(e) => setSubpointInputValue(e.target.value)}
                        className={styles.inputField}
                    />
                    <input
                        id="pdfFileInput"
                        type="file"
                        accept=".pdf"
                        onChange={(e) => setSelectedFile(e.target.files[0])}
                        className={styles.fileInput}
                    />
                    <Button text={"Zapisz"} onClick={handleAddEntry} />
                </div>
            </div>

            <div className={styles.structureSection}>
                <h2>Struktura dokumentacji</h2>
                {Object.keys(currentEditionData).length === 0 ? (
                    <p className={styles.emptyText}>Brak podpunktów dla tej edycji.</p>
                ) : (
                    Object.keys(currentEditionData).map((subpoint) => (
                        <div key={subpoint} className={styles.card}>
                            <div className={styles.cardHeader}>
                                <h3 className={styles.cardTitle}>{subpoint}</h3>
                            </div>
                            <ContentsListBox>
                                {currentEditionData[subpoint].map((pdf) => (
                                    <ContentsListTile key={pdf.id}>
                                        <div className={styles.pdfRow}>
                                            <div className={styles.pdfIconGroup}>
                                                <FaFilePdf className={styles.pdfIcon} />
                                                <a
                                                    href={
                                                        pdf.url.startsWith("http")
                                                            ? pdf.url
                                                            : `${apiUrl}${pdf.url}`
                                                    }
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className={styles.link}
                                                >
                                                    {pdf.title}
                                                </a>
                                            </div>
                                            <div className={styles.rowActions}>
                                                <Button
                                                    text={<FaTrash />}
                                                    onClick={() => handleDeletePdf(subpoint, pdf.id)}
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
