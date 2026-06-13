import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest } from "../utils/apiFetcher";
import styles from "./PublicFooter.module.css";

const PublicFooter = () => {
    const navigate = useNavigate();
    const [sponsors, setSponsors] = useState([]);

    useEffect(() => {
        const fetchSponsors = async () => {
            const data = await apiRequest("/api/admin/sponsors", null, "GET", navigate);
            if (data) {
                setSponsors(data);
            }
        };
        fetchSponsors();
    }, [navigate]);

    return (
        <footer className={styles.footer}>
            <div className={styles.content}>
                {sponsors.length > 0 && <h4 className={styles.title}>Nasi Sponsorzy</h4>}

                <div className={styles.sponsorsGrid}>
                    {sponsors.map((sponsor) => (
                        <a
                            key={sponsor.id}
                            href={sponsor.websiteUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className={
                                sponsor.logoUrl ? styles.sponsorLink : styles.sponsorFallback
                            }
                            title={sponsor.description || sponsor.name}
                        >
                            {sponsor.logoUrl ? (
                                <img
                                    src={sponsor.logoUrl}
                                    alt={sponsor.name}
                                    className={styles.logo}
                                />
                            ) : (
                                sponsor.name
                            )}
                        </a>
                    ))}
                </div>

                <div className={styles.copyright}>
                    &copy; {new Date().getFullYear()} Koala — Wielkopolski konkurs grup szkolnych.
                    All rights reserved.
                </div>
            </div>
        </footer>
    );
};

export default PublicFooter;
