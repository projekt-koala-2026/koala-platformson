// Read-only comparison of printed cards, .env, current fixtures and support files.
import assert from "node:assert/strict";
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { parseEnv } from "node:util";
import {
    base,
    catalog,
    emailVariables,
    folder,
    getSessionCookie,
    json,
    load,
    login,
    sql,
    validateEmailConfiguration,
} from "./prepare.mjs";

validateEmailConfiguration();

const m = load(),
    checks = [],
    issues = [];
const check = (id, label, fn) => {
    try {
        fn();
        checks.push({ id, label, status: "PASS" });
    } catch (error) {
        issues.push({ id, label });
        checks.push({ id, label, status: "FAIL" });
    }
};
const rows = (table) =>
    JSON.parse(sql(`SELECT COALESCE(json_agg(r),'[]') FROM (SELECT * FROM ${table}) r;`));
const tables = Object.fromEntries(
    [
        "account.users",
        "account.roles",
        "account.user_roles",
        "account.teams",
        "account.team_members",
        "account.rodos",
        "core.editions",
        "core.subeditions",
        "core.tasks",
        "core.task_answers",
        "core.task_submissions",
        "core.rankings",
        "core.schools",
        "cms.posts",
        "cms.sponsors",
        "cms.koalicjants",
        "cms.static_pages",
        "cms.public_files",
    ].map((t) => [t, rows(t)])
);
const byId = (table, id) => {
    const row = tables[table].find((r) => r.id === id);
    assert(row);
    return row;
};
const equal = (a, b) => assert.deepEqual(a, b);
const jsonValue = (value) => (typeof value === "string" ? JSON.parse(value) : value);
const sameDate = (a, b) => equal(Date.parse(a), Date.parse(b));
const actors = new Map();
const collectActors = (value) => {
    if (!value || typeof value !== "object") return;
    if (value.id && value.key && value.email && value.name) actors.set(value.id, value);
    for (const child of Object.values(value)) collectActors(child);
};
collectActors(m);
for (const actor of actors.values())
    check(`actor/${actor.key}`, "Osoba wskazana na kartkach ma właściwe dane w bazie", () => {
        const row = byId("account.users", actor.id);
        equal(row.email, actor.email);
        equal(`${row.name_first} ${row.name_last}`, actor.name);
    });
const fixtureRoles = [
    ["admin", "administrator", m.admins],
    ["editor", "edytor", m.editors],
    ["reviewer", "recenzent", m.reviewers],
];
const emailKeys = Object.fromEntries(
    Object.entries(emailVariables).map(([key, variable]) => [key, variable.replace(/_EMAIL$/, "")])
);
const env = existsSync(join(folder, ".env"))
    ? parseEnv(readFileSync(join(folder, ".env"), "utf8"))
    : {};
const printedSection = (text, id) => {
    const start = text.indexOf(`- **${id}. `);
    if (start < 0) return null;
    const next = text.indexOf("\n- **", start + 3);
    const section = text.slice(start, next < 0 ? undefined : next);
    return [section, section.slice(section.indexOf("\n") + 1)];
};

