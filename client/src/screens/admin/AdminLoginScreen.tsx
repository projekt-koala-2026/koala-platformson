import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import { useLoading } from "../../contexts/LoadingContext";
import type { SessionUser } from "../../types/models";
import { apiRequestResult } from "../../utils/apiFetcher";
import { clearStoredSession, rolesToFlags, storeSession } from "../../utils/authService";

const LOGIN_ENDPOINT = "/api/admin/auth/session";
const fieldClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:cursor-not-allowed disabled:bg-slate-100";

const AdminLoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [login, setLogin] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [submitting, setSubmitting] = useState(false);

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
            if (!roles.isAdmin && !roles.isEditor) {
                await apiRequestResult<boolean>(LOGIN_ENDPOINT, null, "DELETE", navigate);
                clearStoredSession();
                setError("To konto nie ma dostępu do panelu administracyjnego.");
                return;
            }

            storeSession(data);
            navigate(roles.isAdmin ? "/admin" : "/admin/posts");
        } finally {
            setSubmitting(false);
            stopLoading();
        }
    };

    return (
        <main className="grid min-h-screen place-items-center bg-slate-50 p-4">
            <form
                onSubmit={handleLogin}
                className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8"
            >
                <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                    Panel zarządzania
                </p>
                <h1 className="mt-2 text-2xl font-bold text-slate-900">Zaloguj się</h1>
                <p className="mt-2 text-sm text-slate-500">
                    Użyj konta administratora lub redaktora.
                </p>

                {error && (
                    <p
                        role="alert"
                        className="mt-5 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                    >
                        {error}
                    </p>
                )}

                <div className="mt-6 space-y-4">
                    <label className="block text-sm font-medium text-slate-700">
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
                    <label className="block text-sm font-medium text-slate-700">
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
                        className="w-full rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-50"
                    >
                        Wróć do strony konkursu
                    </button>
                </div>
            </form>
        </main>
    );
};

export default AdminLoginScreen;
