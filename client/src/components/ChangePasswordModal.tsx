import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import type { User } from "../types/models";
import { apiRequestResult } from "../utils/apiFetcher";
import { getStoredUserId } from "../utils/authService";
import Button from "./Button";
import Modal from "./Modal";

interface ChangePasswordModalProps {
    isOpen: boolean;
    onClose: () => void;
}

const fieldClass =
    "mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 disabled:cursor-not-allowed disabled:bg-slate-100";

const isStrongPassword = (password: string) =>
    password.length >= 8 &&
    /[a-z]/.test(password) &&
    /[A-Z]/.test(password) &&
    /\d/.test(password) &&
    /[\W_]/.test(password);

const ChangePasswordModal = ({ isOpen, onClose }: ChangePasswordModalProps) => {
    const navigate = useNavigate();
    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmation, setConfirmation] = useState("");
    const [error, setError] = useState("");
    const [saving, setSaving] = useState(false);
    const [saved, setSaved] = useState(false);

    const reset = () => {
        setCurrentPassword("");
        setNewPassword("");
        setConfirmation("");
        setError("");
        setSaved(false);
    };

    const close = () => {
        if (saving) return;
        reset();
        onClose();
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setError("");
        const id = getStoredUserId();
        if (!id) {
            setError("Sesja wygasła. Zaloguj się ponownie.");
            return;
        }
        if (!isStrongPassword(newPassword)) {
            setError(
                "Nowe hasło musi mieć co najmniej 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny."
            );
            return;
        }
        if (newPassword === currentPassword) {
            setError("Nowe hasło musi różnić się od obecnego.");
            return;
        }
        if (newPassword !== confirmation) {
            setError("Powtórzone hasło nie jest zgodne z nowym hasłem.");
            return;
        }

        setSaving(true);
        const { data } = await apiRequestResult<User>(
            "/api/admin/user/password",
            { id, password: currentPassword, newPassword },
            "PUT",
            navigate
        );
        if (!data) {
            setError("Nie udało się zmienić hasła. Sprawdź obecne hasło i spróbuj ponownie.");
            setSaving(false);
            return;
        }

        setCurrentPassword("");
        setNewPassword("");
        setConfirmation("");
        setSaved(true);
        setSaving(false);
    };

    return (
        <Modal isOpen={isOpen} onClose={close} title="Zmień hasło" maxWidth="md">
            {saved ? (
                <div className="space-y-5">
                    <p
                        role="status"
                        className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800"
                    >
                        Hasło zostało zmienione.
                    </p>
                    <div className="flex justify-end">
                        <Button text="Zamknij" onClick={close} />
                    </div>
                </div>
            ) : (
                <form onSubmit={submit} className="space-y-4">
                    {error && (
                        <p
                            role="alert"
                            className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                        >
                            {error}
                        </p>
                    )}
                    <label className="block text-sm font-semibold text-slate-700">
                        Obecne hasło
                        <input
                            className={fieldClass}
                            type="password"
                            autoComplete="current-password"
                            value={currentPassword}
                            disabled={saving}
                            onChange={(event) => setCurrentPassword(event.target.value)}
                            required
                        />
                    </label>
                    <label className="block text-sm font-semibold text-slate-700">
                        Nowe hasło
                        <input
                            className={fieldClass}
                            type="password"
                            autoComplete="new-password"
                            value={newPassword}
                            disabled={saving}
                            onChange={(event) => setNewPassword(event.target.value)}
                            required
                        />
                    </label>
                    <label className="block text-sm font-semibold text-slate-700">
                        Powtórz nowe hasło
                        <input
                            className={fieldClass}
                            type="password"
                            autoComplete="new-password"
                            value={confirmation}
                            disabled={saving}
                            onChange={(event) => setConfirmation(event.target.value)}
                            required
                        />
                    </label>
                    <p className="text-xs leading-5 text-slate-500">
                        Minimum 8 znaków, wielka i mała litera, cyfra oraz znak specjalny.
                    </p>
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            disabled={saving}
                            onClick={close}
                            className="rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            text={saving ? "Zapisywanie…" : "Zapisz hasło"}
                            type="submit"
                            disabled={saving}
                        />
                    </div>
                </form>
            )}
        </Modal>
    );
};

export default ChangePasswordModal;
