// Local usability fixtures. No npm dependencies; Node 22+ and Docker CLI required.
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parseEnv } from "node:util";
import { deflateSync } from "node:zlib";

const folder = dirname(fileURLToPath(import.meta.url));
const manifestPath = join(folder, "seed.local.json");
const envPath = join(folder, ".env");
const localEnv = existsSync(envPath) ? parseEnv(readFileSync(envPath, "utf8")) : {};
const emailVariables = {
    "admin.lokalna": "ADMIN_LOCAL_EMAIL",
    "edytor.lokalna": "EDITOR_LOCAL_EMAIL",
    "recenzent.lokalna": "REVIEWER_LOCAL_EMAIL",
    "admin.zdalna": "ADMIN_REMOTE_EMAIL",
    "recenzent.zdalna": "REVIEWER_REMOTE_EMAIL",
};
const configuredEmail = (variable, configuration = localEnv) => {
    const value = configuration[variable]?.trim();
    assert(
        value && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value),
        `Uzupełnij poprawny adres w .env: ${variable}`
    );
    assert(
        !/@[^@]*\.(test|invalid|example)$/i.test(value) && !/@example\.(com|org|net)$/i.test(value),
        `Zastąp fikcyjny adres w .env: ${variable}`
    );
    return value;
};
const participantEmail = (key) =>
    emailVariables[key] ? configuredEmail(emailVariables[key]) : `${key}@koala.test`;
const invitationEmail = (key) =>
    configuredEmail(key === "zdalna" ? "INVITE_REMOTE_EMAIL" : "INVITE_LOCAL_EMAIL");
function validateEmailConfiguration(configuration = localEnv) {
    const variables = [
        ...Object.values(emailVariables),
        "INVITE_LOCAL_EMAIL",
        "INVITE_REMOTE_EMAIL",
    ];
    const invalid = variables.filter((variable) => {
        try {
            configuredEmail(variable, configuration);
            return false;
        } catch {
            return true;
        }
    });
    assert(
        !invalid.length,
        `Przed generowaniem kartek lub zmianą danych uzupełnij .env (bez fikcyjnych adresów): ${invalid.join(", ")}`
    );
    const accountAndInvites = variables.map((v) => configuredEmail(v, configuration).toLowerCase());
    assert.equal(
        new Set(accountAndInvites).size,
        accountAndInvites.length,
        "Adresy kont i zaproszeń muszą być różne, aby scenariusze nie kolidowały."
    );
}
const participantNames = (key, first, last) => {
    const prefix = emailVariables[key]?.replace(/_EMAIL$/, "");
    const nameFirst = prefix ? localEnv[`${prefix}_FIRST_NAME`]?.trim() || first : first;
    const nameLast = prefix ? localEnv[`${prefix}_LAST_NAME`]?.trim() || last : last;
    for (const [field, value] of [
        ["FIRST_NAME", nameFirst],
        ["LAST_NAME", nameLast],
    ])
        assert(
            value.length <= 50 && /^[\p{L}\s-]+$/u.test(value),
            `Invalid ${prefix ?? key}_${field} in .env`
        );
    return { nameFirst, nameLast };
};
const catalog = JSON.parse(readFileSync(join(folder, "scenarios.json"), "utf8"));
const isMain = process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url);
const args = isMain ? process.argv.slice(2) : [];
const option = (name) => {
    const i = args.indexOf(name);
    return i < 0 ? undefined : args[i + 1];
};
const allowedFlags = new Set([
    "--reset",
    "--docs-only",
    "--sync-emails",
    "--sync-assets",
    "--verify",
    "--exercise",
    "--prepare",
    "--variant",
    "--help",
]);
for (let i = 0; i < args.length; i++) {
    assert(allowedFlags.has(args[i]), `Unknown flag: ${args[i]}`);
    if (["--prepare", "--variant"].includes(args[i])) {
        assert(args[i + 1], "Missing flag value");
        i++;
    }
}
assert(
    args.filter((x) =>
        [
            "--reset",
            "--docs-only",
            "--sync-emails",
            "--sync-assets",
            "--verify",
            "--exercise",
            "--prepare",
            "--help",
        ].includes(x)
    ).length <= 1,
    "Choose one operation at a time."
);
assert(
    !args.includes("--variant") || args.includes("--prepare"),
    "--variant is only used with --prepare."
);
const base = (process.env.KOALA_API_URL ?? "http://localhost:8080").replace(/\/$/, "");
const publicBase = (localEnv.PUBLIC_API_URL || base).replace(/\/$/, "");
assert(
    ["http:", "https:"].includes(new URL(publicBase).protocol),
    "PUBLIC_API_URL must be HTTP(S)."
);
const publicAssetUrl = (path) => new URL(new URL(path, base).pathname, publicBase).href;
assert(["localhost", "127.0.0.1"].includes(new URL(base).hostname), "Only a local API is allowed.");
const containerNames = [
    "koala_postgres_container",
    "koala_redis_container",
    "koala_backend_container",
];
const volumes = ["koala_database_volume", "koala_backend_volume"];
const password = "KoalaProba2026!";
const sessions = new Map();
const getSessionCookie = (who) => sessions.get(who);
let requestObserver;
const observeRequests = (observer) => {
    requestObserver = observer;
};
const docker = (...a) =>
    execFileSync("docker", a, { encoding: "utf8", maxBuffer: 8 * 1024 * 1024 }).trim();
const sql = (statement) =>
    execFileSync(
        "docker",
        [
            "exec",
            "-i",
            containerNames[0],
            "psql",
            "-U",
            "user",
            "-d",
            "db",
            "-v",
            "ON_ERROR_STOP=1",
            "-tA",
            "-f",
            "-",
        ],
        { input: statement, encoding: "utf8" }
    ).trim();
const q = (v) => (v == null ? "NULL" : `'${String(v).replaceAll("'", "''")}'`);
const bool = (v) => (v == null ? "NULL" : v ? "true" : "false");
const json = (value) => JSON.stringify(value);
const save = (m) => writeFileSync(manifestPath, json(m) + "\n");
const load = () => JSON.parse(readFileSync(manifestPath, "utf8"));
const date = (value) =>
    new Intl.DateTimeFormat("pl-PL", {
        timeZone: "Europe/Warsaw",
        dateStyle: "short",
        timeStyle: "short",
    }).format(new Date(value));
const pause = (ms) => new Promise((r) => setTimeout(r, ms));

async function api(who, method, path, body, expected) {
    const form = body instanceof FormData;
    const response = await fetch(`${base}/api/koala${path}`, {
        method,
        headers: {
            ...(sessions.has(who) ? { Cookie: sessions.get(who) } : {}),
            ...(body && !form ? { "Content-Type": "application/json" } : {}),
        },
        body: body == null ? undefined : form ? body : json(body),
        signal: AbortSignal.timeout(30000),
    });
    const cookies = response.headers.getSetCookie();
    if (cookies.length) sessions.set(who, cookies.map((c) => c.split(";")[0]).join("; "));
    const text = await response.text();
    // Do not echo registration tokens or cookies if a request fails.
    const safePath = path.replace(/register\/[^/?]+/, "register/[token]");
    requestObserver?.({
        who,
        method,
        path: safePath,
        status: response.status,
        payload:
            body == null
                ? null
                : form
                  ? Object.fromEntries(
                        [...body].map(([key, value]) => [
                            key,
                            typeof value === "string"
                                ? value
                                : { file: value.name, size: value.size, type: value.type },
                        ])
                    )
                  : Object.fromEntries(
                        Object.entries(body).map(([key, value]) => [
                            key,
                            /password/i.test(key) ? "[redacted]" : value,
                        ])
                    ),
    });
    assert(
        expected ? response.status === expected : response.ok,
        `${method} ${safePath}: ${response.status} ${text}`
    );
    if (!text) return null;
    return JSON.parse(text).data;
}
const login = (who, email, pass = password) =>
    api(who, "POST", "/account/sessions", { email, password: pass });
const list = (who, path) =>
    api(who, "GET", path + (path.includes("?") ? "&" : "?") + "PageNumber=0&PageSize=100");

