import StaticPageEditor from "../../components/StaticPageEditor";

const EditHistoryScreen = () => (
    <StaticPageEditor
        title="Historia konkursu"
        description="Edytuj opis historii wyświetlany na publicznej stronie konkursu."
        contentEndpoint="/content/history/history.json"
        saveEndpoint="/api/static-pages/history"
    />
);

export default EditHistoryScreen;
