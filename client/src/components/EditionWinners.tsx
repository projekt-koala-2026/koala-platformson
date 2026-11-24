import { useEffect, useState, type FormEvent } from "react";
import { FaEdit, FaEye, FaEyeSlash, FaPlus } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import type { ApiEdition, ApiWinner } from "../types/models";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import Button from "./Button";
import Modal from "./Modal";

const categories = [
    { tier: "TIER_1", label: "Szkoły podstawowe" },
    { tier: "TIER_2", label: "Szkoły ponadpodstawowe" },
] as const;
type Tier = (typeof categories)[number]["tier"];

// The parent keys this component by edition, keeping forms and responses isolated.
export default function EditionWinners({ edition }: { edition: ApiEdition }) {
    const navigate = useNavigate();
    const [winners, setWinners] = useState<ApiWinner[]>([]);
    const [loaded, setLoaded] = useState(false);
    const [revision, setRevision] = useState(0);
    const [busy, setBusy] = useState(false);
    const [isPublic, setIsPublic] = useState(Boolean(edition.areWinnersPublic));
    const [tier, setTier] = useState<Tier | null>(null);
    const [name, setName] = useState("");
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");

    useEffect(() => {
        let active = true;
        void apiRequestResult<ApiWinner[]>(
            apiEndpoints.editionWinners(edition.id),
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            if (result.data) {
                setWinners(result.data);
                setLoaded(true);
            } else {
                setError(result.error?.message ?? "Nie udało się pobrać zwycięzców.");
            }
        });
        return () => {
            active = false;
        };
    }, [edition.id, navigate, revision]);

    const save = async (event: FormEvent) => {
        event.preventDefault();
        if (busy || !tier || !name.trim()) return;
        setBusy(true);
        setError("");
        setNotice("");
        const current = winners.find((winner) => winner.tier === tier);
        const result = await apiRequestResult<ApiWinner>(
            apiEndpoints.editionWinner(edition.id, tier),
            { teamName: name.trim(), version: current?.version ?? null },
            "PUT",
            navigate
        );
        if (result.data) {
            setWinners((items) => [...items.filter((item) => item.tier !== tier), result.data!]);
            setTier(null);
            setNotice("Zapisano zwycięzcę.");
        } else {
            setError(
                result.status === 409
                    ? "Dane zostały zmienione przez inną osobę. Zamknij formularz i odśwież zwycięzców przed ponowną edycją."
                    : (result.error?.message ?? "Nie udało się zapisać zwycięzcy.")
            );
        }
        setBusy(false);
    };

    const publish = async () => {
        if (busy || !loaded) return;
        setBusy(true);
        setError("");
        setNotice("");
        const result = await apiRequestResult(
            apiEndpoints.winnersPublication(edition.id, !isPublic),
            null,
            "PUT",
            navigate
        );
        if (result.status !== null && result.status >= 200 && result.status < 300) {
            setIsPublic(!isPublic);
            setNotice(isPublic ? "Ukryto zwycięzców." : "Opublikowano zwycięzców.");
        } else setError(result.error?.message ?? "Nie udało się zmienić widoczności zwycięzców.");
        setBusy(false);
    };

    return (
        <section
            className="mt-6 rounded-2xl border border-line bg-surface p-5"
            aria-label="Zwycięzcy edycji"
        >
            <div className="mb-4 flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h2 className="text-xl font-bold">Zwycięzcy edycji</h2>
                    <p className="mt-1 text-sm text-ink-secondary">
                        Wskaż zwycięzcę każdej kategorii. Wybór jest niezależny od rankingów.
                    </p>
                    <p className="mt-1 text-sm text-ink-secondary">
                        {isPublic
                            ? "Widoczni publicznie — zmiana nazwy będzie od razu widoczna."
                            : "Nieopublikowani"}
                    </p>
                </div>
                <div className="flex flex-wrap gap-2">
                    <Button
                        text="Odśwież zwycięzców"
                        disabled={busy || tier !== null}
                        onClick={() => {
                            setError("");
                            setLoaded(false);
                            setRevision((value) => value + 1);
                        }}
                    />
                    <Button
                        text={isPublic ? "Ukryj zwycięzców" : "Opublikuj zwycięzców"}
                        icon={isPublic ? <FaEyeSlash /> : <FaEye />}
                        disabled={
                            busy || !loaded || tier !== null || (!isPublic && winners.length === 0)
                        }
                        onClick={() => void publish()}
                    />
                </div>
            </div>
            {error && !tier && (
                <p role="alert" className="app-status-danger mb-4 rounded-xl border p-3">
                    {error}
                </p>
            )}
            {notice && (
                <p role="status" className="app-status-success mb-4 rounded-xl border p-3">
                    {notice}
                </p>
            )}
            {!loaded ? (
                <p role="status" className="text-ink-secondary">
                    {error
                        ? "Dane zwycięzców są niedostępne. Spróbuj odświeżyć."
                        : "Wczytywanie zwycięzców…"}
                </p>
            ) : (
                <div className="grid gap-4 md:grid-cols-2">
                    {categories.map((category) => {
                        const winner = winners.find((item) => item.tier === category.tier);
                        return (
                            <div
                                key={category.tier}
                                className="min-w-0 rounded-xl border border-line p-4"
                            >
                                <h3 className="font-semibold">{category.label}</h3>
                                <p className="my-3 break-words text-ink-secondary">
                                    {winner?.teamName ?? "Nie wskazano zwycięzcy"}
                                </p>
                                <Button
                                    text={winner ? "Edytuj zwycięzcę" : "Dodaj zwycięzcę"}
                                    icon={winner ? <FaEdit /> : <FaPlus />}
                                    disabled={busy || tier !== null}
                                    onClick={() => {
                                        setError("");
                                        setNotice("");
                                        setName(winner?.teamName ?? "");
                                        setTier(category.tier);
                                    }}
                                />
                            </div>
                        );
                    })}
                </div>
            )}
            <Modal
                isOpen={tier !== null}
                onClose={() => {
                    if (!busy) {
                        setTier(null);
                        setError("");
                    }
                }}
                title="Zwycięzca kategorii"
                maxWidth="md"
            >
                <form onSubmit={(event) => void save(event)} className="space-y-4">
                    <p className="text-ink-secondary">
                        {categories.find((category) => category.tier === tier)?.label}
                    </p>
                    {error && (
                        <p role="alert" className="app-status-danger rounded-xl border p-3">
                            {error}
                        </p>
                    )}
                    <label className="block text-sm font-medium">
                        Nazwa drużyny
                        <input
                            required
                            autoFocus
                            disabled={busy}
                            value={name}
                            onChange={(event) => setName(event.target.value)}
                            className="mt-1.5 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-ink-heading outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                        />
                    </label>
                    <div className="flex flex-wrap justify-end gap-3">
                        <Button
                            text="Anuluj"
                            disabled={busy}
                            onClick={() => {
                                setTier(null);
                                setError("");
                            }}
                        />
                        <Button
                            text={busy ? "Zapisywanie…" : "Zapisz zwycięzcę"}
                            type="submit"
                            disabled={busy || !name.trim()}
                        />
                    </div>
                </form>
            </Modal>
        </section>
    );
}
