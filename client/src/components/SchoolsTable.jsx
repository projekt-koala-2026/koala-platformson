import { useEffect, useMemo, useState } from "react";
import Button from "./Button";
import styles from "./SchoolsTable.module.css";

const SchoolsTable = ({
    schools = [],
    actionsRenderer,
    onAddNewSchool,
    onUpdateSchool,
    hideActions = false,
}) => {
    const ITEMS_PER_PAGE = 50;

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

    const [currentPage, setCurrentPage] = useState(1);

    useEffect(() => {
        setCurrentPage(1);
    }, [schools, filters]);

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

    const totalPages = Math.ceil(filteredSchools.length / ITEMS_PER_PAGE) || 1;

    const paginatedSchools = useMemo(() => {
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        return filteredSchools.slice(startIndex, startIndex + ITEMS_PER_PAGE);
    }, [filteredSchools, currentPage]);

    return (
        <div className={styles.scrollContainer}>
            <table className={styles.table}>
                <thead>
                    <tr>
                        <th className={styles.th}>
                            <div>RSPO</div>
                            <input
                                type="text"
                                value={filters.rspo}
                                onChange={(e) => handleFilterChange("rspo", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Nazwa</div>
                            <input
                                type="text"
                                value={filters.name}
                                onChange={(e) => handleFilterChange("name", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Skrócona nazwa</div>
                            <input
                                type="text"
                                value={filters.nameShort}
                                onChange={(e) => handleFilterChange("nameShort", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Województwo</div>
                            <input
                                type="text"
                                value={filters.state}
                                onChange={(e) => handleFilterChange("state", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Miasto</div>
                            <input
                                type="text"
                                value={filters.city}
                                onChange={(e) => handleFilterChange("city", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Typ</div>
                            <input
                                type="text"
                                value={filters.type}
                                onChange={(e) => handleFilterChange("type", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Adres</div>
                            <input
                                type="text"
                                value={filters.addres}
                                onChange={(e) => handleFilterChange("addres", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        {!hideActions && <th className={styles.th}>Akcje</th>}
                    </tr>
                </thead>
                <tbody>
                    <tr className={styles.paginationRow}>
                        <td
                            colSpan={hideActions ? 7 : 8}
                            className={styles.td}
                            style={{ padding: 0 }}
                        >
                            <div className={styles.paginationBar}>
                                <button
                                    className={styles.pageBtn}
                                    onClick={() => setCurrentPage(1)}
                                    disabled={currentPage === 1}
                                >
                                    &lt;&lt;
                                </button>
                                <button
                                    className={styles.pageBtn}
                                    onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                                    disabled={currentPage === 1}
                                >
                                    Poprzednia
                                </button>
                                <span className={styles.pageInfo}>
                                    Strona <strong>{currentPage}</strong> z{" "}
                                    <strong>{totalPages}</strong> (Filtrowanych:{" "}
                                    {filteredSchools.length})
                                </span>
                                <button
                                    className={styles.pageBtn}
                                    onClick={() =>
                                        setCurrentPage((prev) => Math.min(prev + 1, totalPages))
                                    }
                                    disabled={currentPage === totalPages}
                                >
                                    Następna
                                </button>
                                <button
                                    className={styles.pageBtn}
                                    onClick={() => setCurrentPage(totalPages)}
                                    disabled={currentPage === totalPages}
                                >
                                    &gt;&gt;
                                </button>
                            </div>
                        </td>
                    </tr>

                    {onAddNewSchool && (
                        <tr className={styles.createRow}>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Nowy RSPO"
                                    value={newSchool.rspo}
                                    onChange={(e) => handleNewSchoolChange("rspo", e.target.value)}
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Pełna nazwa placówki"
                                    value={newSchool.name}
                                    onChange={(e) => handleNewSchoolChange("name", e.target.value)}
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Skrót (np. SP1)"
                                    value={newSchool.nameShort}
                                    onChange={(e) =>
                                        handleNewSchoolChange("nameShort", e.target.value)
                                    }
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Województwo"
                                    value={newSchool.state}
                                    onChange={(e) => handleNewSchoolChange("state", e.target.value)}
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Miasto"
                                    value={newSchool.city}
                                    onChange={(e) => handleNewSchoolChange("city", e.target.value)}
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Typ"
                                    value={newSchool.type}
                                    onChange={(e) => handleNewSchoolChange("type", e.target.value)}
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <input
                                    type="text"
                                    placeholder="Adres"
                                    value={newSchool.addres}
                                    onChange={(e) =>
                                        handleNewSchoolChange("addres", e.target.value)
                                    }
                                    className={styles.createInput}
                                />
                            </td>
                            <td className={styles.td}>
                                <button onClick={handleCreateClick} className={styles.btnSuccess}>
                                    Dodaj +
                                </button>
                            </td>
                        </tr>
                    )}

                    {paginatedSchools.length === 0 ? (
                        <tr>
                            <td
                                colSpan={hideActions ? 7 : 8}
                                className={`${styles.td} ${styles.emptyCell}`}
                            >
                                Brak danych spełniających kryteria.
                            </td>
                        </tr>
                    ) : (
                        paginatedSchools.map((school) => {
                            const isEditing = editingRspo === school.rspo;
                            return (
                                <tr key={school.rspo}>
                                    <td className={styles.td}>{school.rspo}</td>
                                    <td className={styles.td}>
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
                                                className={styles.editInput}
                                            />
                                        ) : (
                                            school.name
                                        )}
                                    </td>
                                    <td className={styles.td}>
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
                                                className={styles.editInput}
                                                placeholder="Brak skrótu"
                                            />
                                        ) : (
                                            school.nameShort || "-"
                                        )}
                                    </td>
                                    <td className={styles.td}>{school.state}</td>
                                    <td className={styles.td}>{school.city}</td>
                                    <td className={styles.td}>{school.type}</td>
                                    <td className={styles.td}>{school.addres}</td>
                                    {!hideActions && (
                                        <td className={styles.td}>
                                            {isEditing ? (
                                                <div className={styles.actions}>
                                                    <button
                                                        onClick={() => handleEditSave(school)}
                                                        className={styles.btnPrimary}
                                                    >
                                                        Zapisz
                                                    </button>
                                                    <button
                                                        onClick={handleEditCancel}
                                                        className={styles.btnSecondary}
                                                    >
                                                        Anuluj
                                                    </button>
                                                </div>
                                            ) : (
                                                <div className={styles.actions}>
                                                    <Button
                                                        onClick={() => handleEditStart(school)}
                                                        text={"Edytuj"}
                                                    />
                                                    {actionsRenderer && actionsRenderer(school)}
                                                </div>
                                            )}
                                        </td>
                                    )}
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
