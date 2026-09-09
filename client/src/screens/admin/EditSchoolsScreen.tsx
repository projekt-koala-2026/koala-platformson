import type { FormEvent } from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import SchoolsTable from "../../components/SchoolsTable";
import type { School } from "../../types/models";
import { apiFormRequest, apiRequest } from "../../utils/apiFetcher";

interface SchoolForm {
    rspo: string;
    name: string;
    nameShort: string;
    state: string;
    city: string;
    type: string;
    addres: string;
}

interface EditForm {
    name: string;
    nameShort: string;
}
interface Feedback {
    message: string;
    tone: "success" | "error";
}

const emptySchool: SchoolForm = {
    rspo: "",
    name: "",
    nameShort: "",
    state: "",
    city: "",
    type: "",
    addres: "",
};
const inputClass =
    "mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:bg-slate-100";

const AdminSchoolsScreen = () => {
    const navigate = useNavigate();
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [schools, setSchools] = useState<School[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [action, setAction] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback | null>(null);
    const [csvFile, setCsvFile] = useState<File | null>(null);
    const [newSchool, setNewSchool] = useState<SchoolForm>(emptySchool);
    const [selectedSchool, setSelectedSchool] = useState<School | null>(null);
    const [editForm, setEditForm] = useState<EditForm>({ name: "", nameShort: "" });

    const loadSchools = useCallback(async () => {
        const data = await apiRequest<School[]>("/api/admin/school/school", null, "GET", navigate);
        if (!data) {
            setFeedback({
                tone: "error",
                message: "Operacja zakończyła się, ale nie udało się odświeżyć rejestru szkół.",
            });
            return null;
        }
        setSchools(data);
        return data;
    }, [navigate]);

    useEffect(() => {
        let active = true;
        void apiRequest<School[]>("/api/admin/school/school", null, "GET", navigate).then(
            (data) => {
                if (active) {
                    if (data) setSchools(data);
                    else
                        setFeedback({
                            tone: "error",
                            message: "Nie udało się pobrać rejestru szkół.",
                        });
                    setIsLoading(false);
                }
            }
        );
        return () => {
            active = false;
        };
    }, [navigate]);

    const setError = (message: string) => setFeedback({ message, tone: "error" });
    const setSuccess = (message: string) => setFeedback({ message, tone: "success" });

    const handleImportCsv = async () => {
        if (!csvFile) {
            setError("Wybierz plik CSV przed importem.");
            return;
        }
        const formData = new FormData();
        formData.append("File", csvFile);
        formData.append("Title", csvFile.name);
        setAction("import");
        setFeedback(null);
        const importedCount = await apiFormRequest<number>(
            "/api/admin/school/import/csv",
            formData,
            navigate
        );
        if (importedCount === null)
            setError(
                "Nie udało się zaimportować pliku CSV. Sprawdź format pliku i spróbuj ponownie."
            );
        else {
            setSuccess(
                `Import zakończony. Dodano ${importedCount} ${importedCount === 1 ? "szkołę" : "szkół"}.`
            );
            setCsvFile(null);
            if (fileInputRef.current) fileInputRef.current.value = "";
            await loadSchools();
        }
        setAction(null);
    };

    const handleDeleteAll = async () => {
        if (!window.confirm("KRYTYCZNA AKCJA: usunąć wszystkie szkoły z rejestru?")) return;
        if (!window.confirm("Potwierdź ponownie. Tej operacji nie można cofnąć.")) return;
        setAction("delete-all");
        setFeedback(null);
        const success = await apiRequest<boolean>(
            "/api/admin/school/schools",
            null,
            "DELETE",
            navigate
        );
        if (success) {
            setSchools([]);
            setSelectedSchool(null);
            setSuccess("Rejestr szkół został wyczyszczony.");
        } else setError("Nie udało się wyczyścić rejestru szkół.");
        setAction(null);
    };

    const handleCreateSchool = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFeedback(null);
        const rspo = Number(newSchool.rspo);
        if (!Number.isInteger(rspo) || rspo <= 0) {
            setError("RSPO musi być dodatnią liczbą całkowitą.");
            return;
        }
        if (
            ![
                newSchool.name,
                newSchool.state,
                newSchool.city,
                newSchool.type,
                newSchool.addres,
            ].every((value) => value.trim())
        ) {
            setError("Uzupełnij wszystkie wymagane dane szkoły.");
            return;
        }
        const payload: School = {
            rspo,
            name: newSchool.name.trim(),
            nameShort: newSchool.nameShort.trim(),
            state: newSchool.state.trim(),
            city: newSchool.city.trim(),
            type: newSchool.type.trim(),
            addres: newSchool.addres.trim(),
        };
        setAction("create");
        const created = await apiRequest<School>(
            "/api/admin/school/school",
            payload,
            "POST",
            navigate
        );
        if (created) {
            setNewSchool(emptySchool);
            setSuccess(`Dodano szkołę „${created.name || payload.name}”.`);
            await loadSchools();
        } else setError("Nie udało się dodać szkoły. Sprawdź, czy numer RSPO nie jest już zajęty.");
        setAction(null);
    };

    const selectSchool = (school: School) => {
        setSelectedSchool(school);
        setEditForm({ name: school.name, nameShort: school.nameShort ?? "" });
        setFeedback(null);
    };

    const handleUpdateSchool = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!selectedSchool || !editForm.name.trim()) {
            setError("Pełna nazwa szkoły jest wymagana.");
            return;
        }
        const name = editForm.name.trim();
        const nameShort = editForm.nameShort.trim();
        const nameChanged = name !== selectedSchool.name;
        const shortChanged = nameShort !== (selectedSchool.nameShort ?? "");
        if (!nameChanged && !shortChanged) {
            setFeedback({ message: "Nie wprowadzono żadnych zmian.", tone: "error" });
            return;
        }
        setAction("update");
        setFeedback(null);
        const results: boolean[] = [];
        if (nameChanged)
            results.push(
                Boolean(
                    await apiRequest<School>(
                        "/api/admin/school/name",
                        { rspo: selectedSchool.rspo, name },
                        "PUT",
                        navigate
                    )
                )
            );
        if (shortChanged)
            results.push(
                Boolean(
                    await apiRequest<School>(
                        "/api/admin/school/nameshort",
                        { rspo: selectedSchool.rspo, nameShort },
                        "PUT",
                        navigate
                    )
                )
            );
        if (results.every(Boolean)) {
            setSchools((current) =>
                current.map((school) =>
                    school.rspo === selectedSchool.rspo ? { ...school, name, nameShort } : school
                )
            );
            setSelectedSchool(null);
            setSuccess("Dane szkoły zostały zapisane.");
        } else {
            const refreshed = await loadSchools();
            setSelectedSchool(
                refreshed?.find((school) => school.rspo === selectedSchool.rspo) ?? selectedSchool
            );
            setError(
                refreshed
                    ? "Nie udało się zapisać wszystkich zmian. Dane zostały odświeżone."
                    : "Nie udało się zapisać wszystkich zmian ani odświeżyć danych. Widok może być nieaktualny."
            );
        }
        setAction(null);
    };

    const handleDeleteSingle = async () => {
        if (!selectedSchool || !window.confirm(`Usunąć szkołę „${selectedSchool.name}”?`)) return;
        setAction("delete-one");
        setFeedback(null);
        const success = await apiRequest<boolean>(
            "/api/admin/school/school",
            { rspo: selectedSchool.rspo },
            "DELETE",
            navigate
        );
        if (success) {
            setSelectedSchool(null);
            setSuccess("Szkoła została usunięta.");
            await loadSchools();
        } else setError("Nie udało się usunąć szkoły.");
        setAction(null);
    };

    const updateNewSchool = (field: keyof SchoolForm, value: string) =>
        setNewSchool((previous) => ({ ...previous, [field]: value }));
    const isBusy = action !== null;

    return (
        <>
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl space-y-8 px-4 py-8 sm:px-6 lg:px-8">
                <header className="border-b border-slate-100 pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Administracja
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Rejestr szkół</h1>
                    <p className="mt-3 max-w-3xl text-slate-600">
                        Importuj dane z kuratorium, dodawaj placówki i aktualizuj ich nazwy.
                    </p>
                </header>

                {feedback && (
                    <div
                        role={feedback.tone === "error" ? "alert" : "status"}
                        className={`rounded-xl border p-4 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-700"}`}
                    >
                        {feedback.message}
                    </div>
                )}

                <section className="grid gap-5 lg:grid-cols-2">
                    <article className="rounded-2xl border border-slate-200 bg-slate-50 p-5 sm:p-6">
                        <h2 className="text-lg font-semibold text-slate-900">Import z pliku CSV</h2>
                        <p className="mt-2 text-sm text-slate-600">
                            Zaimportuj aktualny wykaz szkół przygotowany w formacie obsługiwanym
                            przez serwer.
                        </p>
                        <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center">
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept=".csv,text/csv"
                                onChange={(event) => setCsvFile(event.target.files?.[0] ?? null)}
                                className="min-w-0 flex-1 rounded-xl border border-slate-200 bg-white text-sm text-slate-600 file:mr-4 file:border-0 file:bg-emerald-50 file:px-4 file:py-3 file:font-medium file:text-emerald-800 hover:file:bg-emerald-100"
                            />
                            <Button
                                text={action === "import" ? "Importowanie…" : "Importuj"}
                                onClick={() => void handleImportCsv()}
                                disabled={isBusy || !csvFile}
                            />
                        </div>
                    </article>
                    <article className="rounded-2xl border border-red-200 bg-red-50 p-5 sm:p-6">
                        <h2 className="text-lg font-semibold text-red-900">Wyczyść cały rejestr</h2>
                        <p className="mt-2 text-sm text-red-700">
                            Usuwa wszystkie rekordy szkół. Operacji nie można cofnąć.
                        </p>
                        <Button
                            text={action === "delete-all" ? "Usuwanie…" : "Wyczyść całą bazę"}
                            onClick={() => void handleDeleteAll()}
                            disabled={isBusy || schools.length === 0}
                            className="mt-5 bg-red-600 hover:bg-red-700 focus:ring-red-500"
                        />
                    </article>
                </section>

                <section className="grid items-start gap-5 lg:grid-cols-2">
                    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                        <h2 className="text-lg font-semibold text-slate-900">Dodaj szkołę</h2>
                        <form
                            onSubmit={handleCreateSchool}
                            className="mt-5 grid gap-4 sm:grid-cols-2"
                        >
                            <label className="text-sm font-medium text-slate-700">
                                RSPO *
                                <input
                                    type="number"
                                    min="1"
                                    value={newSchool.rspo}
                                    onChange={(event) =>
                                        updateNewSchool("rspo", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700 sm:col-span-2">
                                Pełna nazwa *
                                <input
                                    value={newSchool.name}
                                    onChange={(event) =>
                                        updateNewSchool("name", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700 sm:col-span-2">
                                Nazwa skrócona
                                <input
                                    value={newSchool.nameShort}
                                    onChange={(event) =>
                                        updateNewSchool("nameShort", event.target.value)
                                    }
                                    className={inputClass}
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                Województwo *
                                <input
                                    value={newSchool.state}
                                    onChange={(event) =>
                                        updateNewSchool("state", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                Miejscowość *
                                <input
                                    value={newSchool.city}
                                    onChange={(event) =>
                                        updateNewSchool("city", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                Typ placówki *
                                <input
                                    value={newSchool.type}
                                    onChange={(event) =>
                                        updateNewSchool("type", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                Adres *
                                <input
                                    value={newSchool.addres}
                                    onChange={(event) =>
                                        updateNewSchool("addres", event.target.value)
                                    }
                                    className={inputClass}
                                    required
                                />
                            </label>
                            <div className="sm:col-span-2">
                                <Button
                                    type="submit"
                                    text={action === "create" ? "Dodawanie…" : "Dodaj szkołę"}
                                    disabled={isBusy}
                                />
                            </div>
                        </form>
                    </article>

                    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                        {selectedSchool ? (
                            <>
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h2 className="text-lg font-semibold text-slate-900">
                                            Edytuj szkołę
                                        </h2>
                                        <p className="mt-1 font-mono text-xs text-slate-500">
                                            RSPO {selectedSchool.rspo}
                                        </p>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => setSelectedSchool(null)}
                                        className="rounded-lg px-3 py-2 text-sm text-slate-600 hover:bg-slate-100"
                                    >
                                        Anuluj
                                    </button>
                                </div>
                                <form onSubmit={handleUpdateSchool} className="mt-5 space-y-4">
                                    <label className="block text-sm font-medium text-slate-700">
                                        Pełna nazwa *
                                        <input
                                            value={editForm.name}
                                            onChange={(event) =>
                                                setEditForm((previous) => ({
                                                    ...previous,
                                                    name: event.target.value,
                                                }))
                                            }
                                            className={inputClass}
                                            required
                                        />
                                    </label>
                                    <label className="block text-sm font-medium text-slate-700">
                                        Nazwa skrócona
                                        <input
                                            value={editForm.nameShort}
                                            onChange={(event) =>
                                                setEditForm((previous) => ({
                                                    ...previous,
                                                    nameShort: event.target.value,
                                                }))
                                            }
                                            className={inputClass}
                                        />
                                    </label>
                                    <div className="flex flex-wrap gap-3">
                                        <Button
                                            type="submit"
                                            text={
                                                action === "update"
                                                    ? "Zapisywanie…"
                                                    : "Zapisz zmiany"
                                            }
                                            disabled={isBusy}
                                        />
                                        <Button
                                            text={
                                                action === "delete-one"
                                                    ? "Usuwanie…"
                                                    : "Usuń szkołę"
                                            }
                                            onClick={() => void handleDeleteSingle()}
                                            disabled={isBusy}
                                            className="bg-red-600 hover:bg-red-700 focus:ring-red-500"
                                        />
                                    </div>
                                </form>
                            </>
                        ) : (
                            <div className="grid min-h-72 place-items-center rounded-xl border border-dashed border-slate-200 bg-slate-50 p-8 text-center">
                                <div>
                                    <h2 className="font-semibold text-slate-800">
                                        Nie wybrano szkoły
                                    </h2>
                                    <p className="mt-2 max-w-sm text-sm text-slate-500">
                                        Kliknij wiersz w tabeli, aby edytować nazwę placówki lub
                                        usunąć rekord.
                                    </p>
                                </div>
                            </div>
                        )}
                    </article>
                </section>

                <section>
                    <div className="mb-4 flex items-end justify-between gap-4">
                        <div>
                            <h2 className="text-xl font-semibold text-slate-900">
                                Zarejestrowane placówki
                            </h2>
                            <p className="mt-1 text-sm text-slate-500">
                                {isLoading ? "Ładowanie danych…" : `${schools.length} rekordów`}
                            </p>
                        </div>
                    </div>
                    {isLoading ? (
                        <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center text-sm text-slate-500">
                            Ładowanie szkół…
                        </div>
                    ) : (
                        <SchoolsTable
                            schools={schools}
                            selectedRspo={selectedSchool?.rspo}
                            onRowClick={selectSchool}
                        />
                    )}
                </section>
            </main>
        </>
    );
};

export default AdminSchoolsScreen;
