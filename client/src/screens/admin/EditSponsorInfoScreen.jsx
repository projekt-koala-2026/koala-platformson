import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import { useLoading } from "../../contexts/LoadingContext";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";
import styles from "./EditSponsorInfoScreen.module.css";

const EditSponsorInfo = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);

    const [sponsors, setSponsors] = useState([]);
    const [name, setName] = useState(null);
    const [websiteUrl, setWebsiteUrl] = useState(null);
    const [logoUrl, setLogoUrl] = useState(null);
    const [description, setDescription] = useState(null);
    const [addingSponsor, setAddingSponsor] = useState(null);
    const [editingSponsor, setEditingSponsor] = useState(null);

    const clearForm = () => {
        setName("");
        setWebsiteUrl("");
        setLogoUrl("");
        setDescription("");
    };

    const AddSponsor = () => {
        clearForm();
        setAddingSponsor(true);
    };

    const SaveSponsor = async () => {
        if (
            !websiteUrl ||
            (!websiteUrl.startsWith("http://") && !websiteUrl.startsWith("https://"))
        ) {
            alert(
                "podaj poprawny link do strony Sponsora. (Musi zaczynać się od http:// lub https://)"
            );
            return;
        }

        if (!logoUrl || (!logoUrl.startsWith("http://") && !logoUrl.startsWith("https://"))) {
            alert(
                "podaj poprawny link do logo Sponsora. (Musi zaczynać się od http:// lub https://)"
            );
            return;
        }

        startLoading();

        const data = await apiRequest(
            "/api/admin/sponsors",
            { name, websiteUrl, logoUrl, description },
            "POST",
            navigate
        );

        await new Promise((resolve) => setTimeout(resolve, 500));

        if (data) {
            setSponsors((prev) => [...prev, data]);
        }

        if (data) {
            setSponsors((prev) => [...prev, data]);
            clearForm();
            setAddingSponsor(false);
        }

        setAddingSponsor(false);
        stopLoading();
    };

    const DeleteSponsor = async (sponsor) => {
        const confirmed = window.confirm(`Czy na pewno chcesz usunąć Sponsora ${sponsor.name}?`);

        if (!confirmed) return;

        const apiLink = `/api/admin/sponsors/${sponsor.id}`;
        const data = await apiRequest(apiLink, {}, "DELETE", navigate);
        setSponsors((prev) => prev.filter((s) => s.id !== sponsor.id));
    };

    const EditSponsor = (sponsor) => {
        setEditingSponsor(editingSponsor === sponsor ? null : sponsor);
        setName(sponsor.name);
        setWebsiteUrl(sponsor.websiteUrl);
        setLogoUrl(sponsor.logoUrl);
        setDescription(sponsor.description);
    };

    const UpdateSponsor = async (sponsor) => {
        const apiLink = `/api/admin/sponsors/${sponsor.id}`;
        await apiRequest(apiLink, { name, websiteUrl, logoUrl, description }, "PUT", navigate);

        setSponsors((prev) =>
            prev.map((s) =>
                s.id === sponsor.id
                    ? {
                          ...s,
                          name: name,
                          websiteUrl: websiteUrl,
                          logoUrl: logoUrl,
                          description: description,
                      }
                    : s
            )
        );

        setEditingSponsor(false);
    };

    const handleBack = () => {
        navigate("/admin");
    };

    useEffect(() => {
        if (!isAdminEditor) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/admin/sponsors", null, "GET", navigate);
            if (!data || data.length === 0) {
                navigate("/admin/sponsors");
                return;
            }

            setSponsors(data);
        };

        if (isAdminEditor) getData();
    }, [navigate, isAdminEditor]);

    return (
        <div className={`container ${styles.container}`}>
            {isAdminEditor && (
                <>
                    <h1>Lista Sponsorów</h1>
                    <div>
                        <Button text={"Wróć do panelu"} onClick={handleBack} />
                        <Button text={"Dodaj nowego Sponsora"} onClick={AddSponsor} />
                    </div>
                    {addingSponsor === true && (
                        <div className="container">
                            <h2>Dodaj Sponsora</h2>
                            <input
                                type="text"
                                placeholder="Nazwa"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                required
                            />
                            <input
                                type="text"
                                placeholder="Link do strony Sponsora"
                                value={websiteUrl}
                                onChange={(e) => setWebsiteUrl(e.target.value)}
                                required
                            />
                            <input
                                type="text"
                                placeholder="Link do Loga Sponsora"
                                value={logoUrl}
                                onChange={(e) => setLogoUrl(e.target.value)}
                                required
                            />
                            <input
                                type="text"
                                placeholder="Opis"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                            />
                            <div className={styles.formActions}>
                                <Button text={"Zapisz"} onClick={SaveSponsor} />
                                <Button
                                    text={"Anuluj"}
                                    onClick={() => {
                                        setAddingSponsor(false);
                                        clearForm;
                                    }}
                                />
                            </div>
                        </div>
                    )}
                    {sponsors && (
                        <ContentsListBox>
                            {sponsors.map((item) => (
                                <ContentsListTile key={item.id}>
                                    <div className={styles.listRow}>
                                        <div className={styles.textColumn}>
                                            <h3>
                                                <a
                                                    href={item.websiteUrl}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                >
                                                    {item.name}
                                                </a>
                                            </h3>
                                            <span>{item.description}</span>
                                        </div>
                                        <div className={styles.actions}>
                                            <Button
                                                text={<FaEdit />}
                                                onClick={() => EditSponsor(item)}
                                            />
                                            <Button
                                                text={<FaTrash />}
                                                onClick={() => DeleteSponsor(item)}
                                            />
                                        </div>
                                    </div>
                                    {editingSponsor?.id === item.id && (
                                        <div className="container">
                                            <h3>Logo sponsora</h3>
                                            <img
                                                src={item.logoUrl}
                                                width="200"
                                                className={styles.logo}
                                            />
                                            <h2>Edytuj Dane Sponsora {item.name}</h2>
                                            <input
                                                type="text"
                                                placeholder="Nazwa"
                                                value={name}
                                                onChange={(e) => setName(e.target.value)}
                                                required
                                            />
                                            <input
                                                type="text"
                                                placeholder="Link do strony Sponsora"
                                                value={websiteUrl}
                                                onChange={(e) => setWebsiteUrl(e.target.value)}
                                                required
                                            />
                                            <input
                                                type="text"
                                                placeholder="Link do Loga Sponsora"
                                                value={logoUrl}
                                                onChange={(e) => setLogoUrl(e.target.value)}
                                                required
                                            />
                                            <input
                                                type="text"
                                                placeholder="Opis"
                                                value={description}
                                                onChange={(e) => setDescription(e.target.value)}
                                            />
                                            <div className={styles.formActions}>
                                                <Button
                                                    text={"Zapisz"}
                                                    onClick={() => UpdateSponsor(item)}
                                                />
                                                <Button
                                                    text={"Anuluj"}
                                                    onClick={() => {
                                                        setEditingSponsor(false);
                                                        clearForm();
                                                    }}
                                                />
                                            </div>
                                        </div>
                                    )}
                                </ContentsListTile>
                            ))}
                        </ContentsListBox>
                    )}
                </>
            )}
        </div>
    );
};

export default EditSponsorInfo;
