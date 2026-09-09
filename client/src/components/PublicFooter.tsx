import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { Sponsor } from "../types/models";
import { apiRequest, resolveApiAssetUrl } from "../utils/apiFetcher";

const PublicFooter = () => {
    const navigate = useNavigate();
    const [sponsors, setSponsors] = useState<Sponsor[]>([]);

    useEffect(() => {
        let active = true;
        void apiRequest<Sponsor[]>("/api/admin/sponsors", null, "GET", navigate).then((data) => {
            if (active && data) setSponsors(data);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    return (
        <footer className="mt-auto w-full border-t border-slate-200 bg-white">
            <div className="mx-auto max-w-6xl px-6 py-8">
                {sponsors.length > 0 && (
                    <section aria-labelledby="sponsors-heading">
                        <h2
                            id="sponsors-heading"
                            className="text-center text-sm font-semibold uppercase tracking-wide text-slate-500"
                        >
                            Nasi sponsorzy
                        </h2>
                        <div className="mt-5 flex flex-wrap items-center justify-center gap-4">
                            {sponsors.map((sponsor) => (
                                <a
                                    key={sponsor.id}
                                    href={sponsor.websiteUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    title={sponsor.description || sponsor.name}
                                    className="flex min-h-16 w-36 items-center justify-center rounded-xl p-2 transition hover:bg-emerald-50 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                                >
                                    {sponsor.logoUrl ? (
                                        <img
                                            src={resolveApiAssetUrl(sponsor.logoUrl)}
                                            alt={sponsor.name}
                                            loading="lazy"
                                            className="max-h-12 max-w-full object-contain grayscale transition hover:grayscale-0"
                                        />
                                    ) : (
                                        <span className="text-center text-sm font-medium text-slate-700">
                                            {sponsor.name}
                                        </span>
                                    )}
                                </a>
                            ))}
                        </div>
                    </section>
                )}
                <p
                    className={`${sponsors.length > 0 ? "mt-7" : ""} text-center text-xs text-slate-500`}
                >
                    © {new Date().getFullYear()} KOALA — Wielkopolski konkurs grup szkolnych.
                </p>
            </div>
        </footer>
    );
};

export default PublicFooter;
