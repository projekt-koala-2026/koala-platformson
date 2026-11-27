import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { User } from "../types/models";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import { getStoredUserId } from "../utils/authService";
import Button from "./Button";
import Modal from "./Modal";

interface ChangeNameModalProps {
    isOpen: boolean;
    onClose: () => void;
}

const ChangeNameModal = ({ isOpen, onClose }: ChangeNameModalProps) => {
    const navigate = useNavigate();
    const [sending, setSending] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [nameFirst, setNameFirst] = useState("");
    const [nameLast, setNameLast] = useState("");
    const [version, setVersion] = useState<number | null>(null);

    useEffect(() => {
        if (!isOpen) return;
        let cancelled = false;
        const loadUser = async () => {
            setLoading(true);
            setError("");
            const id = getStoredUserId();
            if (!id) {
                setError("Sesja wygasła. Zaloguj się ponownie.");
                setLoading(false);
                return;
            }
            const result = await apiRequestResult<User>(
                `${apiEndpoints.users}/${id}`,
                null,
                "GET",
                navigate
            );
            if (cancelled) return;
            if (result.data) {
                setNameFirst(result.data.nameFirst ?? "");
                setNameLast(result.data.nameLast ?? "");
                setVersion(result.data.version);
            } else {
                setError("Nie udało się pobrać danych konta.");
            }
            setLoading(false);
        };
        void loadUser();
        return () => {
            cancelled = true;
        };
    }, [isOpen, navigate]);

    const close = () => {
        if (sending) return;
        setError("");
        onClose();
    };

    const saveName = async () => {
        const id = getStoredUserId();
        const first = nameFirst.trim();
        const last = nameLast.trim();
        if (!id) {
            setError("Sesja wygasła. Zaloguj się ponownie.");
            return;
        }
        if (!first || !last) {
            setError("Podaj imię i nazwisko.");
            return;
        }
        if (version === null) {
            setError(
                "Nie udało się odczytać wersji danych konta. Zamknij i otwórz formularz ponownie."
            );
            return;
        }

        setSending(true);
        setError("");
        const result = await apiRequestResult<null>(
            `${apiEndpoints.users}/${id}/name`,
            { nameFirst: first, nameLast: last, version },
            "PUT",
            navigate
        );
        if (result.status !== null && result.status >= 200 && result.status < 300) {
            onClose();
        } else {
            setError("Nie udało się zmienić imienia i nazwiska. Spróbuj ponownie.");
        }
        setSending(false);
    };

    return (
        <Modal isOpen={isOpen} onClose={close} title="Zmień imię i nazwisko" maxWidth="md">
            <form
                className="space-y-5"
                onSubmit={(event) => {
                    event.preventDefault();
                    void saveName();
                }}
            >
                {error && (
                    <p role="alert" className="rounded-xl border p-3 text-sm app-status-danger">
                        {error}
                    </p>
                )}
                <div className="space-y-4">
                    <label className="block space-y-1.5 text-sm font-medium text-ink">
                        <span>Imię</span>
                        <input
                            type="text"
                            autoComplete="given-name"
                            value={nameFirst}
                            onChange={(event) => setNameFirst(event.target.value)}
                            disabled={loading || sending}
                            required
                            className="w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-ink outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-focus/20 disabled:opacity-60"
                        />
                    </label>
                    <label className="block space-y-1.5 text-sm font-medium text-ink">
                        <span>Nazwisko</span>
                        <input
                            type="text"
                            autoComplete="family-name"
                            value={nameLast}
                            onChange={(event) => setNameLast(event.target.value)}
                            disabled={loading || sending}
                            required
                            className="w-full rounded-xl border border-line bg-surface px-3 py-2.5 text-ink outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-focus/20 disabled:opacity-60"
                        />
                    </label>
                </div>
                <div className="flex justify-end gap-3">
                    <button
                        type="button"
                        disabled={sending}
                        onClick={close}
                        className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:opacity-50"
                    >
                        Anuluj
                    </button>
                    <Button
                        text={sending ? "Zapisywanie…" : "Zapisz zmiany"}
                        type="submit"
                        disabled={loading || sending || version === null}
                    />
                </div>
            </form>
        </Modal>
    );
};

export default ChangeNameModal;