for (const [role, fileName, variants] of fixtureRoles)
    for (const v of variants) {
        const key = `${role}/${v.key}`;
        check(key, "Konto, rola i dane identyfikacyjne zgodne z manifestem", () => {
            const u = byId("account.users", v.user.id);
            equal(u.email, v.user.email);
            equal(`${u.name_first} ${u.name_last}`, v.user.name);
            assert(u.verified);
            const roles = tables["account.user_roles"]
                .filter((r) => r.user_id === u.id)
                .map((r) => byId("account.roles", r.role_id).name);
            assert(roles.includes(v.user.role));
            const prefix = emailKeys[v.user.key];
            if (prefix)
                for (const [suffix, field] of [
                    ["EMAIL", "email"],
                    ["FIRST_NAME", "name_first"],
                    ["LAST_NAME", "name_last"],
                ])
                    if (env[`${prefix}_${suffix}`]?.trim())
                        equal(u[field], env[`${prefix}_${suffix}`].trim());
        });
        const doc = readFileSync(
            join(
                folder,
                v.key === "rezerwa"
                    ? "dane-zapasowe.md"
                    : `${fileName}-${v.key === "lokalna" ? "stacjonarnie" : "zdalnie"}.md`
            ),
            "utf8"
        );
        const text =
            v.key === "rezerwa" ? doc.split(`## ${fileName}\n`)[1]?.split("\n## ")[0] : doc;
        for (const task of catalog.filter((c) => c.role === role))
            check(
                `${key}/${task.id}`,
                "Dokładne polecenie i dane na kartce zgodne z manifestem",
                () => {
                    assert(text);
                    const section = printedSection(text, task.id);
                    assert(section);
                    assert(section[0].startsWith(`- **${task.id}. ${task.instruction}**`));
                    const actual = [...section[1].matchAll(/^  - \*\*([^\n]+?):\*\* (.*)$/gm)].map(
                        (r) => [r[1], r[2].replace(/\r$/, "")]
                    );
                    equal(actual, Object.entries(v.cards[task.id]));
                }
            );
    }

const checkEdition = (e, state, id) =>
    check(id, "Edycja i jej stan/daty zgodne z kartą", () => {
        const row = byId("core.editions", e.id);
        equal(row.name, e.name);
        equal(row.state, state);
        sameDate(row.team_register_open, e.teamRegisterOpen);
        sameDate(row.team_register_close, e.teamRegisterClose);
    });
const checkStage = (s, id) =>
    check(id, "Etap: nazwa, stan i terminy zgodne z przygotowaniem", () => {
        const row = byId("core.subeditions", s.id);
        equal(row.name, s.name);
        equal(row.state, s.state);
        sameDate(row.answer_open, s.answerOpen);
        sameDate(row.answer_close, s.answerClose);
    });
const checkTask = (t, id) =>
    check(id, "Zadanie, warianty i klucz odpowiedzi zgodne z przygotowaniem", () => {
        const row = byId("core.tasks", t.id);
        equal(row.name, t.name);
        equal(row.tier, t.tier);
        equal(row.content_json, jsonValue(t.contentJson));
        equal(
            tables["core.task_answers"].filter((r) => r.task_item_id === t.id).map((r) => r.answer),
            [t.correct]
        );
    });
const checkSubmissions = (a, f, mode) =>
    check(`${a.key}/${mode}`, "Odpowiedzi i oceny mają stan wymagany przez scenariusz", () => {
        const actual = tables["core.task_submissions"].filter((x) => x.task_id === f.task.id);
        equal(actual.length, 2);
        for (const [i, team] of a.teams.entries()) {
            const r = actual.find((r) => r.team_id === team.id);
            assert(r);
            equal(r.submission, i === 0 ? "5" : "6");
            equal(r.auto_review_state, mode === "auto" ? null : i === 0);
            const assigned = !["auto", "assign"].includes(mode);
            equal(r.reviewer_1_id, assigned ? a.pair[0].id : null);
            equal(r.reviewer_2_id, assigned ? a.pair[1].id : null);
            let scores = [null, null];
            if (mode === "replace") scores = i === 0 ? [true, true] : [false, null];
            if (mode === "blocked") scores = i === 0 ? [true, false] : [null, null];
            if (["close", "rankGenerate", "rankPublish"].includes(mode))
                scores = [i === 0, i === 0];
            equal([r.review_1_state, r.review_2_state], scores);
        }
    });
