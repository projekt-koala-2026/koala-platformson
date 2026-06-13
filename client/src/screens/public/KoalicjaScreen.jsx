import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import Hamburger from "../../components/Hamburger";
import { apiRequest } from "../../utils/apiFetcher";

const KoalicjaScreen = () => {
    const navigate = useNavigate();
    const [koalicjants, setKoalicjants] = useState([]);

    useEffect(() => {
        const getData = async () => {
            const data = await apiRequest("/api/admin/koalicjants", null, "GET", navigate);
            if (data) setKoalicjants(data);
        };

        getData();
    }, [navigate]);
    return (
        <>
            <header style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
                <h1>Koala</h1>
                <h2>
                    <span style={{ color: "#458756" }}>KO</span>mbinatoryka{" "}
                    <span style={{ color: "#458756" }}>A</span>lgorytmika{" "}
                    <span style={{ color: "#458756" }}>L</span>ogik
                    <span style={{ color: "#458756" }}>A</span>
                </h2>
                <h5>Wielkopolski konkurs grup szkolnych</h5>
                <Hamburger
                    options={[
                        ["Aktualności", () => navigate("/")],
                        ["Zadania", () => navigate("/problems")],
                        ["Regulamin", () => navigate("/rules")],
                        ["Historia", () => navigate("/history")],
                        ["KOALicjA", () => navigate("/koalicja")],
                    ]}
                />
            </header>

            <div className="container" style={{ minWidth: "50%" }}>
                <h1>KOALicjA,</h1>
                <h2>czyli współtworzący konkurs KOALA</h2>
                <ContentsListBox>
                    {koalicjants.map((item, idx) => {
                        return (
                            <ContentsListTile key={item.id}>
                                <div style={{ display: "flex", gap: "4px" }}>
                                    <div style={{ display: "flex", flexDirection: "column" }}>
                                        <h3>{item.name}</h3>
                                        <span style={{ marginLeft: "1em" }}>
                                            {item.description}
                                        </span>
                                    </div>
                                    <div
                                        style={{
                                            display: "flex",
                                            gap: "4px",
                                            marginLeft: "auto",
                                        }}
                                    >
                                        <img
                                            src={item.profilePicture}
                                            height="100"
                                            style={{ borderRadius: "12px" }}
                                        />
                                    </div>
                                </div>
                            </ContentsListTile>
                        );
                    })}
                </ContentsListBox>
            </div>
        </>
    );
};

export default KoalicjaScreen;
