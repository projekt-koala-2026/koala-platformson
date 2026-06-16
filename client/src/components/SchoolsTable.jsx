import { useEffect, useMemo, useState } from "react";
import styles from "./SchoolsTable.module.css";

const SchoolsTable = ({ schools = [], onRowClick }) => {
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

    const [currentPage, setCurrentPage] = useState(1);

    useEffect(() => {
        setCurrentPage(1);
    }, [schools, filters]);

    const handleFilterChange = (column, value) => {
        setFilters((prev) => ({ ...prev, [column]: value }));
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
                    Strona <strong>{currentPage}</strong> z <strong>{totalPages}</strong>{" "}
                    (Filtrowanych: {filteredSchools.length})
                </span>
                <button
                    className={styles.pageBtn}
                    onClick={() => setCurrentPage((prev) => Math.min(prev + 1, totalPages))}
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
                            <div>Pełna nazwa szkoły</div>
                            <input
                                type="text"
                                value={filters.name}
                                onChange={(e) => handleFilterChange("name", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Nazwa skrócona</div>
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
                            <div>Miejscowość</div>
                            <input
                                type="text"
                                value={filters.city}
                                onChange={(e) => handleFilterChange("city", e.target.value)}
                                className={styles.input}
                                placeholder="Szukaj..."
                            />
                        </th>
                        <th className={styles.th}>
                            <div>Typ placówki</div>
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
                    </tr>
                </thead>
                <tbody>
                    {paginatedSchools.length === 0 ? (
                        <tr>
                            <td colSpan={7} className={`${styles.td} ${styles.emptyCell}`}>
                                Brak szkół spełniających kryteria wyszukiwania.
                            </td>
                        </tr>
                    ) : (
                        paginatedSchools.map((school) => (
                            <tr
                                key={school.rspo}
                                onClick={() => onRowClick && onRowClick(school)}
                                className={styles.clickableRow}
                            >
                                <td className={styles.td}>{school.rspo}</td>
                                <td className={styles.td}>{school.name}</td>
                                <td className={styles.td}>{school.nameShort || "-"}</td>
                                <td className={styles.td}>{school.state}</td>
                                <td className={styles.td}>{school.city}</td>
                                <td className={styles.td}>{school.type}</td>
                                <td className={styles.td}>{school.addres}</td>
                            </tr>
                        ))
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default SchoolsTable;
