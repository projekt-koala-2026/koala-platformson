import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import { useLoading } from "../../contexts/LoadingContext";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin, isEditor } from "../../utils/authService";

const EditKoalicjantInfo = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const isAdminEditor = useMemo(() => isAdmin() || isEditor(), []);

    const [koalicjants, setKoalicjants] = useState(null);
    const [name, setName] = useState(null);
    const [profilePicture, setProfilePicture] = useState(null);
    const [description, setDescription] = useState(null);
    const [addingKoalicjant, setAddingKoalicjant] = useState(null);
    const [editingKoalicjant, setEditingKoalicjant] = useState(null);

    const clearForm = () => {
        setName("");
        setProfilePicture("");
        setDescription("");
    };

    const AddKoalicjant = () => {
        clearForm();
        setAddingKoalicjant(true);
    };

    const saveKoalicjant = async () => {
        startLoading();

        const data = await apiRequest(
            "/api/admin/koalicjants",
            {
                id: "019e647a-3bfc-7a71-baf5-3dfd0571b71c",
                name: name,
                profilePicture: profilePicture,
                description: description,
            },
            "POST",
            navigate
        );

        await new Promise((resolve) => setTimeout(resolve, 500));

        if (data) {
            setKoalicjants((prev) => [...prev, data]);
            clearForm();
            navigate("/admin/koalicjants");
        }

        setAddingKoalicjant(false);
        stopLoading();
    };

    const DeleteKoalicjant = async (koalicjant) => {
        const confirmed = window.confirm(
            `Czy na pewno chcesz usunąć Koalicjanta ${koalicjant.name}?`
        );

        if (!confirmed) return;

        const apiLink = `/api/admin/koalicjants/${koalicjant.id}`;
        const data = await apiRequest(apiLink, {}, "DELETE", navigate);
        setKoalicjants((prev) => prev.filter((k) => k.id !== koalicjant.id));
    };

    const EditKoalicjant = (koalicjant) => {
        setEditingKoalicjant(editingKoalicjant === koalicjant ? null : koalicjant);
        setName(koalicjant.name);
        setProfilePicture(koalicjant.profilePicture);
        setDescription(koalicjant.description);
    };

    const updateKoalicjant = async (koalicjant) => {
        const apiLink = `/api/admin/koalicjants/${koalicjant.id}`;
        await apiRequest(
            apiLink,
            {
                id: "019e647a-3bfc-7a71-baf5-3dfd0571b71c",
                name: name,
                profilePicture: profilePicture,
                description: description,
            },
            "PUT",
            navigate
        );

        setKoalicjants((prev) =>
            prev.map((k) =>
                k.id === koalicjant.id
                    ? {
                          ...k,
                          name: name,
                          profilePicture: profilePicture,
                          description: description,
                      }
                    : k
            )
        );

        setEditingKoalicjant(false);
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
            const data = await apiRequest("/api/admin/koalicjants", null, "GET", navigate);

            if (!data || data.length === 0) {
                navigate("/admin/koalicjants");
                return;
            }

            setKoalicjants(data);
        };

        if (isAdminEditor) getData();
    }, [navigate]);

    return (
        <div className="container" style={{ minWidth: "50%" }}>
            {isAdminEditor && (
                <>
                    <h1>Lista Koalicjantów</h1>
                    <div>
                        <Button text={"Wróć do panelu"} onClick={handleBack} />
                        <Button text={"Dodaj nowego/ą Koalicjanta/ke"} onClick={AddKoalicjant} />
                    </div>
                    {addingKoalicjant === true && (
                        <div className="container">
                            <h2>Dodaj Koalicjanta/ke</h2>
                            <input
                                type="text"
                                placeholder="Imię i Nazwisko"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                required
                            />
                            <input
                                type="text"
                                placeholder="Link do Zdjęcia Profilu"
                                value={profilePicture}
                                onChange={(e) => setProfilePicture(e.target.value)}
                                required
                            />
                            <input
                                type="text"
                                placeholder="Opis"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                            />
                            <div style={{ display: "flex", gap: "10px", marginTop: "10px" }}>
                                <Button text={"Zapisz"} onClick={saveKoalicjant} />
                                <Button
                                    text={"Anuluj"}
                                    onClick={() => {
                                        (setAddingKoalicjant(false), clearForm);
                                    }}
                                />
                            </div>
                        </div>
                    )}
                    {koalicjants && (
                        <ContentsListBox>
                            {koalicjants.map((item, idx) => (
                                <ContentsListTile key={item.id}>
                                    <div style={{ display: "flex", gap: "4px" }}>
                                        <div style={{ display: "flex", gap: "4px" }}>
                                            <img
                                                src={item.profilePicture}
                                                height="62"
                                                style={{ borderRadius: "12px" }}
                                            />
                                            <div
                                                style={{ display: "flex", flexDirection: "column" }}
                                            >
                                                <h3>{item.name}</h3>
                                                <span>{item.description}</span>
                                            </div>
                                        </div>
                                        <div
                                            style={{
                                                display: "flex",
                                                gap: "4px",
                                                marginLeft: "auto",
                                            }}
                                        >
                                            <Button
                                                text={<FaEdit />}
                                                onClick={() => EditKoalicjant(item)}
                                            />
                                            <Button
                                                text={<FaTrash />}
                                                onClick={() => DeleteKoalicjant(item)}
                                            />
                                        </div>
                                    </div>
                                    {editingKoalicjant?.id === item.id && (
                                        <div className="container">
                                            <h2>Edytuj Dane Koalicjanta/ki {item.name}</h2>
                                            <input
                                                type="text"
                                                placeholder="Imię i Nazwisko"
                                                value={name}
                                                onChange={(e) => setName(e.target.value)}
                                                required
                                            />
                                            <input
                                                type="text"
                                                placeholder="Link do Zdjęcia Profilu"
                                                value={profilePicture}
                                                onChange={(e) => setProfilePicture(e.target.value)}
                                                required
                                            />
                                            <input
                                                type="text"
                                                placeholder="Opis"
                                                value={description}
                                                onChange={(e) => setDescription(e.target.value)}
                                            />
                                            <div
                                                style={{
                                                    display: "flex",
                                                    gap: "10px",
                                                    marginTop: "10px",
                                                }}
                                            >
                                                <Button
                                                    text={"Zapisz"}
                                                    onClick={() => updateKoalicjant(item)}
                                                />
                                                <Button
                                                    text={"Anuluj"}
                                                    onClick={() => {
                                                        (setEditingKoalicjant(false), clearForm);
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

export default EditKoalicjantInfo;