async function reset() {
    console.log("Reset wyłącznie dwóch wolumenów KOALA oraz cache Redis.");
    const configs = containerNames.map((name) => JSON.parse(docker("inspect", name))[0]);
    const mounts = configs.flatMap((c) => c.Mounts);
    for (const m of mounts)
        assert(
            m.Type === "volume" && volumes.includes(m.Name),
            `Unexpected mount: ${m.Destination}; reset aborted.`
        );
    assert(
        mounts.length === 2 && volumes.every((v) => mounts.some((m) => m.Name === v)),
        "Unexpected KOALA volume configuration."
    );
    for (const c of configs)
        assert(c.NetworkSettings.Networks.koala_network, "Unexpected network; reset aborted.");
    docker("rm", "-f", containerNames[2], containerNames[0], containerNames[1]);
    for (const v of volumes) {
        docker("volume", "rm", v);
        docker("volume", "create", v);
    }
    for (const c of configs) {
        const a = [
            "run",
            "-d",
            "--name",
            c.Name.slice(1),
            "--network",
            "koala_network",
            "--restart",
            c.HostConfig.RestartPolicy.Name || "no",
        ];
        if (c.HostConfig.Memory) a.push("--memory", String(c.HostConfig.Memory));
        for (const env of c.Config.Env) a.push("--env", env);
        if (c.Name === "/koala_backend_container") {
            if (!c.Config.Env.some((e) => e.startsWith("DefaultAdmin__Email=")))
                a.push("--env", "DefaultAdmin__Email=bootstrap@koala.test");
            if (!c.Config.Env.some((e) => e.startsWith("DefaultAdmin__Password=")))
                a.push("--env", `DefaultAdmin__Password=${password}`);
        }
        for (const mount of c.Mounts) a.push("-v", `${mount.Name}:${mount.Destination}`);
        for (const [port, bindings] of Object.entries(c.HostConfig.PortBindings ?? {}))
            for (const binding of bindings ?? [])
                a.push(
                    "-p",
                    `${binding.HostIp ? binding.HostIp + ":" : ""}${binding.HostPort}:${port}`
                );
        a.push(c.Image); // Reuse the exact image already running, not an unrelated tag.
        docker(...a);
    }
    for (let i = 0; i < 90; i++) {
        try {
            const r = await fetch(base + "/api/koala/core/editions");
            if (r.ok) break;
        } catch {
            /* startup */
        }
        assert(i < 89, "Backend did not start after resetting volumes; inspect its Docker logs.");
        if (i % 10 === 0) console.log("Oczekiwanie na PostgreSQL, migracje i backend…");
        await pause(1000);
    }
    const mail = await fetch("http://localhost:8025/api/v1/messages", { method: "DELETE" });
    assert(mail.ok, "Could not clear the local Mailpit inbox.");
}

async function bootstrap() {
    const settings = JSON.parse(
        readFileSync(resolve(folder, "../../..", "backend/koala/appsettings.json"), "utf8")
    );
    const info = JSON.parse(docker("inspect", containerNames[2]))[0];
    const env = Object.fromEntries(
        info.Config.Env.map((x) => {
            const at = x.indexOf("=");
            return [x.slice(0, at), x.slice(at + 1)];
        })
    );
    assert(
        ["koala_test_mailpit_container", "localhost", "127.0.0.1"].includes(
            env.EmailSettings__SmtpHost ?? settings.EmailSettings?.SmtpHost
        ),
        "Use local Mailpit before seeding invitations."
    );
    await login(
        "bootstrap",
        process.env.KOALA_SEED_ADMIN_EMAIL ??
            env.DefaultAdmin__Email ??
            settings.DefaultAdmin?.Email,
        process.env.KOALA_SEED_ADMIN_PASSWORD ??
            env.DefaultAdmin__Password ??
            settings.DefaultAdmin?.Password
    );
}

async function account(key, role, first, last, admin = "bootstrap") {
    const email = participantEmail(key);
    const { nameFirst, nameLast } = participantNames(key, first, last);
    await api(role.startsWith("TEAM_") ? "anonymous" : admin, "POST", "/account/users", {
        email,
        userRoles: [role],
    });
    const token = sql(
        `SELECT l.token FROM account.links l JOIN account.users u ON u.id=l.user_id WHERE u.email=${q(email)} AND l.type='REGISTRATION' AND l.state='ACTIVE';`
    );
    assert(token && !token.includes("\n"), "Registration token missing or ambiguous.");
    await api("anonymous", "POST", "/account/links/register/" + token, {
        nameFirst,
        nameLast,
        password,
        acceptedRodo: true,
        acceptedRules: true,
        declaredAtLeast13: false,
        declaredAtLeast18: true,
    });
    const user = await login(key, email);
    return {
        key,
        id: user.id,
        email,
        password,
        name: `${nameFirst} ${nameLast}`,
        nameFirst,
        nameLast,
        role,
    };
}

function png() {
    const width = 320,
        height = 180;
    const pixels = Buffer.alloc((width * 4 + 1) * height);
    for (let y = 0; y < height; y++)
        for (let x = 0; x < width; x++) {
            const i = y * (width * 4 + 1) + 1 + x * 4;
            const circle = (x - 160) ** 2 + (y - 90) ** 2 < 45 ** 2;
            pixels.set(circle ? [237, 247, 242, 255] : [16, 125, 104, 255], i);
        }
    const crc = (b) => {
        let n = 0xffffffff;
        for (const byte of b) {
            n ^= byte;
            for (let i = 0; i < 8; i++) n = (n >>> 1) ^ (n & 1 ? 0xedb88320 : 0);
        }
        return (n ^ 0xffffffff) >>> 0;
    };
    const chunk = (name, data) => {
        const tag = Buffer.from(name),
            size = Buffer.alloc(4),
            sum = Buffer.alloc(4);
        size.writeUInt32BE(data.length);
        sum.writeUInt32BE(crc(Buffer.concat([tag, data])));
        return Buffer.concat([size, tag, data, sum]);
    };
    const header = Buffer.alloc(13);
    header.writeUInt32BE(width);
    header.writeUInt32BE(height, 4);
    header[8] = 8;
    header[9] = 6;
    return Buffer.concat([
        Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
        chunk("IHDR", header),
        chunk("IDAT", deflateSync(pixels)),
        chunk("IEND", Buffer.alloc(0)),
    ]);
}
function pdf(label) {
    const clean = label
        .normalize("NFD")
        .replace(/[^\x20-\x7e]/g, "")
        .replace(/[()\\]/g, "");
    const stream = `BT /F1 16 Tf 50 760 Td (${clean}) Tj 0 -30 Td (Fikcyjny dokument do proby interfejsu KOALA.) Tj ET`;
    const objects = [
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        `<< /Length ${Buffer.byteLength(stream)} >>\nstream\n${stream}\nendstream`,
    ];
    let out = "%PDF-1.4\n";
    const offsets = [0];
    objects.forEach((value, i) => {
        offsets.push(Buffer.byteLength(out));
        out += `${i + 1} 0 obj\n${value}\nendobj\n`;
    });
    const start = Buffer.byteLength(out);
    out += `xref\n0 6\n0000000000 65535 f \n${offsets
        .slice(1)
        .map((v) => String(v).padStart(10, "0") + " 00000 n \n")
        .join("")}trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n${start}\n%%EOF\n`;
    return Buffer.from(out);
}
async function upload(name, bytes, type, who = "bootstrap") {
    const f = new FormData();
    f.set("Name", name);
    f.set("File", new Blob([bytes], { type }), name);
    return api(who, "POST", "/cms/public-files", f);
}
function asset(name, bytes) {
    mkdirSync(join(folder, "assets"), { recursive: true });
    writeFileSync(join(folder, "assets", name), bytes);
    return `assets/${name}`;
}

