import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import type { Role, User } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const USERS_ENDPOINT = apiEndpoints.users;
const editableRoles = [
    "ORGANIZATION_ADMIN",
    "ORGANIZATION_EDITOR",
    "ORGANIZATION_REVIEWER",
] as const satisfies readonly Role[];
const roleLabels: Partial<Record<Role, string>> = {
    ORGANIZATION_ADMIN: "administrator",
    ORGANIZATION_EDITOR: "redaktor",
    ORGANIZATION_REVIEWER: "recenzent",
};
const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted";

type Feedback = { tone: "success" | "error"; message: string } | null;

const sortUsers = (users: User[]) =>
    [...users].sort((first, second) => first.email.localeCompare(second.email, "pl"));

const PanelScreen = () => {
    const navigate = useNavigate();
    const currentUserId = useMemo(() => localStorage.getItem("userId"), []);
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [feedback, setFeedback] = useState<Feedback>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [email, setEmail] = useState("");
    const [newRoles, setNewRoles] = useState<Role[]>([]);
    const [createError, setCreateError] = useState("");
    const [creating, setCreating] = useState(false);

    useEffect(() => {
        let active = true;
        void apiRequestResult<User[]>(`${USERS_ENDPOINT}?${firstPage}`, null, "GET", navigate).then(
            ({ data }) => {
                if (!active) return;
                if (data) setUsers(sortUsers(data));
                else setFeedback({ tone: "error", message: "Nie udało się pobrać użytkowników." });
                setLoading(false);
            }
        );
        return () => {
            active = false;
        };
    }, [navigate]);

    const closeCreate = () => {
        if (creating) return;
        setIsCreateOpen(false);
        setEmail("");
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
        if (newRoles.length === 0) {
            setCreateError("Wybierz co najmniej jedną rolę.");
            return;
        }

        setCreating(true);
        const { status } = await apiRequestResult<null>(
            USERS_ENDPOINT,
            { email: normalizedEmail, roles: newRoles },
            "POST",
            navigate
        );
        if (status !== 201) {
            setCreateError("Nie udało się wysłać zaproszenia. Adres może być już zajęty.");
            setCreating(false);
            return;
        }

        setCreating(false);
        setIsCreateOpen(false);
        setEmail("");
        setNewRoles([]);
        setCreateError("");
        setFeedback({
            tone: "success",
            message: `Wysłano zaproszenie na adres ${normalizedEmail}.`,
        });
    };

    const deleteUser = async (user: User) => {
        if (user.id === currentUserId) {
            setFeedback({ tone: "error", message: "Nie można usunąć aktualnie używanego konta." });
            return;
        }
        if (!window.confirm(`Czy na pewno chcesz usunąć użytkownika ${user.email}?`)) return;

        setDeletingId(user.id);
        setFeedback(null);
        const { status } = await apiRequestResult<null>(
            `${USERS_ENDPOINT}/${user.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status !== 200 && status !== 204) {
            setFeedback({ tone: "error", message: "Nie udało się usunąć użytkownika." });
        } else {
            setUsers((current) => current.filter((item) => item.id !== user.id));
            setFeedback({ tone: "success", message: `Usunięto konto ${user.email}.` });
        }
        setDeletingId(null);
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                <section className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                            Administracja
                        </p>
                        <h1 className="mt-1 text-3xl font-bold tracking-tight">
                            Użytkownicy panelu
                        </h1>
                        <p className="mt-2 max-w-2xl text-sm text-ink-secondary">
                            Zapraszaj użytkowników panelu i przypisuj im role. Zaproszona osoba
                            kończy rejestrację przez odnośnik wysłany w wiadomości e-mail.
                        </p>
                    </div>
                    <Button
                        text={
                            <>
                                <FaPlus />
                                <span className="ml-2">Zaproś użytkownika</span>
                            </>
                        }
                        onClick={() => setIsCreateOpen(true)}
                    />
                </section>

                {feedback && (
                    <div
                        role="status"
                        className={`mt-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success" ? "app-status-success" : "app-status-danger"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                <section className="mt-8 space-y-4">
                    {loading ? (
                        <div className="rounded-2xl border border-line bg-surface p-8 text-center text-sm text-ink-muted shadow-sm">
                            Ładowanie użytkowników…
                        </div>
                    ) : users.length === 0 ? (
                        <p className="rounded-2xl border border-dashed border-line-strong bg-surface p-10 text-center text-sm text-ink-muted">
                            Brak użytkowników do wyświetlenia.
                        </p>
                    ) : (
                        users.map((user) => {
                            const isCurrentUser = user.id === currentUserId;
                            return (
                                <article
                                    key={user.id}
                                    className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6"
                                >
                                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                        <div className="min-w-0">
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h2 className="truncate text-lg font-semibold">
                                                    {user.email}
                                                </h2>
                                                {isCurrentUser && (
                                                    <span className="rounded-full bg-surface-muted px-2.5 py-1 text-xs font-medium text-ink-secondary">
                                                        Twoje konto
                                                    </span>
                                                )}
                                            </div>
                                            <div className="mt-3 flex flex-wrap gap-2">
                                                {user.roles.map((role) => (
                                                    <span
                                                        key={role}
                                                        className="rounded-full bg-brand-soft px-3 py-1 text-xs font-semibold text-brand-text-strong"
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
                                                disabled
                                                title="Zmiana ról będzie dostępna po dodaniu endpointu backendu."
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
                                                className="bg-danger px-3 hover:bg-danger-hover"
                                            />
                                        </div>
                                    </div>
                                </article>
                            );
                        })
                    )}
                </section>
            </main>

            <Modal
                isOpen={isCreateOpen}
                onClose={closeCreate}
                title="Zaproś użytkownika"
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
                        <p role="alert" className="rounded-lg border p-3 text-sm app-status-danger">
                            {createError}
                        </p>
                    )}
                    <label className="block text-sm font-semibold text-ink">
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
                    <div>
                        <p className="mb-2 text-sm font-semibold text-ink">Role</p>
                        <div className="flex flex-wrap gap-3">
                            {editableRoles.map((role) => (
                                <label
                                    key={role}
                                    className="flex cursor-pointer items-center gap-2 rounded-lg border border-line px-3 py-2 text-sm font-medium text-ink transition hover:bg-brand-soft"
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
                        </div>
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={creating}
                            onClick={closeCreate}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            type="submit"
                            text={creating ? "Wysyłanie…" : "Wyślij zaproszenie"}
                            disabled={creating}
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
};

export default PanelScreen;
