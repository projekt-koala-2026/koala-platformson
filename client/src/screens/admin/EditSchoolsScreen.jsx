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

    const [newSchool, setNewSchool] = useState({
        rspo: "",
        name: "",
        nameShort: "",
        state: "",
        city: "",
        type: "",
        addres: "",
    });

    const [selectedSchool, setSelectedSchool] = useState(null);
    const [editForm, setEditForm] = useState({
        name: "",
        nameShort: "",
    });

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
            setSelectedSchool(null);
        }
    };

    const handleCreateSchool = async (e) => {
        if (e && e.preventDefault) e.preventDefault();

        const payload = {
            rspo: Number(newSchool.rspo),
            name: newSchool.name,
            nameShort: newSchool.nameShort,
            state: newSchool.state,
            city: newSchool.city,
            type: newSchool.type,
            addres: newSchool.addres,
        };

        const success = await apiRequest("/api/admin/school/school", payload, "POST", navigate);
        if (success) {
            setNewSchool({
                rspo: "",
                name: "",
                nameShort: "",
                state: "",
                city: "",
                type: "",
                addres: "",
            });
            await loadSchools();
        }
    };

    const handleRowClick = (school) => {
        setSelectedSchool(school);
        setEditForm({
            name: school.name || "",
            nameShort: school.nameShort || "",
        });
    };

    const handleUpdateSchool = async (e) => {
        if (e && e.preventDefault) e.preventDefault();
        if (!selectedSchool) return;

        const payload = {
            rspo: Number(selectedSchool.rspo),
            name: editForm.name,
            nameShort: editForm.nameShort,
            state: selectedSchool.state,
            city: selectedSchool.city,
            type: selectedSchool.type,
            addres: selectedSchool.addres,
        };

        const successName = await apiRequest("/api/admin/school/name", payload, "PUT", navigate);

        if (successName) {
            const successShort = await apiRequest(
                "/api/admin/school/nameshort",
                payload,
                "PUT",
                navigate
            );

            if (successShort) {
                await loadSchools();
                setSelectedSchool(null);
            }
        }
    };

    const handleDeleteSingle = async () => {
        if (!selectedSchool) return;

        const confirmed = window.confirm(`Czy na pewno usunąć szkołę: ${selectedSchool.name}?`);
        if (!confirmed) return;

        const success = await apiRequest(
            "/api/admin/school/school",
            { rspo: Number(selectedSchool.rspo) },
            "DELETE",
            navigate
        );
        if (success) {
            await loadSchools();
            setSelectedSchool(null);
        }
    };

    const handleNewSchoolChange = (field, value) => {
        setNewSchool((prev) => ({ ...prev, [field]: value }));
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

            <div className={styles.formsGrid}>
                <div className={styles.formCard}>
                    <h3 className={styles.cardTitle}>Dodaj nową szkołę</h3>
                    <form className={styles.formLayout}>
                        <input
                            type="number"
                            placeholder="RSPO"
                            value={newSchool.rspo}
                            onChange={(e) => handleNewSchoolChange("rspo", e.target.value)}
                            className={styles.inputField}
                            required
                        />
                        <input
                            type="text"
                            placeholder="Pełna nazwa szkoły"
                            value={newSchool.name}
                            onChange={(e) => handleNewSchoolChange("name", e.target.value)}
                            className={styles.inputField}
                            required
                        />
                        <input
                            type="text"
                            placeholder="Nazwa skrócona"
                            value={newSchool.nameShort}
                            onChange={(e) => handleNewSchoolChange("nameShort", e.target.value)}
                            className={styles.inputField}
                        />
                        <input
                            type="text"
                            placeholder="Województwo"
                            value={newSchool.state}
                            onChange={(e) => handleNewSchoolChange("state", e.target.value)}
                            className={styles.inputField}
                        />
                        <input
                            type="text"
                            placeholder="Miejscowość"
                            value={newSchool.city}
                            onChange={(e) => handleNewSchoolChange("city", e.target.value)}
                            className={styles.inputField}
                        />
                        <input
                            type="text"
                            placeholder="Typ placówki"
                            value={newSchool.type}
                            onChange={(e) => handleNewSchoolChange("type", e.target.value)}
                            className={styles.inputField}
                        />
                        <input
                            type="text"
                            placeholder="Adres"
                            value={newSchool.addres}
                            onChange={(e) => handleNewSchoolChange("addres", e.target.value)}
                            className={styles.inputField}
                        />
                        <div className={styles.submitRow}>
                            <Button text="Dodaj szkołę" onClick={handleCreateSchool} />
                        </div>
                    </form>
                </div>

                <div className={styles.formCard}>
                    {selectedSchool ? (
                        <>
                            <h3 className={styles.cardTitle}>
                                Edycja szkoły: {selectedSchool.rspo}
                            </h3>
                            <form className={styles.formLayout}>
                                <input
                                    type="text"
                                    placeholder="Pełna nazwa szkoły"
                                    value={editForm.name}
                                    onChange={(e) =>
                                        setEditForm((prev) => ({ ...prev, name: e.target.value }))
                                    }
                                    className={styles.inputField}
                                    required
                                />
                                <input
                                    type="text"
                                    placeholder="Nazwa skrócona"
                                    value={editForm.nameShort}
                                    onChange={(e) =>
                                        setEditForm((prev) => ({
                                            ...prev,
                                            nameShort: e.target.value,
                                        }))
                                    }
                                    className={styles.inputField}
                                />
                                <div className={styles.actionRow}>
                                    <Button text="Zapisz zmiany" onClick={handleUpdateSchool} />
                                    <button
                                        type="button"
                                        onClick={handleDeleteSingle}
                                        className={styles.dangerBtn}
                                    >
                                        Usuń szkołę
                                    </button>
                                </div>
                            </form>
                        </>
                    ) : (
                        <div className={styles.emptyState}>
                            Kliknij szkołę w tabeli poniżej, aby ją edytować lub usunąć.
                        </div>
                    )}
                </div>
            </div>

            <h2>Zarejestrowane placówki ({schools.length})</h2>

            <SchoolsTable schools={schools} onRowClick={handleRowClick} />
        </div>
    );
};

export default AdminSchoolsScreen;