async function seed() {
    if (!args.includes("--reset"))
        assert(
            !existsSync(manifestPath),
            "Fixtures already exist. Use --verify / --docs-only, or --reset to replace all local data."
        );
    await bootstrap();
    assert(
        sql("SELECT count(*) FROM core.editions;") === "0",
        "Seeding expects an empty database. Use --reset."
    );
    const now = Date.now(),
        at = (d) => new Date(now + d * 86400000).toISOString();
    const m = {
        status: "seeding",
        generatedAt: new Date(now).toISOString(),
        base,
        baseline: null,
        admins: [],
        reviewers: [],
        editors: [],
        dates: {
            pastOpen: at(-14),
            pastClose: at(-2),
            futureOpen: at(30),
            futureClose: at(60),
            laterClose: at(67),
            registerOpen: at(-30),
            registerClose: at(120),
        },
    };
    save(m);
    const edition = (name) =>
        api("bootstrap", "POST", "/core/editions", {
            name,
            teamRegisterOpen: m.dates.registerOpen,
            teamRegisterClose: m.dates.registerClose,
        });
    m.baseline = await edition("KOALA — edycja pokazowa");
    await api("bootstrap", "PUT", `/core/editions/${m.baseline.id}/activate`);
    const support = await account("moderator", "ORGANIZATION_ADMIN", "Alex", "Moderator");
    m.support = support;
    const defaultPostImage = await upload("Ilustracja pokazowa.png", png(), "image/png");
    const imageUrl = `/api/koala/content/${defaultPostImage.id}.png`;
    const school = (name, rspo) =>
        api("bootstrap", "POST", "/core/schools", {
            nameFull: name,
            nameShort: "Szkoła pokazowa",
            state: "wielkopolskie",
            city: "Poznań",
            road: "Próbna",
            building: "12",
            rspo: String(rspo),
            type: "Szkoła podstawowa",
            email: `szkola.${rspo}@koala.test`,
        });
    const baseSchool = await school("Szkoła Podstawowa imienia Koali", 990001);
    const stage = async (ed, name, past = false, active = false) => {
        const s = await api("bootstrap", "POST", `/core/editions/${ed.id}/subeditions`, {
            name,
            answerOpen: past ? m.dates.pastOpen : m.dates.futureOpen,
            answerClose: past ? m.dates.pastClose : m.dates.futureClose,
        });
        if (active)
            await api("bootstrap", "PUT", `/core/editions/${ed.id}/activate_subedition/${s.id}`);
        return { ...s, state: active ? "ACTIVE" : s.state };
    };
    const task = async (
        ed,
        s,
        name,
        type = "open",
        body = "Ile wynosi suma dwóch i trzech?",
        correct = "5"
    ) => {
        const content = { version: 1, body_pl: body, answer_type: type };
        if (["single_choice", "multiple_choice"].includes(type))
            content.options = [
                { id: "A", text: "Dwa" },
                { id: "B", text: "Pięć" },
                { id: "C", text: "Siedem" },
            ];
        const t = await api(
            "bootstrap",
            "POST",
            `/core/editions/${ed.id}/subeditions/${s.id}/tasks`,
            { name, tier: "TIER_1", contentJson: json(content) }
        );
        await api("bootstrap", "POST", `/core/tasks/${t.id}/corr`, { answer: correct });
        return { ...t, correct };
    };
    const submission = (t, team, text, pair = [], scores = [], auto = null) =>
        sql(
            `INSERT INTO core.task_submissions (team_id,task_id,reviewer_1_id,reviewer_2_id,auto_review_state,review_1_state,review_2_state,state,submission,created_at,updated_at,version) VALUES (${q(team.id)},${q(t.id)},${q(pair[0]?.id)},${q(pair[1]?.id)},${bool(auto)},${bool(scores[0])},${bool(scores[1])},'UN_CHECKED',${q(text)},now(),now(),1);`
        );
    const makeTeam = async (key, name, guardian, schoolId) => {
        const captain = await account(key, "TEAM_CAPTAIN", "Kamil", "Pokazowy");
        const t = await api(captain.key, "POST", "/account/teams", { name, schoolId });
        const code = await api(captain.key, "POST", `/account/teams/${t.id}/new-join-code`);
        await api(guardian.key, "POST", "/account/teams/join/" + code.joinCode);
        sql(`UPDATE account.teams SET name_accepted=true WHERE id=${q(t.id)};`);
        return { ...t, captain };
    };
    const rodo = async (team, guardian, first, last, accepted = false) => {
        await api(team.captain.key, "POST", `/account/teams/${team.id}/member`, {
            nameFirst: first,
            nameLast: last,
        });
        const memberId = sql(
            `SELECT id FROM account.team_members WHERE team_id=${q(team.id)} AND name_first=${q(first)} AND name_last=${q(last)};`
        );
        const f = new FormData();
        f.set(
            "File",
            new Blob([pdf(`Zgoda ${first} ${last}`)], { type: "application/pdf" }),
            "zgoda.pdf"
        );
        const r = await api(guardian.key, "POST", `/account/teams/${team.id}/rodos/${memberId}`, f);
        if (accepted)
            await api("bootstrap", "PUT", `/account/teams/${team.id}/rodos/${memberId}/state`, {
                state: "RODO_ACCEPTED",
                version: r.version,
                rodoVersion: r.rodoVersion,
            });
        return { id: memberId, name: `${first} ${last}`, accepted };
    };
    const variants = [
        ["lokalna", "Lokalna", "stacjonarne"],
        ["zdalna", "Zdalna", "zdalne"],
        ["rezerwa", "Rezerwowa", "zapasowe"],
    ];
    for (const [key, label, mode] of variants) {
        console.log(`Dane administratora i recenzenta: próba ${key}.`);
        const a = { key, label, mode, cards: {} };
        m.admins.push(a);
        a.user = await account(`admin.${key}`, "ORGANIZATION_ADMIN", "Alicja", label);
        a.roleTarget = await account(`redakcja.${key}`, "ORGANIZATION_EDITOR", "Eliza", label);
        a.deleteTarget = await account(`konto.${key}`, "ORGANIZATION_EDITOR", "Tomasz", label);
        a.inviteEmail = invitationEmail(key);
        a.editEdition = await edition(`KOALA — plan ${label.toLowerCase()}`);
        a.startEdition = await edition(`KOALA — inauguracja ${label.toLowerCase()}`);
        a.workEdition = await edition(`KOALA — warsztat ${label.toLowerCase()}`);
        a.editStage = await stage(a.workEdition, `Etap wiosenny — próba ${key}`);
        a.activateStage = await stage(a.workEdition, `Etap jesienny — próba ${key}`);
        a.crudStage = await stage(a.workEdition, `Łamigłówki — próba ${key}`);
        a.editTask = await task(
            a.workEdition,
            a.crudStage,
            `Kolorowe pudełka — ${key}`,
            "single_choice",
            "W pudełku są dwie czerwone i trzy niebieskie kule. Ile kul jest razem?",
            "B"
        );
        a.deleteTask = await task(a.workEdition, a.crudStage, `Szkic łamigłówki — ${key}`);
        a.school = await school(`Szkoła Podstawowa Leśna — próba ${key}`, 990010 + m.admins.length);
        const guardian = await account(`opiekun.${key}`, "TEAM_GUARDIAN", "Olga", label);
        a.teams = [
            await makeTeam(
                `kapitan.${key}.jantar`,
                `Jantar — próba ${key}`,
                guardian,
                baseSchool.id
            ),
            await makeTeam(
                `kapitan.${key}.bursztyn`,
                `Bursztyn — próba ${key}`,
                guardian,
                baseSchool.id
            ),
        ];
        a.rodoView = await rodo(a.teams[0], guardian, "Adam", label, true);
        a.rodoDecision = await rodo(a.teams[1], guardian, "Marta", label);
        const r1 = await account(`recenzent.${key}`, "ORGANIZATION_REVIEWER", "Anna", label);
        const r2 = await account(`konsultant.${key}`, "ORGANIZATION_REVIEWER", "Jan", label);
        const replacement = await account(
            `zastepstwo.${key}`,
            "ORGANIZATION_REVIEWER",
            "Ewa",
            label
        );
        a.pair = [r1, r2];
        a.replacement = replacement;
        const reviewFixture = async (name, config) => {
            const s = await stage(m.baseline, `${name} — próba ${key}`, true, true);
            const t = await task(m.baseline, s, `${name} — suma kulek`);
            for (let i = 0; i < 2; i++)
                submission(
                    t,
                    a.teams[i],
                    i === 0 ? "5" : "6",
                    config.pair ? a.pair : [],
                    config.scores?.[i] ?? [],
                    config.auto === "mixed" ? i === 0 : (config.auto ?? null)
                );
            return { stage: s, task: t };
        };
        a.auto = await reviewFixture("Etap Wrzosowy", {});
        a.assign = await reviewFixture("Etap Klonowy", { auto: "mixed" });
        a.replace = await reviewFixture("Etap Dębowy", {
            pair: true,
            scores: [
                [true, true],
                [false, null],
            ],
            auto: "mixed",
        });
        a.blocked = await reviewFixture("Etap Brzozowy", {
            pair: true,
            scores: [[true, false], []],
            auto: "mixed",
        });
        a.close = await reviewFixture("Etap Sosnowy", {
            pair: true,
            scores: [
                [true, true],
                [false, false],
            ],
            auto: "mixed",
        });
        a.rankGenerate = await reviewFixture("Etap Lipowy", {
            pair: true,
            scores: [
                [true, true],
                [false, false],
            ],
            auto: "mixed",
        });
        a.rankPublish = await reviewFixture("Etap Jesionowy", {
            pair: true,
            scores: [
                [true, true],
                [false, false],
            ],
            auto: "mixed",
        });
        await api(
            "bootstrap",
            "POST",
            `/core/ranking/generate/subedition/${a.rankPublish.stage.id}`
        );
        const review = {
            key,
            label,
            mode,
            user: r1,
            pair: a.pair,
            team: a.teams[0],
            tasks: [],
            cards: {},
        };
        m.reviewers.push(review);
        review.stage = await stage(m.baseline, `Próba recenzencka — ${key}`, true, true);
        for (const [type, name, body, correct] of [
            ["open", "Suma kamyków", "Oblicz sumę dwóch i trzech.", "5"],
            ["true_false", "Parzyste liczby", "Czy liczba cztery jest parzysta?", "true"],
            ["single_choice", "Liczba kulek", "Wybierz liczbę równą sumie dwóch i trzech.", "B"],
            [
                "multiple_choice",
                "Małe liczby",
                "Wybierz wszystkie liczby mniejsze od sześciu.",
                '["A","B"]',
            ],
        ]) {
            const t = await task(
                m.baseline,
                review.stage,
                `${name} — próba ${key}`,
                type,
                body,
                correct
            );
            submission(t, a.teams[0], correct, a.pair, [null, true], true);
            review.tasks.push(t);
        }
        review.conflict = await task(
            m.baseline,
            review.stage,
            `Połowa całości — próba ${key}`,
            "open",
            "Zapisz połowę całości jako ułamek.",
            "1/2"
        );
        submission(review.conflict, a.teams[0], "1/2", a.pair, [false, true], false);
        // New-school and CSV names/RSPOs are intentionally NOT seeded: they are participant inputs.
        const n = m.admins.length;
        a.csv = asset(
            `szkoly-${key}.csv`,
            "\ufeffNumer RSPO;Nazwa;Województwo;Miejscowość;Ulica;Numer budynku;Typ;E-mail\r\n" +
                [1, 2]
                    .map(
                        (i) =>
                            `${991000 + n * 10 + i};Szkoła Próbna ${i} — ${label};wielkopolskie;Poznań;Leśna;${i};Szkoła podstawowa;import.${key}.${i}@koala.test`
                    )
                    .join("\r\n") +
                "\r\n"
        );
        save(m);
    }
    const archives = [];
    for (const key of ["lokalna", "rezerwa"]) {
        console.log(`Dane edytora: próba ${key}.`);
        const label = key === "lokalna" ? "Lokalna" : "Rezerwowa";
        const e = { key, label, mode: key === "lokalna" ? "stacjonarne" : "zapasowe", cards: {} };
        m.editors.push(e);
        e.user = await account(`edytor.${key}`, "ORGANIZATION_EDITOR", "Maria", label);
        e.profile = await account(`organizator.${key}`, "ORGANIZATION_EDITOR", "Joanna", label);
        await api("bootstrap", "PUT", `/cms/koalicjants/${e.profile.id}`, {
            nameFirst: "Joanna",
            nameLast: label,
            email: e.profile.email,
            contentJson: json({
                version: "1",
                body_pl: "Wspiera organizację konkursu i kontakt ze szkołami.",
                koalicjant_profile_image_url: imageUrl,
            }),
            isVisible: true,
            version: 1,
        });
        e.post = await api("bootstrap", "POST", "/cms/posts", {
            editionId: m.baseline.id,
            name: `Komunikat ${label.toLowerCase()}`,
            contentJson: json({
                version: "1",
                title_pl: `Spotkanie organizacyjne - próba ${key}`,
                body_pl: "Spotkanie organizacyjne odbędzie się o godzinie 15:00.",
            }),
            isVisible: true,
        });
        e.sponsor = await api("bootstrap", "POST", "/cms/sponsors", {
            name: `Pracownia ${label.toLowerCase()}`,
            contentJson: json({
                version: "1",
                sponsor_web_url: "https://example.org",
                sponsor_logo_image_url: publicAssetUrl(imageUrl),
                body_pl: "Fikcyjna pracownia wspierająca próbę konkursową.",
            }),
            isVisible: true,
        });
        e.archive = await edition(`KOALA — archiwum ${label.toLowerCase()}`);
        sql(`UPDATE core.editions SET state='CLOSED' WHERE id=${q(e.archive.id)};`);
        e.removePdf = await upload(
            `Informator-${key}-poprzedni.pdf`,
            pdf("Poprzedni informator"),
            "application/pdf"
        );
        e.removeImage = await upload(`Kadr-${key}-roboczy.png`, png(), "image/png");
        e.pdf = asset(`Informator-${key}.pdf`, pdf("Informator konkursowy"));
        e.image = asset(`Ilustracja-${key}.png`, png());
        archives.push(e);
        save(m);
    }
    const pages = await list("bootstrap", "/cms/static-pages");
    const updatePage = async (name, body) => {
        const p = pages.find((x) => x.name === name);
        assert(p, `Missing static page ${name}`);
        return api("bootstrap", "PUT", `/cms/static-pages/${p.id}`, {
            contentJson: json({ version: "1", body_pl: body }),
            version: p.version,
        });
    };
    m.rules = await updatePage(
        "RULES_PAGE",
        "# Regulamin próby konkursowej\n\nTo fikcyjny regulamin do badania interfejsu.\n\n## Kontakt — próba lokalna\n\nNa pytania odpowiadamy w ciągu pięciu dni roboczych.\n\n## Kontakt — próba rezerwowa\n\nNa pytania odpowiadamy w ciągu pięciu dni roboczych."
    );
    m.history = await updatePage(
        "HISTORY_PAGE",
        "# Historia konkursu\n\n## Próba lokalna\n\nW próbie konkursowej uczestniczyło pięć szkół.\n\n## Próba rezerwowa\n\nW próbie konkursowej uczestniczyło sześć szkół."
    );
    m.problems = await updatePage(
        "TASKS_PAGE",
        json(
            Object.fromEntries(
                archives.map((e) => [
                    e.archive.id,
                    {
                        "Materiały organizacyjne": [
                            {
                                id: e.removePdf.id,
                                title: e.removePdf.name,
                                fileName: `${e.removePdf.id}.pdf`,
                                url: `/api/koala/content/${e.removePdf.id}.pdf`,
                            },
                        ],
                    },
                ])
            )
        )
    );
    m.status = "ready";
    bindCards(m);
    save(m);
    generateDocs(m);
    await verify(m);
    const cleared = await fetch("http://localhost:8025/api/v1/messages", { method: "DELETE" });
    assert(cleared.ok, "Could not clear consumed setup emails from Mailpit.");
    console.log(
        "Gotowe. Wygenerowano pięć kartek uczestników i osobny plik dane-zapasowe.md. Konta zapisane w kartkach i seed.local.json."
    );
}

