// Execute every card against the real local API and roll back database/files/cache.
// Run with no arguments: node client/tests/usability/check-scenarios.mjs
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { basename, join } from "node:path";
import {
    api,
    base,
    catalog,
    docker,
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
} from "./prepare.mjs";

const m = load();
assert.equal(m.status, "ready");
const recoverOnly = process.argv.slice(2).includes("--restore");
assert(
    process.argv.length === 2 || (process.argv.length === 3 && recoverOnly),
    "Use no arguments, or --restore to recover a saved snapshot."
);
const backupPath = join(folder, "rollback.local.json");
const reportPath = join(folder, "api-scenarios.local.json");
const reportMarkdown = join(folder, "api-scenarios.local.md");
assert(
    recoverOnly ? existsSync(backupPath) : !existsSync(backupPath),
    recoverOnly
        ? "No saved snapshot to restore."
        : "Unrestored snapshot exists. Run check-scenarios.mjs --restore before another check."
);
const results = [],
    requests = [];
let currentCase;
const recordRequest = (request) => {
    const entry = { scenario: currentCase?.id, variant: currentCase?.variant, ...request };
    requests.push(entry);
    currentCase?.requests.push(entry);
};
observeRequests(recordRequest);
const run = async (variant, id, fn, note) => {
    const entry = {
        variant,
        id,
        instruction: catalog.find((c) => c.id === id).instruction,
        status: "PASS",
        note,
        requests: [],
    };
    results.push(entry);
    currentCase = entry;
    try {
        await fn();
    } catch (e) {
        entry.status = "FAIL";
        entry.error = String(e.message);
        console.log(`FAIL ${variant}/${id}: ${entry.error}`);
    } finally {
        currentCase = null;
    }
};
const contentOf = (x) => JSON.parse(x.contentJson);
const stageTasks = (editionId, stageId) =>
    `/core/editions/${editionId}/subeditions/${stageId}/tasks`;
const taskRows = (who, t) => list(who, `/core/tasks/submissions/${t.id}`);
const cardDate = (text) => {
    // Parse exactly the Warsaw date/time printed on the participant's card.
    const match = text.match(/^(\d{1,2})\.(\d{1,2})\.(\d{4}),?\s+(\d{1,2}):(\d{2})$/);
    assert(match, `Unknown printed date: ${text}`);
    const [, day, month, year, hour, minute] = match.map(Number);
    const wall = Date.UTC(year, month - 1, day, hour, minute);
    // Solve the offset using Intl, including summertime, rather than the host timezone.
    let instant = wall;
    for (let i = 0; i < 3; i++) {
        const fields = Object.fromEntries(
            new Intl.DateTimeFormat("en-GB", {
                timeZone: "Europe/Warsaw",
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit",
                hourCycle: "h23",
            })
                .formatToParts(instant)
                .map((p) => [p.type, p.value])
        );
        const shown = Date.UTC(
            +fields.year,
            +fields.month - 1,
            +fields.day,
            +fields.hour,
            +fields.minute
        );
        instant += wall - shown;
    }
    return new Date(instant).toISOString();
};

function databaseFingerprint() {
    const tables = sql(
        "SELECT table_schema||'.'||table_name FROM information_schema.tables WHERE table_schema IN ('account','core','cms') AND table_type='BASE TABLE' ORDER BY 1;"
    ).split("\n");
    return sql(
        tables
            .map((t) => {
                assert(/^[a-z_]+\.[a-zA-Z_]+$/.test(t));
                const qualified = t
                    .split(".")
                    .map((x) => `"${x}"`)
                    .join(".");
                return `SELECT ${q(t)}||':'||md5(COALESCE(string_agg(row_to_json(r)::text,E'\\n' ORDER BY row_to_json(r)::text),'')) FROM ${qualified} r;`;
            })
            .join("\n")
    );
}
const fileFingerprint = () =>
    docker(
        "exec",
        "koala_backend_container",
        "sh",
        "-c",
        "find /app/backend_data -type f -exec sha256sum {} + | sort"
    );
