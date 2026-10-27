import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import Button from "../../components/Button";
import PublicHeader from "../../components/PublicHeader";
import { useLoading } from "../../contexts/LoadingContext";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequestResult } from "../../utils/apiFetcher";

const inputClass =
    "mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted disabled:bg-surface-muted";

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
    const [onboardingOpen, setOnboardingOpen] = useState(true);
    const [onboardingStep, setOnboardingStep] = useState(0);

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
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="grid flex-1 place-items-center px-4 py-10 sm:px-6">
                <section className="w-full max-w-md rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Rejestracja
                    </p>
                    <h1 className="mt-2 text-2xl font-bold text-ink-heading">
                        Dokończ tworzenie konta
                    </h1>
                    {token && onboardingOpen && (
                        <div
                            className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 px-4 py-6"
                            role="presentation"
                        >
                            <section
                                role="dialog"
                                aria-modal="true"
                                aria-labelledby="onboarding-title"
                                aria-describedby="onboarding-description"
                                className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-2xl sm:p-8"
                            >
                                <div className="flex items-center justify-between">
                                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                                        Zanim zaczniesz
                                    </p>

                                    <button
                                        type="button"
                                        onClick={() => setOnboardingOpen(false)}
                                        className="rounded-lg px-3 py-1 text-xl text-slate-500 hover:bg-slate-100 hover:text-slate-900"
                                        aria-label="Zamknij wprowadzenie"
                                    >
                                        ×
                                    </button>
                                </div>

                                <div className="mt-5 flex gap-2" aria-label="Postęp">
                                    {[0, 1].map((step) => (
                                        <div
                                            key={step}
                                            className={`h-1.5 flex-1 rounded-full ${
                                                onboardingStep >= step
                                                    ? "bg-emerald-600"
                                                    : "bg-slate-200"
                                            }`}
                                        />
                                    ))}
                                </div>

                                {onboardingStep === 0 ? (
                                    <>
                                        <h2
                                            id="onboarding-title"
                                            className="mt-4 text-xl font-bold text-slate-900"
                                        >
                                            Poznaj zasady konkursu
                                        </h2>

                                        <p
                                            id="onboarding-description"
                                            className="mt-3 text-sm leading-6 text-slate-600"
                                        >
                                            Przed utworzeniem konta zapoznaj się z regulaminem
                                            konkursu. Znajdziesz w nim zasady udziału, warunki
                                            uczestnictwa oraz informacje o obowiązkach uczestników.
                                        </p>

                                        <a
                                            href="/rules"
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="mt-4 inline-flex font-medium text-emerald-700 underline underline-offset-4 hover:text-emerald-800"
                                        >
                                            Przeczytaj regulamin konkursu
                                        </a>

                                        <div className="mt-5 rounded-xl bg-slate-50 p-4 text-sm leading-6 text-slate-600">
                                            W formularzu samodzielnie zaznaczysz pole potwierdzające
                                            akceptację regulaminu. Zapoznaj się z jego treścią przed
                                            podjęciem decyzji.
                                        </div>
                                    </>
                                ) : (
                                    <>
                                        <h2
                                            id="onboarding-title"
                                            className="mt-4 text-xl font-bold text-slate-900"
                                        >
                                            Twoje dane i prywatność
                                        </h2>

                                        <p
                                            id="onboarding-description"
                                            className="mt-3 text-sm leading-6 text-slate-600"
                                        >
                                            Podczas rejestracji podajesz imię, nazwisko, adres
                                            e-mail i hasło. Dane osobowe są przetwarzane zgodnie z
                                            RODO.
                                        </p>
                                        <div className="mt-5 rounded-xl border border-emerald-100 bg-emerald-50 p-4 text-sm leading-6 text-slate-700">
                                            <p className="font-semibold text-slate-900">
                                                Co warto wiedzieć?
                                            </p>
                                            <ul className="mt-2 list-disc space-y-1 pl-5">
                                                <li>
                                                    Administratorem Twoich danych jest WMI UAM.??
                                                </li>
                                                <li>
                                                    Twoje dane będą przetwarzane na potrzeby
                                                    konkursu KOALA i zostaną usunięte po roku od
                                                    zakończenia ostatniej edycji konkursu.
                                                </li>
                                                <li>
                                                    Poznaj swoje prawa dotyczące danych osobowych.
                                                    <a
                                                        href="/polityka-prywatnosci"
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        className="mt-4 inline-flex font-medium text-emerald-700 underline underline-offset-4 hover:text-emerald-800"
                                                    >
                                                        Przeczytaj politykę prywatności - do
                                                        wstawienia zasady RODO
                                                    </a>
                                                </li>
                                            </ul>
                                        </div>

                                        <p className="mt-4 text-xs leading-5 text-slate-500">
                                            Przejście dalej nie oznacza automatycznej zgody. Decyzję
                                            podejmiesz samodzielnie w formularzu rejestracji.
                                        </p>
                                    </>
                                )}

                                <div className="mt-7 flex items-center justify-between gap-3">
                                    {onboardingStep === 0 ? (
                                        <button
                                            type="button"
                                            onClick={() => setOnboardingOpen(false)}
                                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
                                        >
                                            Pomiń wprowadzenie
                                        </button>
                                    ) : (
                                        <button
                                            type="button"
                                            onClick={() => setOnboardingStep(0)}
                                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
                                        >
                                            Wstecz
                                        </button>
                                    )}

                                    {onboardingStep === 0 ? (
                                        <button
                                            type="button"
                                            onClick={() => setOnboardingStep(1)}
                                            className="rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700"
                                        >
                                            Dalej
                                        </button>
                                    ) : (
                                        <button
                                            type="button"
                                            onClick={() => setOnboardingOpen(false)}
                                            className="rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700"
                                        >
                                            Przejdź do formularza
                                        </button>
                                    )}
                                </div>
                            </section>
                        </div>
                    )}
                    {!token ? (
                        <p
                            role="alert"
                            className="mt-5 rounded-xl border p-3 text-sm app-status-danger"
                        >
                            W adresie brakuje tokenu rejestracyjnego.
                        </p>
                    ) : (
                        <form onSubmit={submit} className="mt-6 space-y-4">
                            {error && (
                                <p
                                    role="alert"
                                    className="rounded-xl border p-3 text-sm app-status-danger"
                                >
                                    {error}
                                </p>
                            )}
                            <label className="block text-sm font-medium text-ink">
                                Imię
                                <input
                                    className={inputClass}
                                    value={nameFirst}
                                    onChange={(event) => setNameFirst(event.target.value)}
                                    disabled={submitting}
                                    required
                                />
                            </label>
                            <label className="block text-sm font-medium text-ink">
                                Nazwisko
                                <input
                                    className={inputClass}
                                    value={nameLast}
                                    onChange={(event) => setNameLast(event.target.value)}
                                    disabled={submitting}
                                    required
                                />
                            </label>
                            <label className="block text-sm font-medium text-ink">
                                Hasło
                                <input
                                    className={inputClass}
                                    type="password"
                                    autoComplete="new-password"
                                    value={password}
                                    onChange={(event) => setPassword(event.target.value)}
                                    disabled={submitting}
                                    required
                                />
                            </label>
                            <label className="flex items-start gap-3 text-sm text-ink">
                                <input
                                    className="mt-1"
                                    type="checkbox"
                                    checked={acceptedRules}
                                    onChange={(event) => setAcceptedRules(event.target.checked)}
                                    disabled={submitting}
                                />
                                Akceptuję regulamin konkursu.
                            </label>
                            <label className="flex items-start gap-3 text-sm text-ink">
                                <input
                                    className="mt-1"
                                    type="checkbox"
                                    checked={acceptedRodo}
                                    onChange={(event) => setAcceptedRodo(event.target.checked)}
                                    disabled={submitting}
                                />
                                Wyrażam zgodę na przetwarzanie danych osobowych.
                            </label>
                            <Button
                                type="submit"
                                text={submitting ? "Zapisywanie…" : "Utwórz konto"}
                                disabled={submitting}
                                className="w-full"
                            />
                        </form>
                    )}
                </section>
            </main>
        </div>
    );
};

export default CompleteRegistrationScreen;