checkEdition(m.baseline, "ACTIVE", "baseline");
const assets = [];
for (const a of m.admins) {
    for (const name of ["editEdition", "startEdition", "workEdition"])
        checkEdition(a[name], "INACTIVE", `${a.key}/${name}`);
    for (const name of ["editStage", "activateStage", "crudStage"])
        checkStage(a[name], `${a.key}/${name}`);
    for (const name of ["editTask", "deleteTask"]) checkTask(a[name], `${a.key}/${name}`);
    for (const name of [
        "auto",
        "assign",
        "replace",
        "blocked",
        "close",
        "rankGenerate",
        "rankPublish",
    ]) {
        checkStage(a[name].stage, `${a.key}/${name}`);
        checkTask(a[name].task, `${a.key}/${name}`);
        checkSubmissions(a, a[name], name);
    }
    for (const [i, team] of a.teams.entries())
        check(`${a.key}/team-${i}`, "Drużyna i uczestnik wskazani na kartkach istnieją", () => {
            const row = byId("account.teams", team.id);
            equal(row.name, team.name);
            equal(row.edition_id, m.baseline.id);
            equal(row.captain_id, team.captain.id);
            const member = byId("account.team_members", (i === 0 ? a.rodoView : a.rodoDecision).id);
            equal(member.team_id, team.id);
            equal(
                `${member.name_first} ${member.name_last}`,
                (i === 0 ? a.rodoView : a.rodoDecision).name
            );
            equal(member.rodo_state, i === 0 ? "RODO_ACCEPTED" : "RODO_UNVERIFIED");
            assert(member.rodo_id);
            byId("account.rodos", member.rodo_id);
        });
    check(
        `${a.key}/rankings`,
        "Ranking do generowania nie istnieje, ranking do publikacji jest gotowy i ukryty",
        () => {
            assert(
                !tables["core.rankings"].some((r) => r.subedition_id === a.rankGenerate.stage.id)
            );
            assert(tables["core.rankings"].some((r) => r.subedition_id === a.rankPublish.stage.id));
            equal(byId("core.subeditions", a.rankPublish.stage.id).ranking_published, false);
        }
    );
    check(`${a.key}/school`, "Szkoła do edycji istnieje z pierwotnymi danymi", () => {
        const s = byId("core.schools", a.school.id);
        equal(s.name_full, a.cards.A29["Szkoła"]);
        equal(s.rspo, a.cards.A29["Numer RSPO"]);
        equal(s.road, a.school.road);
        equal(s.building, a.school.building);
    });
    check(`${a.key}/new-objects`, "Dane do tworzenia nie są już zajęte w bazie", () => {
        assert(!tables["core.schools"].some((s) => s.rspo === a.cards.A30["Numer RSPO"]));
        assert(!tables["core.editions"].some((e) => e.name === a.cards.A08["Nazwa nowej edycji"]));
        assert(!tables["account.users"].some((u) => u.email === a.inviteEmail));
        for (const u of [a.roleTarget, a.deleteTarget]) byId("account.users", u.id);
    });
    const f = a.cards.A31.Plik;
    assets.push({ participant: `administrator-${a.key}`, file: f });
    check(
        `${a.key}/CSV`,
        "Plik CSV wskazany na kartce ma właściwe szkoły i niezajęte numery RSPO",
        () => {
            equal(f, a.csv);
            const text = readFileSync(join(folder, f), "utf8")
                .replace(/^\uFEFF/, "")
                .trim();
            const lines = text.split(/\r?\n/);
            equal(
                lines[0],
                "Numer RSPO;Nazwa;Województwo;Miejscowość;Ulica;Numer budynku;Typ;E-mail"
            );
            equal(lines.length, 3);
            for (const [i, line] of lines.slice(1).entries()) {
                const row = line.split(";");
                equal(row.length, 8);
                equal(row[0], String(991000 + (m.admins.indexOf(a) + 1) * 10 + i + 1));
                equal(row[1], `Szkoła Próbna ${i + 1} — ${a.label}`);
                equal(row[6], "Szkoła podstawowa");
                equal(row[7], `import.${a.key}.${i + 1}@koala.test`);
                assert(!tables["core.schools"].some((s) => s.rspo === row[0]));
            }
        }
    );
}
for (const r of m.reviewers) {
    check(
        `reviewer/${r.key}/visible-identifiers`,
        "Kartka wskazuje identyfikatory widoczne w panelu recenzenta",
        () => {
            for (const id of ["R02", "R03", "R04", "R05", "R06"]) {
                assert(!Object.hasOwn(r.cards[id], "Drużyna"));
                assert(r.cards[id].Etap && r.cards[id].Zadanie);
            }
        }
    );
    checkStage(r.stage, `reviewer/${r.key}`);
    for (const t of [...r.tasks, r.conflict]) {
        checkTask(t, `reviewer/${r.key}/${t.id}`);
        check(
            `reviewer/${r.key}/${t.id}`,
            "Recenzent ma własną odpowiedź w oczekiwanym stanie",
            () => {
                const rows = tables["core.task_submissions"].filter((x) => x.task_id === t.id);
                equal(rows.length, 1);
                const row = rows[0];
                equal(row.team_id, r.team.id);
                equal(row.submission, t.correct);
                equal(row.reviewer_1_id, r.user.id);
                equal(row.reviewer_2_id, r.pair[1].id);
                equal(row.review_1_state, t.id === r.conflict.id ? false : null);
                equal(row.review_2_state, true);
            }
        );
    }
}
for (const e of m.editors) {
    checkEdition(e.archive, "CLOSED", `editor/${e.key}/archive`);
    check(`editor/${e.key}/post`, "Wpis do edycji ma podany tytuł i godzinę 15:00", () => {
        const p = byId("cms.posts", e.post.id);
        equal(p.name, e.cards.E04["Nazwa robocza"]);
        equal(p.content_json.title_pl, e.cards.E04["Tytuł"]);
        assert(p.content_json.body_pl.includes("15:00"));
    });
    check(
        `editor/${e.key}/sponsor`,
        "Sponsor i profil koalicjanta istnieją i nie są jeszcze zmienione",
        () => {
            const p = byId("cms.sponsors", e.sponsor.id);
            equal(p.name, e.cards.E11.Sponsor);
            equal(p.is_visiable, e.sponsor.isVisible);
            equal(p.content_json, jsonValue(e.sponsor.contentJson));
            const profile = byId("cms.koalicjants", e.profile.id);
            equal(`${profile.name_first} ${profile.name_last}`, e.cards.E12.Osoba);
            equal(profile.email, e.cards.E12["E-mail"]);
            assert(!tables["cms.posts"].some((p) => p.name === e.cards.E03["Tytuł"]));
            assert(!Object.hasOwn(e.cards.E03, "Nazwa robocza"));
            assert(
                e.cards.E03["Tytuł"].length <= 50 && /^[\p{L}\s-]+$/u.test(e.cards.E03["Tytuł"])
            );
            assert(!Object.hasOwn(e.cards.E10, "Obraz"));
            for (const url of [p.content_json.sponsor_logo_image_url, e.cards.E10["Adres logo"]])
                assert(["http:", "https:"].includes(new URL(url).protocol));
            assert(!tables["cms.sponsors"].some((p) => p.name === e.cards.E10["Nazwa sponsora"]));
        }
    );
    for (const name of ["removePdf", "removeImage"])
        check(
            `editor/${e.key}/${name}`,
            "Plik do usunięcia jest w bibliotece pod właściwą nazwą",
            () => equal(byId("cms.public_files", e[name].id).name, e[name].name)
        );
    for (const [id, expected] of [
        ["E02", e.image],
        ["E07", e.pdf],
    ]) {
        assets.push({ participant: `edytor-${e.key}`, file: e.cards[id].Plik });
        check(`editor/${e.key}/${id}`, "Materiał do wysłania istnieje i ma właściwy format", () => {
            equal(e.cards[id].Plik, expected);
            const bytes = readFileSync(join(folder, expected));
            if (id === "E02") equal([...bytes.subarray(0, 8)], [137, 80, 78, 71, 13, 10, 26, 10]);
            else {
                assert(bytes.toString("ascii").startsWith("%PDF-1.4"));
                assert(bytes.toString("ascii").trimEnd().endsWith("%%EOF"));
            }
        });
    }
    check(
        `editor/${e.key}/static-pages`,
        "Sekcje regulaminu, historii i dokumentów odpowiadają kartkom",
        () => {
            const rules = byId("cms.static_pages", m.rules.id).content_json.body_pl;
            const history = byId("cms.static_pages", m.history.id).content_json.body_pl;
            const section = rules.split(`## ${e.cards.E05.Fragment}\n`)[1]?.split("\n## ")[0];
            assert(section?.includes("pięciu dni roboczych"));
            assert(history.includes(`## ${e.cards.E06.Fragment}`));
            assert(!history.includes(e.cards.E06["Informacja do dopisania"]));
            const metadata = JSON.parse(
                byId("cms.static_pages", m.problems.id).content_json.body_pl
            );
            assert(
                metadata[e.archive.id][e.cards.E08.Sekcja].some(
                    (f) => f.id === e.removePdf.id && f.title === e.cards.E08.Dokument
                )
            );
        }
    );
}

