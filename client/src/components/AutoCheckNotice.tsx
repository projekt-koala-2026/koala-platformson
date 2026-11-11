import { useEffect, useState } from "react";
import { FaCheckDouble } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import Button from "./Button";

type Progress = {
    total: number;
    readyToCheck: number;
    missingKey: number;
    alreadyReviewedWithoutResult: number;
};

export default function AutoCheckNotice({ id, name }: { id: string; name: string }) {
    const navigate = useNavigate();
    const [progress, setProgress] = useState<Progress | null>(null);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    const [busy, setBusy] = useState(false);
    const [revision, setRevision] = useState(0);
    useEffect(() => {
        let active = true;
        void apiRequestResult<Progress>(
            apiEndpoints.autoCheckProgress(id),
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            if (result.data) setProgress(result.data);
            else setError("Nie udało się pobrać stanu sprawdzania odpowiedzi.");
        });
        return () => {
            active = false;
        };
    }, [id, navigate, revision]);

    const run = async () => {
        setBusy(true);
        setError("");
        setMessage("");
        const result = await apiRequestResult(apiEndpoints.autoCheck(id), null, "PUT", navigate);
        if (result.status && result.status >= 200 && result.status < 300) {
            setProgress(null);
            setMessage("Uruchomiono sprawdzanie. Zaktualizowano dostępne wyniki.");
            setRevision((value) => value + 1);
        } else
            setError(
                "Nie udało się uruchomić sprawdzarki. Sprawdź termin oraz klucze odpowiedzi i spróbuj ponownie."
            );
        setBusy(false);
    };

    if (
        !error &&
        !message &&
        (!progress ||
            (!progress.readyToCheck &&
                !progress.missingKey &&
                !progress.alreadyReviewedWithoutResult))
    )
        return null;
    return (
        <section
            className="rounded-2xl border border-line bg-surface p-5"
            aria-label={`Sprawdzarka: ${name}`}
        >
            <h3 className="font-semibold text-ink-heading">Sprawdzanie odpowiedzi: {name}</h3>
            {error && (
                <p role="alert" className="app-status-danger mt-3 rounded-xl border p-3">
                    {error}
                </p>
            )}
            {message && (
                <p role="status" className="app-status-success mt-3 rounded-xl border p-3">
                    {message}
                </p>
            )}
            {progress && (
                <div className="mt-3 space-y-2 text-sm text-ink-secondary">
                    {progress.readyToCheck > 0 && (
                        <p>
                            Odpowiedzi bez wyniku, gotowe do sprawdzenia:{" "}
                            <strong>
                                {progress.readyToCheck} z {progress.total}
                            </strong>
                            .
                        </p>
                    )}
                    {progress.missingKey > 0 && (
                        <p>
                            Odpowiedzi bez wyniku i klucza odpowiedzi:{" "}
                            <strong>{progress.missingKey}</strong>. Dodaj klucze w zadaniach.
                        </p>
                    )}
                    {progress.alreadyReviewedWithoutResult > 0 && (
                        <p>
                            Odpowiedzi bez wyniku z rozpoczętą recenzją lub konfliktem ocen:{" "}
                            <strong>{progress.alreadyReviewedWithoutResult}</strong>. Sprawdzarka
                            ich nie przeliczy.
                        </p>
                    )}
                    {progress.readyToCheck > 0 && (
                        <Button
                            text={busy ? "Sprawdzanie…" : "Uruchom sprawdzarkę"}
                            icon={<FaCheckDouble />}
                            disabled={busy}
                            onClick={() => void run()}
                        />
                    )}
                </div>
            )}
        </section>
    );
}