function redisCli(...a) {
    const c = JSON.parse(docker("inspect", "koala_redis_container"))[0];
    const env = Object.fromEntries(
        c.Config.Env.map((x) => {
            const i = x.indexOf("=");
            return [x.slice(0, i), x.slice(i + 1)];
        })
    );
    return [
        "exec",
        "-i",
        "--env",
        `REDISCLI_AUTH=${env.REDIS_PASSWORD}`,
        "koala_redis_container",
        "redis-cli",
        ...a,
    ];
}
function snapshot() {
    const b = JSON.parse(docker("inspect", "koala_backend_container"))[0];
    assert.equal(b.Mounts.length, 1);
    assert.equal(b.Mounts[0].Name, "koala_backend_volume");
    assert.equal(b.Mounts[0].Destination, "/app/backend_data");
    const db = execFileSync(
        "docker",
        ["exec", "koala_postgres_container", "pg_dump", "-U", "user", "-d", "db", "-Fc"],
        { maxBuffer: 32 * 1024 * 1024 }
    );
    const files = execFileSync(
        "docker",
        ["exec", "koala_backend_container", "tar", "-cf", "-", "-C", "/app/backend_data", "."],
        { maxBuffer: 32 * 1024 * 1024 }
    );
    const keys = execFileSync("docker", redisCli("--raw", "KEYS", "*"), { encoding: "utf8" })
        .trim()
        .split("\n")
        .filter(Boolean);
    const cache = keys.map((key) => {
        const ttl = Number(
            execFileSync("docker", redisCli("--raw", "PTTL", key), { encoding: "utf8" }).trim()
        );
        const dump = execFileSync("docker", redisCli("--raw", "DUMP", key));
        return { key, ttl, data: dump.subarray(0, -1).toString("base64") };
    });
    const result = {
        at: Date.now(),
        db: db.toString("base64"),
        files: files.toString("base64"),
        cache,
        backendImage: b.Image,
        dbFingerprint: databaseFingerprint(),
        fileFingerprint: fileFingerprint(),
    };
    writeFileSync(backupPath, json(result));
    return result;
}
const resp = (parts) =>
    Buffer.concat([
        Buffer.from(`*${parts.length}\r\n`),
        ...parts.flatMap((value) => {
            const b = Buffer.isBuffer(value) ? value : Buffer.from(String(value));
            return [Buffer.from(`$${b.length}\r\n`), b, Buffer.from("\r\n")];
        }),
    ]);
async function restore(backup) {
    console.log("Przywracanie tego samego zestawu danych (z zachowaniem identyfikatorów)…");
    docker("stop", "koala_backend_container");
    execFileSync(
        "docker",
        [
            "exec",
            "-i",
            "koala_postgres_container",
            "pg_restore",
            "-U",
            "user",
            "-d",
            "db",
            "--clean",
            "--if-exists",
            "--no-owner",
            "--exit-on-error",
        ],
        { input: Buffer.from(backup.db, "base64"), maxBuffer: 32 * 1024 * 1024 }
    );
    // The only writable path is the explicitly checked, named backend volume.
    execFileSync(
        "docker",
        [
            "run",
            "--rm",
            "-i",
            "--network",
            "none",
            "--mount",
            "type=volume,source=koala_backend_volume,target=/data",
            "--entrypoint",
            "sh",
            backup.backendImage,
            "-c",
            "find /data -mindepth 1 -delete && tar -xf - -C /data",
        ],
        { input: Buffer.from(backup.files, "base64") }
    );
    const elapsed = Date.now() - backup.at;
    const commands = [resp(["FLUSHALL"])];
    for (const c of backup.cache) {
        if (c.ttl >= 0 && c.ttl <= elapsed) continue;
        commands.push(
            resp([
                "RESTORE",
                c.key,
                c.ttl < 0 ? 0 : Math.max(1, c.ttl - elapsed),
                Buffer.from(c.data, "base64"),
                "REPLACE",
            ])
        );
    }
    execFileSync("docker", redisCli("--pipe"), { input: Buffer.concat(commands) });
    assert.equal(
        databaseFingerprint(),
        backup.dbFingerprint,
        "Database fingerprint differs after restore."
    );
    docker("start", "koala_backend_container");
    assert.equal(fileFingerprint(), backup.fileFingerprint, "File checksums differ after restore.");
    for (let i = 0; i < 60; i++) {
        try {
            if ((await fetch(base + "/api/koala/core/editions")).ok) {
                unlinkSync(backupPath);
                return;
            }
        } catch {
            /* startup */
        }
        await new Promise((r) => setTimeout(r, 500));
    }
    throw new Error("Backend did not restart; snapshot retained in rollback.local.json.");
}

