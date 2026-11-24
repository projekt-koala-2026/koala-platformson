import type { SponsorType } from "../types/models";

export const sponsorTypeOptions: { value: SponsorType; label: string }[] = [
    { value: "organizer", label: "Organizator" },
    { value: "sponsor", label: "Sponsor" },
    { value: "patron", label: "Patron" },
];

export const sponsorGroups: { type: SponsorType; title: string }[] = [
    { type: "organizer", title: "Organizatorzy" },
    { type: "sponsor", title: "Sponsorzy" },
    { type: "patron", title: "Patroni" },
];
