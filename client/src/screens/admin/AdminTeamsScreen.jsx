import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import { apiRequest } from "../../utils/apiFetcher";
import styles from "./AdminTeamsScreen.module.css";

const AdminTeamsScreen = () => {
    const navigate = useNavigate();
    const ITEMS_PER_PAGE = 50;

    const [teams, setTeams] = useState([]);
    const [loading, setLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [editingId, setEditingId] = useState(null);

    const [filters, setFilters] = useState({
        teamName: "",
        name1: "",
        name2: "",
        name3: "",
        name4: "",
        schoolRSPO: "",
    });

    const [editForm, setEditForm] = useState({
        teamName: "",
        name1: "",
        name2: "",
        name3: "",
        name4: "",
        schoolRSPO: "",
    });

    const fetchTeams = async () => {
        setLoading(true);
        const data = await apiRequest("/api/teams", null, "GET", navigate);
        if (data) {
            setTeams(data);
        }
        setLoading(false);
    };

    useEffect(() => {
        fetchTeams();
    }, [navigate]);

    useEffect(() => {
        setCurrentPage(1);
    }, [filters]);

    const handleFilterChange = (column, value) => {
        setFilters((prev) => ({ ...prev, [column]: value }));
    };

    const handleEditStart = (team) => {
        setEditingId(team.id);
        setEditForm({
            teamName: team.teamName || "",
            name1: team.name1 || "",
            name2: team.name2 || "",
            name3: team.name3 || "",
            name4: team.name4 || "",
            schoolRSPO: team.schoolRSPO || "",
        });
    };

    const handleEditCancel = () => {
        setEditingId(null);
    };

    const handleEditChange = (e) => {
        const { name, value } = e.target;
        setEditForm((prev) => ({ ...prev, [name]: value }));
    };

    const handleUpdateTeam = async (id) => {
        if (!editForm.teamName.trim()) {
            return alert("Nazwa zespołu nie może być pusta!");
        }

        const data = await apiRequest(
            `/api/admin/teams/${id}`,
            {
                id: id,
                teamName: editForm.teamName.trim(),
                name1: editForm.name1.trim(),
                name2: editForm.name2.trim(),
                name3: editForm.name3.trim(),
                name4: editForm.name4.trim(),
                schoolRSPO: Number(editForm.schoolRSPO),
            },
            "PUT",
            navigate
        );

        if (data) {
            setEditingId(null);
            await fetchTeams();
        }
    };

    const handleDeleteTeam = async (id) => {
        if (!window.confirm("Czy na pewno chcesz usunąć ten zespół z poziomu administratora?")) {
            return;
        }

        const success = await apiRequest(`/api/admin/teams/${id}`, null, "DELETE", navigate);

        if (success) {
            await fetchTeams();
        }
    };

    const filteredTeams = useMemo(() => {
        return teams.filter((team) => {
            const teamNameMatch = String(team.teamName ?? "")
                .toLowerCase()
                .includes(filters.teamName.toLowerCase());
            const name1Match = String(team.name1 ?? "")
                .toLowerCase()
                .includes(filters.name1.toLowerCase());
            const name2Match = String(team.name2 ?? "")
                .toLowerCase()
                .includes(filters.name2.toLowerCase());
            const name3Match = String(team.name3 ?? "")
                .toLowerCase()
                .includes(filters.name3.toLowerCase());
            const name4Match = String(team.name4 ?? "")
                .toLowerCase()
                .includes(filters.name4.toLowerCase());
            const schoolRspoMatch = String(team.schoolRSPO ?? "")
                .toLowerCase()
                .includes(filters.schoolRSPO.toLowerCase());

            return (
                teamNameMatch &&
                name1Match &&
                name2Match &&
                name3Match &&
                name4Match &&
                schoolRspoMatch
            );
        });
    }, [teams, filters]);

    const totalPages = Math.ceil(filteredTeams.length / ITEMS_PER_PAGE) || 1;

    const paginatedTeams = useMemo(() => {
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        return filteredTeams.slice(startIndex, startIndex + ITEMS_PER_PAGE);
    }, [filteredTeams, currentPage]);

    return (
        <div className="container">
            <AdminHeader navigate={navigate} />
            <div className={styles.scrollContainer}>
                {loading ? (
                    <div className={styles.loader}>Ładowanie list zespołów...</div>
                ) : (
                    <table className={styles.table}>
                        <thead>
                            <tr>
                                <th className={styles.th}>
                                    <div>Nazwa zespołu</div>
                                    <input
                                        type="text"
                                        value={filters.teamName}
                                        onChange={(e) =>
                                            handleFilterChange("teamName", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>
                                    <div>Członek 1</div>
                                    <input
                                        type="text"
                                        value={filters.name1}
                                        onChange={(e) =>
                                            handleFilterChange("name1", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>
                                    <div>Członek 2</div>
                                    <input
                                        type="text"
                                        value={filters.name2}
                                        onChange={(e) =>
                                            handleFilterChange("name2", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>
                                    <div>Członek 3</div>
                                    <input
                                        type="text"
                                        value={filters.name3}
                                        onChange={(e) =>
                                            handleFilterChange("name3", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>
                                    <div>Członek 4</div>
                                    <input
                                        type="text"
                                        value={filters.name4}
                                        onChange={(e) =>
                                            handleFilterChange("name4", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>
                                    <div>RSPO Szkoły</div>
                                    <input
                                        type="text"
                                        value={filters.schoolRSPO}
                                        onChange={(e) =>
                                            handleFilterChange("schoolRSPO", e.target.value)
                                        }
                                        className={styles.input}
                                        placeholder="Szukaj..."
                                    />
                                </th>
                                <th className={styles.th}>Akcje</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr className={styles.paginationRow}>
                                <td colSpan={7} className={styles.td} style={{ padding: 0 }}>
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
                                            onClick={() =>
                                                setCurrentPage((prev) => Math.max(prev - 1, 1))
                                            }
                                            disabled={currentPage === 1}
                                        >
                                            Poprzednia
                                        </button>
                                        <span className={styles.pageInfo}>
                                            Strona <strong>{currentPage}</strong> z{" "}
                                            <strong>{totalPages}</strong> (Filtrowanych:{" "}
                                            {filteredTeams.length})
                                        </span>
                                        <button
                                            className={styles.pageBtn}
                                            onClick={() =>
                                                setCurrentPage((prev) =>
                                                    Math.min(prev + 1, totalPages)
                                                )
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

                            {paginatedTeams.length === 0 ? (
                                <tr>
                                    <td colSpan={7} className={`${styles.td} ${styles.emptyCell}`}>
                                        Brak zespołów spełniających kryteria wyszukiwania.
                                    </td>
                                </tr>
                            ) : (
                                paginatedTeams.map((team) => {
                                    const isEditing = editingId === team.id;
                                    return (
                                        <tr key={team.id}>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <input
                                                        type="text"
                                                        name="teamName"
                                                        value={editForm.teamName}
                                                        onChange={handleEditChange}
                                                        className={styles.editInput}
                                                    />
                                                ) : (
                                                    team.teamName
                                                )}
                                            </td>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <input
                                                        type="text"
                                                        name="name1"
                                                        value={editForm.name1}
                                                        onChange={handleEditChange}
                                                        className={styles.editInput}
                                                    />
                                                ) : (
                                                    team.name1 || "-"
                                                )}
                                            </td>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <input
                                                        type="text"
                                                        name="name2"
                                                        value={editForm.name2}
                                                        onChange={handleEditChange}
                                                        className={styles.editInput}
                                                    />
                                                ) : (
                                                    team.name2 || "-"
                                                )}
                                            </td>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <input
                                                        type="text"
                                                        name="name3"
                                                        value={editForm.name3}
                                                        onChange={handleEditChange}
                                                        className={styles.editInput}
                                                    />
                                                ) : (
                                                    team.name3 || "-"
                                                )}
                                            </td>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <input
                                                        type="text"
                                                        name="name4"
                                                        value={editForm.name4}
                                                        onChange={handleEditChange}
                                                        className={styles.editInput}
                                                    />
                                                ) : (
                                                    team.name4 || "-"
                                                )}
                                            </td>
                                            <td className={styles.td}>{team.schoolRSPO || "-"}</td>
                                            <td className={styles.td}>
                                                {isEditing ? (
                                                    <div className={styles.actions}>
                                                        <button
                                                            onClick={() =>
                                                                handleUpdateTeam(team.id)
                                                            }
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
                                                        <button
                                                            onClick={() => handleEditStart(team)}
                                                            className={styles.btnEdit}
                                                        >
                                                            Edytuj
                                                        </button>
                                                        <button
                                                            onClick={() =>
                                                                handleDeleteTeam(team.id)
                                                            }
                                                            className={styles.btnDelete}
                                                        >
                                                            Usuń
                                                        </button>
                                                    </div>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
};

export default AdminTeamsScreen;
