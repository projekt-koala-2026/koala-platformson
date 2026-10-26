import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import PublicHeader from "../../components/PublicHeader";
import { useLoading } from "../../contexts/LoadingContext";
import type { SessionUser } from "../../types/models";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";
import {
    clearStoredSession,
    getAuthenticatedHomePath,
    rolesToFlags,
    storeSession,
} from "../../utils/authService";

const LOGIN_ENDPOINT = apiEndpoints.sessions;
const REGISTER_ENDPOINT = apiEndpoints.users;
const fieldClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:cursor-not-allowed disabled:bg-surface-muted";

type RegistrationRole = "TEAM_PLAYER" | "TEAM_ADMIN";
type Mode = "login" | "register";

const LoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [mode, setMode] = useState<Mode>("login");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [registerEmail, setRegisterEmail] = useState("");
    const [registrationRole, setRegistrationRole] = useState<RegistrationRole>("TEAM_PLAYER");
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        const destination = getAuthenticatedHomePath();
        if (destination) navigate(destination, { replace: true });
    }, [navigate]);

    const switchMode = (nextMode: Mode) => {
        setMode(nextMode);
        setError("");
        setNotice("");
    };

    const login = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (submitting) return;
        setError("");
        setNotice("");
        setSubmitting(true);
        startLoading();
        try {
            const { data } = await apiRequestResult<SessionUser>(
                LOGIN_ENDPOINT,
                { email: email.trim(), password },
                "POST",
                navigate
            );
            if (!data) {
                setError("Nie udało się zalogować. Sprawdź login i hasło.");
                return;
            }

            const roles = rolesToFlags(data.roles);
            if (!roles.isCaptain && !roles.isGuardian) {
                await apiRequestResult<boolean>(LOGIN_ENDPOINT, null, "DELETE", navigate);
                clearStoredSession();
                setError("To konto nie ma dostępu do części przeznaczonej dla uczestników.");
                return;
            }

            storeSession(data);
            navigate(roles.isCaptain ? "/captain" : "/");
        } finally {
            setSubmitting(false);
            stopLoading();
        }
    };

    const createAccount = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (submitting) return;
        const normalizedEmail = registerEmail.trim().toLowerCase();
        setError("");
        setNotice("");
        if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) {
            setError("Podaj prawidłowy adres e-mail.");
            return;
        }
        setSubmitting(true);
        startLoading();
        try {
            const { status } = await apiRequestResult<null>(
                REGISTER_ENDPOINT,
                {
                    email: normalizedEmail,
                    roles: [registrationRole],
                },
                "POST",
                navigate
            );
            if (status !== 201) {
                setError("Nie udało się wysłać zaproszenia. Adres może być już zajęty.");
                return;
            }

            setEmail(normalizedEmail);
            setPassword("");
            setRegisterEmail("");
            setRegistrationRole("TEAM_PLAYER");
            setMode("login");
            setNotice("Wysłaliśmy wiadomość z linkiem umożliwiającym dokończenie rejestracji.");
        } finally {
            setSubmitting(false);
            stopLoading();
        }
    };

    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="grid flex-1 place-items-center px-4 py-10 sm:px-6">
                <section className="w-full max-w-md rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Konto uczestnika
                    </p>
                    <h1 className="mt-2 text-2xl font-bold text-ink-heading">
                        {mode === "register" ? "Utwórz konto" : "Zaloguj się"}
                    </h1>
                    <p className="mt-2 text-sm text-ink-muted">
                        {mode === "register"
                            ? "Rejestracja jest dostępna dla kapitanów i opiekunów drużyn."
                            : "Użyj loginu lub adresu e-mail przypisanego do konta."}
                    </p>

                    {error && (
                        <p
                            role="alert"
                            className="mt-5 rounded-xl border p-3 text-sm app-status-danger"
                        >
                            {error}
                        </p>
                    )}
                    {notice && (
                        <p
                            role="status"
                            className="mt-5 rounded-xl border p-3 text-sm app-status-success"
                        >
                            {notice}
                        </p>
                    )}

                    {mode === "login" ? (
                        <form onSubmit={login} className="mt-6 space-y-4">
                            <label className="block text-sm font-medium text-ink">
                                Login lub e-mail
                                <input
                                    className={fieldClass}
                                    type="text"
                                    autoComplete="username"
                                    value={email}
                                    disabled={submitting}
                                    onChange={(event) => setEmail(event.target.value)}
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
                                onClick={() => navigate("/reset-password")}
                                className="w-full rounded-lg px-3 py-2 text-sm font-medium text-ink-secondary hover:bg-surface-muted disabled:opacity-50"
                            >
                                Nie pamiętasz hasła?
                            </button>
                            <button
                                type="button"
                                disabled={submitting}
                                onClick={() => switchMode("register")}
                                className="w-full rounded-lg px-3 py-2 text-sm font-medium text-brand-text hover:bg-brand-soft disabled:opacity-50"
                            >
                                Nie masz konta? Zarejestruj się
                            </button>
                        </form>
                    ) : (
                        <form onSubmit={createAccount} className="mt-6 space-y-4">
                            <label className="block text-sm font-medium text-ink">
                                E-mail
                                <input
                                    className={fieldClass}
                                    type="email"
                                    autoComplete="email"
                                    value={registerEmail}
                                    disabled={submitting}
                                    onChange={(event) => setRegisterEmail(event.target.value)}
                                    required
                                />
                            </label>
                            <fieldset>
                                <legend className="text-sm font-medium text-ink">
                                    Rodzaj konta
                                </legend>
                                <div className="mt-2 grid gap-2 sm:grid-cols-2">
                                    {(["TEAM_PLAYER", "TEAM_ADMIN"] as RegistrationRole[]).map(
                                        (role) => (
                                            <label
                                                key={role}
                                                className={`flex cursor-pointer items-center gap-2 rounded-xl border px-3 py-3 text-sm transition focus-within:ring-2 focus-within:ring-brand-focus ${
                                                    registrationRole === role
                                                        ? "border-brand-focus bg-brand-soft font-medium text-brand-text-strong"
                                                        : "border-line text-ink hover:border-brand-border-strong hover:bg-brand-soft"
                                                }`}
                                            >
                                                <input
                                                    type="radio"
                                                    name="registration-role"
                                                    value={role}
                                                    checked={registrationRole === role}
                                                    disabled={submitting}
                                                    onChange={() => setRegistrationRole(role)}
                                                />
                                                {role === "TEAM_PLAYER" ? "Kapitan" : "Opiekun"}
                                            </label>
                                        )
                                    )}
                                </div>
                            </fieldset>
                            <Button
                                text={submitting ? "Tworzenie konta…" : "Zarejestruj"}
                                type="submit"
                                disabled={submitting}
                                className="w-full"
                            />
                            <button
                                type="button"
                                disabled={submitting}
                                onClick={() => switchMode("login")}
                                className="w-full rounded-lg px-3 py-2 text-sm font-medium text-ink-secondary hover:bg-surface-muted disabled:opacity-50"
                            >
                                Wróć do logowania
                            </button>
                        </form>
                    )}
                </section>
            </main>
        </div>
    );
};

export default LoginScreen;