function bindCards(m) {
    validateEmailConfiguration();
    for (const user of configuredActors(m))
        assert.equal(
            user.email,
            participantEmail(user.key),
            `Zmieniono ${emailVariables[user.key]}; uruchom --sync-emails przed generowaniem kartek.`
        );
    const d = m.dates;
    for (const a of m.admins) {
        assert.equal(
            a.inviteEmail,
            invitationEmail(a.key),
            "Zmieniono adres zaproszenia; uruchom --sync-emails."
        );
        const basics = { Edycja: m.baseline.name };
        const contest = (f) => ({ ...basics, Etap: f.stage.name, Zadanie: f.task.name });
        a.cards = {
            A01: { "E-mail": a.user.email, Hasło: password },
            A02: basics,
            A03: basics,
            A04: {},
            A05: { "E-mail zapraszanej osoby": a.inviteEmail, Uprawnienia: "Edytor" },
            A06: {
                Osoba: a.roleTarget.name,
                "E-mail": a.roleTarget.email,
                "Nowe uprawnienia": "Recenzent (zamiast edytora)",
            },
            A07: { Osoba: a.deleteTarget.name, "E-mail": a.deleteTarget.email },
            A08: {
                "Nazwa nowej edycji": `KOALA — horyzont ${a.key}`,
                "Początek zapisów": date(d.futureOpen),
                "Koniec zapisów": date(d.registerClose),
            },
            A09: {
                Edycja: a.editEdition.name,
                "Nowa nazwa": `KOALA — horyzont poprawiony ${a.key}`,
                "Nowy koniec zapisów": date(d.laterClose),
            },
            A10: { Edycja: a.startEdition.name },
            A11: {
                Edycja: a.workEdition.name,
                "Nazwa nowego etapu": `Etap letni — próba ${a.key}`,
                "Początek odpowiedzi": date(d.futureOpen),
                "Koniec odpowiedzi": date(d.futureClose),
            },
            A12: {
                Edycja: a.workEdition.name,
                Etap: a.editStage.name,
                "Nowy koniec odpowiedzi": date(d.laterClose),
            },
            A13: {
                Edycja: a.workEdition.name,
                Etap: a.crudStage.name,
                "Nazwa nowego zadania": `Suma guzików — próba ${a.key}`,
                Kategoria: "I",
                Treść: "W pudełku są trzy czerwone i dwa zielone guziki. Ile guzików jest razem?",
                Warianty: "A: 3; B: 5; C: 6",
                "Poprawny wariant": "B",
            },
            A14: {
                Edycja: a.workEdition.name,
                Etap: a.crudStage.name,
                "Nazwa nowego zadania": `Parzyste kamyki — próba ${a.key}`,
                Kategoria: "I",
                Treść: "Wybierz wszystkie liczby parzyste.",
                Warianty: "A: 2; B: 3; C: 4; D: 7; E: 8",
                "Poprawne warianty": "A, C, E",
            },
            A15: {
                Edycja: a.workEdition.name,
                Etap: a.crudStage.name,
                Zadanie: a.editTask.name,
                "Nowa treść":
                    "W pudełku są cztery czerwone i trzy niebieskie kule. Ile kul jest razem?",
                "Warianty po zmianie": "A: 4; B: 5; C: 7",
                "Poprawny wariant po zmianie": "C",
            },
            A16: { Edycja: a.workEdition.name, Etap: a.crudStage.name, Zadanie: a.deleteTask.name },
            A17: { Edycja: a.workEdition.name, Etap: a.activateStage.name },
            A18: { ...basics, Etap: a.auto.stage.name },
            A19: contest(a.assign),
            A20: {
                ...contest(a.assign),
                "Pierwsza osoba": `${a.pair[0].name} (${a.pair[0].email})`,
                "Druga osoba": `${a.pair[1].name} (${a.pair[1].email})`,
            },
            A21: {
                ...contest(a.replace),
                "Osoba przerywająca pracę": `${a.pair[1].name} (${a.pair[1].email})`,
                "Osoba zastępująca": `${a.replacement.name} (${a.replacement.email})`,
            },
            A22: { ...basics, Etap: a.blocked.stage.name },
            A23: { ...basics, Etap: a.close.stage.name },
            A24: { ...basics, Etap: a.rankGenerate.stage.name, Kategoria: "I" },
            A25: { ...basics, Etap: a.rankPublish.stage.name, Kategoria: "I" },
            A26: { Drużyna: a.teams[0].name, Osoba: a.rodoView.name },
            A27: {
                Drużyna: a.teams[1].name,
                Osoba: a.rodoDecision.name,
                "Informacja do rozpatrzenia":
                    "To dokument próbny przygotowany jako kompletny i prawidłowy. Zatwierdź zgodę.",
            },
            A28: {
                Drużyna: a.teams[0].name,
                Edycja: m.baseline.name,
                Sprawdź: "szkołę i liczbę członków drużyny",
            },
            A29: {
                Szkoła: a.school.nameFull,
                "Numer RSPO": a.school.rspo,
                "Nowa ulica": "Parkowa",
                "Nowy numer budynku": "14",
            },
            A30: {
                "Nazwa szkoły": `Szkoła Podstawowa Słoneczna — próba ${a.key}`,
                "Nazwa skrócona": "SP Słoneczna",
                "Numer RSPO": String(992000 + m.admins.indexOf(a)),
                Typ: "Szkoła podstawowa",
                Województwo: "wielkopolskie",
                Miejscowość: "Poznań",
                Ulica: "Słoneczna",
                "Numer budynku": "8",
                "E-mail": `sloneczna.${a.key}@koala.test`,
            },
            A31: { Plik: a.csv, Zawartość: "Dwie fikcyjne szkoły z odrębnymi numerami RSPO" },
            A32: {},
        };
    }
    for (const r of m.reviewers) {
        const common = { Edycja: m.baseline.name, Etap: r.stage.name };
        r.cards = { R01: { "E-mail": r.user.email, Hasło: password }, R07: {} };
        r.tasks.forEach(
            (t, i) =>
                (r.cards[`R0${i + 2}`] = {
                    ...common,
                    Zadanie: t.name,
                    "Przygotowany przykład":
                        "Odpowiedź drużyny jest poprawna. Oceń ją jako poprawną.",
                })
        );
        r.cards.R06 = {
            ...common,
            Zadanie: r.conflict.name,
            "Informacja po konsultacji":
                "Połowa zapisana jako 1/2 jest poprawna. Zmień swoją dotychczasową ocenę na poprawną.",
        };
    }
    for (const e of m.editors)
        e.cards = {
            E01: { "E-mail": e.user.email, Hasło: password },
            E02: { Plik: e.image },
            E03: {
                Tytuł: `Zbliża się termin - próba ${e.key}`,
                Edycja: m.baseline.name,
                Treść: `Przypominamy, że termin przesłania odpowiedzi upływa ${date(m.dates.futureClose)}. Życzymy powodzenia wszystkim drużynom.`,
                Widoczność: "Widoczny",
            },
            E04: {
                "Nazwa robocza": e.post.name,
                Tytuł: `Spotkanie organizacyjne - próba ${e.key}`,
                "Zmiana w treści": "Zmień godzinę spotkania z 15:00 na 16:30.",
            },
            E05: {
                Fragment: `Kontakt — próba ${e.label.toLowerCase()}`,
                "Nowa treść": "Na pytania odpowiadamy w ciągu trzech dni roboczych.",
            },
            E06: {
                Fragment: `Próba ${e.label.toLowerCase()}`,
                "Informacja do dopisania": `W próbie ${e.key === "lokalna" ? "lokalnej" : "rezerwowej"} przeprowadzono również warsztaty logiczne dla drużyn.`,
            },
            E07: { Edycja: e.archive.name, Sekcja: "Materiały organizacyjne", Plik: e.pdf },
            E08: {
                Edycja: e.archive.name,
                Sekcja: "Materiały organizacyjne",
                Dokument: e.removePdf.name,
            },
            E09: { Obraz: e.removeImage.name },
            E10: {
                "Nazwa sponsora": `Fundacja ${e.label.toLowerCase()}`,
                "Strona internetowa": "https://example.org",
                Opis: "Fikcyjna fundacja wspierająca edukację matematyczną i pracę zespołową.",
                "Adres logo": publicAssetUrl(
                    JSON.parse(e.sponsor.contentJson).sponsor_logo_image_url
                ),
                Widoczność: "Widoczny",
            },
            E11: {
                Sponsor: e.sponsor.name,
                "Nowy opis": "Pracownia wspiera organizację warsztatów logicznych dla szkół.",
                "Widoczność po zmianie": "Ukryty",
            },
            E12: {
                Osoba: e.profile.name,
                "E-mail": e.profile.email,
                "Nowy opis":
                    "Koordynuje kontakt ze szkołami i przygotowanie materiałów konkursowych.",
            },
            E13: {},
        };
    for (const group of [...m.admins, ...m.reviewers, ...m.editors])
        for (const c of catalog.filter(
            (x) =>
                x.role ===
                (m.admins.includes(group)
                    ? "admin"
                    : m.reviewers.includes(group)
                      ? "reviewer"
                      : "editor")
        ))
            assert(Object.hasOwn(group.cards, c.id), `Missing card binding ${c.id}/${group.key}`);
}

