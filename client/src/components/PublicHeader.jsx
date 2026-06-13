import Hamburger from "./Hamburger";

const PublicHeader = ({ navigate }) => {
    return (
        <div
            style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                padding: "16px",
            }}
        >
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
        </div>
    );
};

export default PublicHeader;
