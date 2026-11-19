import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { User } from "../types/models";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import { getStoredUserId } from "../utils/authService";
import Button from "./Button";
import Modal from "./Modal";

interface ChangePasswordModalProps {
    isOpen: boolean;
    onClose: () => void;
}

const ChangePasswordModal = ({ isOpen, onClose }: ChangePasswordModalProps) => {
    const navigate = useNavigate();
    const [sending, setSending] = useState(false);
    const [error, setError] = useState("");
    const [sent, setSent] = useState(false);

    const close = () => {
        if (sending) return;
        setError("");
        setSent(false);
        onClose();
    };

    const requestReset = async () => {
        const id = getStoredUserId();
        if (!id) {
            setError("Sesja wygasła. Zaloguj się ponownie.");
            return;
        }
        setSending(true);
        setError("");
        const userResult = await apiRequestResult<User>(
            `${apiEndpoints.users}/${id}`,
            null,
            "GET",
            navigate
        );
        if (!userResult.data?.email) {
            setError("Nie udało się odczytać adresu e-mail konta.");
            setSending(false);
            return;
        }
        const { status } = await apiRequestResult<null>(
            `${apiEndpoints.links}/reset-password`,
            { email: userResult.data.email },
            "POST",
            navigate
        );
        if (status === 201) setSent(true);
        else setError("Nie udało się wysłać wiadomości resetującej hasło.");
        setSending(false);
    };

    return (
        <Modal isOpen={isOpen} onClose={close} title="Zmień hasło" maxWidth="md">
            <div className="space-y-5">
                {error && (
                    <p role="alert" className="rounded-xl border p-3 text-sm app-status-danger">
                        {error}
                    </p>
                )}
                {sent ? (
                    <p role="status" className="rounded-xl border p-4 text-sm app-status-success">
                        Wysłaliśmy wiadomość z bezpiecznym linkiem do ustawienia nowego hasła.
                    </p>
                ) : (
                    <p className="text-sm leading-6 text-ink-secondary">
                        Ze względów bezpieczeństwa zmiana hasła odbywa się przez jednorazowy link
                        wysłany na adres e-mail przypisany do konta.
                    </p>
                )}
                <div className="flex justify-end gap-3">
                    <button
                        type="button"
                        disabled={sending}
                        onClick={close}
                        className="rounded-xl px-4 py-2.5 text-sm font-medium text-ink hover:bg-surface-muted disabled:opacity-50"
                    >
                        {sent ? "Zamknij" : "Anuluj"}
                    </button>
                    {!sent && (
                        <Button
                            text={sending ? "Wysyłanie…" : "Wyślij link"}
                            onClick={() => void requestReset()}
                            disabled={sending}
                        />
                    )}
                </div>
            </div>
        </Modal>
    );
};

export default ChangePasswordModal;