function generateDocs(m) {
    validateEmailConfiguration();
    assert(
        m.status === "ready",
        "Seed did not finish; generate instructions only after successful setup."
    );
    for (const group of [...m.admins, ...m.editors, ...m.reviewers]) {
        if (emailVariables[group.user.key])
            assert(
                group.user.email === participantEmail(group.user.key),
                "Email configuration changed. Run --sync-emails to update existing accounts before generating cards."
            );
    }
    const roles = [
        ["admin", "administrator", m.admins],
        ["editor", "edytor", m.editors],
        ["reviewer", "recenzent", m.reviewers],
    ];
    const reserve = [];
    for (const [role, name, variants] of roles) {
        for (const v of variants) {
            const out = [];
            for (const c of catalog.filter((x) => x.role === role)) {
                out.push(`- **${c.id}. ${c.instruction}**`);
                for (const [label, value] of Object.entries(v.cards[c.id]))
                    out.push(`  - **${label}:** ${value}`);
                out.push("");
            }
            if (v.key === "rezerwa") reserve.push(`## ${name}`, "", ...out);
            else {
                const mode = v.key === "lokalna" ? "stacjonarnie" : "zdalnie";
                writeFileSync(join(folder, `${name}-${mode}.md`), out.join("\n") + "\n");
            }
        }
        const oldFile = join(folder, `${name}.md`);
        if (existsSync(oldFile)) unlinkSync(oldFile);
    }
    writeFileSync(join(folder, "dane-zapasowe.md"), reserve.join("\n") + "\n");
}

