import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import { apiRequest } from "../../utils/apiFetcher";
import styles from "./CaptainHomeScreen.module.css";

const CaptainHomeScreen = () => {
    const id = useMemo(() => localStorage.getItem("userId"), []);
    const navigate = useNavigate();
    const [team, setTeam] = useState(null);
    const [loading, setLoading] = useState(true);
    const [isEditing, setIsEditing] = useState(false);

    const [teamForm, setTeamForm] = useState({
        teamName: "",
        member1: "",
        member2: "",
        member3: "",
        member4: "",
    });

    const fetchMyTeam = async () => {
        setLoading(true);
        const data = await apiRequest("/api/teams/my-team", null, "GET", navigate);
        if (data) {
            setTeam(data);
            setTeamForm({
                teamName: data.teamName || "",
                member1: data.name1 || "",
                member2: data.name2 || "",
                member3: data.name3 || "",
                member4: data.name4 || "",
            });
        } else {
            setTeam(null);
        }
        setLoading(false);
    };

    useEffect(() => {
        fetchMyTeam();
    }, [navigate]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setTeamForm((prev) => ({ ...prev, [name]: value }));
    };

    const handleCreateTeam = async (e) => {
        e.preventDefault();

        const data = await apiRequest(
            "/api/teams",
            {
                teamName: teamForm.teamName,
                name1: teamForm.member1,
                name2: teamForm.member2,
                name3: teamForm.member3,
                name4: teamForm.member4,
            },
            "POST",
            navigate
        );

        if (data) {
            await fetchMyTeam();
        }
    };

    const handleUpdateTeam = async (e) => {
        e.preventDefault();

        const data = await apiRequest(
            `/api/teams/${team.id}`,
            {
                id: team.id,
                teamName: teamForm.teamName,
                name1: teamForm.member1,
                name2: teamForm.member2,
                name3: teamForm.member3,
                name4: teamForm.member4,
            },
            "PUT",
            navigate
        );

        if (data) {
            setIsEditing(false);
            await fetchMyTeam();
        }
    };

    const handleDeleteTeam = async () => {
        if (
            !window.confirm("Czy na pewno chcesz usunąć ten zespół? Tej operacji nie można cofnąć.")
        ) {
            return;
        }

        const success = await apiRequest(`/api/teams/${team.id}`, null, "DELETE", navigate);

        if (success) {
            setTeam(null);
            setTeamForm({
                teamName: "",
                member1: "",
                member2: "",
                member3: "",
                member4: "",
            });
            setIsEditing(false);
        }
    };

    return (
        <>
            <PublicHeader navigate={navigate} />
            <div className={styles.container}>
                {loading ? (
                    <div className={styles.loader}>Ładowanie danych zespołu...</div>
                ) : team && !isEditing ? (
                    <div className={styles.card}>
                        <h3 className={styles.welcome}>Witaj w panelu kapitana!</h3>
                        <p className={styles.text}>Twój zarejestrowany zespół to:</p>
                        <div className={styles.teamBadge}>
                            <h2>{team.teamName}</h2>
                        </div>

                        <div className={styles.membersList}>
                            <h4>Skład drużyny:</h4>
                            <ul>
                                {team.name1 && <li>{team.name1}</li>}
                                {team.name2 && <li>{team.name2}</li>}
                                {team.name3 && <li>{team.name3}</li>}
                                {team.name4 && <li>{team.name4}</li>}
                            </ul>
                        </div>

                        <div className={styles.actionRow}>
                            <button onClick={() => setIsEditing(true)} className={styles.btnEdit}>
                                Edytuj dane
                            </button>
                            <button onClick={handleDeleteTeam} className={styles.btnDelete}>
                                Usuń zespół
                            </button>
                        </div>
                    </div>
                ) : (
                    <div className={styles.card}>
                        <h3 className={styles.title}>
                            {isEditing
                                ? "Edycja danych zespołu"
                                : "Wygląda na to, że nie posiadasz jeszcze zespołu"}
                        </h3>
                        <p className={styles.text}>
                            {isEditing
                                ? "Wprowadź nowe dane dla swojej drużyny i zapisz zmiany."
                                : "Aby móc w pełni uczestniczyć w rozgrywkach i zarządzać swoją drużyną, należy najpierw dokonać jej rejestracji w systemie."}
                        </p>
                        <form
                            onSubmit={isEditing ? handleUpdateTeam : handleCreateTeam}
                            className={styles.form}
                        >
                            <input
                                type="text"
                                name="teamName"
                                placeholder="Nazwa zespołu"
                                value={teamForm.teamName}
                                onChange={handleChange}
                            />

                            <input
                                type="text"
                                name="member1"
                                placeholder="Członek 1"
                                value={teamForm.member1}
                                onChange={handleChange}
                            />

                            <input
                                type="text"
                                name="member2"
                                placeholder="Członek 2"
                                value={teamForm.member2}
                                onChange={handleChange}
                            />

                            <input
                                type="text"
                                name="member3"
                                placeholder="Członek 3"
                                value={teamForm.member3}
                                onChange={handleChange}
                            />

                            <input
                                type="text"
                                name="member4"
                                placeholder="Członek 4"
                                value={teamForm.member4}
                                onChange={handleChange}
                            />

                            <button type="submit" className={styles.button}>
                                {isEditing ? "Zapisz zmiany" : "Załóż zespół"}
                            </button>

                            {isEditing && (
                                <button
                                    type="button"
                                    onClick={() => {
                                        setIsEditing(false);
                                        fetchMyTeam();
                                    }}
                                    className={styles.buttonSecondary}
                                >
                                    Anuluj
                                </button>
                            )}
                        </form>
                    </div>
                )}
            </div>
            <PublicFooter />
        </>
    );
};

export default CaptainHomeScreen;
