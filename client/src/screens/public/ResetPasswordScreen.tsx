import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import Button from "../../components/Button";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";
import { clearStoredSession, getLoginPath } from "../../utils/authService";

const fieldClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:bg-slate-100";
const isStrongPassword = (password: string) =>
    password.length >= 8 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /\d/.test(password) && /[\W_]/.test(password);

const ResetPasswordScreen = () => {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const token = searchParams.get("token")?.trim() ?? "";
    const accountArea = searchParams.get("area");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmation, setConfirmation] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");
    const loginPath =
        accountArea === "organization"
            ? "/admin/login"
            : accountArea === "team"
              ? "/login"
              : getLoginPath();

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setError("");
        setSuccess("");
        if (token) {
            if (!isStrongPassword(password)) {
                setError("Hasło musi mieć co najmniej 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny.");
                return;
            }
            if (password !== confirmation) {
                setError("Powtórzone hasło nie jest zgodne z nowym hasłem.");
                return;
            }
        } else if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
            setError("Podaj prawidłowy adres e-mail.");
            return;
        }

        setSubmitting(true);
        const { status } = token
            ? await apiRequestResult<null>(
                  `${apiEndpoints.links}/reset-password/${encodeURIComponent(token)}`,
                  { password },
                  "POST",
                  navigate
              )
            : await apiRequestResult<null>(
                  `${apiEndpoints.links}/reset-password`,
                  { email: email.trim().toLowerCase() },
                  "POST",
                  navigate
              );
        const expectedStatus = token ? 200 : 201;
        if (status === expectedStatus) {
            if (token) clearStoredSession();
            setSuccess(
                token
                    ? "Hasło zostało zmienione. Możesz się teraz zalogować."
                    : "Jeżeli konto istnieje, wiadomość z linkiem została wysłana."
            );
            setEmail("");
            setPassword("");
            setConfirmation("");
        } else setError(token ? "Link jest nieprawidłowy lub wygasł." : "Nie udało się wysłać wiadomości resetującej.");
        setSubmitting(false);
    };

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="grid flex-1 place-items-center px-4 py-10 sm:px-6">
                <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Bezpieczeństwo konta</p>
                    <h1 className="mt-2 text-2xl font-bold text-slate-900">{token ? "Ustaw nowe hasło" : "Zresetuj hasło"}</h1>
                    <p className="mt-2 text-sm text-slate-500">{token ? "Wprowadź nowe hasło dla konta." : "Podaj adres e-mail, a wyślemy link do ustawienia nowego hasła."}</p>
                    {error && <p role="alert" className="mt-5 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>}
                    {success && <p role="status" className="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800">{success}</p>}
                    {!success && (
                        <form onSubmit={submit} className="mt-6 space-y-4">
                            {token ? (
                                <>
                                    <label className="block text-sm font-medium text-slate-700">Nowe hasło<input className={fieldClass} type="password" autoComplete="new-password" value={password} disabled={submitting} onChange={(event) => setPassword(event.target.value)} required /></label>
                                    <label className="block text-sm font-medium text-slate-700">Powtórz nowe hasło<input className={fieldClass} type="password" autoComplete="new-password" value={confirmation} disabled={submitting} onChange={(event) => setConfirmation(event.target.value)} required /></label>
                                </>
                            ) : (
                                <label className="block text-sm font-medium text-slate-700">E-mail<input className={fieldClass} type="email" autoComplete="email" value={email} disabled={submitting} onChange={(event) => setEmail(event.target.value)} required /></label>
                            )}
                            <Button type="submit" text={submitting ? "Wysyłanie…" : token ? "Zapisz nowe hasło" : "Wyślij link"} disabled={submitting} className="w-full" />
                        </form>
                    )}
                    <button type="button" onClick={() => navigate(loginPath)} className="mt-4 w-full rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100">Wróć do logowania</button>
                </section>
            </main>
            <PublicFooter />
        </div>
    );
};

export default ResetPasswordScreen;