function configuredActors(m) {
    return [...m.admins, ...m.editors, ...m.reviewers]
        .map((v) => v.user)
        .filter((u) => emailVariables[u.key]);
}

async function syncEmails(m) {
    validateEmailConfiguration();
    assert(m.status === "ready", "Seed did not finish.");
    const users = configuredActors(m);
    assert.equal(users.length, Object.keys(emailVariables).length);
    const desired = new Map(
        users.map((u) => {
            const parts = u.name.split(" ");
            return [
                u.id,
                {
                    email: participantEmail(u.key),
                    ...participantNames(
                        u.key,
                        u.nameFirst ?? parts[0],
                        u.nameLast ?? parts.slice(1).join(" ")
                    ),
                },
            ];
        })
    );
    const changes = users.filter((u) => {
        const value = desired.get(u.id);
        return u.email !== value.email || u.name !== `${value.nameFirst} ${value.nameLast}`;
    });
    const ids = users.map((u) => q(u.id)).join(",");
    for (const a of m.admins)
        assert.equal(
            sql(
                `SELECT count(*) FROM account.users WHERE lower(email)=lower(${q(invitationEmail(a.key))});`
            ),
            "0",
            "Adres zaproszenia jest już używany przez istniejące konto."
        );
    for (const u of changes) {
        assert.equal(
            sql(
                `SELECT count(*) FROM account.users WHERE lower(email)=lower(${q(participantEmail(u.key))}) AND id NOT IN (${ids});`
            ),
            "0",
            "A configured address is already used by another account."
        );
        assert.equal(
            sql(`SELECT count(*) FROM account.users WHERE id=${q(u.id)};`),
            "1",
            "Participant account no longer exists."
        );
    }
    if (changes.length) {
        // Two phases also allow swapping the addresses of two participants.
        sql(`BEGIN; ${changes.map((u) => `UPDATE account.users SET email=${q(`email-sync.${u.id}@koala.test`)} WHERE id=${q(u.id)};`).join("\n")}
      ${changes
          .map((u) => {
              const v = desired.get(u.id);
              const fields = `email=${q(v.email)},name_first=${q(v.nameFirst)},name_last=${q(v.nameLast)},updated_at=now(),version=version+1`;
              return `UPDATE account.users SET ${fields} WHERE id=${q(u.id)}; UPDATE cms.koalicjants SET ${fields} WHERE id=${q(u.id)};`;
          })
          .join("\n")}
      COMMIT;`);
        const update = (value) => {
            if (!value || typeof value !== "object") return;
            if (desired.has(value.id) && "email" in value) {
                const v = desired.get(value.id);
                value.email = v.email;
                if ("name" in value) value.name = `${v.nameFirst} ${v.nameLast}`;
                value.nameFirst = v.nameFirst;
                value.nameLast = v.nameLast;
            }
            for (const child of Object.values(value)) update(child);
        };
        update(m);
    }
    for (const a of m.admins) {
        a.inviteEmail = invitationEmail(a.key);
    }
    bindCards(m);
    save(m);
    generateDocs(m);
    for (const user of users) {
        const loggedIn = await login(user.key, participantEmail(user.key));
        assert.equal(
            loggedIn.id,
            user.id,
            "Configured email logs into the wrong participant account."
        );
        assert.equal(
            loggedIn.nameFirst,
            desired.get(user.id).nameFirst,
            "Participant first name differs from .env."
        );
        assert.equal(
            loggedIn.nameLast,
            desired.get(user.id).nameLast,
            "Participant last name differs from .env."
        );
    }
    console.log(
        `Zsynchronizowano dane pięciu testerów (${changes.length} zmian) i dwa adresy zaproszeń. Prywatne dane nie są wypisywane w logu.`
    );
}

async function syncAssets(m) {
    assert.equal(m.status, "ready");
    await login("asset-sync", m.support.email, m.support.password);
    const sponsors = await list("asset-sync", "/cms/sponsors");
    let changes = 0;
    for (const e of m.editors) {
        const current = sponsors.find((s) => s.id === e.sponsor.id);
        assert(current, "Fixture sponsor missing.");
        const content = JSON.parse(current.contentJson);
        const nextUrl = publicAssetUrl(content.sponsor_logo_image_url);
        if (content.sponsor_logo_image_url !== nextUrl) {
            content.sponsor_logo_image_url = nextUrl;
            e.sponsor = await api("asset-sync", "PUT", `/cms/sponsors/${current.id}`, {
                ...current,
                contentJson: json(content),
            });
            changes++;
        } else e.sponsor = current;
    }
    bindCards(m);
    generateDocs(m);
    save(m);
    console.log(
        `Zaktualizowano adresy logo (${changes} zmian) i odświeżono kartki bez resetu danych.`
    );
}

