import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import Button from "../../components/Button";
import PublicHeader from "../../components/PublicHeader";
import { useLoading } from "../../contexts/LoadingContext";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const inputClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:bg-slate-100";

const isStrongPassword = (password: string) =>
    password.length >= 8 &&
    /[a-z]/.test(password) &&
    /[A-Z]/.test(password) &&
    /\d/.test(password) &&
    /[\W_]/.test(password);

const CompleteRegistrationScreen = () => {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const { startLoading, stopLoading } = useLoading();
    const token = searchParams.get("token")?.trim() ?? "";
    const [nameFirst, setNameFirst] = useState("");
    const [nameLast, setNameLast] = useState("");
    const [password, setPassword] = useState("");
    const [acceptedRodo, setAcceptedRodo] = useState(false);
    const [acceptedRules, setAcceptedRules] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!token || submitting) return;
        setError("");
        if (!isStrongPassword(password)) {
            setError(
                "Hasło musi mieć co najmniej 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny."
            );
            return;
        }
        if (!acceptedRodo || !acceptedRules) {
            setError("Zaakceptuj regulamin oraz zgodę na przetwarzanie danych.");
            return;
        }

        setSubmitting(true);
        startLoading();
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.links}/register/${encodeURIComponent(token)}`,
            {
                nameFirst: nameFirst.trim(),
                nameLast: nameLast.trim(),
                password,
                acceptedRodo,
                acceptedRules,
            },
            "POST",
            navigate
        );
        stopLoading();
        setSubmitting(false);
        if (status === 200) {
            navigate("/login", { replace: true });
            return;
        }
        setError(
            status === 404
                ? "Link rejestracyjny jest nieprawidłowy lub wygasł."
                : "Nie udało się dokończyć rejestracji. Spróbuj ponownie."
        );
    };

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="grid flex-1 place-items-center px-4 py-10 sm:px-6">
                <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Rejestracja
                    </p>
                    <h1 className="mt-2 text-2xl font-bold text-slate-900">Dokończ tworzenie konta</h1>

                    {!token ? (
                        <p role="alert" className="mt-5 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                            W adresie brakuje tokenu rejestracyjnego.
                        </p>
                    ) : (
                        <form onSubmit={submit} className="mt-6 space-y-4">
                            {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>}
                            <label className="block text-sm font-medium text-slate-700">Imię
                                <input className={inputClass} value={nameFirst} onChange={(event) => setNameFirst(event.target.value)} disabled={submitting} required />
                            </label>
                            <label className="block text-sm font-medium text-slate-700">Nazwisko
                                <input className={inputClass} value={nameLast} onChange={(event) => setNameLast(event.target.value)} disabled={submitting} required />
                            </label>
                            <label className="block text-sm font-medium text-slate-700">Hasło
                                <input className={inputClass} type="password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} disabled={submitting} required />
                            </label>
                            <label className="flex items-start gap-3 text-sm text-slate-700">
                                <input className="mt-1" type="checkbox" checked={acceptedRules} onChange={(event) => setAcceptedRules(event.target.checked)} disabled={submitting} />
                                Akceptuję regulamin konkursu.
                            </label>
                            <label className="flex items-start gap-3 text-sm text-slate-700">
                                <input className="mt-1" type="checkbox" checked={acceptedRodo} onChange={(event) => setAcceptedRodo(event.target.checked)} disabled={submitting} />
                                Wyrażam zgodę na przetwarzanie danych osobowych.
                            </label>
                            <Button type="submit" text={submitting ? "Zapisywanie…" : "Utwórz konto"} disabled={submitting} className="w-full" />
                        </form>
                    )}
                </section>
            </main>
        </div>
    );
};

export default CompleteRegistrationScreen;
