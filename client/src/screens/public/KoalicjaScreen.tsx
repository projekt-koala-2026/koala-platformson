import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiKoalicjant, Koalicjant } from "../../types/models";
import { adaptKoalicjant } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiRequest, resolveApiAssetUrl } from "../../utils/apiFetcher";

const KoalicjaScreen = () => {
    const navigate = useNavigate();
    const [koalicjants, setKoalicjants] = useState<Koalicjant[]>([]);
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    useEffect(() => {
        let active = true;
        const load = async () => {
            const data = await apiRequest<ApiKoalicjant[]>(
                `${apiEndpoints.koalicjants}?${firstPage}`,
                null,
                "GET",
                navigate
            );
            if (!active) return;
            if (data) {
                setKoalicjants(data.filter((person) => person.isVisible).map(adaptKoalicjant));
            } else {
                setHasError(true);
            }
            setLoading(false);
        };
        void load();
        return () => {
            active = false;
        };
    }, [navigate]);
    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-brand-text">
                        Ludzie KOALA
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-ink-heading">KOALicjA</h1>
                    <p className="mt-3 text-ink-secondary">
                        Poznaj osoby współtworzące konkurs KOALA.
                    </p>
                </div>
                {hasError && (
                    <p
                        role="status"
                        className="mb-5 rounded-xl border px-4 py-3 text-sm app-status-warning"
                    >
                        Nie udało się pobrać listy koalicjantów. Spróbuj ponownie później.
                    </p>
                )}
                <section className="grid grid-cols-1 gap-6 md:grid-cols-2">
                    {loading && (
                        <p className="col-span-full rounded-2xl border border-line bg-surface p-8 text-center text-sm text-ink-muted shadow-sm">
                            Ładowanie koalicjantów…
                        </p>
                    )}
                    {!loading && !hasError && koalicjants.length === 0 && (
                        <p className="col-span-full rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                            Lista koalicjantów nie została jeszcze opublikowana.
                        </p>
                    )}
                    {koalicjants.map((person) => (
                        <article
                            key={person.id}
                            className="flex flex-col gap-4 rounded-2xl border border-line bg-surface p-5 shadow-sm sm:flex-row sm:gap-5 sm:p-6"
                        >
                            <img
                                src={resolveApiAssetUrl(person.profilePicture)}
                                alt={person.name}
                                loading="lazy"
                                className="h-24 w-24 shrink-0 rounded-full object-cover shadow-md sm:h-28 sm:w-28"
                            />
                            <div className="min-w-0">
                                <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                    Organizator KOALA
                                </p>
                                <h2 className="mt-1 text-xl font-semibold text-ink-strong">
                                    {person.name}
                                </h2>
                                <p className="mt-3 text-sm leading-6 text-ink-secondary">
                                    {person.description}
                                </p>
                            </div>
                        </article>
                    ))}
                </section>
            </main>
            <PublicFooter />
        </div>
    );
};
export default KoalicjaScreen;