async function verify(m) {
    assert(m.status === "ready", "The previous seed did not finish.");
    await login("verify-admin", m.support.email);
    let checks = 0;
    for (const a of m.admins) {
        for (const [f, unassigned, incomplete] of [
            [a.assign, 2, 0],
            [a.replace, 0, 0],
        ]) {
            const p = await api("verify-admin", "GET", `/core/tasks/${f.task.id}/review-progress`);
            assert.equal(p.totalSubmissions, 2);
            assert.equal(p.unassigned, unassigned);
            assert.equal(p.incomplete, incomplete);
            checks++;
        }
        const auto = await api(
            "verify-admin",
            "GET",
            `/core/tasks/subedition/${a.auto.stage.id}/autocheck-progress`
        );
        assert.equal(auto.total, 2);
        assert.equal(auto.readyToCheck, 2);
        checks++;
        const pending = await api(
            "verify-admin",
            "GET",
            `/core/tasks/${a.replace.task.id}/pending-reviewers`
        );
        assert(pending.some((r) => r.reviewerId === a.pair[1].id && r.pending === 1));
        checks++;
        const drafts = await list(
            "verify-admin",
            `/core/ranking/subedition/${a.rankPublish.stage.id}/draft`
        );
        assert(drafts.length > 0);
        checks++;
        const publicRows = await list(
            "anonymous",
            `/core/ranking?subeditionId=${a.rankPublish.stage.id}`
        );
        assert.equal(publicRows.length, 0);
        checks++;
        const rodos = await list("verify-admin", `/account/teams/${a.teams[1].id}/rodos`);
        assert(
            rodos.some(
                (r) =>
                    r.teamMemberId === a.rodoDecision.id &&
                    r.rodoVersion > 0 &&
                    r.rodoState === "RODO_UNVERIFIED"
            )
        );
        checks++;
    }
    for (const r of m.reviewers) {
        await login(r.user.key, r.user.email);
        const rows = await list(r.user.key, "/core/tasks/review/me");
        for (const t of r.tasks)
            assert(rows.some((x) => x.taskId === t.id && x.review1Score === null));
        assert(
            rows.some(
                (x) =>
                    x.taskId === r.conflict.id &&
                    x.review1Score === false &&
                    x.review2Score === true
            )
        );
        checks++;
    }
    for (const e of m.editors) {
        await login(e.user.key, e.user.email);
        const posts = await list(e.user.key, "/cms/posts");
        assert(posts.some((p) => p.id === e.post.id));
        const profiles = await list(e.user.key, "/cms/koalicjants");
        assert(profiles.some((p) => p.id === e.profile.id));
        const file = await fetch(base + `/api/koala/content/${e.removePdf.id}.pdf`);
        assert(file.ok);
        checks++;
    }
    for (const scenario of catalog)
        assert([...m.admins, ...m.reviewers, ...m.editors].some((v) => v.cards[scenario.id]));
    console.log(
        `Weryfikacja API i powiązań: ${checks} grup sprawdzeń OK; ${catalog.length} polecenia mają karty danych.`
    );
}

async function prepare(m, taskId, variant) {
    assert(
        catalog.some((c) => c.id === taskId),
        "Unknown scenario id."
    );
    const a = m.admins.find((v) => v.key === variant);
    assert(a, "Choose --variant lokalna|zdalna|rezerwa");
    // Activating an edition is a global operation. Change only the global context;
    // do not call CloseEdition, which destroys competition answers and account data.
    sql(
        `BEGIN; UPDATE core.editions SET state='INACTIVE' WHERE state='ACTIVE'; ${taskId === "A10" ? `UPDATE core.editions SET state='INACTIVE' WHERE id=${q(a.startEdition.id)};` : `UPDATE core.editions SET state='ACTIVE' WHERE id=${q(m.baseline.id)};`} COMMIT;`
    );
    docker("restart", containerNames[2]); // Clears scoped/cached active-edition lookups.
    console.log(
        taskId === "A10"
            ? `Przygotowano uruchomienie edycji: ${a.startEdition.name}.`
            : "Przywrócono kontekst edycji pokazowej bez kasowania wyników pracy."
    );
}

