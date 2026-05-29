import Hamburger from "../../components/Hamburger";

const HomeScreen = () => {
    return (
        <>
            <header>
                <h1>Koala</h1>
                <Hamburger options={[["Napisz do loga", () => console.log("test")]]} />
            </header>

            <div className="container" style={{ minWidth: "50%" }}></div>
        </>
    );
};

export default HomeScreen;
