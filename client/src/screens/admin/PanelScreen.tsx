import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { Role, User } from "../../types/models";
import { apiRequest, apiRequestResult } from "../../utils/apiFetcher";

const USERS_ENDPOINT = "/api/admin/user/users";
const USER_ENDPOINT = "/api/admin/user/user";
const ROLES_ENDPOINT = "/api/admin/user/roles";
const editableRoles = ["ADMIN", "EDITOR"] as const satisfies readonly Role[];
const roleLabels: Partial<Record<Role, string>> = {
    ADMIN: "administrator",
    EDITOR: "redaktor",
    REVIEWER: "recenzent",
};
const inputClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100";

type Feedback = { tone: "success" | "error"; message: string } | null;

const sortUsers = (users: User[]) =>
    [...users].sort((first, second) => first.email.localeCompare(second.email, "pl"));

const isStrongPassword = (password: string) =>
    password.length >= 8 &&
    /[a-z]/.test(password) &&
    /[A-Z]/.test(password) &&
    /\d/.test(password) &&
    /[\W_]/.test(password);

const PanelScreen = () => {
    const navigate = useNavigate();
    const currentUserId = useMemo(() => localStorage.getItem("userId"), []);
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [editingUser, setEditingUser] = useState<User | null>(null);
    const [selectedRoles, setSelectedRoles] = useState<Role[]>([]);
    const [savingRoles, setSavingRoles] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [newRoles, setNewRoles] = useState<Role[]>([]);
    const [createError, setCreateError] = useState("");
    const [creating, setCreating] = useState(false);

    useEffect(() => {
        let active = true;
        void apiRequest<User[]>(USERS_ENDPOINT, null, "GET", navigate).then((data) => {
            if (!active) return;
            if (data) setUsers(sortUsers(data));
            else setFeedback({ tone: "error", message: "Nie udało się pobrać użytkowników." });
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const closeCreate = () => {
        if (creating) return;
        setIsCreateOpen(false);
        setEmail("");
        setPassword("");
        setNewRoles([]);
        setCreateError("");
    };

    const toggleRole = (
        role: Role,
        roles: Role[],
        setter: React.Dispatch<React.SetStateAction<Role[]>>
    ) => {
        setter(roles.includes(role) ? roles.filter((item) => item !== role) : [...roles, role]);
    };

    const createUser = async () => {
        const normalizedEmail = email.trim().toLowerCase();
        setCreateError("");
        if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) {
            setCreateError("Podaj prawidłowy adres e-mail.");
            return;
        }
        if (!isStrongPassword(password)) {
            setCreateError(
                "Hasło musi mieć co najmniej 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny."
            );
            return;
        }
        if (newRoles.length === 0) {
            setCreateError("Wybierz co najmniej jedną rolę.");
            return;
        }

        setCreating(true);
        const { data: created } = await apiRequestResult<User>(
            USER_ENDPOINT,
            { email: normalizedEmail, password, roles: newRoles },
            "POST",
            navigate
        );
        if (!created) {
            setCreateError("Nie udało się utworzyć użytkownika. Adres może być już zajęty.");
            setCreating(false);
            return;
        }

        setUsers((current) => sortUsers([...current, created]));
        setCreating(false);
        setIsCreateOpen(false);
        setEmail("");
        setPassword("");
        setNewRoles([]);
        setCreateError("");
        setFeedback({ tone: "success", message: `Utworzono konto ${created.email}.` });
    };

    const deleteUser = async (user: User) => {
        if (user.id === currentUserId) {
            setFeedback({ tone: "error", message: "Nie można usunąć aktualnie używanego konta." });
            return;
        }
        if (!window.confirm(`Czy na pewno chcesz usunąć użytkownika ${user.email}?`)) return;

        setDeletingId(user.id);
        setFeedback(null);
        const { data } = await apiRequestResult(USER_ENDPOINT, { id: user.id }, "DELETE", navigate);
        if (data === null) {
            setFeedback({ tone: "error", message: "Nie udało się usunąć użytkownika." });
        } else {
            setUsers((current) => current.filter((item) => item.id !== user.id));
            if (editingUser?.id === user.id) setEditingUser(null);
            setFeedback({ tone: "success", message: `Usunięto konto ${user.email}.` });
        }
        setDeletingId(null);
    };

    const saveRoles = async () => {
        if (!editingUser) return;
        if (selectedRoles.length === 0) {
            setFeedback({ tone: "error", message: "Użytkownik musi mieć co najmniej jedną rolę." });
            return;
        }
        if (editingUser.id === currentUserId && !selectedRoles.includes("ADMIN")) {
            setFeedback({
                tone: "error",
                message: "Nie możesz odebrać sobie roli administratora.",
            });
            return;
        }

        setSavingRoles(true);
        setFeedback(null);
        const { data: updated } = await apiRequestResult<User>(
            ROLES_ENDPOINT,
            { id: editingUser.id, newRoles: selectedRoles },
            "PUT",
            navigate
        );
        if (!updated) {
            setFeedback({ tone: "error", message: "Nie udało się zmienić uprawnień." });
        } else {
            setUsers((current) => current.map((user) => (user.id === updated.id ? updated : user)));
            const rejectedRoles = selectedRoles.filter((role) => !updated.roles.includes(role));
            if (rejectedRoles.length > 0) {
                setEditingUser(updated);
                setSelectedRoles(updated.roles);
                setFeedback({
                    tone: "error",
                    message: `Serwer nie obsługuje ról: ${rejectedRoles.join(", ")}. Pozostałe uprawnienia zapisano.`,
                });
            } else {
                setEditingUser(null);
                setFeedback({ tone: "success", message: "Uprawnienia zostały zapisane." });
            }
        }
        setSavingRoles(false);
    };

    const beginRoleEdit = (user: User) => {
        setEditingUser(user);
        setSelectedRoles(user.roles);
        setFeedback(null);
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <section className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">
                            Użytkownicy panelu
                        </h1>
                        <p className="mt-2 max-w-2xl text-sm text-slate-600">
                            Zarządzaj kontami i uprawnieniami administratorów oraz redaktorów. Rola
                            recenzenta czeka na obsługę w nowym API.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Dodaj użytkownika</span>
                            </>
                        }
                        onClick={() => setIsCreateOpen(true)}
                    />
                </section>

                {feedback && (
                    <div
                        role="status"
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                                : "border-red-200 bg-red-50 text-red-800"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                <section className="mt-8 space-y-4">
                    {loading ? (
                        <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 shadow-sm">
                            Ładowanie użytkowników…
                        </div>
                    ) : users.length === 0 ? (
                        <p className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">
                            Brak użytkowników do wyświetlenia.
                        </p>
                    ) : (
                        users.map((user) => {
                            const isCurrentUser = user.id === currentUserId;
                            return (
                                <article
                                    key={user.id}
                                    className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6"
                                >
                                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                        <div className="min-w-0">
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h2 className="truncate text-lg font-semibold">
                                                    {user.email}
                                                </h2>
                                                {isCurrentUser && (
                                                    <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
                                                        Twoje konto
                                                    </span>
                                                )}
                                            </div>
                                            <div className="mt-3 flex flex-wrap gap-2">
                                                {user.roles.map((role) => (
                                                    <span
                                                        key={role}
                                                        className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-800"
                                                    >
                                                        {roleLabels[
                                                            role as keyof typeof roleLabels
                                                        ] ?? role.toLowerCase()}
                                                    </span>
                                                ))}
                                            </div>
                                        </div>
                                        <div className="flex shrink-0 gap-2">
                                            <Button
                                                text={
                                                    <>
                                                        <FaEdit />
                                                        <span className="sr-only">Edytuj role</span>
                                                    </>
                                                }
                                                onClick={() => beginRoleEdit(user)}
                                                className="px-3"
                                            />
                                            <Button
                                                text={
                                                    <>
                                                        <FaTrash />
                                                        <span className="sr-only">
                                                            Usuń użytkownika
                                                        </span>
                                                    </>
                                                }
                                                disabled={isCurrentUser || deletingId === user.id}
                                                onClick={() => void deleteUser(user)}
                                                className="bg-red-600 px-3 hover:bg-red-700"
                                            />
                                        </div>
                                    </div>

                                    {editingUser?.id === user.id && (
                                        <div className="mt-6 border-t border-slate-100 pt-6">
                                            <h3 className="text-base font-semibold text-slate-800">
                                                Uprawnienia
                                            </h3>
                                            <div className="mt-4 flex flex-wrap gap-3">
                                                {editableRoles.map((role) => {
                                                    const protectsOwnAdmin =
                                                        isCurrentUser && role === "ADMIN";
                                                    return (
                                                        <label
                                                            key={role}
                                                            className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-emerald-50 has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-60"
                                                        >
                                                            <input
                                                                type="checkbox"
                                                                checked={selectedRoles.includes(
                                                                    role
                                                                )}
                                                                disabled={
                                                                    protectsOwnAdmin || savingRoles
                                                                }
                                                                onChange={() =>
                                                                    toggleRole(
                                                                        role,
                                                                        selectedRoles,
                                                                        setSelectedRoles
                                                                    )
                                                                }
                                                            />
                                                            {roleLabels[role]}
                                                        </label>
                                                    );
                                                })}
                                                <label className="flex cursor-not-allowed items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800 opacity-75">
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedRoles.includes("REVIEWER")}
                                                        disabled
                                                    />
                                                    recenzent
                                                    <span className="text-xs font-normal">
                                                        (brak w obecnym API)
                                                    </span>
                                                </label>
                                            </div>
                                            <p className="mt-3 text-xs text-amber-700">
                                                Obecny backend nie tworzy roli REVIEWER. Opcja
                                                pozostaje widoczna informacyjnie i będzie mogła
                                                zostać włączona po migracji API.
                                            </p>
                                            {isCurrentUser && (
                                                <p className="mt-3 text-xs text-slate-500">
                                                    Rola administratora jest chroniona na aktualnie
                                                    używanym koncie.
                                                </p>
                                            )}
                                            <div className="mt-6 flex flex-wrap gap-3">
                                                <Button
                                                    text={
                                                        savingRoles
                                                            ? "Zapisywanie…"
                                                            : "Zapisz uprawnienia"
                                                    }
                                                    disabled={savingRoles}
                                                    onClick={() => void saveRoles()}
                                                />
                                                <button
                                                    type="button"
                                                    disabled={savingRoles}
                                                    onClick={() => setEditingUser(null)}
                                                    className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
                                                >
                                                    Anuluj
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                </article>
                            );
                        })
                    )}
                </section>
            </main>

            <Modal
                isOpen={isCreateOpen}
                onClose={closeCreate}
                title="Dodaj użytkownika"
                maxWidth="md"
            >
                <form
                    className="space-y-4"
                    onSubmit={(event) => {
                        event.preventDefault();
                        void createUser();
                    }}
                >
                    {createError && (
                        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
                            {createError}
                        </p>
                    )}
                    <label className="block text-sm font-semibold text-slate-700">
                        E-mail
                        <input
                            className={inputClass}
                            type="email"
                            autoComplete="email"
                            value={email}
                            disabled={creating}
                            onChange={(event) => setEmail(event.target.value)}
                            required
                        />
                    </label>
                    <label className="block text-sm font-semibold text-slate-700">
                        Hasło
                        <input
                            className={inputClass}
                            type="password"
                            autoComplete="new-password"
                            value={password}
                            disabled={creating}
                            onChange={(event) => setPassword(event.target.value)}
                            required
                        />
                    </label>
                    <div>
                        <p className="mb-2 text-sm font-semibold text-slate-700">Role</p>
                        <div className="flex flex-wrap gap-3">
                            {editableRoles.map((role) => (
                                <label
                                    key={role}
                                    className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-emerald-50"
                                >
                                    <input
                                        type="checkbox"
                                        checked={newRoles.includes(role)}
                                        disabled={creating}
                                        onChange={() => toggleRole(role, newRoles, setNewRoles)}
                                    />
                                    {roleLabels[role]}
                                </label>
                            ))}
                            <label className="flex cursor-not-allowed items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800 opacity-75">
                                <input type="checkbox" disabled />
                                recenzent
                                <span className="text-xs font-normal">(brak w obecnym API)</span>
                            </label>
                        </div>
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={creating}
                            onClick={closeCreate}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={creating ? "Tworzenie…" : "Utwórz użytkownika"}
                            disabled={creating}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
};

export default PanelScreen;
