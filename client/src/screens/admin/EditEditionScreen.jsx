import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

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
        <div className="container" style={{ minWidth: "50%" }}>
            <h1>Zarządzanie Edycjami</h1>
            <Button text={"Wróć do panelu"} onClick={handleBack} />

            {/* Panel dodawania nowej edycji */}
            <div style={{ marginTop: "20px", marginBottom: "20px" }}>
                {isAdding ? (
                    <div
                        style={{
                            border: "1px solid var(--border-color)",
                            padding: "15px",
                            borderRadius: "8px",
                        }}
                    >
                        <h3>Dodaj nową edycję</h3>
                        <div style={{ marginBottom: "10px" }}>
                            <label
                                style={{
                                    display: "block",
                                    fontSize: "0.85rem",
                                    fontWeight: "bold",
                                }}
                            >
                                Tytuł edycji:
                            </label>
                            <input
                                type="text"
                                placeholder="np. Edycja II"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                style={{
                                    width: "100%",
                                    padding: "6px",
                                    borderRadius: "4px",
                                    border: "1px solid #ccc",
                                }}
                            />
                        </div>
                        <div style={{ marginBottom: "10px" }}>
                            <label
                                style={{
                                    display: "block",
                                    fontSize: "0.85rem",
                                    fontWeight: "bold",
                                }}
                            >
                                Data rozpoczęcia:
                            </label>
                            <input
                                type="datetime-local"
                                value={startDate}
                                onChange={(e) => setStartDate(e.target.value)}
                                style={{
                                    width: "100%",
                                    padding: "6px",
                                    borderRadius: "4px",
                                    border: "1px solid #ccc",
                                }}
                            />
                        </div>
                        <div style={{ marginBottom: "10px" }}>
                            <label
                                style={{
                                    display: "block",
                                    fontSize: "0.85rem",
                                    fontWeight: "bold",
                                }}
                            >
                                Data zakończenia:
                            </label>
                            <input
                                type="datetime-local"
                                value={endDate}
                                onChange={(e) => setEndDate(e.target.value)}
                                style={{
                                    width: "100%",
                                    padding: "6px",
                                    borderRadius: "4px",
                                    border: "1px solid #ccc",
                                }}
                            />
                        </div>
                        <div style={{ display: "flex", gap: "10px" }}>
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
                <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
                    {editions.map((item, idx) => (
                        <div
                            key={"edition-tile-" + idx}
                            style={{
                                border: "1px solid var(--border-color)",
                                padding: "15px",
                                borderRadius: "8px",
                                background: "#fff",
                            }}
                        >
                            <div style={{ display: "flex", alignItems: "center" }}>
                                <div style={{ display: "flex", flexDirection: "column" }}>
                                    <span style={{ fontWeight: "bold", fontSize: "1.1rem" }}>
                                        {item.title}
                                    </span>
                                    <span
                                        style={{
                                            fontSize: "0.85rem",
                                            color: "#666",
                                            marginTop: "4px",
                                        }}
                                    >
                                        Od: {new Date(item.startDate).toLocaleString()} | Do:{" "}
                                        {new Date(item.endDate).toLocaleString()}
                                    </span>
                                </div>

                                <div style={{ display: "flex", gap: "6px", marginLeft: "auto" }}>
                                    <Button
                                        text={<FaEdit />}
                                        onClick={() => handleEditClick(item)}
                                    />
                                    <Button text={<FaTrash />} onClick={() => handleDelete(item)} />
                                </div>
                            </div>

                            {/* Formularz edycji konkretnego rekordu */}
                            {editingEdition?.id === item.id && (
                                <div
                                    style={{
                                        marginTop: "15px",
                                        paddingTop: "15px",
                                        borderTop: "1px solid var(--border-color)",
                                    }}
                                >
                                    <h3>Modyfikacja edycji: {editingEdition.title}</h3>
                                    <div style={{ marginBottom: "10px" }}>
                                        <label
                                            style={{
                                                display: "block",
                                                fontSize: "0.85rem",
                                                fontWeight: "bold",
                                            }}
                                        >
                                            Zmień tytuł:
                                        </label>
                                        <input
                                            type="text"
                                            value={title}
                                            onChange={(e) => setTitle(e.target.value)}
                                            style={{
                                                width: "100%",
                                                padding: "6px",
                                                borderRadius: "4px",
                                                border: "1px solid #ccc",
                                            }}
                                        />
                                    </div>
                                    <div style={{ marginBottom: "10px" }}>
                                        <label
                                            style={{
                                                display: "block",
                                                fontSize: "0.85rem",
                                                fontWeight: "bold",
                                            }}
                                        >
                                            Zmień datę startu:
                                        </label>
                                        <input
                                            type="datetime-local"
                                            value={startDate}
                                            onChange={(e) => setStartDate(e.target.value)}
                                            style={{
                                                width: "100%",
                                                padding: "6px",
                                                borderRadius: "4px",
                                                border: "1px solid #ccc",
                                            }}
                                        />
                                    </div>
                                    <div style={{ marginBottom: "10px" }}>
                                        <label
                                            style={{
                                                display: "block",
                                                fontSize: "0.85rem",
                                                fontWeight: "bold",
                                            }}
                                        >
                                            Zmień datę końca:
                                        </label>
                                        <input
                                            type="datetime-local"
                                            value={endDate}
                                            onChange={(e) => setEndDate(e.target.value)}
                                            style={{
                                                width: "100%",
                                                padding: "6px",
                                                borderRadius: "4px",
                                                border: "1px solid #ccc",
                                            }}
                                        />
                                    </div>
                                    <div style={{ display: "flex", gap: "10px" }}>
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
