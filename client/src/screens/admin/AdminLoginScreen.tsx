import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import type { SessionUser } from "../../types/models";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";
import {
    clearStoredSession,
    getAuthenticatedHomePath,
    getOrganizationHomePath,
    rolesToFlags,
    storeSession,
} from "../../utils/authService";

const LOGIN_ENDPOINT = apiEndpoints.sessions;
const fieldClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted";

const AdminLoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [login, setLogin] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        const destination = getAuthenticatedHomePath();
        if (destination) navigate(destination, { replace: true });
    }, [navigate]);

    const handleLogin = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (submitting) return;
        setError("");
        setSubmitting(true);
        startLoading();
        try {
            const { data } = await apiRequestResult<SessionUser>(
                LOGIN_ENDPOINT,
                { email: login.trim(), password },
                "POST",
                navigate
            );
            if (!data) {
                setError("Nie udało się zalogować. Sprawdź login i hasło.");
                return;
            }

            const roles = rolesToFlags(data.roles);
            if (!roles.isAdmin && !roles.isEditor && !roles.isReviewer) {
                await apiRequestResult<boolean>(LOGIN_ENDPOINT, null, "DELETE", navigate);
                clearStoredSession();
                setError("To konto nie ma dostępu do panelu administracyjnego.");
                return;
            }

            storeSession(data);
            navigate(getOrganizationHomePath(roles));
        } finally {
            setSubmitting(false);
            stopLoading();
        }
    };

    return (
        <main className="grid min-h-screen place-items-center bg-canvas p-4">
            <form
                onSubmit={handleLogin}
                className="w-full max-w-md rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8"
            >
                <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                    Panel zarządzania
                </p>
                <h1 className="mt-2 text-2xl font-bold text-ink-heading">Zaloguj się</h1>
                <p className="mt-2 text-sm text-ink-muted">
                    Użyj konta administratora, redaktora lub recenzenta.
                </p>

                {error && (
                    <p
                        role="alert"
                        className="mt-5 rounded-xl border p-3 text-sm app-status-danger"
                    >
                        {error}
                    </p>
                )}

                <div className="mt-6 space-y-4">
                    <label className="block text-sm font-medium text-ink">
                        Login lub e-mail
                        <input
                            className={fieldClass}
                            type="text"
                            autoComplete="username"
                            value={login}
                            disabled={submitting}
                            onChange={(event) => setLogin(event.target.value)}
                            required
                        />
                    </label>
                    <label className="block text-sm font-medium text-ink">
                        Hasło
                        <input
                            className={fieldClass}
                            type="password"
                            autoComplete="current-password"
                            value={password}
                            disabled={submitting}
                            onChange={(event) => setPassword(event.target.value)}
                            required
                        />
                    </label>
                    <Button
                        text={submitting ? "Logowanie…" : "Zaloguj"}
                        type="submit"
                        disabled={submitting}
                        className="w-full"
                    />
                    <button
                        type="button"
                        disabled={submitting}
                        onClick={() => navigate("/")}
                        className="w-full rounded-lg px-3 py-2 text-sm font-medium text-ink-secondary hover:bg-surface-muted disabled:opacity-50"
                    >
                        Wróć do strony konkursu
                    </button>
                </div>
            </form>
        </main>
    );
};

export default AdminLoginScreen;
