import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import RecordDates from "../../components/RecordDates";
import type { ApiAdminTeam, ApiEdition, ApiSchool, ApiTeam } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";
import { isRodoModerator } from "../../utils/authService";

const emailPreviewLimit = 48;
const teamPreviewLimit = 3;

const AdminTeamsScreen = ({ view = "teams" }: { view?: "teams" | "guardians" }) => {
    const navigate = useNavigate();
    const canViewMembers = useMemo(() => isRodoModerator(), []);
    const [teams, setTeams] = useState<(ApiAdminTeam | ApiTeam)[]>([]);
    const [schoolNames, setSchoolNames] = useState<Record<string, string>>({});
    const [editionNames, setEditionNames] = useState<Record<string, string>>({});
    const [query, setQuery] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    const [lookupError, setLookupError] = useState(false);
    const [revision, setRevision] = useState(0);
    const [selected, setSelected] = useState<ApiTeam | null>(null);
    const [expandedGuardians, setExpandedGuardians] = useState<Set<string>>(() => new Set());
    const guardianView = view === "guardians" && canViewMembers;

    useEffect(() => {
        let active = true;
        setLoading(true);
        void Promise.all([
            apiListRequestResult<ApiAdminTeam | ApiTeam>(
                `${canViewMembers ? apiEndpoints.teams : apiEndpoints.adminTeams}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiSchool>(
                `${apiEndpoints.schools}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}`,
                null,
                "GET",
                navigate
            ),
        ]).then(([teamsResult, schoolsResult, editionsResult]) => {
            if (!active) return;
            setTeams(teamsResult.data ?? []);
            setHasError(teamsResult.data === null);
            setSchoolNames(
                Object.fromEntries(
                    (schoolsResult.data ?? []).map((school) => [school.id, school.nameFull])
                )
            );
            setEditionNames(
                Object.fromEntries(
                    (editionsResult.data ?? []).map((edition) => [edition.id, edition.name])
                )
            );
            setLookupError(schoolsResult.data === null || editionsResult.data === null);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate, canViewMembers, revision]);

    const visibleTeams = useMemo(() => {
        const normalized = query.trim().toLocaleLowerCase("pl-PL");
        if (!normalized) return teams;
        return teams.filter((team) =>
            [
                team.name,
                schoolNames[team.schoolId],
                editionNames[team.editionId],
                team.schoolId,
                team.editionId,
                ...("teamGuardian" in team
                    ? [
                          team.teamGuardian?.nameFirst,
                          team.teamGuardian?.nameLast,
                          team.teamGuardian?.email,
                          team.teamCaptain?.nameFirst,
                          team.teamCaptain?.nameLast,
                          team.teamCaptain?.email,
                      ]
                    : []),
                ...("teamMembers" in team
                    ? team.teamMembers.map((member) => member.teamMemberId)
                    : []),
            ]
                .join(" ")
                .toLocaleLowerCase("pl-PL")
                .includes(normalized)
        );
    }, [editionNames, query, schoolNames, teams]);

    const guardians = useMemo(() => {
        const grouped = new Map<
            string,
            { person: NonNullable<ApiTeam["teamGuardian"]>; teams: ApiTeam[] }
        >();
        for (const team of visibleTeams) {
            if (!("teamGuardian" in team) || !team.teamGuardian) continue;
            const existing = grouped.get(team.teamGuardian.id);
            if (existing) existing.teams.push(team);
            else grouped.set(team.teamGuardian.id, { person: team.teamGuardian, teams: [team] });
        }
        return [...grouped.values()].sort((a, b) =>
            a.person.nameLast.localeCompare(b.person.nameLast, "pl")
        );
    }, [visibleTeams]);

    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <header className="border-b border-line-subtle pb-6">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Administracja
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">
                        {guardianView ? "Opiekunowie drużyn" : "Zarejestrowane drużyny"}
                    </h1>
                    <p className="mt-3 text-ink-secondary">
                        {guardianView
                            ? "Lista opiekunów przypisanych do drużyn. Konta bez drużyny nie są dostępne w tym widoku."
                            : canViewMembers
                              ? "Przeglądaj dane kapitanów, opiekunów i członków drużyn. Dokumenty rozpatrzysz w sekcji RODO."
                              : "Przeglądaj nazwy drużyn, szkoły i edycje. Dane osobowe i dokumenty RODO są dostępne dla moderatora RODO."}
                    </p>
                </header>

                <label className="mt-6 block max-w-xl text-sm font-medium text-ink">
                    {canViewMembers
                        ? "Szukaj po osobie, e-mailu, drużynie, szkole lub edycji"
                        : "Szukaj po nazwie drużyny, szkole lub edycji"}
                    <input
                        type="search"
                        value={query}
                        onChange={(event) => setQuery(event.target.value)}
                        className="mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 outline-none focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                    />
                </label>
                <div className="mt-4">
                    <Button
                        text="Odśwież listę"
                        disabled={loading}
                        onClick={() => setRevision((value) => value + 1)}
                    />
                </div>

                {hasError && (
                    <p
                        role="alert"
                        className="mt-6 rounded-xl border p-4 text-sm app-status-danger"
                    >
                        Nie udało się pobrać listy drużyn.
                    </p>
                )}
                {lookupError && (
                    <p
                        role="status"
                        className="mt-4 rounded-xl border p-4 text-sm app-status-warning"
                    >
                        Nie udało się pobrać nazw wszystkich szkół lub edycji.
                    </p>
                )}

                {loading ? (
                    <div className="mt-6 rounded-2xl border border-line bg-surface p-12 text-center text-sm text-ink-muted">
                        Ładowanie listy drużyn…
                    </div>
                ) : (guardianView ? guardians.length === 0 : visibleTeams.length === 0) ? (
                    <div className="mt-6 rounded-2xl border border-dashed border-line-strong bg-surface p-12 text-center text-sm text-ink-muted">
                        {guardianView
                            ? "Brak opiekunów spełniających kryteria."
                            : "Brak drużyn spełniających kryteria."}
                    </div>
                ) : guardianView ? (
                    <div className="mt-6 overflow-x-auto rounded-2xl border border-line bg-surface">
                        <table className="w-full min-w-[900px] table-fixed text-left text-sm">
                            <colgroup>
                                <col className="w-[28%]" />
                                <col className="w-[32%]" />
                                <col className="w-[40%]" />
                            </colgroup>
                            <thead className="bg-surface-muted text-ink">
                                <tr>
                                    <th className="px-4 py-3">Opiekun</th>
                                    <th className="px-4 py-3">E-mail</th>
                                    <th className="px-4 py-3">Drużyny</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-line-subtle">
                                {guardians.map(({ person, teams: assigned }) => (
                                    <tr key={person.id} className="[&>td]:align-top">
                                        <td className="px-4 py-4 font-semibold text-ink-heading">
                                            {person.nameFirst} {person.nameLast}
                                        </td>
                                        <td className="px-4 py-4 text-ink-secondary">
                                            <span
                                                className="block max-w-[28rem] truncate"
                                                title={person.email}
                                                aria-label={person.email}
                                            >
                                                {person.email.length > emailPreviewLimit
                                                    ? `${person.email.slice(0, emailPreviewLimit - 3)}...`
                                                    : person.email}
                                            </span>
                                        </td>
                                        <td className="px-4 py-4">
                                            <div
                                                id={`guardian-teams-${person.id}`}
                                                className="flex flex-wrap items-center gap-2"
                                            >
                                                {(expandedGuardians.has(person.id)
                                                    ? assigned
                                                    : assigned.slice(0, teamPreviewLimit)
                                                ).map((team) => (
                                                    <Button
                                                        key={team.id}
                                                        text={
                                                            <span className="min-w-0 break-words [overflow-wrap:anywhere]">
                                                                {team.name}
                                                            </span>
                                                        }
                                                        className="max-w-full"
                                                        onClick={() => setSelected(team)}
                                                    />
                                                ))}
                                                {assigned.length > teamPreviewLimit &&
                                                    !expandedGuardians.has(person.id) && (
                                                        <button
                                                            type="button"
                                                            aria-expanded={false}
                                                            aria-controls={`guardian-teams-${person.id}`}
                                                            aria-label={`Pokaż pozostałe ${assigned.length - teamPreviewLimit} drużyn opiekuna ${person.nameFirst} ${person.nameLast}`}
                                                            className="shrink-0 rounded-md bg-surface-muted px-2 py-1 text-sm text-ink-secondary transition hover:bg-brand-soft hover:text-brand-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
                                                            onClick={() =>
                                                                setExpandedGuardians((current) => {
                                                                    const next = new Set(current);
                                                                    next.add(person.id);
                                                                    return next;
                                                                })
                                                            }
                                                        >
                                                            {`+${assigned.length - teamPreviewLimit}`}
                                                        </button>
                                                    )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                ) : (
                    <div className="mt-6 overflow-hidden rounded-2xl border border-line bg-surface shadow-sm">
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[900px] text-left text-sm">
                                <thead className="bg-surface-muted text-ink">
                                    <tr>
                                        <th className="px-4 py-3">Nazwa</th>
                                        <th className="px-4 py-3">Poziom</th>
                                        {canViewMembers && (
                                            <th className="px-4 py-3">Stan nazwy</th>
                                        )}
                                        {canViewMembers && (
                                            <th className="px-4 py-3">Członkowie</th>
                                        )}
                                        <th className="px-4 py-3">Szkoła</th>
                                        <th className="px-4 py-3">Edycja</th>
                                        <th className="px-4 py-3">Daty</th>
                                        {canViewMembers && <th className="px-4 py-3">Szczegóły</th>}
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-line-subtle">
                                    {visibleTeams.map((team) => (
                                        <tr key={team.id} className="hover:bg-canvas">
                                            <td className="px-4 py-4 font-semibold text-ink-heading">
                                                {team.name}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {team.tier === "TIER_2" ? "Średnia" : "Podstawowa"}
                                            </td>
                                            {canViewMembers && (
                                                <td className="px-4 py-4">
                                                    <span
                                                        className={`inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${team.isCensored ? "bg-warning-muted text-warning-text" : "bg-brand-muted text-brand-text-strong"}`}
                                                    >
                                                        {team.isCensored
                                                            ? "do weryfikacji"
                                                            : "zaakceptowana"}
                                                    </span>
                                                </td>
                                            )}
                                            {canViewMembers && (
                                                <td className="px-4 py-4 text-ink-secondary">
                                                    {"teamMembers" in team
                                                        ? team.teamMembers.length +
                                                          Number(Boolean(team.teamCaptain))
                                                        : "—"}
                                                </td>
                                            )}
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {schoolNames[team.schoolId] ?? "Nieznana szkoła"}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                {editionNames[team.editionId] ?? "Nieznana edycja"}
                                            </td>
                                            <td className="px-4 py-4 text-ink-secondary">
                                                <RecordDates
                                                    createdAt={team.createdAt}
                                                    updatedAt={team.updatedAt}
                                                />
                                            </td>
                                            {canViewMembers && (
                                                <td className="px-4 py-4">
                                                    {"teamMembers" in team && (
                                                        <Button
                                                            text="Szczegóły"
                                                            onClick={() => setSelected(team)}
                                                        />
                                                    )}
                                                </td>
                                            )}
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </main>
            <Modal
                isOpen={selected !== null}
                onClose={() => setSelected(null)}
                title="Szczegóły drużyny"
                maxWidth="lg"
            >
                {selected && (
                    <div className="space-y-5 text-ink-heading">
                        <div>
                            <h2 className="break-words text-xl font-semibold">{selected.name}</h2>
                            <p className="mt-1 text-sm text-ink-secondary">
                                {schoolNames[selected.schoolId] ?? "Nieznana szkoła"} ·{" "}
                                {editionNames[selected.editionId] ?? "Nieznana edycja"}
                            </p>
                        </div>
                        <div className="grid gap-4 sm:grid-cols-2">
                            {[
                                { title: "Kapitan", person: selected.teamCaptain },
                                { title: "Opiekun", person: selected.teamGuardian },
                            ].map(({ title, person }) => (
                                <section
                                    key={title}
                                    className="min-w-0 rounded-xl border border-line p-4"
                                >
                                    <h3 className="font-semibold">{title}</h3>
                                    {person ? (
                                        <>
                                            <p className="mt-2 break-words">
                                                {person.nameFirst} {person.nameLast}
                                            </p>
                                            <p className="break-all text-sm text-ink-secondary">
                                                {person.email}
                                            </p>
                                        </>
                                    ) : (
                                        <p className="mt-2 text-ink-secondary">Nie przypisano</p>
                                    )}
                                </section>
                            ))}
                        </div>
                        <section>
                            <h3 className="mb-2 font-semibold">Pozostali uczestnicy</h3>
                            {selected.teamMembers.length ? (
                                <ul className="space-y-2">
                                    {selected.teamMembers.map((member) => (
                                        <li
                                            key={member.teamMemberId}
                                            className="break-words rounded-xl bg-surface-muted p-3"
                                        >
                                            {member.nameFist} {member.nameLast}
                                        </li>
                                    ))}
                                </ul>
                            ) : (
                                <p className="text-sm text-ink-secondary">
                                    Brak dodatkowych uczestników.
                                </p>
                            )}
                        </section>
                        <div className="flex flex-wrap justify-end gap-3">
                            <Button
                                text="Przejdź do zgód tej drużyny"
                                onClick={() =>
                                    navigate(
                                        `/admin/rodos?team=${encodeURIComponent(selected.name)}`
                                    )
                                }
                            />
                            <Button text="Zamknij" onClick={() => setSelected(null)} />
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    );
};

export default AdminTeamsScreen;
