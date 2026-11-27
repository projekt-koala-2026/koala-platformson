import { useEffect, useState, type FormEvent } from "react";
import { FaBan, FaRedo, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../components/AdminHeader";
import Button from "../components/Button";
import Modal from "../components/Modal";
import PublicFooter from "../components/PublicFooter";
import PublicHeader from "../components/PublicHeader";
import SelectListbox from "../components/SelectListbox";
import type { ApiSession } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";
import { clearStoredSession, getStoredUserId, type AuthArea } from "../utils/authService";

type Action = { type: "delete" | "deactivate"; session: ApiSession } | { type: "all" };
const pageSize = 20;
const isActive = (session: ApiSession) =>
    session.state === "ACTIVE" && Date.parse(session.expiresAt) > Date.now();
const date = (value: string) =>
    Number.isNaN(Date.parse(value)) ? "—" : new Date(value).toLocaleString("pl-PL");
const stateLabel = (session: ApiSession) =>
    session.state === "ACTIVE"
        ? isActive(session)
            ? "Aktywna"
            : "Wygasła"
        : session.state === "INACTIVE"
          ? "Dezaktywowana"
          : session.state;

export default function SessionsScreen({ area }: { area: AuthArea }) {
    const navigate = useNavigate();
    const [sessions, setSessions] = useState<ApiSession[]>([]);
    const [filter, setFilter] = useState("all");
    const [page, setPage] = useState(0);
    const [revision, setRevision] = useState(0);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [action, setAction] = useState<Action | null>(null);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const loginPath = area === "organization" ? "/admin/login" : "/login";

    useEffect(() => {
        let active = true;
        setLoading(true);
        setError("");
        void apiListRequestResult<ApiSession>(
            `${filter === "active" ? apiEndpoints.activeSessions : apiEndpoints.sessions}?${firstPage}`,
            null,
            "GET"
        ).then((result) => {
            if (!active) return;
            if (result.status === 401) {
                clearStoredSession();
                navigate(loginPath, { replace: true });
                return;
            }
            if (result.data) {
                const items = result.data.filter(
                    (session) => filter !== "active" || isActive(session)
                );
                items.sort(
                    (a, b) =>
                        Date.parse(b.createdAt) - Date.parse(a.createdAt) ||
                        a.sessionId.localeCompare(b.sessionId)
                );
                setSessions(items);
                setPage((current) =>
                    Math.min(current, Math.max(0, Math.ceil(items.length / pageSize) - 1))
                );
            } else {
                setSessions([]);
                setError(result.error?.message ?? "Nie udało się pobrać sesji.");
            }
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [filter, revision, loginPath, navigate]);

    const submit = async (event: FormEvent) => {
        event.preventDefault();
        if (busy || !action) return;
        setBusy(true);
        setError("");
        setNotice("");
        const url =
            action.type === "all"
                ? apiEndpoints.allSessions
                : action.type === "deactivate"
                  ? apiEndpoints.deactivateSession(action.session.sessionId)
                  : apiEndpoints.session(action.session.sessionId);
        const result = await apiRequestResult(
            url,
            null,
            action.type === "deactivate" ? "PUT" : "DELETE"
        );
        if (result.status === 401) {
            clearStoredSession();
            navigate(loginPath, { replace: true });
            return;
        }
        if (result.status === null || result.status < 200 || result.status >= 300) {
            setError(
                result.error?.message ??
                    "Nie udało się zakończyć operacji. Odśwież listę i spróbuj ponownie."
            );
            setBusy(false);
            return;
        }
        if (action.type === "all") {
            clearStoredSession();
            navigate(loginPath, { replace: true });
            return;
        }
        // The API does not identify the current session; verify it after revocation.
        const userId = getStoredUserId();
        const current = userId
            ? await apiRequestResult(
                  `${apiEndpoints.users}/${encodeURIComponent(userId)}`,
                  null,
                  "GET"
              )
            : null;
        if (!current || current.status === 401) {
            clearStoredSession();
            navigate(loginPath, { replace: true });
            return;
        }
        setNotice(
            action.type === "deactivate"
                ? "Dezaktywowano sesję. Wpis pozostaje w historii."
                : "Usunięto sesję."
        );
        setAction(null);
        setBusy(false);
        setRevision((value) => value + 1);
    };

    return (
        <div className="flex min-h-screen flex-col bg-canvas text-ink-heading">
            {area === "organization" ? (
                <AdminHeader navigate={navigate} />
            ) : (
                <PublicHeader navigate={navigate} />
            )}
            <main className={area === "organization" ? "app-admin-page" : "app-public-page"}>
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Ustawienia konta
                    </p>
                    <h1 className="mt-1 text-3xl font-bold">Moje sesje</h1>
                    <p className="mt-3 text-ink-secondary">
                        Zarządzaj sesjami logowania swojego konta. Dezaktywacja kończy dostęp i
                        zachowuje wpis w historii. Usunięcie kończy dostęp i usuwa wpis.
                    </p>
                </header>
                <section className="mb-6 flex flex-wrap items-end gap-4 rounded-2xl border border-line bg-surface p-5">
                    <fieldset disabled={busy || action !== null} className="w-full max-w-sm">
                        <p id="session-filter" className="mb-2 text-sm font-semibold">
                            Wyświetlane sesje
                        </p>
                        <SelectListbox
                            value={filter}
                            options={[
                                { value: "all", label: "Wszystkie sesje" },
                                { value: "active", label: "Aktywne sesje" },
                            ]}
                            onChange={(value) => {
                                setFilter(value);
                                setPage(0);
                            }}
                            ariaLabelledBy="session-filter"
                        />
                    </fieldset>
                    <Button
                        text="Odśwież"
                        icon={<FaRedo />}
                        disabled={busy || loading || action !== null}
                        onClick={() => setRevision((value) => value + 1)}
                    />
                    <Button
                        text="Usuń wszystkie sesje"
                        icon={<FaTrash />}
                        disabled={busy || loading || sessions.length === 0}
                        className="bg-danger hover:bg-danger-hover"
                        onClick={() => {
                            setError("");
                            setAction({ type: "all" });
                        }}
                    />
                </section>
                {error && !action && (
                    <p role="alert" className="app-status-danger mb-4 rounded-xl border p-4">
                        {error}
                    </p>
                )}
                {notice && (
                    <p role="status" className="app-status-success mb-4 rounded-xl border p-4">
                        {notice}
                    </p>
                )}
                <p className="mb-4 text-sm text-ink-secondary">
                    Lista nie wskazuje, która sesja należy do tego okna. Zakończenie jej spowoduje
                    wylogowanie również tutaj.
                </p>
                {loading ? (
                    <p role="status" className="py-8 text-ink-secondary">
                        Ładowanie sesji…
                    </p>
                ) : !error && sessions.length === 0 ? (
                    <p className="rounded-xl border border-dashed border-line-strong bg-surface p-8 text-center text-ink-secondary">
                        Brak sesji spełniających wybrane kryteria.
                    </p>
                ) : (
                    !error && (
                        <>
                            <div className="overflow-x-auto rounded-2xl border border-line bg-surface">
                                <table className="w-full text-left text-sm">
                                    <thead className="bg-surface-muted text-ink-secondary">
                                        <tr>
                                            <th className="px-4 py-3">Identyfikator</th>
                                            <th className="px-4 py-3">Stan</th>
                                            <th className="px-4 py-3">Utworzono</th>
                                            <th className="px-4 py-3">Ostatnia zmiana</th>
                                            <th className="px-4 py-3">Wygasa</th>
                                            <th className="px-4 py-3">Akcje</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-line-subtle">
                                        {sessions
                                            .slice(page * pageSize, (page + 1) * pageSize)
                                            .map((session) => (
                                                <tr key={session.sessionId}>
                                                    <td className="px-4 py-4">
                                                        <span
                                                            className="block max-w-[12rem] truncate font-mono text-xs"
                                                            title={session.sessionId}
                                                        >
                                                            {session.sessionId}
                                                        </span>
                                                    </td>
                                                    <td className="px-4 py-4">
                                                        <span
                                                            className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${isActive(session) ? "bg-brand-muted text-brand-text-strong" : "bg-surface-muted text-ink-secondary"}`}
                                                        >
                                                            {stateLabel(session)}
                                                        </span>
                                                    </td>
                                                    <td className="whitespace-nowrap px-4 py-4 text-ink-secondary">
                                                        {date(session.createdAt)}
                                                    </td>
                                                    <td className="whitespace-nowrap px-4 py-4 text-ink-secondary">
                                                        {date(session.updatedAt)}
                                                    </td>
                                                    <td className="whitespace-nowrap px-4 py-4 text-ink-secondary">
                                                        {date(session.expiresAt)}
                                                    </td>
                                                    <td className="px-4 py-4">
                                                        <div className="flex gap-2">
                                                            <Button
                                                                text="Dezaktywuj"
                                                                icon={<FaBan />}
                                                                disabled={
                                                                    busy || !isActive(session)
                                                                }
                                                                onClick={() => {
                                                                    setError("");
                                                                    setAction({
                                                                        type: "deactivate",
                                                                        session,
                                                                    });
                                                                }}
                                                            />
                                                            <Button
                                                                text="Usuń"
                                                                icon={<FaTrash />}
                                                                className="bg-danger hover:bg-danger-hover"
                                                                disabled={busy}
                                                                onClick={() => {
                                                                    setError("");
                                                                    setAction({
                                                                        type: "delete",
                                                                        session,
                                                                    });
                                                                }}
                                                            />
                                                        </div>
                                                    </td>
                                                </tr>
                                            ))}
                                    </tbody>
                                </table>
                            </div>
                            <nav
                                aria-label="Strony sesji"
                                className="mt-4 flex flex-wrap items-center justify-between gap-3"
                            >
                                <Button
                                    text="Poprzednia"
                                    disabled={busy || page === 0}
                                    onClick={() => setPage(page - 1)}
                                />
                                <p className="text-sm text-ink-secondary">
                                    Strona {page + 1} z{" "}
                                    {Math.max(1, Math.ceil(sessions.length / pageSize))} ·{" "}
                                    {sessions.length} sesji
                                </p>
                                <Button
                                    text="Następna"
                                    disabled={busy || (page + 1) * pageSize >= sessions.length}
                                    onClick={() => setPage(page + 1)}
                                />
                            </nav>
                        </>
                    )
                )}
            </main>
            {area === "team" && <PublicFooter />}
            <Modal
                isOpen={action !== null}
                onClose={() => {
                    if (!busy) {
                        setAction(null);
                        setError("");
                    }
                }}
                title={
                    action?.type === "all"
                        ? "Usuń wszystkie sesje"
                        : action?.type === "deactivate"
                          ? "Dezaktywuj sesję"
                          : "Usuń sesję"
                }
                maxWidth="md"
            >
                <form onSubmit={(event) => void submit(event)} className="space-y-4">
                    {error && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3">
                            {error}
                        </p>
                    )}
                    <p>
                        {action?.type === "all"
                            ? "Usunąć wszystkie sesje konta, łącznie z bieżącą? Nastąpi wylogowanie na wszystkich urządzeniach, również w tym oknie."
                            : action?.type === "deactivate"
                              ? "Zakończyć dostęp dla tej sesji? Wpis pozostanie w historii. Jeśli to sesja tego okna, nastąpi wylogowanie."
                              : "Usunąć tę sesję i zakończyć jej dostęp? Jeśli to sesja tego okna, nastąpi wylogowanie."}
                    </p>
                    <div className="flex flex-wrap justify-end gap-3">
                        <Button
                            text="Anuluj"
                            disabled={busy}
                            onClick={() => {
                                setAction(null);
                                setError("");
                            }}
                        />
                        <Button
                            text={busy ? "Zapisywanie…" : "Potwierdź"}
                            type="submit"
                            disabled={busy}
                            className="bg-danger hover:bg-danger-hover"
                        />
                    </div>
                </form>
            </Modal>
        </div>
    );
}
