import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { Koalicjant } from "../../types/models";
import { apiRequest, resolveApiAssetUrl } from "../../utils/apiFetcher";

const KoalicjaScreen = () => {
    const navigate = useNavigate();
    const [koalicjants, setKoalicjants] = useState<Koalicjant[]>([]);
    useEffect(() => {
        const load = async () => {
            const data = await apiRequest<Koalicjant[]>(
                "/api/admin/koalicjants",
                null,
                "GET",
                navigate
            );
            if (data) setKoalicjants(data);
        };
        void load();
    }, [navigate]);
    return (
        <>
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-6 py-10">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-emerald-700">
                        Ludzie KOALA
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">KOALicjA</h1>
                    <p className="mt-3 text-slate-600">Poznaj osoby współtworzące konkurs KOALA.</p>
                </div>
                <section className="grid grid-cols-1 gap-6 md:grid-cols-2">
                    {koalicjants.map((person) => (
                        <article
                            key={person.id}
                            className="flex gap-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
                        >
                            <img
                                src={resolveApiAssetUrl(person.profilePicture)}
                                alt={person.name}
                                className="h-28 w-28 shrink-0 rounded-full object-cover shadow-md"
                            />
                            <div className="min-w-0">
                                <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                    Organizator KOALA
                                </p>
                                <h2 className="mt-1 text-xl font-semibold text-slate-800">
                                    {person.name}
                                </h2>
                                <p className="mt-3 text-sm leading-6 text-slate-600">
                                    {person.description}
                                </p>
                            </div>
                        </article>
                    ))}
                </section>
            </main>
            <PublicFooter />
        </>
    );
};
export default KoalicjaScreen;