async function exercise(m) {
    // Only disposable reserve records are modified. Never run this during a session.
    const a = m.admins.find((v) => v.key === "rezerwa");
    const r = m.reviewers.find((v) => v.key === "rezerwa");
    await login("exercise", m.support.email);
    const editionPath = `/core/editions/${a.workEdition.id}`;
    const stagePath = `${editionPath}/subeditions/${a.crudStage.id}`;
    const createdEdition = await api("exercise", "POST", "/core/editions", {
        name: a.cards.A08["Nazwa nowej edycji"],
        teamRegisterOpen: m.dates.futureOpen,
        teamRegisterClose: m.dates.registerClose,
    });
    assert(createdEdition.id);
    await api("exercise", "PUT", `/core/editions/${a.editEdition.id}`, {
        name: a.cards.A09["Nowa nazwa"],
        teamRegisterOpen: a.editEdition.teamRegisterOpen,
        teamRegisterClose: m.dates.laterClose,
        version: a.editEdition.version,
    });
    sql(`UPDATE core.editions SET state='INACTIVE' WHERE state='ACTIVE';`);
    try {
        await api("exercise", "PUT", `/core/editions/${a.startEdition.id}/activate`);
    } finally {
        sql(
            `BEGIN; UPDATE core.editions SET state='INACTIVE' WHERE state='ACTIVE'; UPDATE core.editions SET state='ACTIVE' WHERE id=${q(m.baseline.id)}; COMMIT;`
        );
    }
    await api("exercise", "POST", `${editionPath}/subeditions`, {
        name: a.cards.A11["Nazwa nowego etapu"],
        answerOpen: m.dates.futureOpen,
        answerClose: m.dates.futureClose,
    });
    await api("exercise", "PUT", `${editionPath}/subeditions/${a.editStage.id}`, {
        name: a.editStage.name,
        answerOpen: m.dates.futureOpen,
        answerClose: m.dates.laterClose,
        version: a.editStage.version,
    });
    await api("exercise", "PUT", `${editionPath}/activate_subedition/${a.activateStage.id}`);
    for (const [id, type, answer] of [
        ["A13", "single_choice", "B"],
        ["A14", "multiple_choice", '["A","C","E"]'],
    ]) {
        const card = a.cards[id];
        const options = card.Warianty.split("; ").map((x) => ({ id: x[0], text: x.slice(3) }));
        const t = await api("exercise", "POST", stagePath + "/tasks", {
            name: card["Nazwa nowego zadania"],
            tier: "TIER_1",
            contentJson: json({ version: 1, answer_type: type, body_pl: card["Treść"], options }),
        });
        await api("exercise", "POST", `/core/tasks/${t.id}/corr`, { answer });
    }
    const content = JSON.parse(a.editTask.contentJson);
    content.body_pl = a.cards.A15["Nowa treść"];
    content.options = [
        { id: "A", text: "4" },
        { id: "B", text: "5" },
        { id: "C", text: "7" },
    ];
    await api("exercise", "PUT", stagePath + "/tasks/" + a.editTask.id, {
        name: a.editTask.name,
        tier: a.editTask.tier,
        contentJson: json(content),
        version: a.editTask.version,
    });
    const keys = await list("exercise", `/core/tasks/${a.editTask.id}/corr`);
    await api("exercise", "PUT", `/core/tasks/${a.editTask.id}/corr/${keys[0].id}`, {
        answer: "C",
        version: keys[0].version,
    });
    await api("exercise", "DELETE", stagePath + "/tasks/" + a.deleteTask.id);
    await api("exercise", "POST", "/account/users", {
        email: a.inviteEmail,
        userRoles: ["ORGANIZATION_EDITOR"],
    });
    const roleTarget = await api("exercise", "GET", "/account/users/" + a.roleTarget.id);
    await api("exercise", "PUT", `/account/users/${a.roleTarget.id}/roles`, {
        userRoles: ["ORGANIZATION_REVIEWER"],
        version: roleTarget.version,
    });
    await api("exercise", "DELETE", "/account/users/" + a.deleteTarget.id);
    await api("exercise", "PUT", "/core/schools/" + a.school.id, {
        ...a.school,
        road: "Parkowa",
        building: "14",
    });
    const c = a.cards.A30;
    await api("exercise", "POST", "/core/schools", {
        nameFull: c["Nazwa szkoły"],
        nameShort: c["Nazwa skrócona"],
        state: c["Województwo"],
        city: c["Miejscowość"],
        road: c["Ulica"],
        building: c["Numer budynku"],
        rspo: c["Numer RSPO"],
        type: c.Typ,
        email: c["E-mail"],
    });
    const csv = new FormData();
    csv.set(
        "File",
        new Blob([readFileSync(join(folder, a.csv))], { type: "text/csv" }),
        "szkoly.csv"
    );
    assert.equal(await api("exercise", "POST", "/core/schools/import", csv), 2);
    const members = await list("exercise", `/account/teams/${a.teams[1].id}/rodos`);
    const rodo = members.find((x) => x.teamMemberId === a.rodoDecision.id);
    await api(
        "exercise",
        "PUT",
        `/account/teams/${a.teams[1].id}/rodos/${rodo.teamMemberId}/state`,
        { state: "RODO_ACCEPTED", version: rodo.version, rodoVersion: rodo.rodoVersion }
    );
    await api("exercise", "PUT", `/core/tasks/subedition/${a.auto.stage.id}/autocheck`);
    await api(
        "exercise",
        "PUT",
        `/core/tasks/${a.assign.stage.id}/assign-reviewers/task/${a.assign.task.id}`,
        { reviewer1Id: a.pair[0].id, reviewer2Id: a.pair[1].id }
    );
    await api(
        "exercise",
        "PUT",
        `/core/tasks/${a.replace.stage.id}/updated-assign-reviewer/task/${a.replace.task.id}`,
        { oldReviewer: a.pair[1].id, newReviewer: a.replacement.id }
    );
    const rows = await list("exercise", `/core/tasks/submissions/${a.replace.task.id}`);
    assert(rows.some((x) => x.review2Score === true && x.reviewer2Id === a.pair[1].id));
    assert(rows.some((x) => x.review2Score === null && x.reviewer2Id === a.replacement.id));
    await api(
        "exercise",
        "PUT",
        `/core/editions/${m.baseline.id}/close_subedition/${a.blocked.stage.id}`,
        null,
        409
    );
    await api(
        "exercise",
        "PUT",
        `/core/editions/${m.baseline.id}/close_subedition/${a.close.stage.id}`
    );
    await api("exercise", "POST", `/core/ranking/generate/subedition/${a.rankGenerate.stage.id}`);
    await api("exercise", "PUT", `/core/ranking/subedition/${a.rankPublish.stage.id}/publish`);
    await login(r.user.key, r.user.email);
    for (const t of [...r.tasks, r.conflict])
        await api(
            r.user.key,
            "PUT",
            `/core/tasks/review/me/submission/${t.id}/team/${r.team.id}/subedition/${r.stage.id}`,
            { score: true }
        );
    const e = m.editors.find((v) => v.key === "rezerwa");
    await login(e.user.key, e.user.email);
    await upload(
        "Ilustracja-rezerwa.png",
        readFileSync(join(folder, e.image)),
        "image/png",
        e.user.key
    );
    await api(e.user.key, "POST", "/cms/posts", {
        name: e.cards.E03["Tytuł"],
        editionId: m.baseline.id,
        contentJson: json({
            version: "1",
            title_pl: e.cards.E03["Tytuł"],
            body_pl: e.cards.E03["Treść"],
        }),
        isVisible: true,
    });
    const postContent = JSON.parse(e.post.contentJson);
    postContent.body_pl = postContent.body_pl.replace("15:00", "16:30");
    await api(e.user.key, "PUT", `/cms/posts/${e.post.id}`, {
        ...e.post,
        contentJson: json(postContent),
    });
    for (const [pageId, mutate] of [
        [
            m.rules.id,
            (value) => value.replace(/(Kontakt — próba rezerwowa[\s\S]*)pięciu/, "$1trzech"),
        ],
        [m.history.id, (value) => value + "\n\n" + e.cards.E06["Informacja do dopisania"]],
    ]) {
        const pages = await list(e.user.key, "/cms/static-pages");
        const p = pages.find((p) => p.id === pageId);
        const content = JSON.parse(p.contentJson);
        content.body_pl = mutate(content.body_pl);
        await api(e.user.key, "PUT", "/cms/static-pages/" + p.id, {
            contentJson: json(content),
            version: p.version,
        });
    }
    const uploadedPdf = await upload(
        "Informator-rezerwa.pdf",
        readFileSync(join(folder, e.pdf)),
        "application/pdf",
        e.user.key
    );
    const pages = await list(e.user.key, "/cms/static-pages");
    const p = pages.find((p) => p.id === m.problems.id);
    const metadata = JSON.parse(JSON.parse(p.contentJson).body_pl);
    metadata[e.archive.id]["Materiały organizacyjne"] = [
        {
            id: uploadedPdf.id,
            title: uploadedPdf.name,
            fileName: `${uploadedPdf.id}.pdf`,
            url: `/api/koala/content/${uploadedPdf.id}.pdf`,
        },
    ];
    await api(e.user.key, "PUT", "/cms/static-pages/" + p.id, {
        contentJson: json({ version: "1", body_pl: json(metadata) }),
        version: p.version,
    });
    await api(e.user.key, "DELETE", "/cms/public-files/" + e.removePdf.id);
    await api(e.user.key, "DELETE", "/cms/public-files/" + e.removeImage.id);
    const logo = e.cards.E10["Adres logo"];
    await api(e.user.key, "POST", "/cms/sponsors", {
        name: e.cards.E10["Nazwa sponsora"],
        contentJson: json({
            version: "1",
            body_pl: e.cards.E10.Opis,
            sponsor_web_url: "https://example.org",
            sponsor_logo_image_url: logo,
        }),
        isVisible: true,
    });
    await api(e.user.key, "PUT", "/cms/sponsors/" + e.sponsor.id, {
        ...e.sponsor,
        contentJson: json({
            ...JSON.parse(e.sponsor.contentJson),
            body_pl: e.cards.E11["Nowy opis"],
        }),
        isVisible: false,
    });
    const profiles = await list(e.user.key, "/cms/koalicjants");
    const profile = profiles.find((x) => x.id === e.profile.id);
    await api(e.user.key, "PUT", "/cms/koalicjants/" + profile.id, {
        ...profile,
        contentJson: json({
            ...JSON.parse(profile.contentJson),
            body_pl: e.cards.E12["Nowy opis"],
        }),
    });
    await api(
        e.user.key,
        "POST",
        `/core/ranking/generate/subedition/${a.rankGenerate.stage.id}`,
        null,
        403
    );
    console.log(
        "Próba zapasowa: CRUD użytkowników/edycji/etapów/zadań, szkoły i import, RODO, sprawdzarka, przydziały, zastępstwo, oceny, rankingi oraz treści/pliki edytora działają przez API. Rezerwa zużyta — wykonaj --reset."
    );
}

export {
    api,
    base,
    catalog,
    docker,
    emailVariables,
    folder,
    getSessionCookie,
    json,
    list,
    load,
    login,
    observeRequests,
    q,
    sql,
    upload,
    validateEmailConfiguration,
};

if (isMain) {
    if (!args.includes("--help") && !args.includes("--prepare")) validateEmailConfiguration();
    if (args.includes("--help")) {
        console.log(
            "node client/tests/usability/prepare.mjs [--reset | --docs-only | --sync-emails | --sync-assets | --verify | --exercise | --prepare A10 --variant lokalna]"
        );
    } else if (args.includes("--docs-only")) {
        const m = load();
        bindCards(m);
        generateDocs(m);
        save(m);
        console.log(
            "Wygenerowano pięć kartek uczestników i dane-zapasowe.md bez dostępu do Dockera ani API."
        );
    } else if (args.includes("--sync-emails")) {
        await syncEmails(load());
    } else if (args.includes("--sync-assets")) {
        await syncAssets(load());
    } else if (args.includes("--prepare")) {
        await prepare(load(), option("--prepare"), option("--variant") ?? "lokalna");
    } else if (args.includes("--exercise")) {
        await exercise(load());
    } else if (args.includes("--verify")) {
        await verify(load());
    } else {
        if (args.includes("--reset")) await reset();
        await seed();
    }
}
