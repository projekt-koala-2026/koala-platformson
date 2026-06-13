import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";
import styles from "./EditEditionScreen.module.css";

const EditionsScreen = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);

    const [editions, setEditions] = useState(null);
    const [editingEdition, setEditingEdition] = useState(null);
    const [isAdding, setIsAdding] = useState(false);

    const [title, setTitle] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");

    const handleBack = () => {
        navigate("/admin");
    };

    const handleDelete = async (edition) => {
        const confirmed = window.confirm(`Czy na pewno chcesz usunąć edycję: ${edition.title}?`);
        if (!confirmed) return;

        const data = await apiRequest(`/api/admin/edition/${edition.id}`, {}, "DELETE", navigate);
        if (data) {
            setEditions((prev) => prev.filter((e) => e.id !== edition.id));
        }
    };

    const handleEditClick = (edition) => {
        if (editingEdition?.id === edition.id) {
            setEditingEdition(null);
            setTitle("");
            setStartDate("");
            setEndDate("");
        } else {
            setIsAdding(false);
            setEditingEdition(edition);
            setTitle(edition.title || "");
            setStartDate(edition.startDate ? edition.startDate.substring(0, 16) : "");
            setEndDate(edition.endDate ? edition.endDate.substring(0, 16) : "");
        }
    };

    const handleSaveEdit = async () => {
        const id = editingEdition.id;

        if (title !== editingEdition.title) {
            await apiRequest("/api/admin/edition/title", { id, title }, "PUT", navigate);
        }

        if (startDate !== editingEdition.startDate) {
            await apiRequest(
                "/api/admin/edition/start-date",
                { id, startDate: new Date(startDate).toISOString() },
                "PUT",
                navigate
            );
        }

        if (endDate !== editingEdition.endDate) {
            await apiRequest(
                "/api/admin/edition/end-date",
                { id, endDate: new Date(endDate).toISOString() },
                "PUT",
                navigate
            );
        }

        setEditions((prev) =>
            prev.map((e) =>
                e.id === id
                    ? {
                          ...e,
                          title,
                          startDate: new Date(startDate).toISOString(),
                          endDate: new Date(endDate).toISOString(),
                      }
                    : e
            )
        );

        setEditingEdition(null);
        setTitle("");
        setStartDate("");
        setEndDate("");
    };

    const handleCreate = async () => {
        const bodyData = {
            title,
            startDate: new Date(startDate).toISOString(),
            endDate: new Date(endDate).toISOString(),
        };

        const data = await apiRequest("/api/admin/edition", bodyData, "POST", navigate);
        if (data) {
            setEditions((prev) => [...prev, data]);
            setIsAdding(false);
            setTitle("");
            setStartDate("");
            setEndDate("");
        }
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/admin/edition", null, "GET", navigate);
            setEditions(data);
        };

        getData();
    }, [navigate, isAdminUser]);

    return (
        <div className={`container ${styles.container}`}>
            <h1>Zarządzanie Edycjami</h1>
            <Button text={"Wróć do panelu"} onClick={handleBack} />

            {/* Panel dodawania nowej edycji */}
            <div className={styles.formSection}>
                {isAdding ? (
                    <div className={styles.formBox}>
                        <h3>Dodaj nową edycję</h3>
                        <div className={styles.fieldGroup}>
                            <label className={styles.label}>Tytuł edycji:</label>
                            <input
                                type="text"
                                placeholder="np. Edycja II"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                className={styles.input}
                            />
                        </div>
                        <div className={styles.fieldGroup}>
                            <label className={styles.label}>Data rozpoczęcia:</label>
                            <input
                                type="datetime-local"
                                value={startDate}
                                onChange={(e) => setStartDate(e.target.value)}
                                className={styles.input}
                            />
                        </div>
                        <div className={styles.fieldGroup}>
                            <label className={styles.label}>Data zakończenia:</label>
                            <input
                                type="datetime-local"
                                value={endDate}
                                onChange={(e) => setEndDate(e.target.value)}
                                className={styles.input}
                            />
                        </div>
                        <div className={styles.buttonRow}>
                            <Button text={"Zapisz edycję"} onClick={handleCreate} />
                            <Button
                                text={"Anuluj"}
                                onClick={() => {
                                    setIsAdding(false);
                                    setTitle("");
                                    setStartDate("");
                                    setEndDate("");
                                }}
                            />
                        </div>
                    </div>
                ) : (
                    !editingEdition && (
                        <Button text={"Dodaj nową edycję"} onClick={() => setIsAdding(true)} />
                    )
                )}
            </div>

            {/* Listowanie zasobów z bazy danych */}
            {editions && (
                <div className={styles.list}>
                    {editions.map((item, idx) => (
                        <div key={"edition-tile-" + idx} className={styles.tile}>
                            <div className={styles.tileHeader}>
                                <div className={styles.tileInfo}>
                                    <span className={styles.tileTitle}>{item.title}</span>
                                    <span className={styles.tileMeta}>
                                        Od: {new Date(item.startDate).toLocaleString()} | Do:{" "}
                                        {new Date(item.endDate).toLocaleString()}
                                    </span>
                                </div>

                                <div className={styles.tileActions}>
                                    <Button
                                        text={<FaEdit />}
                                        onClick={() => handleEditClick(item)}
                                    />
                                    <Button text={<FaTrash />} onClick={() => handleDelete(item)} />
                                </div>
                            </div>

                            {/* Formularz edycji konkretnego rekordu */}
                            {editingEdition?.id === item.id && (
                                <div className={styles.editBox}>
                                    <h3>Modyfikacja edycji: {editingEdition.title}</h3>
                                    <div className={styles.fieldGroup}>
                                        <label className={styles.label}>Zmień tytuł:</label>
                                        <input
                                            type="text"
                                            value={title}
                                            onChange={(e) => setTitle(e.target.value)}
                                            className={styles.input}
                                        />
                                    </div>
                                    <div className={styles.fieldGroup}>
                                        <label className={styles.label}>Zmień datę startu:</label>
                                        <input
                                            type="datetime-local"
                                            value={startDate}
                                            onChange={(e) => setStartDate(e.target.value)}
                                            className={styles.input}
                                        />
                                    </div>
                                    <div className={styles.fieldGroup}>
                                        <label className={styles.label}>Zmień datę końca:</label>
                                        <input
                                            type="datetime-local"
                                            value={endDate}
                                            onChange={(e) => setEndDate(e.target.value)}
                                            className={styles.input}
                                        />
                                    </div>
                                    <div className={styles.buttonRow}>
                                        <Button text={"Zapisz zmiany"} onClick={handleSaveEdit} />
                                        <Button
                                            text={"Anuluj"}
                                            onClick={() => handleEditClick(item)}
                                        />
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default EditionsScreen;
