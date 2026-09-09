import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import PublicHeader from "../../components/PublicHeader";
import { useLoading } from "../../contexts/LoadingContext";
import type { SessionUser, User } from "../../types/models";
import { apiRequestResult } from "../../utils/apiFetcher";
import { clearStoredSession, rolesToFlags, storeSession } from "../../utils/authService";

const LOGIN_ENDPOINT = "/api/admin/auth/session";
const REGISTER_ENDPOINT = "/api/admin/user/create-account";
const fieldClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:cursor-not-allowed disabled:bg-slate-100";

type RegistrationRole = "CAPTAIN" | "GUARDIAN";
type Mode = "login" | "register";

const isStrongPassword = (password: string) =>
    password.length >= 8 &&
    /[a-z]/.test(password) &&
    /[A-Z]/.test(password) &&
    /\d/.test(password) &&
    /[\W_]/.test(password);

const LoginScreen = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const [mode, setMode] = useState<Mode>("login");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [registerEmail, setRegisterEmail] = useState("");
    const [registerPassword, setRegisterPassword] = useState("");
    const [registrationRole, setRegistrationRole] = useState<RegistrationRole>("CAPTAIN");
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [submitting, setSubmitting] = useState(false);

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
        if (!isStrongPassword(registerPassword)) {
            setError(
                "Hasło musi mieć co najmniej 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny."
            );
            return;
        }

        setSubmitting(true);
        startLoading();
        try {
            const { data } = await apiRequestResult<User>(
                REGISTER_ENDPOINT,
                {
                    email: normalizedEmail,
                    password: registerPassword,
                    roles: [registrationRole],
                },
                "POST",
                navigate
            );
            if (!data) {
                setError("Nie udało się utworzyć konta. Adres może być już zajęty.");
                return;
            }

            setEmail(normalizedEmail);
            setPassword("");
            setRegisterEmail("");
            setRegisterPassword("");
            setRegistrationRole("CAPTAIN");
            setMode("login");
            setNotice("Konto zostało utworzone. Możesz się teraz zalogować.");
        } finally {
            setSubmitting(false);
            stopLoading();
        }
    };

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="grid flex-1 place-items-center px-4 py-10 sm:px-6">
                <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Konto uczestnika
                    </p>
                    <h1 className="mt-2 text-2xl font-bold text-slate-900">
                        {mode === "register" ? "Utwórz konto" : "Zaloguj się"}
                    </h1>
                    <p className="mt-2 text-sm text-slate-500">
                        {mode === "register"
                            ? "Rejestracja jest dostępna dla kapitanów i opiekunów drużyn."
                            : "Użyj loginu lub adresu e-mail przypisanego do konta."}
                    </p>

                    {error && (
                        <p
                            role="alert"
                            className="mt-5 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                        >
                            {error}
                        </p>
                    )}
                    {notice && (
                        <p
                            role="status"
                            className="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800"
                        >
                            {notice}
                        </p>
                    )}

                    {mode === "login" ? (
                        <form onSubmit={login} className="mt-6 space-y-4">
                            <label className="block text-sm font-medium text-slate-700">
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
                                onClick={() => switchMode("register")}
                                className="w-full rounded-lg px-3 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                            >
                                Nie masz konta? Zarejestruj się
                            </button>
                        </form>
                    ) : (
                        <form onSubmit={createAccount} className="mt-6 space-y-4">
                            <label className="block text-sm font-medium text-slate-700">
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
                            <label className="block text-sm font-medium text-slate-700">
                                Hasło
                                <input
                                    className={fieldClass}
                                    type="password"
                                    autoComplete="new-password"
                                    value={registerPassword}
                                    disabled={submitting}
                                    onChange={(event) => setRegisterPassword(event.target.value)}
                                    required
                                />
                            </label>
                            <fieldset>
                                <legend className="text-sm font-medium text-slate-700">
                                    Rodzaj konta
                                </legend>
                                <div className="mt-2 grid gap-2 sm:grid-cols-2">
                                    {(["CAPTAIN", "GUARDIAN"] as RegistrationRole[]).map((role) => (
                                        <label
                                            key={role}
                                            className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 px-3 py-3 text-sm text-slate-700 hover:bg-emerald-50"
                                        >
                                            <input
                                                type="radio"
                                                name="registration-role"
                                                value={role}
                                                checked={registrationRole === role}
                                                disabled={submitting}
                                                onChange={() => setRegistrationRole(role)}
                                            />
                                            {role === "CAPTAIN" ? "Kapitan" : "Opiekun"}
                                        </label>
                                    ))}
                                </div>
                            </fieldset>
                            <p className="text-xs leading-5 text-slate-500">
                                Hasło: minimum 8 znaków, mała i wielka litera, cyfra oraz znak
                                specjalny.
                            </p>
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
                                className="w-full rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-50"
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
