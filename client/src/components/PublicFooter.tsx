import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiSponsor, Sponsor } from "../types/models";
import { adaptSponsor } from "../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequest, resolveApiAssetUrl } from "../utils/apiFetcher";

const PublicFooter = () => {
    const navigate = useNavigate();
    const [sponsors, setSponsors] = useState<Sponsor[]>([]);

    useEffect(() => {
        let active = true;
        void apiListRequest<ApiSponsor>(
            `${apiEndpoints.sponsors}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then((data) => {
            if (active && data)
                setSponsors(data.filter((sponsor) => sponsor.isVisible).map(adaptSponsor));
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    return (
        <footer className="mt-auto w-full border-t border-line bg-surface">
            <div className="mx-auto max-w-6xl px-4 py-4 sm:px-6">
                {sponsors.length > 0 && (
                    <section aria-labelledby="sponsors-heading">
                        <h2
                            id="sponsors-heading"
                            className="text-center text-xs font-semibold uppercase tracking-wide text-ink-muted"
                        >
                            Nasi sponsorzy
                        </h2>
                        <div
                            role="region"
                            aria-labelledby="sponsors-heading"
                            tabIndex={sponsors.length > 8 ? 0 : undefined}
                            className="mx-auto mt-3 grid max-h-[6.25rem] max-w-4xl grid-cols-4 gap-x-2 gap-y-1 overflow-y-auto overscroll-contain rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus sm:grid-cols-5 lg:grid-cols-6"
                        >
                            {sponsors.map((sponsor) => (
                                <a
                                    key={sponsor.id}
                                    href={sponsor.websiteUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    title={sponsor.description || sponsor.name}
                                    className="flex h-12 min-w-0 items-center justify-center overflow-hidden rounded-lg p-1.5 transition hover:bg-brand-soft focus:outline-none focus:ring-2 focus:ring-inset focus:ring-brand-focus"
                                >
                                    {sponsor.logoUrl ? (
                                        <img
                                            src={resolveApiAssetUrl(sponsor.logoUrl)}
                                            alt={sponsor.name}
                                            loading="lazy"
                                            className="block h-full w-full min-w-0 object-contain grayscale transition hover:grayscale-0"
                                        />
                                    ) : (
                                        <span className="line-clamp-2 break-words text-center text-xs font-medium leading-4 text-ink [overflow-wrap:anywhere]">
                                            {sponsor.name}
                                        </span>
                                    )}
                                </a>
                            ))}
                        </div>
                        {sponsors.length > 8 && (
                            <p
                                className={`mt-1 text-center text-xs text-ink-muted ${
                                    sponsors.length <= 10
                                        ? "sm:hidden"
                                        : sponsors.length <= 12
                                          ? "lg:hidden"
                                          : ""
                                }`}
                            >
                                Przewiń listę, aby zobaczyć pozostałych sponsorów.
                            </p>
                        )}
                    </section>
                )}
                <p
                    className={`${sponsors.length > 0 ? "mt-3" : ""} text-center text-xs text-ink-muted`}
                >
                    © {new Date().getFullYear()} KOALA — Wielkopolski konkurs grup szkolnych.
                </p>
            </div>
        </footer>
    );
};

export default PublicFooter;
