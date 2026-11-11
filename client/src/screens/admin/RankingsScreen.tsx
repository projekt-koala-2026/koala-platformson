import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import SelectListbox from "../../components/SelectListbox";
import type { ApiEdition, ApiRanking, ApiSubedition } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

const rankingsQuery = `${firstPage}`;

export default function RankingsScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [rankings, setRankings] = useState<ApiRanking[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingRanking, setLoadingRanking] = useState(false);
    const [busy, setBusy] = useState(false);
    const [published, setPublished] = useState(false);
    const [notice, setNotice] = useState("");
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const items = data ?? [];
            setEditions(items);
            setEditionId(items[0]?.id ?? "");
            if (!data) setError("Nie udało się pobrać edycji.");
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    useEffect(() => {
        if (!editionId) {
            setSubeditions([]);
            setSubeditionId("");
            return;
        }
        let active = true;
        setSubeditions([]);
        setSubeditionId("");
        setRankings([]);
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const items = data ?? [];
            setSubeditions(items);
            setSubeditionId(items[0]?.id ?? "");
            if (!data) setError("Nie udało się pobrać podedycji.");
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate]);

    useEffect(() => {
        if (!subeditionId) {
            setRankings([]);
            setPublished(false);
            return;
        }
        let active = true;
        setLoadingRanking(true);
        setRankings([]);
        setNotice("");
        void Promise.all([
            apiListRequestResult<ApiRanking>(
                `${apiEndpoints.rankingDraft(subeditionId)}?${rankingsQuery}`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiRanking>(
                `${apiEndpoints.rankings}?subeditionId=${encodeURIComponent(subeditionId)}&${rankingsQuery}`,
                null,
                "GET",
                navigate
            ),
        ]).then(([draft, publicRanking]) => {
            if (!active) return;
            setRankings(draft.data ?? []);
            setPublished(Boolean(publicRanking.data?.length));
            setLoadingRanking(false);
        });
        return () => {
            active = false;
        };
    }, [subeditionId, navigate]);

    const generate = async () => {
        if (!subeditionId || busy) return;
        setBusy(true);
        setError("");
        setNotice("");
        const result = await apiRequestResult(
            apiEndpoints.generateRanking(subeditionId),
            null,
            "POST",
            navigate
        );
        if (!result.error && result.status && result.status >= 200 && result.status < 300) {
            const data = await apiListRequestResult<ApiRanking>(
                `${apiEndpoints.rankingDraft(subeditionId)}?${rankingsQuery}`,
                null,
                "GET",
                navigate
            );
            setRankings(data.data ?? []);
            setPublished(false);
            setNotice("Ranking został wygenerowany. Możesz go teraz opublikować.");
        } else setError(result.error?.message ?? "Nie udało się wygenerować rankingu.");
        setBusy(false);
    };

    const publish = async () => {
        if (!subeditionId || busy || !rankings.length) return;
        setBusy(true);
        setError("");
        setNotice("");
        const result = await apiRequestResult(
            apiEndpoints.publishRanking(subeditionId),
            null,
            "PUT",
            navigate
        );
        if (!result.error && result.status && result.status >= 200 && result.status < 300) {
            setPublished(true);
            setNotice("Ranking został opublikowany na stronie publicznej.");
        } else setError(result.error?.message ?? "Nie udało się opublikować rankingu.");
        setBusy(false);
    };

    const selectedSubedition = subeditions.find((item) => item.id === subeditionId);
    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
                <header className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Panel administracyjny
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">Rankingi drużyn</h1>
                    <p className="mt-3 max-w-2xl text-ink-secondary">
                        Generuj ranking na podstawie zatwierdzonych punktów, sprawdź wynik i
                        opublikuj go dla uczestników.
                    </p>
                </header>
                {error && (
                    <p role="alert" className="app-status-danger mb-4 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                {notice && (
                    <p role="status" className="app-status-success mb-4 rounded-xl border p-3">
                        {notice}
                    </p>
                )}
                <section className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-7">
                    {loading ? (
                        <p className="text-sm text-ink-muted">Ładowanie edycji…</p>
                    ) : editions.length === 0 ? (
                        <p className="text-sm text-ink-muted">Nie ma jeszcze zamkniętych edycji.</p>
                    ) : (
                        <div className="grid gap-4 sm:grid-cols-2">
                            <div>
                                <span
                                    id="ranking-edition-label"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Edycja
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="ranking-edition-label"
                                    value={editionId}
                                    options={editions.map((item) => ({
                                        value: item.id,
                                        label: item.name,
                                    }))}
                                    onChange={setEditionId}
                                />
                            </div>
                            <div>
                                <span
                                    id="ranking-subedition-label"
                                    className="mb-2 block text-sm font-medium text-ink"
                                >
                                    Podedycja
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="ranking-subedition-label"
                                    value={subeditionId}
                                    options={subeditions.map((item) => ({
                                        value: item.id,
                                        label: item.name,
                                    }))}
                                    onChange={setSubeditionId}
                                />
                            </div>
                        </div>
                    )}
                    {selectedSubedition && (
                        <>
                            <p className="mt-4 text-sm text-ink-secondary">
                                Ranking dla podedycji{" "}
                                <strong className="text-ink-heading">
                                    {selectedSubedition.name}
                                </strong>
                                . Generowanie wymaga zamkniętej edycji, zakończonego czasu
                                odpowiedzi i rozstrzygniętych ocen.
                            </p>
                            <div className="mt-5 flex flex-wrap gap-3">
                                <Button
                                    text={busy ? "Przetwarzanie…" : "Generuj ranking"}
                                    disabled={busy || loadingRanking}
                                    onClick={() => void generate()}
                                />
                                <Button
                                    text={
                                        busy
                                            ? "Przetwarzanie…"
                                            : published
                                              ? "Opublikowano"
                                              : "Opublikuj ranking"
                                    }
                                    disabled={
                                        busy || loadingRanking || !rankings.length || published
                                    }
                                    onClick={() => void publish()}
                                />
                            </div>
                        </>
                    )}
                </section>
                {subeditionId && (
                    <section className="mt-8" aria-labelledby="draft-ranking-heading">
                        <div className="mb-4 flex items-end justify-between gap-3">
                            <div>
                                <h2
                                    id="draft-ranking-heading"
                                    className="text-xl font-semibold text-ink-heading"
                                >
                                    Podgląd rankingu
                                </h2>
                                <p className="mt-1 text-sm text-ink-muted">
                                    Posortowany według liczby zdobytych punktów.
                                </p>
                            </div>
                            <span className="text-xs text-ink-muted">
                                {published ? "Opublikowany" : "Roboczy"}
                            </span>
                        </div>
                        {loadingRanking ? (
                            <p className="rounded-xl border border-line bg-surface p-6 text-sm text-ink-muted">
                                Ładowanie rankingu…
                            </p>
                        ) : rankings.length === 0 ? (
                            <p className="rounded-xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                Brak rankingu. Wygeneruj go po zakończeniu oceniania.
                            </p>
                        ) : (
                            <div className="overflow-x-auto rounded-2xl border border-line bg-surface">
                                <table className="w-full text-left text-sm">
                                    <thead className="bg-surface-muted text-xs uppercase tracking-wide text-ink-secondary">
                                        <tr>
                                            <th className="px-4 py-3">Miejsce</th>
                                            <th className="px-4 py-3">Drużyna</th>
                                            <th className="px-4 py-3">Poziom</th>
                                            <th className="px-4 py-3 text-right">Punkty</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {rankings.map((item, index) => (
                                            <tr
                                                key={`${item.teamName}-${item.tier}`}
                                                className="border-t border-line"
                                            >
                                                <td className="px-4 py-3 font-semibold text-ink-heading">
                                                    {index + 1}.
                                                </td>
                                                <td className="px-4 py-3 font-medium text-ink-heading">
                                                    {item.teamName}
                                                </td>
                                                <td className="px-4 py-3 text-ink-secondary">
                                                    {item.tier === "TIER_2"
                                                        ? "Poziom II"
                                                        : "Poziom I"}
                                                </td>
                                                <td className="px-4 py-3 text-right font-semibold text-ink-heading">
                                                    {item.totalScore} / {item.maxScore}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                )}
            </main>
        </div>
    );
}
