import type { FormEvent } from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import SchoolsTable from "../../components/SchoolsTable";
import type { ApiSchool, School } from "../../types/models";
import { adaptSchool } from "../../utils/apiAdapters";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiFormRequest, apiRequestResult } from "../../utils/apiFetcher";

const SCHOOLS_PAGE_SIZE = 500;
const MAX_SCHOOL_PAGES = 200;

interface SchoolForm {
    rspo: string;
    name: string;
    nameShort: string;
    state: string;
    city: string;
    type: string;
    road: string;
    building: string;
    email: string;
}

interface Feedback {
    message: string;
    tone: "success" | "error";
}

interface SchoolFieldsProps {
    value: SchoolForm;
    disabled: boolean;
    onChange: (field: keyof SchoolForm, value: string) => void;
}

const emptySchool: SchoolForm = {
    rspo: "",
    name: "",
    nameShort: "",
    state: "",
    city: "",
    type: "",
    road: "",
    building: "",
    email: "",
};

const inputClass =
    "mt-1.5 w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-sm text-ink-heading outline-none transition placeholder:text-ink-subtle focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted";

const SchoolFields = ({ value, disabled, onChange }: SchoolFieldsProps) => (
    <div className="grid gap-4 sm:grid-cols-2">
        <label className="text-sm font-medium text-ink">
            RSPO *
            <input
                type="number"
                min="1"
                value={value.rspo}
                onChange={(event) => onChange("rspo", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink">
            Nazwa skrócona
            <input
                value={value.nameShort}
                onChange={(event) => onChange("nameShort", event.target.value)}
                className={inputClass}
                disabled={disabled}
            />
        </label>
        <label className="text-sm font-medium text-ink sm:col-span-2">
            Pełna nazwa *
            <input
                value={value.name}
                onChange={(event) => onChange("name", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink">
            Województwo *
            <input
                value={value.state}
                onChange={(event) => onChange("state", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink">
            Miejscowość *
            <input
                value={value.city}
                onChange={(event) => onChange("city", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink sm:col-span-2">
            Typ placówki *
            <input
                value={value.type}
                onChange={(event) => onChange("type", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink">
            Ulica *
            <input
                value={value.road}
                onChange={(event) => onChange("road", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink">
            Numer budynku *
            <input
                value={value.building}
                onChange={(event) => onChange("building", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
        <label className="text-sm font-medium text-ink sm:col-span-2">
            E-mail *
            <input
                type="email"
                value={value.email}
                onChange={(event) => onChange("email", event.target.value)}
                className={inputClass}
                disabled={disabled}
                required
            />
        </label>
    </div>
);

const formFromApiSchool = (school: ApiSchool): SchoolForm => ({
    rspo: school.rspo,
    name: school.nameFull,
    nameShort: school.nameShort,
    state: school.state,
    city: school.city,
    type: school.type,
    road: school.road,
    building: school.building,
    email: school.email,
});

const payloadFromForm = (form: SchoolForm) => ({
    rspo: String(Number(form.rspo)),
    nameFull: form.name.trim(),
    nameShort: form.nameShort.trim(),
    state: form.state.trim(),
    city: form.city.trim(),
    type: form.type.trim(),
    road: form.road.trim(),
    building: form.building.trim(),
    email: form.email.trim(),
});

const isValidSchool = (form: SchoolForm) => {
    const rspo = Number(form.rspo);
    return (
        Number.isInteger(rspo) &&
        rspo > 0 &&
        [form.name, form.state, form.city, form.type, form.road, form.building, form.email].every(
            (field) => field.trim()
        )
    );
};

const AdminSchoolsScreen = () => {
    const navigate = useNavigate();
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [schools, setSchools] = useState<School[]>([]);
    const [rawSchools, setRawSchools] = useState<Record<string, ApiSchool>>({});
    const [isLoading, setIsLoading] = useState(true);
    const [action, setAction] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback | null>(null);
    const [csvFile, setCsvFile] = useState<File | null>(null);
    const [newSchool, setNewSchool] = useState<SchoolForm>(emptySchool);
    const [selectedSchool, setSelectedSchool] = useState<School | null>(null);
    const [editSchool, setEditSchool] = useState<SchoolForm>(emptySchool);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isImportOpen, setIsImportOpen] = useState(false);

    const fetchAllSchools = useCallback(async () => {
        const loaded: ApiSchool[] = [];
        for (let pageNumber = 0; pageNumber < MAX_SCHOOL_PAGES; pageNumber += 1) {
            const { data } = await apiRequestResult<ApiSchool[]>(
                `${apiEndpoints.schools}?PageNumber=${pageNumber}&PageSize=${SCHOOLS_PAGE_SIZE}`,
                null,
                "GET",
                navigate
            );
            if (!data) return null;
            loaded.push(...data);
            if (data.length < SCHOOLS_PAGE_SIZE) return loaded;
        }
        return null;
    }, [navigate]);

    const applySchools = useCallback((data: ApiSchool[]) => {
        const adapted = data.map(adaptSchool);
        setSchools(adapted);
        setRawSchools(Object.fromEntries(data.map((school) => [school.id, school])));
        return adapted;
    }, []);

    const loadSchools = useCallback(async () => {
        const data = await fetchAllSchools();
        if (data === null) {
            setFeedback({
                tone: "error",
                message: "Operacja zakończyła się, ale nie udało się odświeżyć rejestru szkół.",
            });
            return null;
        }
        return applySchools(data);
    }, [applySchools, fetchAllSchools]);

    useEffect(() => {
        let active = true;
        void fetchAllSchools().then((data) => {
            if (!active) return;
            if (data) {
                applySchools(data);
            } else {
                setFeedback({ tone: "error", message: "Nie udało się pobrać rejestru szkół." });
            }
            setIsLoading(false);
        });
        return () => {
            active = false;
        };
    }, [applySchools, fetchAllSchools]);

    const setError = (message: string) => setFeedback({ message, tone: "error" });
    const setSuccess = (message: string) => setFeedback({ message, tone: "success" });
    const isBusy = action !== null;

    const updateNewSchool = (field: keyof SchoolForm, value: string) =>
        setNewSchool((previous) => ({ ...previous, [field]: value }));
    const updateEditedSchool = (field: keyof SchoolForm, value: string) =>
        setEditSchool((previous) => ({ ...previous, [field]: value }));

    const openCreateModal = () => {
        setFeedback(null);
        setNewSchool(emptySchool);
        setIsCreateOpen(true);
    };

    const openImportModal = () => {
        setFeedback(null);
        setCsvFile(null);
        if (fileInputRef.current) fileInputRef.current.value = "";
        setIsImportOpen(true);
    };

    const closeCreateModal = () => {
        if (!isBusy) setIsCreateOpen(false);
    };

    const closeImportModal = () => {
        if (!isBusy) setIsImportOpen(false);
    };

    const closeEditModal = () => {
        if (!isBusy) setSelectedSchool(null);
    };

    const handleImportCsv = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!csvFile) {
            setError("Wybierz plik CSV przed importem.");
            return;
        }
        const formData = new FormData();
        formData.append("File", csvFile);
        setAction("import");
        setFeedback(null);
        const importedCount = await apiFormRequest<number>(
            `${apiEndpoints.schools}/import`,
            formData,
            navigate
        );
        if (importedCount === null) {
            setError(
                "Nie udało się zaimportować pliku CSV. Sprawdź format pliku i spróbuj ponownie."
            );
        } else {
            setSuccess(
                `Import zakończony. Dodano ${importedCount} ${importedCount === 1 ? "szkołę" : "szkół"}.`
            );
            setCsvFile(null);
            if (fileInputRef.current) fileInputRef.current.value = "";
            setIsImportOpen(false);
            await loadSchools();
        }
        setAction(null);
    };

    const handleCreateSchool = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFeedback(null);
        if (!isValidSchool(newSchool)) {
            setError("Uzupełnij wymagane dane. RSPO musi być dodatnią liczbą całkowitą.");
            return;
        }
        const payload = payloadFromForm(newSchool);
        setAction("create");
        const { data: created } = await apiRequestResult<ApiSchool>(
            apiEndpoints.schools,
            payload,
            "POST",
            navigate
        );
        if (created) {
            setNewSchool(emptySchool);
            setIsCreateOpen(false);
            setSuccess(`Dodano szkołę „${created.nameFull || payload.nameFull}”.`);
            await loadSchools();
        } else {
            setError("Nie udało się dodać szkoły. Sprawdź, czy numer RSPO nie jest już zajęty.");
        }
        setAction(null);
    };

    const selectSchool = (school: School) => {
        if (!school.id) return;
        const source = rawSchools[school.id];
        if (!source) {
            setError("Nie udało się odczytać pełnych danych wybranej szkoły.");
            return;
        }
        setFeedback(null);
        setSelectedSchool(school);
        setEditSchool(formFromApiSchool(source));
    };

    const handleUpdateSchool = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!selectedSchool?.id || !isValidSchool(editSchool)) {
            setError("Uzupełnij wymagane dane. RSPO musi być dodatnią liczbą całkowitą.");
            return;
        }
        const source = rawSchools[selectedSchool.id];
        const payload = payloadFromForm(editSchool);
        if (!source) {
            setError("Nie udało się odczytać aktualnych danych szkoły.");
            return;
        }
        const sourcePayload = payloadFromForm(formFromApiSchool(source));
        if (JSON.stringify(payload) === JSON.stringify(sourcePayload)) {
            setError("Nie wprowadzono żadnych zmian.");
            return;
        }
        setAction("update");
        setFeedback(null);
        const { data: updated } = await apiRequestResult<ApiSchool>(
            `${apiEndpoints.schools}/${source.id}`,
            payload,
            "PUT",
            navigate
        );
        if (updated) {
            const adapted = adaptSchool(updated);
            setSchools((current) =>
                current.map((school) => (school.id === updated.id ? adapted : school))
            );
            setRawSchools((current) => ({ ...current, [updated.id]: updated }));
            setSelectedSchool(null);
            setSuccess("Dane szkoły zostały zapisane.");
        } else {
            setError("Nie udało się zapisać zmian. Dane w formularzu zostały zachowane.");
        }
        setAction(null);
    };

    const handleDeleteSingle = async () => {
        if (!selectedSchool?.id || !window.confirm(`Usunąć szkołę „${selectedSchool.name}”?`))
            return;
        setAction("delete-one");
        setFeedback(null);
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.schools}/${selectedSchool.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status === 200 || status === 204) {
            setSelectedSchool(null);
            setSuccess("Szkoła została usunięta.");
            await loadSchools();
        } else {
            setError("Nie udało się usunąć szkoły.");
        }
        setAction(null);
    };

    const handleDeleteAll = async () => {
        if (!window.confirm("KRYTYCZNA AKCJA: usunąć wszystkie szkoły z rejestru?")) return;
        if (!window.confirm("Potwierdź ponownie. Tej operacji nie można cofnąć.")) return;
        setAction("delete-all");
        setFeedback(null);
        const { status } = await apiRequestResult<null>(
            apiEndpoints.schools,
            schools.flatMap((school) => (school.id ? [school.id] : [])),
            "DELETE",
            navigate
        );
        if (status === 200 || status === 204) {
            setSchools([]);
            setRawSchools({});
            setSelectedSchool(null);
            setSuccess("Rejestr szkół został wyczyszczony.");
        } else {
            setError("Nie udało się wyczyścić rejestru szkół.");
        }
        setAction(null);
    };

    const modalError = feedback?.tone === "error" && (
        <p role="alert" className="mb-5 rounded-xl border p-3 text-sm app-status-danger">
            {feedback.message}
        </p>
    );

    return (
        <>
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl space-y-8 px-4 py-8 sm:px-6 lg:px-8">
                <header className="border-b border-line-subtle pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Administracja
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">Rejestr szkół</h1>
                    <p className="mt-3 max-w-3xl text-ink-secondary">
                        Importuj dane z kuratorium, dodawaj placówki i aktualizuj ich dane.
                    </p>
                </header>

                {feedback && (
                    <div
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "app-status-success" : "app-status-danger"}`}
                    >
                        {feedback.message}
                    </div>
                )}

                <section className="rounded-2xl border border-line bg-surface p-4 shadow-sm sm:flex sm:items-center sm:justify-between sm:gap-5 sm:p-5">
                    <div>
                        <h2 className="font-semibold text-ink-heading">Zarządzanie rejestrem</h2>
                        <p className="mt-1 text-sm text-ink-muted">
                            Dodaj pojedynczą placówkę albo zaimportuj przygotowany wykaz CSV.
                        </p>
                    </div>
                    <div className="mt-4 flex flex-wrap gap-3 sm:mt-0 sm:shrink-0">
                        <Button text="Dodaj szkołę" onClick={openCreateModal} disabled={isBusy} />
                        <button
                            type="button"
                            onClick={openImportModal}
                            disabled={isBusy}
                            className="inline-flex min-h-10 items-center justify-center rounded-xl border border-brand-border bg-brand-soft px-4 py-2.5 text-sm font-medium text-brand-text-strong transition hover:border-brand-border-strong hover:bg-brand-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Importuj CSV
                        </button>
                    </div>
                </section>

                <section>
                    <div className="mb-4 flex items-end justify-between gap-4">
                        <div>
                            <h2 className="text-xl font-semibold text-ink-heading">
                                Zarejestrowane placówki
                            </h2>
                            <p className="mt-1 text-sm text-ink-muted">
                                {isLoading
                                    ? "Ładowanie danych…"
                                    : `${schools.length} rekordów · kliknij wiersz, aby edytować`}
                            </p>
                        </div>
                    </div>
                    {isLoading ? (
                        <div className="rounded-2xl border border-line bg-surface p-12 text-center text-sm text-ink-muted">
                            Ładowanie szkół…
                        </div>
                    ) : (
                        <SchoolsTable
                            schools={schools}
                            selectedRspo={selectedSchool?.rspo}
                            onRowClick={selectSchool}
                        />
                    )}

                    <div className="mt-6 rounded-2xl border border-danger-border bg-danger-soft p-4 sm:flex sm:items-center sm:justify-between sm:gap-5 sm:p-5">
                        <div>
                            <h2 className="font-semibold text-danger-text">Wyczyść cały rejestr</h2>
                            <p className="mt-1 text-sm text-danger-text">
                                Usuwa wszystkie szkoły. Operacji nie można cofnąć.
                            </p>
                        </div>
                        <Button
                            text={action === "delete-all" ? "Usuwanie…" : "Wyczyść całą bazę"}
                            onClick={() => void handleDeleteAll()}
                            disabled={isBusy || schools.length === 0}
                            className="mt-4 bg-danger hover:bg-danger-hover active:bg-danger-active focus-visible:ring-danger-focus sm:mt-0 sm:shrink-0"
                        />
                    </div>
                </section>
            </main>

            <Modal
                isOpen={isCreateOpen}
                onClose={closeCreateModal}
                title="Dodaj szkołę"
                maxWidth="lg"
            >
                {modalError}
                <form onSubmit={handleCreateSchool}>
                    <SchoolFields value={newSchool} disabled={isBusy} onChange={updateNewSchool} />
                    <div className="mt-6 flex flex-wrap justify-end gap-3">
                        <button
                            type="button"
                            onClick={closeCreateModal}
                            disabled={isBusy}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink-secondary transition hover:bg-surface-muted disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={action === "create" ? "Dodawanie…" : "Dodaj szkołę"}
                            disabled={isBusy}
                        />
                    </div>
                </form>
            </Modal>

            <Modal
                isOpen={isImportOpen}
                onClose={closeImportModal}
                title="Importuj szkoły z CSV"
                maxWidth="md"
            >
                {modalError}
                <form onSubmit={handleImportCsv}>
                    <p className="text-sm leading-6 text-ink-secondary">
                        Wybierz aktualny wykaz szkół w formacie CSV obsługiwanym przez serwer.
                    </p>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept=".csv,text/csv"
                        disabled={isBusy}
                        onChange={(event) => setCsvFile(event.target.files?.[0] ?? null)}
                        className="mt-5 w-full min-w-0 rounded-xl border border-line bg-surface text-sm text-ink-secondary file:mr-4 file:border-0 file:bg-brand-soft file:px-4 file:py-3 file:font-medium file:text-brand-text-strong hover:file:bg-brand-muted disabled:opacity-50"
                    />
                    <div className="mt-6 flex flex-wrap justify-end gap-3">
                        <button
                            type="button"
                            onClick={closeImportModal}
                            disabled={isBusy}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink-secondary transition hover:bg-surface-muted disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={action === "import" ? "Importowanie…" : "Importuj"}
                            disabled={isBusy || !csvFile}
                        />
                    </div>
                </form>
            </Modal>

            <Modal
                isOpen={selectedSchool !== null}
                onClose={closeEditModal}
                title="Edytuj szkołę"
                maxWidth="lg"
            >
                {modalError}
                {selectedSchool && (
                    <form onSubmit={handleUpdateSchool}>
                        <SchoolFields
                            value={editSchool}
                            disabled={isBusy}
                            onChange={updateEditedSchool}
                        />
                        <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
                            <Button
                                text={action === "delete-one" ? "Usuwanie…" : "Usuń szkołę"}
                                onClick={() => void handleDeleteSingle()}
                                disabled={isBusy}
                                className="bg-danger hover:bg-danger-hover active:bg-danger-active focus-visible:ring-danger-focus"
                            />
                            <div className="flex flex-wrap gap-3">
                                <button
                                    type="button"
                                    onClick={closeEditModal}
                                    disabled={isBusy}
                                    className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink-secondary transition hover:bg-surface-muted disabled:opacity-50"
                                >
                                    Anuluj
                                </button>
                                <Button
                                    type="submit"
                                    text={action === "update" ? "Zapisywanie…" : "Zapisz zmiany"}
                                    disabled={isBusy}
                                />
                            </div>
                        </div>
                    </form>
                )}
            </Modal>
        </>
    );
};

export default AdminSchoolsScreen;