// Check real credentials without printing private addresses or names.
for (const [, , variants] of fixtureRoles)
    for (const v of variants.filter((v) => v.key !== "rezerwa")) {
        try {
            const user = await login(v.user.key, v.user.email, v.user.password);
            check(`${v.user.key}/login`, "Dane logowania z kartki działają", () =>
                equal(user.id, v.user.id)
            );
        } catch {
            issues.push({ id: v.user.key, label: "Dane logowania z kartki nie działają" });
        }
    }
await login("files-check", m.support.email, m.support.password);
for (const a of m.admins)
    for (const [i, team] of a.teams.entries()) {
        const member = i === 0 ? a.rodoView : a.rodoDecision;
        const response = await fetch(
            `${base}/api/koala/account/teams/${team.id}/rodos/${member.id}/file`,
            { headers: { Cookie: getSessionCookie("files-check") } }
        );
        check(`${a.key}/rodo-${i}/file`, "Dokument RODO dostępny do podglądu", () => {
            equal(response.status, 200);
            assert(response.headers.get("content-type")?.startsWith("application/pdf"));
        });
    }
for (const e of m.editors)
    for (const [name, ext] of [
        ["removePdf", "pdf"],
        ["removeImage", "png"],
    ]) {
        const response = await fetch(`${base}/api/koala/content/${e[name].id}.${ext}`);
        check(
            `editor/${e.key}/${name}/download`,
            "Plik wskazany do usunięcia jest dostępny na serwerze",
            () => equal(response.status, 200)
        );
    }
