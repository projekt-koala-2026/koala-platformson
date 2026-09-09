import StaticPageEditor from "../../components/StaticPageEditor";

const EditRuleScreen = () => (
    <StaticPageEditor
        title="Regulamin"
        description="Aktualizuj regulamin publikowany dla uczestników konkursu."
        contentEndpoint="/content/rules/rules.json"
        saveEndpoint="/api/static-pages/rules"
    />
);

export default EditRuleScreen;
