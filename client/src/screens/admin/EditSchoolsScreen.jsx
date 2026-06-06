import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import SchoolsTable from "../../components/SchoolsTable";
import { apiRequest, apiUrl } from "../../utils/apiFetcher";

const AdminSchoolsScreen = () => {
    const navigate = useNavigate();
    const [schools, setSchools] = useState([]);
    const [csvFile, setCsvFile] = useState(null);

    const loadSchools = async () => {
        const response = await apiRequest("/api/admin/school/school", null, "GET", navigate);
        if (response) {
            setSchools(response);
        }
    };

    useEffect(() => {
        loadSchools();
    }, [navigate]);

    const handleBack = () => {
        navigate("/admin");
    };

    const handleImportCsv = async () => {
        if (!csvFile) return alert("Wybierz plik CSV przed importem!");

        const formData = new FormData();
        formData.append("File", csvFile);
        formData.append("Title", "file");

        try {
            const tokenResponse = await fetch(`${apiUrl}/api/admin/school/import/csv`, {
                method: "POST",
                body: formData,
                credentials: "include",
            });

            if (tokenResponse.ok) {
                alert("Pomyślnie zaimportowano bazę szkół z pliku CSV.");
                setCsvFile(null);
                document.getElementById("csvFileInput").value = "";
                await loadSchools();
            } else {
                alert("Błąd podczas przetwarzania pliku CSV przez serwer.");
            }
        } catch (error) {
            alert("Błąd sieci podczas wysyłania formularza.");
        }
    };

    const handleCreateManualSchool = async (payload) => {
        const success = await apiRequest("/api/admin/school/school", payload, "POST", navigate);
        if (success) {
            await loadSchools();
        }
    };

    const handleUpdateInlineSchool = async (updatedSchool) => {
        const payload = {
            rspo: updatedSchool.rspo,
            name: updatedSchool.name,
            nameShort: updatedSchool.nameShort,
            state: updatedSchool.state,
            city: updatedSchool.city,
            addres: updatedSchool.addres,
        };

        const success = await apiRequest("/api/admin/school/name", payload, "PUT", navigate);
        if (success) {
            const successShort = await apiRequest(
                "/api/admin/school/nameshort",
                payload,
                "PUT",
                navigate
            );
            if (successShort) {
                await loadSchools();
            }
        }
    };

    const handleDeleteSingle = async (school) => {
        const confirmed = window.confirm(`Czy na pewno usunąć szkołę: ${school.name}?`);
        if (!confirmed) return;

        const success = await apiRequest(
            "/api/admin/school/school",
            { rspo: school.rspo },
            "DELETE",
            navigate
        );
        if (success) {
            await loadSchools();
        }
    };

    const handleDeleteAll = async () => {
        const confirmed = window.confirm(
            "KRYTYCZNA AKCJA! Czy na pewno chcesz całkowicie wyczyścić bazę danych i usunąć wszystkie szkoły?"
        );
        if (!confirmed) return;

        const secondConfirmation = window.confirm(
            "Czy jesteś absolutnie pewien? Tej operacji nie da się cofnąć."
        );
        if (!secondConfirmation) return;

        const success = await apiRequest("/api/admin/school/schools", null, "DELETE", navigate);
        if (success) {
            alert("Baza danych szkół została całkowicie wyczyszczona.");
            await loadSchools();
        }
    };

    const styles = {
        container: {
            padding: "2rem",
        },
        headerBox: {
            display: "flex",
            flexDirection: "column",
            justifyContent: "between",
            alignItems: "center",
            marginBottom: "20px",
        },
        toolsetGrid: {
            display: "flex",
            gap: "20px",
            marginBottom: "30px",
            flexWrap: "wrap",
        },
        card: {
            padding: "15px",
            border: "1px solid #ccc",
            borderRadius: "8px",
            background: "#fff",
            flexGrow: 1,
            minWidth: "300px",
        },
        flexInline: {
            display: "flex",
            gap: "10px",
            marginTop: "10px",
            alignItems: "center",
        },
    };

    return (
        <div className="container" style={styles.container}>
            <div style={styles.headerBox}>
                <h1>Zarządzanie Bazą Szkół</h1>
                <Button text={"Wróć do panelu"} onClick={handleBack} />
            </div>

            <div style={styles.toolsetGrid}>
                <div style={styles.card}>
                    <h3 style={{ margin: "0 0 5px 0" }}>Import bazy z kuratorium (CSV)</h3>
                    <div style={styles.flexInline}>
                        <input
                            id="csvFileInput"
                            type="file"
                            accept=".csv"
                            onChange={(e) => setCsvFile(e.target.files[0])}
                        />
                        <Button text={"Importuj"} onClick={handleImportCsv} />
                    </div>
                </div>

                <div
                    style={{
                        ...styles.card,
                        border: "1px solid #de1414",
                        backgroundColor: "rgba(222, 20, 20, 0.05)",
                    }}
                >
                    <h3 style={{ margin: "0 0 5px 0", color: "#de1414" }}>Czyszczenie rejestru</h3>
                    <p style={{ fontSize: "0.85rem", margin: "5px 0" }}>
                        Usuwa wszystkie rekordy szkół z bazy danych.
                    </p>
                    <div style={{ marginTop: "10px" }}>
                        <Button text={"Wyczyść całą bazę"} onClick={handleDeleteAll} />
                    </div>
                </div>
            </div>

            <h2>Zarejestrowane placówki ({schools.length})</h2>

            <SchoolsTable
                schools={schools}
                onAddNewSchool={handleCreateManualSchool}
                onUpdateSchool={handleUpdateInlineSchool}
                actionsRenderer={(school) => (
                    <Button text="Usuń" onClick={() => handleDeleteSingle(school)} />
                )}
            />
        </div>
    );
};

export default AdminSchoolsScreen;
