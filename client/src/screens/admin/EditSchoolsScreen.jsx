import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import SchoolsTable from "../../components/SchoolsTable";
import { apiRequest, apiUrl } from "../../utils/apiFetcher";
import styles from "./EditSchoolsScreen.module.css";

const AdminSchoolsScreen = () => {
    const navigate = useNavigate();
    const [schools, setSchools] = useState([]);
    const [csvFile, setCsvFile] = useState(null);

    const loadSchools = async () => {
        const response = await apiRequest("/api/admin/school/school", null, "GET", navigate);
        if (response) setSchools(response);
    };

    useEffect(() => {
        loadSchools();
    }, [navigate]);

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
        } catch {
            alert("Błąd sieci podczas wysyłania formularza.");
        }
    };

    const handleCreateManualSchool = async (payload) => {
        const success = await apiRequest("/api/admin/school/school", payload, "POST", navigate);
        if (success) await loadSchools();
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
            if (successShort) await loadSchools();
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
        if (success) await loadSchools();
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

    return (
        <div className={`container ${styles.container}`}>
            <AdminHeader navigate={navigate} />
            <div className={styles.headerBox}>
                <h1>Zarządzanie Bazą Szkół</h1>
            </div>

            <div className={styles.toolsetGrid}>
                <div className={styles.card}>
                    <h3 className={styles.cardTitle}>Import bazy z kuratorium (CSV)</h3>
                    <div className={styles.flexInline}>
                        <input
                            id="csvFileInput"
                            type="file"
                            accept=".csv"
                            onChange={(e) => setCsvFile(e.target.files[0])}
                        />
                        <Button text={"Importuj"} onClick={handleImportCsv} />
                    </div>
                </div>

                <div className={`${styles.card} ${styles.dangerCard}`}>
                    <h3 className={`${styles.cardTitle} ${styles.dangerTitle}`}>
                        Czyszczenie rejestru
                    </h3>
                    <p className={styles.cardDescription}>
                        Usuwa wszystkie rekordy szkół z bazy danych.
                    </p>
                    <div className={styles.cardActions}>
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
