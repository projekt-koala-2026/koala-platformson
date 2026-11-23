import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiSponsor, Sponsor } from "../types/models";
import { adaptSponsor } from "../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequest, resolveApiAssetUrl } from "../utils/apiFetcher";
import { sponsorGroups } from "../utils/sponsorTypes";

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
            <div className="mx-auto max-w-6xl px-4 py-2 sm:px-6">
                {sponsors.length > 0 && (
                    <div className="space-y-1">
                        {sponsorGroups.map(({ type, title }) => {
                            const items = sponsors.filter((sponsor) => sponsor.type === type);
                            if (!items.length) return null;
                            const prominent = type !== "sponsor";
                            const headingId = `footer-${type}-heading`;
                            return (
                                <section
                                    key={type}
                                    aria-labelledby={headingId}
                                    className={`min-w-0 rounded-lg border px-2 py-1 ${prominent ? "border-brand-border bg-brand-soft" : "border-transparent"}`}
                                >
                                    <h2
                                        id={headingId}
                                        className={`text-center text-[10px] font-semibold uppercase leading-3 tracking-wide ${prominent ? "text-brand-text-strong" : "text-ink-muted"}`}
                                    >
                                        {title}
                                    </h2>
                                    <div
                                        role="region"
                                        aria-label={`Logotypy: ${title}`}
                                        tabIndex={0}
                                        className="app-footer-logos mx-auto mt-1 flex gap-2 overflow-x-auto overflow-y-hidden overscroll-x-contain rounded-lg pb-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus"
                                    >
                                        {items.map((sponsor) => (
                                            <a
                                                key={sponsor.id}
                                                href={sponsor.websiteUrl}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                title={sponsor.description || sponsor.name}
                                                className={`flex shrink-0 items-center justify-center overflow-hidden rounded-md p-1 transition hover:bg-surface-muted focus:outline-none focus:ring-2 focus:ring-inset focus:ring-brand-focus ${prominent ? "h-9 w-24 bg-surface sm:w-28" : "h-7 w-20 sm:w-24"}`}
                                            >
                                                {sponsor.logoUrl ? (
                                                    <img
                                                        src={resolveApiAssetUrl(sponsor.logoUrl)}
                                                        alt={sponsor.name}
                                                        loading="lazy"
                                                        className={`block h-full w-full min-w-0 object-contain transition ${prominent ? "" : "grayscale hover:grayscale-0"}`}
                                                    />
                                                ) : (
                                                    <span className="line-clamp-2 break-words text-center text-xs font-medium leading-4 text-ink [overflow-wrap:anywhere]">
                                                        {sponsor.name}
                                                    </span>
                                                )}
                                            </a>
                                        ))}
                                    </div>
                                </section>
                            );
                        })}
                    </div>
                )}
                <p
                    className={`${sponsors.length > 0 ? "mt-1.5" : ""} text-center text-xs text-ink-muted`}
                >
                    © {new Date().getFullYear()} KOALA — Wielkopolski konkurs grup szkolnych.
                </p>
            </div>
        </footer>
    );
};

export default PublicFooter;