const logo = tables["cms.public_files"].find((f) => f.name === "Ilustracja pokazowa.png");
check("editor/shared-logo", "Obraz dla prezentacji sponsora jest już w bibliotece", () =>
    assert(logo)
);
if (logo) {
    const response = await fetch(`${base}/api/koala/content/${logo.id}.png`);
    check("editor/shared-logo/download", "Obraz sponsora jest dostępny na serwerze", () =>
        equal(response.status, 200)
    );
}
const report = { at: new Date().toISOString(), checks, issues, assets };
writeFileSync(join(folder, "fixture-consistency.local.json"), json(report) + "\n");
writeFileSync(
    join(folder, "fixture-consistency.local.md"),
    "# Zgodność danych i materiałów\n\n" +
        `Sprawdzenia: ${checks.length}. Niezgodności: ${issues.length}. Dane konkursowe nie były zmieniane; kontrola logowania tworzy jedynie sesje.\n\n` +
        "| Element | Kontrola | Wynik |\n|---|---|---|\n" +
        checks.map((c) => `| ${c.id} | ${c.label} | ${c.status} |`).join("\n") +
        "\n"
);
console.log(
    `Zgodność kartek/bazy/plików: ${checks.length} sprawdzeń; niezgodności: ${issues.length}.`
);
for (const issue of issues) console.log(`Niezgodność ${issue.id}: ${issue.label}`);
if (issues.length) process.exitCode = 1;
