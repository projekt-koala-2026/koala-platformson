import { useMemo, useState } from "react";
import Button from "./Button";

const SchoolsTable = ({ schools = [], actionsRenderer, onAddNewSchool, onUpdateSchool }) => {
    const [filters, setFilters] = useState({
        rspo: "",
        name: "",
        nameShort: "",
        state: "",
        city: "",
        type: "",
        addres: "",
    });

    const [newSchool, setNewSchool] = useState({
        rspo: "",
        name: "",
        nameShort: "",
        state: "",
        city: "",
        type: "",
        addres: "",
    });

    const [editingRspo, setEditingRspo] = useState(null);
    const [editData, setEditData] = useState({
        name: "",
        nameShort: "",
    });

    const handleFilterChange = (column, value) => {
        setFilters((prev) => ({ ...prev, [column]: value }));
    };

    const handleNewSchoolChange = (column, value) => {
        setNewSchool((prev) => ({ ...prev, [column]: value }));
    };

    const handleEditStart = (school) => {
        setEditingRspo(school.rspo);
        setEditData({
            name: school.name,
            nameShort: school.nameShort || "",
        });
    };

    const handleEditCancel = () => {
        setEditingRspo(null);
        setEditData({ name: "", nameShort: "" });
    };

    const handleEditSave = (school) => {
        if (!editData.name.trim()) {
            return alert("Nazwa szkoły nie może być pusta!");
        }
        onUpdateSchool({
            ...school,
            name: editData.name.trim(),
            nameShort: editData.nameShort.trim() || null,
        });
        setEditingRspo(null);
    };

    const handleCreateClick = () => {
        if (!newSchool.rspo.trim() || !newSchool.name.trim()) {
            return alert("Pola RSPO oraz Nazwa są obowiązkowe!");
        }

        const payload = {
            rspo: parseInt(newSchool.rspo.trim(), 10),
            name: newSchool.name.trim(),
            nameShort: newSchool.nameShort.trim() || null,
            state: newSchool.state.trim(),
            city: newSchool.city.trim(),
            type: newSchool.type.trim(),
            addres: newSchool.addres.trim(),
        };

        if (isNaN(payload.rspo)) {
            return alert("Numer RSPO musi być liczbą!");
        }

        onAddNewSchool(payload);
        setNewSchool({
            rspo: "",
            name: "",
            nameShort: "",
            state: "",
            city: "",
            type: "",
            addres: "",
        });
    };

    const filteredSchools = useMemo(() => {
        return schools.filter((school) => {
            const rspoMatch = String(school.rspo ?? "")
                .toLowerCase()
                .includes(filters.rspo.toLowerCase());
            const nameMatch = String(school.name ?? "")
                .toLowerCase()
                .includes(filters.name.toLowerCase());
            const nameShortMatch = String(school.nameShort ?? "")
                .toLowerCase()
                .includes(filters.nameShort.toLowerCase());
            const stateMatch = String(school.state ?? "")
                .toLowerCase()
                .includes(filters.state.toLowerCase());
            const cityMatch = String(school.city ?? "")
                .toLowerCase()
                .includes(filters.city.toLowerCase());
            const typeMatch = String(school.type ?? "")
                .toLowerCase()
                .includes(filters.type.toLowerCase());
            const addresMatch = String(school.addres ?? "")
                .toLowerCase()
                .includes(filters.addres.toLowerCase());

            return (
                rspoMatch &&
                nameMatch &&
                nameShortMatch &&
                stateMatch &&
                cityMatch &&
                typeMatch &&
                addresMatch
            );
        });
    }, [schools, filters]);

    const styles = {
        scrollContainer: {
            width: "100%",
            maxHeight: "600px",
            overflowX: "auto",
            overflowY: "auto",
            marginTop: "15px",
            border: "1px solid #ddd",
            borderRadius: "6px",
        },
        table: {
            width: "100%",
            borderCollapse: "collapse",
            fontSize: "0.9rem",
            textAlign: "left",
            minWidth: "1100px",
        },
        th: {
            backgroundColor: "#f5f5f5",
            padding: "10px",
            borderBottom: "2px solid #ddd",
            fontWeight: "bold",
            position: "sticky",
            top: 0,
            zIndex: 2,
        },
        td: {
            padding: "10px",
            borderBottom: "1px solid #ddd",
            verticalAlign: "middle",
        },
        input: {
            width: "100%",
            padding: "5px",
            marginTop: "5px",
            boxSizing: "border-box",
            border: "1px solid #ccc",
            borderRadius: "4px",
            fontSize: "0.8rem",
        },
        createInput: {
            width: "100%",
            padding: "6px",
            boxSizing: "border-box",
            border: "1px solid #2D8A4E",
            borderRadius: "4px",
            fontSize: "0.85rem",
        },
        editInput: {
            width: "100%",
            padding: "6px",
            boxSizing: "border-box",
            border: "1px solid #007bff",
            borderRadius: "4px",
            fontSize: "0.85rem",
        },
        createRow: {
            backgroundColor: "rgba(45, 138, 78, 0.05)",
            position: "sticky",
            top: "75px",
            zIndex: 1,
        },
        btnSuccess: {
            backgroundColor: "#2D8A4E",
            color: "#fff",
            border: "none",
            padding: "6px 12px",
            borderRadius: "4px",
            cursor: "pointer",
            fontWeight: "bold",
            width: "100%",
        },
        btnPrimary: {
            backgroundColor: "#007bff",
            color: "#fff",
            border: "none",
            padding: "6px 10px",
            borderRadius: "4px",
            cursor: "pointer",
        },
        btnSecondary: {
            backgroundColor: "#6c757d",
            color: "#fff",
            border: "none",
            padding: "6px 10px",
            borderRadius: "4px",
            cursor: "pointer",
        },
    };

    return (
        <div style={styles.scrollContainer}>
            <table style={styles.table}>
                <thead>
                    <tr>
                        <th style={styles.th}>
                            <div>RSPO</div>
                            <input
                                type="text"
                                value={filters.rspo}
                                onChange={(e) => handleFilterChange("rspo", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Nazwa</div>
                            <input
                                type="text"
                                value={filters.name}
                                onChange={(e) => handleFilterChange("name", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Skrócona nazwa</div>
                            <input
                                type="text"
                                value={filters.nameShort}
                                onChange={(e) => handleFilterChange("nameShort", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Województwo</div>
                            <input
                                type="text"
                                value={filters.state}
                                onChange={(e) => handleFilterChange("state", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Miasto</div>
                            <input
                                type="text"
                                value={filters.city}
                                onChange={(e) => handleFilterChange("city", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Typ</div>
                            <input
                                type="text"
                                value={filters.type}
                                onChange={(e) => handleFilterChange("type", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>
                            <div>Adres</div>
                            <input
                                type="text"
                                value={filters.addres}
                                onChange={(e) => handleFilterChange("addres", e.target.value)}
                                style={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th style={styles.th}>Akcje</th>
                    </tr>
                </thead>
                <tbody>
                    <tr style={styles.createRow}>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Nowy RSPO"
                                value={newSchool.rspo}
                                onChange={(e) => handleNewSchoolChange("rspo", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Pełna nazwa placówki"
                                value={newSchool.name}
                                onChange={(e) => handleNewSchoolChange("name", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Skrót (np. SP1)"
                                value={newSchool.nameShort}
                                onChange={(e) => handleNewSchoolChange("nameShort", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Województwo"
                                value={newSchool.state}
                                onChange={(e) => handleNewSchoolChange("state", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Miasto"
                                value={newSchool.city}
                                onChange={(e) => handleNewSchoolChange("city", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Typ"
                                value={newSchool.type}
                                onChange={(e) => handleNewSchoolChange("type", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <input
                                type="text"
                                placeholder="Adres"
                                value={newSchool.addres}
                                onChange={(e) => handleNewSchoolChange("addres", e.target.value)}
                                style={styles.createInput}
                            />
                        </td>
                        <td style={styles.td}>
                            <button onClick={handleCreateClick} style={styles.btnSuccess}>
                                Dodaj +
                            </button>
                        </td>
                    </tr>

                    {filteredSchools.length === 0 ? (
                        <tr>
                            <td
                                colSpan={8}
                                style={{ ...styles.td, textAlign: "center", color: "#666" }}
                            >
                                Brak danych spełniających kryteria.
                            </td>
                        </tr>
                    ) : (
                        filteredSchools.map((school) => {
                            const isEditing = editingRspo === school.rspo;
                            return (
                                <tr key={school.rspo}>
                                    <td style={styles.td}>{school.rspo}</td>
                                    <td style={styles.td}>
                                        {isEditing ? (
                                            <input
                                                type="text"
                                                value={editData.name}
                                                onChange={(e) =>
                                                    setEditData((prev) => ({
                                                        ...prev,
                                                        name: e.target.value,
                                                    }))
                                                }
                                                style={styles.editInput}
                                            />
                                        ) : (
                                            school.name
                                        )}
                                    </td>
                                    <td style={styles.td}>
                                        {isEditing ? (
                                            <input
                                                type="text"
                                                value={editData.nameShort}
                                                onChange={(e) =>
                                                    setEditData((prev) => ({
                                                        ...prev,
                                                        nameShort: e.target.value,
                                                    }))
                                                }
                                                style={styles.editInput}
                                                placeholder="Brak skrótu"
                                            />
                                        ) : (
                                            school.nameShort || "-"
                                        )}
                                    </td>
                                    <td style={styles.td}>{school.state}</td>
                                    <td style={styles.td}>{school.city}</td>
                                    <td style={styles.td}>{school.type}</td>
                                    <td style={styles.td}>{school.addres}</td>
                                    <td style={styles.td}>
                                        {isEditing ? (
                                            <div style={{ display: "flex", gap: "5px" }}>
                                                <button
                                                    onClick={() => handleEditSave(school)}
                                                    style={styles.btnPrimary}
                                                >
                                                    Zapisz
                                                </button>
                                                <button
                                                    onClick={handleEditCancel}
                                                    style={styles.btnSecondary}
                                                >
                                                    Anuluj
                                                </button>
                                            </div>
                                        ) : (
                                            <div style={{ display: "flex", gap: "5px" }}>
                                                <Button
                                                    onClick={() => handleEditStart(school)}
                                                    text={"Edytuj"}
                                                />
                                                {actionsRenderer(school)}
                                            </div>
                                        )}
                                    </td>
                                </tr>
                            );
                        })
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default SchoolsTable;
