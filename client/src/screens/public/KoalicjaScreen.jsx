import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
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
            <PublicHeader navigate={navigate} />

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
            <PublicFooter />
        </>
    );
};

export default KoalicjaScreen;