async function checkAdmin(a) {
    console.log(`Wszystkie karty administratora: ${a.key}.`);
    const who = a.user.key,
        cards = a.cards;
    await run(a.key, "A01", async () => {
        const user = await login(who, cards.A01["E-mail"], cards.A01["Hasło"]);
        assert.equal(user.id, a.user.id);
        assert.equal(user.email, cards.A01["E-mail"]);
        const account = await api(who, "GET", "/account/users/" + user.id);
        assert.equal(account.email, user.email);
    });
    await run(a.key, "A02", async () => {
        const editions = await list(who, "/core/editions?States=ACTIVE");
        assert(editions.some((e) => e.id === m.baseline.id));
        const stages = await list(who, `/core/editions/${m.baseline.id}/subeditions`);
        const teams = await list(who, `/account/teams?EditionId=${m.baseline.id}`);
        for (const team of teams) await list(who, `/account/teams/${team.id}/rodos`);
        for (const s of stages.filter((s) => s.state === "ACTIVE")) {
            const tasks = await list(who, stageTasks(m.baseline.id, s.id));
            for (const t of tasks) {
                await api(who, "GET", `/core/tasks/${t.id}/review-progress`);
                await taskRows(who, t);
            }
        }
    });
    await run(
        a.key,
        "A03",
        async () => {
            const rows = await taskRows(who, a.blocked.task);
            assert(
                rows.some(
                    (x) =>
                        x.review1Score !== null &&
                        x.review2Score !== null &&
                        x.review1Score !== x.review2Score
                )
            );
        },
        "API dostarcza konflikt; ocena opisu dalszych działań wymaga obserwacji uczestnika."
    );
    await run(
        a.key,
        "A04",
        async () => {
            await api(who, "GET", "/account/users/" + a.user.id);
        },
        "Samo znalezienie menu zmiany hasła jest zadaniem GUI i nie wysyła żądania zmiany hasła."
    );
    await run(a.key, "A05", async () => {
        await api(who, "POST", "/account/users", {
            email: cards.A05["E-mail zapraszanej osoby"],
            userRoles: ["ORGANIZATION_EDITOR"],
        });
        const users = await list(who, "/account/users");
        assert(users.some((u) => u.email === a.inviteEmail));
        assert.equal(
            sql(
                `SELECT count(*) FROM account.links l JOIN account.users u ON u.id=l.user_id WHERE u.email=${q(a.inviteEmail)} AND l.type='REGISTRATION' AND l.state='ACTIVE';`
            ),
            "1"
        );
    });
    await run(a.key, "A06", async () => {
        const target = await api(who, "GET", "/account/users/" + a.roleTarget.id);
        const updated = await api(who, "PUT", `/account/users/${target.id}/roles`, {
            userRoles: ["ORGANIZATION_REVIEWER"],
            version: target.version,
        });
        assert.deepEqual(updated.roles, ["ORGANIZATION_REVIEWER"]);
    });
    await run(a.key, "A07", async () => {
        await api(who, "DELETE", "/account/users/" + a.deleteTarget.id);
        await api(who, "GET", "/account/users/" + a.deleteTarget.id, null, 404);
    });
    await run(a.key, "A08", async () => {
        const c = cards.A08;
        const created = await api(who, "POST", "/core/editions", {
            name: c["Nazwa nowej edycji"],
            teamRegisterOpen: cardDate(c["Początek zapisów"]),
            teamRegisterClose: cardDate(c["Koniec zapisów"]),
        });
        assert.equal(created.name, c["Nazwa nowej edycji"]);
        assert.equal(created.state, "INACTIVE");
    });
    await run(a.key, "A09", async () => {
        const e = a.editEdition;
        const c = cards.A09;
        const updated = await api(who, "PUT", `/core/editions/${e.id}`, {
            name: c["Nowa nazwa"],
            teamRegisterOpen: e.teamRegisterOpen,
            teamRegisterClose: cardDate(c["Nowy koniec zapisów"]),
            version: e.version,
        });
        assert.equal(updated.name, c["Nowa nazwa"]);
        assert.equal(
            Date.parse(updated.teamRegisterClose),
            Date.parse(cardDate(c["Nowy koniec zapisów"]))
        );
    });
    await run(
        a.key,
        "A10",
        async () => {
            sql("UPDATE core.editions SET state='INACTIVE' WHERE state='ACTIVE';");
            try {
                const updated = await api(
                    who,
                    "PUT",
                    `/core/editions/${a.startEdition.id}/activate`
                );
                assert.equal(updated.state, "ACTIVE");
            } finally {
                sql(
                    `BEGIN; UPDATE core.editions SET state='INACTIVE' WHERE state='ACTIVE'; UPDATE core.editions SET state='ACTIVE' WHERE id=${q(m.baseline.id)}; COMMIT;`
                );
            }
        },
        "Przełączenie globalnego kontekstu przez moderatora zgodnie z README."
    );
    await run(a.key, "A11", async () => {
        const c = cards.A11;
        const created = await api(who, "POST", `/core/editions/${a.workEdition.id}/subeditions`, {
            name: c["Nazwa nowego etapu"],
            answerOpen: cardDate(c["Początek odpowiedzi"]),
            answerClose: cardDate(c["Koniec odpowiedzi"]),
        });
        assert.equal(created.name, c["Nazwa nowego etapu"]);
    });
    await run(a.key, "A12", async () => {
        const e = a.editStage;
        const updated = await api(
            who,
            "PUT",
            `/core/editions/${a.workEdition.id}/subeditions/${e.id}`,
            {
                name: e.name,
                answerOpen: e.answerOpen,
                answerClose: cardDate(cards.A12["Nowy koniec odpowiedzi"]),
                version: e.version,
            }
        );
        assert.equal(
            Date.parse(updated.answerClose),
            Date.parse(cardDate(cards.A12["Nowy koniec odpowiedzi"]))
        );
    });
    for (const [id, type] of [
        ["A13", "single_choice"],
        ["A14", "multiple_choice"],
    ])
        await run(a.key, id, async () => {
            const c = cards[id],
                options = c.Warianty.split("; ").map((x) => ({ id: x[0], text: x.slice(3) }));
            const content = { version: 1, body_pl: c["Treść"], answer_type: type, options };
            const created = await api(who, "POST", stageTasks(a.workEdition.id, a.crudStage.id), {
                name: c["Nazwa nowego zadania"],
                tier: "TIER_1",
                contentJson: json(content),
            });
            const answer =
                type === "single_choice"
                    ? c["Poprawny wariant"]
                    : json(c["Poprawne warianty"].split(", ").sort());
            await api(who, "POST", `/core/tasks/${created.id}/corr`, { answer });
            const saved = await api(
                who,
                "GET",
                stageTasks(a.workEdition.id, a.crudStage.id) + "/" + created.id
            );
            assert.deepEqual(contentOf(saved), content);
            const keys = await list(who, `/core/tasks/${created.id}/corr`);
            assert.equal(keys[0].answer, answer);
        });
    await run(a.key, "A15", async () => {
        const c = cards.A15,
            t = a.editTask;
        const content = contentOf(t);
        content.body_pl = c["Nowa treść"];
        content.options = c["Warianty po zmianie"]
            .split("; ")
            .map((x) => ({ id: x[0], text: x.slice(3) }));
        await api(who, "PUT", stageTasks(a.workEdition.id, a.crudStage.id) + "/" + t.id, {
            name: t.name,
            tier: t.tier,
            contentJson: json(content),
            version: t.version,
        });
        const keys = await list(who, `/core/tasks/${t.id}/corr`);
        await api(who, "PUT", `/core/tasks/${t.id}/corr/${keys[0].id}`, {
            answer: c["Poprawny wariant po zmianie"],
            version: keys[0].version,
        });
        const saved = await api(
            who,
            "GET",
            stageTasks(a.workEdition.id, a.crudStage.id) + "/" + t.id
        );
        assert.deepEqual(contentOf(saved), content);
    });
    await run(a.key, "A16", async () => {
        const path = stageTasks(a.workEdition.id, a.crudStage.id);
        await api(who, "DELETE", path + "/" + a.deleteTask.id);
        assert(!(await list(who, path)).some((t) => t.id === a.deleteTask.id));
    });
    await run(a.key, "A17", async () => {
        const s = await api(
            who,
            "PUT",
            `/core/editions/${a.workEdition.id}/activate_subedition/${a.activateStage.id}`
        );
        assert.equal(s.state, "ACTIVE");
    });
    await run(a.key, "A18", async () => {
        const p = `/core/tasks/subedition/${a.auto.stage.id}`;
        assert.equal((await api(who, "GET", p + "/autocheck-progress")).readyToCheck, 2);
        await api(who, "PUT", p + "/autocheck");
        assert.equal((await api(who, "GET", p + "/autocheck-progress")).readyToCheck, 0);
        const rows = await taskRows(who, a.auto.task);
        assert(rows.every((r) => r.autoReviewState === (r.submission === a.auto.task.correct)));
    });
    await run(a.key, "A19", async () => {
        const progress = await api(who, "GET", `/core/tasks/${a.assign.task.id}/review-progress`);
        assert.equal(progress.totalSubmissions, 2);
        assert.equal(progress.unassigned, 2);
        assert.equal(progress.incomplete, 0);
        const rows = await taskRows(who, a.assign.task);
        assert.equal(
            rows.filter((x) => x.review1Score === null).length +
                rows.filter((x) => x.review2Score === null).length,
            4
        );
    });
    await run(a.key, "A20", async () => {
        const reviewers = await list(who, "/account/users?UserRoles=ORGANIZATION_REVIEWER");
        assert(a.pair.every((p) => reviewers.some((r) => r.id === p.id)));
        await api(
            who,
            "PUT",
            `/core/tasks/${a.assign.stage.id}/assign-reviewers/task/${a.assign.task.id}`,
            { reviewer1Id: a.pair[0].id, reviewer2Id: a.pair[1].id }
        );
        const rows = await taskRows(who, a.assign.task);
        assert(rows.every((r) => r.reviewer1Id === a.pair[0].id && r.reviewer2Id === a.pair[1].id));
    });
    await run(a.key, "A21", async () => {
        const pending = await api(who, "GET", `/core/tasks/${a.replace.task.id}/pending-reviewers`);
        assert(pending.some((p) => p.reviewerId === a.pair[1].id && p.pending === 1));
        await api(
            who,
            "PUT",
            `/core/tasks/${a.replace.stage.id}/updated-assign-reviewer/task/${a.replace.task.id}`,
            { oldReviewer: a.pair[1].id, newReviewer: a.replacement.id }
        );
        const rows = await taskRows(who, a.replace.task);
        assert(rows.some((r) => r.review2Score === true && r.reviewer2Id === a.pair[1].id));
        assert(rows.some((r) => r.review2Score === null && r.reviewer2Id === a.replacement.id));
    });
    await run(a.key, "A22", async () => {
        await api(
            who,
            "PUT",
            `/core/editions/${m.baseline.id}/close_subedition/${a.blocked.stage.id}`,
            null,
            409
        );
    });
    await run(a.key, "A23", async () => {
        const s = await api(
            who,
            "PUT",
            `/core/editions/${m.baseline.id}/close_subedition/${a.close.stage.id}`
        );
        assert.equal(s.state, "CLOSED");
    });
    await run(a.key, "A24", async () => {
        await api(who, "POST", `/core/ranking/generate/subedition/${a.rankGenerate.stage.id}`);
        const draft = await list(
            who,
            `/core/ranking/subedition/${a.rankGenerate.stage.id}/draft?tier=TIER_1`
        );
        assert(draft.some((r) => r.teamName === a.teams[0].name && r.totalScore === 1));
        const hidden = await list(
            "anonymous",
            `/core/ranking?subeditionId=${a.rankGenerate.stage.id}&tier=TIER_1`
        );
        assert.equal(hidden.length, 0);
    });
    await run(a.key, "A25", async () => {
        assert(
            (
                await list(
                    who,
                    `/core/ranking/subedition/${a.rankPublish.stage.id}/draft?tier=TIER_1`
                )
            ).length > 0
        );
        await api(who, "PUT", `/core/ranking/subedition/${a.rankPublish.stage.id}/publish`);
        const published = await list(
            "anonymous",
            `/core/ranking?subeditionId=${a.rankPublish.stage.id}&tier=TIER_1`
        );
        assert(published.some((r) => r.teamName === a.teams[0].name));
    });
    await run(a.key, "A26", async () => {
        const rows = await list(who, `/account/teams/${a.teams[0].id}/rodos`);
        const r = rows.find((r) => r.teamMemberId === a.rodoView.id);
        assert.equal(r.rodoState, "RODO_ACCEPTED");
        const detail = await api(
            who,
            "GET",
            `/account/teams/${a.teams[0].id}/rodos/${r.teamMemberId}`
        );
        assert.equal(detail.teamMemberId, r.teamMemberId);
    });
    await run(a.key, "A27", async () => {
        const p = `/account/teams/${a.teams[1].id}/rodos/${a.rodoDecision.id}`;
        const r = await api(who, "GET", p);
        await checkFile(a.user, p + "/file", "application/pdf");
        const accepted = await api(who, "PUT", p + "/state", {
            state: "RODO_ACCEPTED",
            version: r.version,
            rodoVersion: r.rodoVersion,
        });
        assert.equal(accepted.rodoState, "RODO_ACCEPTED");
    });
    await run(a.key, "A28", async () => {
        const teams = await list(who, "/account/teams");
        const t = teams.find((t) => t.id === a.teams[0].id);
        assert(t && t.name === cards.A28["Drużyna"]);
        const schools = await list(who, "/core/schools");
        assert(schools.some((s) => s.id === t.schoolId));
        assert.equal(t.teamCaptain.id, a.teams[0].captain.id);
        assert(t.teamMembers.some((x) => x.teamMemberId === a.rodoView.id));
    });
    await run(a.key, "A29", async () => {
        const s = await api(who, "PUT", "/core/schools/" + a.school.id, {
            ...a.school,
            road: cards.A29["Nowa ulica"],
            building: cards.A29["Nowy numer budynku"],
        });
        assert.equal(s.road, "Parkowa");
        assert.equal(s.building, "14");
    });
    await run(a.key, "A30", async () => {
        const c = cards.A30;
        const school = await api(who, "POST", "/core/schools", {
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
        assert.equal(school.nameFull, c["Nazwa szkoły"]);
        assert.equal((await api(who, "GET", "/core/schools/" + school.id)).rspo, c["Numer RSPO"]);
    });
    await run(a.key, "A31", async () => {
        const f = new FormData();
        f.set(
            "File",
            new Blob([readFileSync(join(folder, cards.A31.Plik))], { type: "text/csv" }),
            basename(cards.A31.Plik)
        );
        assert.equal(await api(who, "POST", "/core/schools/import", f), 2);
    });
    await run(a.key, "A32", async () => {
        await api(who, "DELETE", "/account/sessions");
        await api(who, "GET", "/account/users/" + a.user.id, null, 401);
    });
}

// File GETs have binary responses, unlike the JSON wrapper used by api().
// Reuse the same session as the other requests from this participant.
async function checkFile(user, path, type, expectedStatus = 200) {
    const cookie = getSessionCookie(user.key);
    assert(cookie, "Participant session missing.");
    const response = await fetch(base + "/api/koala" + path, { headers: { Cookie: cookie } });
    recordRequest({ who: user.key, method: "GET", path, status: response.status });
    assert.equal(response.status, expectedStatus);
    if (expectedStatus === 200) {
        assert(response.headers.get("content-type")?.startsWith(type));
        assert((await response.arrayBuffer()).byteLength > 0);
    }
}

async function checkReviewer(r) {
    console.log(`Wszystkie karty recenzenta: ${r.key}.`);
    const who = r.user.key;
    await run(r.key, "R01", async () => {
        const user = await login(who, r.cards.R01["E-mail"], r.cards.R01["Hasło"]);
        assert.equal(user.id, r.user.id);
    });
    for (const [i, t] of r.tasks.entries())
        await run(r.key, `R0${i + 2}`, async () => {
            const assignments = await list(who, "/core/tasks/review/me");
            assert(assignments.some((x) => x.taskId === t.id && x.teamId === r.team.id));
            const editions = await list(
                who,
                "/core/editions?States=ACTIVE&States=INACTIVE&States=CLOSED"
            );
            assert(editions.some((x) => x.id === m.baseline.id));
            const stages = await list(who, `/core/editions/${m.baseline.id}/subeditions`);
            assert(stages.some((x) => x.id === r.stage.id));
            const tasks = await list(who, stageTasks(m.baseline.id, r.stage.id));
            assert(tasks.some((x) => x.id === t.id && x.contentJson));
            const reviewed = await api(
                who,
                "PUT",
                `/core/tasks/review/me/submission/${t.id}/team/${r.team.id}/subedition/${r.stage.id}`,
                { score: true }
            );
            assert.equal(reviewed.review1Score, true);
            assert(
                (await list(who, "/core/tasks/review/me")).some(
                    (x) => x.taskId === t.id && x.review1Score === true
                )
            );
        });
    await run(r.key, "R06", async () => {
        const conflicts = await list(who, "/core/tasks/review-conflicts/me/");
        assert(conflicts.some((x) => x.taskId === r.conflict.id));
        const score = await api(
            who,
            "PUT",
            `/core/tasks/review/me/submission/${r.conflict.id}/team/${r.team.id}/subedition/${r.stage.id}`,
            { score: true }
        );
        assert.equal(score.review1Score, score.review2Score);
        assert(
            !(await list(who, "/core/tasks/review-conflicts/me/")).some(
                (x) => x.taskId === r.conflict.id
            )
        );
    });
    await run(r.key, "R07", async () => {
        await api(who, "DELETE", "/account/sessions");
        await api(who, "GET", "/core/tasks/review/me", null, 401);
    });
}

async function checkEditor(e) {
    console.log(`Wszystkie karty edytora: ${e.key}.`);
    const who = e.user.key,
        c = e.cards;
    await run(e.key, "E01", async () => {
        const u = await login(who, c.E01["E-mail"], c.E01["Hasło"]);
        assert.equal(u.id, e.user.id);
    });
    await run(e.key, "E02", async () => {
        const image = await upload(
            basename(c.E02.Plik),
            readFileSync(join(folder, c.E02.Plik)),
            "image/png",
            who
        );
        assert((await list(who, "/cms/public-files")).some((f) => f.id === image.id));
        await checkFile(e.user, `/content/${image.id}.png`, "image/png");
    });
    await run(e.key, "E03", async () => {
        const card = c.E03;
        const p = await api(who, "POST", "/cms/posts", {
            name: card["Nazwa robocza"],
            editionId: m.baseline.id,
            contentJson: json({ version: "1", title_pl: card["Tytuł"], body_pl: card["Treść"] }),
            isVisible: true,
        });
        assert.equal(contentOf(p).body_pl, card["Treść"]);
        assert.equal(p.isVisible, true);
        assert((await list("anonymous", "/cms/posts?ShowHidden=false")).some((x) => x.id === p.id));
    });
    await run(e.key, "E04", async () => {
        const posts = await list(who, "/cms/posts");
        const p = posts.find((x) => x.id === e.post.id);
        assert(p && contentOf(p).body_pl.includes("15:00"));
        const content = contentOf(p);
        content.body_pl = content.body_pl.replace("15:00", "16:30");
        const updated = await api(who, "PUT", "/cms/posts/" + p.id, {
            ...p,
            contentJson: json(content),
        });
        assert.equal(contentOf(updated).body_pl, content.body_pl);
    });
    for (const [id, pageId] of [
        ["E05", m.rules.id],
        ["E06", m.history.id],
    ])
        await run(e.key, id, async () => {
            const pages = await list(who, "/cms/static-pages");
            const p = pages.find((x) => x.id === pageId);
            const content = contentOf(p);
            const section = c[id].Fragment;
            const begin = content.body_pl.indexOf("## " + section);
            assert(begin >= 0);
            const next = content.body_pl.indexOf("\n## ", begin + 3);
            const end = next < 0 ? content.body_pl.length : next;
            const oldSection = content.body_pl.slice(begin, end);
            const newSection =
                id === "E05"
                    ? oldSection.replace(
                          "Na pytania odpowiadamy w ciągu pięciu dni roboczych.",
                          c[id]["Nowa treść"]
                      )
                    : oldSection + "\n\n" + c[id]["Informacja do dopisania"];
            assert.notEqual(newSection, oldSection);
            content.body_pl =
                content.body_pl.slice(0, begin) + newSection + content.body_pl.slice(end);
            const updated = await api(who, "PUT", "/cms/static-pages/" + p.id, {
                contentJson: json(content),
                version: p.version,
            });
            assert.equal(contentOf(updated).body_pl, content.body_pl);
        });
    await run(e.key, "E07", async () => {
        const archives = await list(who, "/core/editions?States=CLOSED");
        assert(archives.some((x) => x.id === e.archive.id));
        const uploaded = await upload(
            basename(c.E07.Plik),
            readFileSync(join(folder, c.E07.Plik)),
            "application/pdf",
            who
        );
        const pages = await list(who, "/cms/static-pages");
        const p = pages.find((x) => x.id === m.problems.id);
        const content = contentOf(p);
        const metadata = JSON.parse(content.body_pl);
        metadata[e.archive.id][c.E07.Sekcja].push({
            id: uploaded.id,
            title: basename(c.E07.Plik),
            fileName: `${uploaded.id}.pdf`,
            url: `/api/koala/content/${uploaded.id}.pdf`,
        });
        content.body_pl = json(metadata);
        const updated = await api(who, "PUT", "/cms/static-pages/" + p.id, {
            contentJson: json(content),
            version: p.version,
        });
        assert.equal(contentOf(updated).body_pl, content.body_pl);
        await checkFile(e.user, `/content/${uploaded.id}.pdf`, "application/pdf");
    });
    await run(e.key, "E08", async () => {
        const pages = await list(who, "/cms/static-pages");
        const p = pages.find((x) => x.id === m.problems.id);
        const content = contentOf(p);
        const metadata = JSON.parse(content.body_pl);
        const files = metadata[e.archive.id][c.E08.Sekcja];
        assert(files.some((f) => f.id === e.removePdf.id));
        metadata[e.archive.id][c.E08.Sekcja] = files.filter((f) => f.id !== e.removePdf.id);
        content.body_pl = json(metadata);
        await api(who, "PUT", "/cms/static-pages/" + p.id, {
            contentJson: json(content),
            version: p.version,
        });
        await api(who, "DELETE", "/cms/public-files/" + e.removePdf.id);
        await checkFile(e.user, `/content/${e.removePdf.id}.pdf`, "application/pdf", 404);
    });
    await run(e.key, "E09", async () => {
        await api(who, "DELETE", "/cms/public-files/" + e.removeImage.id);
        assert(!(await list(who, "/cms/public-files")).some((x) => x.id === e.removeImage.id));
    });
    await run(e.key, "E10", async () => {
        const files = await list(who, "/cms/public-files");
        const logo = files.find((f) => f.name === "Ilustracja pokazowa.png");
        assert(logo);
        const p = await api(who, "POST", "/cms/sponsors", {
            name: c.E10["Nazwa sponsora"],
            contentJson: json({
                version: "1",
                body_pl: c.E10.Opis,
                sponsor_web_url: c.E10["Strona internetowa"],
                sponsor_logo_image_url: `/api/koala/content/${logo.id}.png`,
            }),
            isVisible: true,
        });
        assert((await list("anonymous", "/cms/sponsors")).some((x) => x.id === p.id));
    });
    await run(e.key, "E11", async () => {
        const sponsors = await list(who, "/cms/sponsors");
        const p = sponsors.find((x) => x.id === e.sponsor.id);
        assert(p);
        const updated = await api(who, "PUT", "/cms/sponsors/" + p.id, {
            ...p,
            contentJson: json({ ...contentOf(p), body_pl: c.E11["Nowy opis"] }),
            isVisible: false,
        });
        assert.equal(updated.isVisible, false);
        assert(!(await list("anonymous", "/cms/sponsors")).some((x) => x.id === p.id));
    });
    await run(e.key, "E12", async () => {
        const profiles = await list(who, "/cms/koalicjants");
        const p = profiles.find((x) => x.id === e.profile.id);
        assert(p);
        const updated = await api(who, "PUT", "/cms/koalicjants/" + p.id, {
            ...p,
            contentJson: json({ ...contentOf(p), body_pl: c.E12["Nowy opis"] }),
        });
        assert.equal(contentOf(updated).body_pl, c.E12["Nowy opis"]);
        assert(
            (await list("anonymous", "/cms/koalicjants")).some(
                (x) => x.id === p.id && contentOf(x).body_pl === c.E12["Nowy opis"]
            )
        );
    });
    await run(e.key, "E13", async () => {
        await api(who, "DELETE", "/account/sessions");
        await api(who, "GET", "/account/users/" + e.user.id, null, 401);
    });
}

if (recoverOnly) {
    await restore(JSON.parse(readFileSync(backupPath, "utf8")));
    console.log("Przywrócono zapisany stan bazy, plików i Redis.");
} else {
    let backup,
        restored = false;
    const mailBefore = await (await fetch("http://localhost:8025/api/v1/messages")).json();
    assert.equal(
        mailBefore.total,
        0,
        "Local Mailpit has messages: archive them before running this isolated test."
    );
    try {
        console.log("Zapis stanu bazy, plików i Redis przed weryfikacją.");
        backup = snapshot();
        for (const a of m.admins) await checkAdmin(a);
        for (const r of m.reviewers) await checkReviewer(r);
        for (const e of m.editors) await checkEditor(e);
    } finally {
        if (backup) {
            await restore(backup);
            restored = true;
            const cleared = await fetch("http://localhost:8025/api/v1/messages", {
                method: "DELETE",
            });
            assert(cleared.ok);
        }
        const report = { at: new Date().toISOString(), restored, results, requests };
        writeFileSync(reportPath, json(report) + "\n");
        const lines = [
            "# Weryfikacja kart danych przez API",
            "",
            `Przywrócono bazę i pliki: ${restored ? "TAK (zgodne sumy kontrolne)" : "NIE — sprawdź rollback.local.json"}.`,
            "",
            "To kontrola zapytań i skutków operacji backendu. Nie zastępuje sprawdzenia GUI ani obserwacji uczestników.",
            "",
            "| Wariant | Scenariusz | Wynik | Żądania | Uwagi |",
            "|---|---|---|---:|---|",
            ...results.map(
                (r) =>
                    `| ${r.variant} | ${r.id} | ${r.status} | ${r.requests.length} | ${(r.error ?? r.note ?? "").replaceAll("|", "/").replaceAll("\n", " ")} |`
            ),
        ];
        writeFileSync(reportMarkdown, lines.join("\n") + "\n");
    }
    const failed = results.filter((r) => r.status === "FAIL");
    console.log(
        `Karty: ${results.length}; OK: ${results.length - failed.length}; błędy: ${failed.length}; żądania API: ${requests.length}; przywrócenie danych: ${restored}.`
    );
    if (failed.length) process.exitCode = 1;
}
