// Local development fixtures. Run from client/: node scripts/seed-ui-data.mjs
// Existing records are retained; named GUI fixtures are reused on subsequent runs.
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { mkdtempSync, rmdirSync, unlinkSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, posix } from "node:path";
import { deflateSync } from "node:zlib";

const base = (process.env.KOALA_API_URL ?? "http://localhost:8080").replace(/\/$/, "");
assert.ok(["localhost", "127.0.0.1"].includes(new URL(base).hostname), "Use a local backend.");
const password = "KoalaTest2026!";
const sessions = new Map();
const accounts = new Map();
const q = (value) => `'${String(value).replaceAll("'", "''")}'`;
const db = (sql) =>
    execFileSync(
        "docker",
        [
            "exec",
            "-i",
            "koala_postgres_container",
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
        { input: sql, encoding: "utf8" }
    ).trim();

async function api(who, method, path, data) {
    const form = data instanceof FormData;
    const response = await fetch(`${base}/api/koala${path}`, {
        method,
        headers: {
            ...(sessions.has(who) ? { Cookie: sessions.get(who) } : {}),
            ...(data != null && !form ? { "Content-Type": "application/json" } : {}),
        },
        body: data == null ? undefined : form ? data : JSON.stringify(data),
    });
    const cookies = response.headers.getSetCookie();
    if (cookies.length) sessions.set(who, cookies.map((cookie) => cookie.split(";")[0]).join("; "));
    const text = await response.text();
    let body;
    try {
        body = JSON.parse(text);
    } catch {
        body = text;
    }
    if (!response.ok)
        throw new Error(
            `${method} ${path.replace(/\/register\/.+/, "/register/[token]")}: ${response.status} ${text}`
        );
    return body?.data ?? body;
}

async function list(path, who = "admin") {
    const url = new URL(`${base}/api/koala${path}`);
    url.searchParams.set("PageSize", "100");
    const result = [];
    for (let page = 0; page < 100; page++) {
        url.searchParams.set("PageNumber", String(page));
        const rows = await api(who, "GET", url.pathname.replace("/api/koala", "") + url.search);
        assert.ok(Array.isArray(rows));
        result.push(...rows);
        if (rows.length < 100) return result;
    }
    throw new Error("Unexpected fixture list size.");
}

async function account(email, roles, nameFirst = "Test", nameLast = "Użytkownik") {
    if (accounts.has(email)) return accounts.get(email);
    const exists = db(`SELECT id FROM account.users WHERE email=${q(email)} AND verified=true;`);
    if (!exists) {
        await api(
            roles[0].startsWith("ORGANIZATION_") ? "admin" : "anonymous",
            "POST",
            "/account/users",
            { email, userRoles: roles }
        );
        const token = db(
            `SELECT l.token FROM account.links l JOIN account.users u ON u.id=l.user_id WHERE u.email=${q(email)} AND l.type='REGISTRATION' AND l.state='ACTIVE' ORDER BY l.created_at DESC LIMIT 1;`
        );
        assert.ok(token, `Missing registration token for ${email}`);
        await api("anonymous", "POST", `/account/links/register/${token}`, {
            nameFirst,
            nameLast,
            password,
            acceptedRodo: true,
            acceptedRules: true,
            declaredAtLeast13: false,
            declaredAtLeast18: true,
        });
    }
    const user = await api(email, "POST", "/account/sessions", { email, password });
    accounts.set(email, user);
    return user;
}

function png(width, height, variant) {
    const pixels = Buffer.alloc((width * 4 + 1) * height);
    const palettes = [
        [22, 163, 132],
        [59, 130, 246],
        [180, 83, 9],
        [124, 58, 237],
        [190, 24, 93],
        [15, 118, 110],
    ];
    const color = palettes[variant % palettes.length];
    const radius = Math.min(width, height) * 0.265;
    for (let y = 0; y < height; y++) {
        const offset = y * (width * 4 + 1);
        for (let x = 0; x < width; x++) {
            const i = offset + 1 + x * 4;
            const light = 0.45 + (x / width + y / height) * 0.32;
            const circle = (x - width * 0.5) ** 2 + (y - height * 0.47) ** 2 < radius ** 2;
            const stripe = y > height * 0.8 && x > width * 0.12 && x < width * 0.88;
            for (let c = 0; c < 3; c++)
                pixels[i + c] =
                    circle || stripe ? 235 : Math.min(255, Math.round(color[c] * light));
            pixels[i + 3] = variant === 11 && !circle && !stripe ? 0 : 255;
        }
    }
    const crc = (bytes) => {
        let value = 0xffffffff;
        for (const byte of bytes) {
            value ^= byte;
            for (let bit = 0; bit < 8; bit++) value = (value >>> 1) ^ (value & 1 ? 0xedb88320 : 0);
        }
        return (value ^ 0xffffffff) >>> 0;
    };
    const chunk = (type, data) => {
        const name = Buffer.from(type);
        const length = Buffer.alloc(4);
        length.writeUInt32BE(data.length);
        const checksum = Buffer.alloc(4);
        checksum.writeUInt32BE(crc(Buffer.concat([name, data])));
        return Buffer.concat([length, name, data, checksum]);
    };
    const header = Buffer.alloc(13);
    header.writeUInt32BE(width);
    header.writeUInt32BE(height, 4);
    header[8] = 8;
    header[9] = 6;
    return new Blob(
        [
            Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
            chunk("IHDR", header),
            chunk("IDAT", deflateSync(pixels)),
            chunk("IEND", Buffer.alloc(0)),
        ],
        { type: "image/png" }
    );
}

function pdf(label, pages = 2) {
    const objects = [
        "<< /Type /Catalog /Pages 2 0 R >>",
        "",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ];
    const kids = [];
    for (let page = 0; page < pages; page++) {
        const pageId = objects.length + 1;
        const streamId = pageId + 1;
        kids.push(`${pageId} 0 R`);
        objects.push(
            `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents ${streamId} 0 R >>`
        );
        const lines = [
            "KOALA - GUI TEST DOCUMENT",
            label,
            `Page ${page + 1} / ${pages}`,
            "",
            "Fictional fixture. Not a real consent or competition task.",
            "",
            ...Array.from(
                { length: 14 },
                (_, i) => `${i + 1}. Example content for preview, download and scaling checks.`
            ),
        ];
        const stream = `BT /F1 13 Tf 50 785 Td 22 TL ${lines.map((line, i) => `${i ? "T* " : ""}(${line.replace(/[()\\]/g, "\\$&")}) Tj`).join("\n")} ET`;
        objects.push(`<< /Length ${Buffer.byteLength(stream)} >>\nstream\n${stream}\nendstream`);
    }
    objects[1] = `<< /Type /Pages /Kids [${kids.join(" ")}] /Count ${pages} >>`;
    let document = "%PDF-1.4\n";
    const offsets = [0];
    objects.forEach((object, i) => {
        offsets.push(Buffer.byteLength(document));
        document += `${i + 1} 0 obj\n${object}\nendobj\n`;
    });
    const xref = Buffer.byteLength(document);
    document += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
    document += offsets
        .slice(1)
        .map((offset) => `${String(offset).padStart(10, "0")} 00000 n \n`)
        .join("");
    document += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`;
    return new Blob([document], { type: "application/pdf" });
}

async function upload(name, blob, filename, existing) {
    const found = existing.find((file) => file.name === name);
    if (found) return found;
    const data = new FormData();
    data.append("Name", name);
    data.append("File", blob, filename);
    const file = await api("admin", "POST", "/cms/public-files", data);
    existing.push(file);
    return file;
}

const longText =
    "To fikcyjna treść do sprawdzania zawijania tekstu, odstępów, wysokości kart i czytelności na telefonie, tablecie oraz dużym monitorze. W konkursie liczą się współpraca, logiczne myślenie i umiejętność spokojnego przedstawienia rozwiązania.";
const longWord = "BardzoDługiIdentyfikatorDokumentuDoSprawdzeniaZawijaniaBezSpacji".repeat(3);

async function main() {
    const admin = await api("admin", "POST", "/account/sessions", {
        email: "admin",
        password: "admin",
    });
    console.log("1/7 Konta i role");
    await account("admin@koala.test", ["ORGANIZATION_ADMIN"], "Adam", "Administrator");
    await account("edytor@koala.test", ["ORGANIZATION_EDITOR"], "Ewa", "Redaktorka");
    await account("recenzent@koala.test", ["ORGANIZATION_REVIEWER"], "Roman", "Recenzent");
    await account("kapitan.pusty@koala.test", ["TEAM_CAPTAIN"], "Kamil", "Bez Drużyny");
    await account("guardian.pusty@koala.test", ["TEAM_GUARDIAN"], "Olga", "Bez Drużyny");
    const staff = [];
    const firstNames = [
        "Aleksandra",
        "Michał",
        "Anna",
        "Stanisław",
        "Joanna",
        "Krzysztof",
        "Maria",
        "Zofia",
        "Grzegorz",
        "Małgorzata",
        "Łukasz",
        "Agnieszka",
    ];
    for (let i = 0; i < 12; i++) {
        const roles =
            i % 3 === 0
                ? ["ORGANIZATION_EDITOR", "ORGANIZATION_REVIEWER"]
                : [i % 2 ? "ORGANIZATION_EDITOR" : "ORGANIZATION_REVIEWER"];
        staff.push(
            await account(
                `gui.redakcja.${String(i + 1).padStart(2, "0")}@koala.test`,
                roles,
                firstNames[i],
                i % 3 ? "Testowa" : "Nowak-Kowalska-Wiśniewska"
            )
        );
    }
    staff.push(
        await account(
            "gui.bardzo.dlugi.adres.uzytkownika.do.testowania.zawijania.w.tabeli@koala.test",
            ["ORGANIZATION_ADMIN", "ORGANIZATION_EDITOR", "ORGANIZATION_REVIEWER"],
            "Aleksandra Magdalena",
            "Brzęczyszczykiewicz-Kowalska"
        )
    );

    console.log("2/7 Szkoły i edycje");
    let schools = await list("/core/schools");
    const schoolTypes = [
        "Szkoła podstawowa",
        "Liceum ogólnokształcące",
        "Technikum",
        "Ogólnokształcąca szkoła muzyczna I stopnia",
    ];
    const cities = [
        "Poznań",
        "Gniezno",
        "Leszno",
        "Kalisz",
        "Ostrów Wielkopolski",
        "Konin",
        "Nowy Tomyśl",
        "Szamotuły",
    ];
    const rows = [];
    for (let i = 0; i < 120; i++) {
        const rspo = String(880000000 + i);
        if (schools.some((school) => school.rspo === rspo)) continue;
        const name =
            i % 4 === 0
                ? `[GUI] Zespół Szkół Ogólnokształcących i Technicznych imienia Wybitnych Odkrywców Matematyki i Informatyki numer ${i + 1}`
                : `[GUI] ${schoolTypes[i % 4]} nr ${i + 1}`;
        rows.push(
            [
                rspo,
                name,
                "wielkopolskie",
                cities[i % cities.length],
                i % 3 ? "Szkolna" : "Aleja Zasłużonych Nauczycieli i Wychowawców",
                `${i + 1}A/12`,
                schoolTypes[i % 4],
                `gui.szkola.${i + 1}@koala.test`,
            ].join(";")
        );
    }
    if (rows.length) {
        const csv =
            "Numer RSPO;Nazwa;Województwo;Miejscowość;Ulica;Numer budynku;Typ;E-mail\n" +
            rows.join("\n") +
            "\n";
        const form = new FormData();
        form.append("File", new Blob([csv], { type: "text/csv" }), "gui-schools.csv");
        await api("admin", "POST", "/core/schools/import", form);
    }
    schools = (await list("/core/schools")).filter(
        (school) => Number(school.rspo) >= 880000000 && Number(school.rspo) < 880000120
    );
    assert.equal(schools.length, 120);
    let editions = await list("/core/editions");
    let active = editions.find((edition) => edition.state === "ACTIVE");
    const now = Date.now();
    const open = new Date(now - 30 * 86400000).toISOString();
    const close = new Date(now + 365 * 86400000).toISOString();
    if (!active) {
        active = await api("admin", "POST", "/core/editions", {
            name: "[GUI] KOALA 2026/2027",
            teamRegisterOpen: open,
            teamRegisterClose: close,
        });
        active = await api("admin", "PUT", `/core/editions/${active.id}/activate`);
    } else if (
        new Date(active.teamRegisterOpen).getTime() > now ||
        new Date(active.teamRegisterClose).getTime() < now
    ) {
        active = await api("admin", "PUT", `/core/editions/${active.id}`, {
            name: active.name,
            teamRegisterOpen: open,
            teamRegisterClose: close,
            version: active.version,
        });
    }
    const fixtureEditions = [active];
    for (const [name, state, start, end] of [
        ["[GUI] Archiwum konkursu 2024/2025", "CLOSED", "2024-09-01", "2025-06-30"],
        ["[GUI] Archiwum konkursu 2025/2026", "CLOSED", "2025-09-01", "2026-06-30"],
        ["[GUI] Przyszła edycja konkursu 2027/2028", "INACTIVE", "2027-09-01", "2028-06-30"],
    ]) {
        let edition = editions.find((item) => item.name === name);
        if (!edition)
            edition = await api("admin", "POST", "/core/editions", {
                name,
                teamRegisterOpen: `${start}T00:00:00Z`,
                teamRegisterClose: `${end}T23:59:59Z`,
            });
        // Historical fixtures cannot be activated/closed without interrupting the real active edition.
        db(`UPDATE core.editions SET state=${q(state)} WHERE id=${q(edition.id)};`);
        fixtureEditions.push({ ...edition, state });
    }

    console.log("3/7 Obrazy i prawdziwe pliki PDF");
    const existingFiles = await list("/cms/public-files");
    const imageSpecs = [
        [1440, 480, "panorama"],
        [480, 800, "portret"],
        [640, 640, "kwadrat"],
        [2000, 320, "szeroki-baner"],
        [300, 1000, "wysoka-ilustracja"],
        [960, 540, "pozioma-ilustracja"],
        [480, 160, "logo-szerokie"],
        [200, 400, "logo-pionowe"],
        [320, 320, "logo-kwadrat"],
        [800, 240, "logo-panoramiczne"],
        [120, 120, "mala-miniatura"],
        [500, 300, "logo-przezroczyste"],
    ];
    const images = [];
    for (let i = 0; i < imageSpecs.length; i++) {
        const [width, height, label] = imageSpecs[i];
        images.push(
            await upload(
                `[GUI] ${label} ${width}x${height}.png`,
                png(width, height, i),
                `gui-${label}.png`,
                existingFiles
            )
        );
    }
    const documents = [];
    const documentsByEdition = new Map();
    for (let editionIndex = 0; editionIndex < fixtureEditions.length; editionIndex++) {
        const editionFiles = [];
        for (let i = 0; i < 6; i++) {
            const suffix = editionIndex
                ? ` — ${fixtureEditions[editionIndex].name.replace("[GUI] ", "")}`
                : "";
            const name =
                i % 2
                    ? `[GUI] Zadania etapu szkolnego z bardzo długim opisem zakresu materiału oraz przykładowymi odpowiedziami ${i + 1}${suffix}.pdf`
                    : `[GUI] Zadania i rozwiązania ${i + 1}${suffix}.pdf`;
            const file = await upload(
                name,
                pdf(
                    `Example competition material ${i + 1}, edition ${editionIndex + 1}`,
                    (i % 3) + 1
                ),
                `gui-zadania-${editionIndex + 1}-${i + 1}.pdf`,
                existingFiles
            );
            documents.push(file);
            editionFiles.push(file);
        }
        documentsByEdition.set(fixtureEditions[editionIndex].id, editionFiles);
    }
    const asset = (file) => `/api/koala/content/${file.path}`;
    const absoluteAsset = (file) => base + asset(file);

    console.log("4/7 Wpisy, sponsorzy i koalicjanci");
    const existingPosts = await list("/cms/posts");
    const markdown = (index) =>
        `# Materiał testowy: konkurs KOALA\n\n${longText}\n\n![Panorama konkursowa](${asset(images[index % 6])})\n\n## Harmonogram i najważniejsze informacje\n\n${longText}\n\n- Krótka pozycja listy.\n- ${longText}\n- Sprawdź też **pogrubienie**, *kursywę* i [przykładowy odnośnik](https://example.test/informacje-o-konkursie).\n\n### Tabela wyników — przewijanie poziome\n\n| Drużyna | Szkoła i miejscowość | Kombinatoryka | Algorytmika | Logika | Razem |\n|:--|:--|--:|--:|--:|--:|\n| Dzielne Koale | Zespół Szkół Ogólnokształcących i Technicznych w Ostrowie Wielkopolskim | 28 | 25 | 30 | 83 |\n| Mistrzowie Logicznego Myślenia | Szkoła Podstawowa im. Marii Skłodowskiej-Curie | 27 | 26 | 29 | 82 |\n| Łamigłówkowicze | Liceum Ogólnokształcące w Poznaniu | 25 | 28 | 27 | 80 |\n\n> Przykładowy komunikat organizacyjny. ${longText}\n\n### Fragment kodu i długi identyfikator\n\n\`\`\`text\nfor each team in competition: calculate_combined_score(combinatorics, algorithms, logic, additional_long_column_for_horizontal_scrolling)\n\`\`\`\n\n${longWord}\n\n## Podsumowanie\n\n${longText}\n`;
    for (let i = 0; i < 24; i++) {
        const name =
            i % 5 === 0
                ? `[GUI] Ważne informacje organizacyjne dotyczące harmonogramu, rejestracji drużyn oraz przebiegu kolejnych etapów konkursu — komunikat ${i + 1}`
                : `[GUI] ${["Zapisy ruszyły", "Wyniki etapu szkolnego", "Materiały dla uczestników", "Spotkanie organizacyjne"][i % 4]} ${i + 1}`;
        const edition = i < 14 ? active : fixtureEditions[1 + (i % 3)];
        const content =
            i % 3 === 0
                ? markdown(i)
                : i % 3 === 1
                  ? `## Informacja\n\n${longText}\n\n${longText}\n\n![Ilustracja](${asset(images[i % 6])})`
                  : `Krótki komunikat: powodzenia wszystkim drużynom!\n\nTermin spotkania: **wtorek, godzina 10:00**.`;
        const previous = existingPosts.find((post) => post.name === name);
        const payload = {
            name,
            editionId: edition.id,
            contentJson: JSON.stringify({ markdownBody: content }),
            isVisible: i !== 5 && i !== 12 && i !== 22,
            version: previous?.version ?? 0,
        };
        const post = await api(
            "admin",
            previous ? "PUT" : "POST",
            `/cms/posts${previous ? `/${previous.id}` : ""}`,
            payload
        );
        db(`UPDATE cms.posts SET created_at=now()-interval '${i} days' WHERE id=${q(post.id)};`);
    }
    const existingSponsors = await list("/cms/sponsors");
    for (let i = 0; i < 10; i++) {
        const name =
            i % 3 === 0
                ? `[GUI] Fundacja Wspierania Edukacji Matematycznej i Rozwoju Młodych Talentów ${i + 1}`
                : `[GUI] Partner konkursu ${i + 1}`;
        const previous = existingSponsors.find((sponsor) => sponsor.name === name);
        const payload = {
            name,
            contentJson: JSON.stringify({
                websiteUrl: `https://example.test/partner-${i + 1}`,
                logoUrl: absoluteAsset(images[6 + (i % 6)]),
                description: i % 2 ? "Fikcyjny sponsor konkursu." : longText,
            }),
            isVisible: i < 8,
            version: previous?.version ?? 0,
        };
        await api(
            "admin",
            previous ? "PUT" : "POST",
            `/cms/sponsors${previous ? `/${previous.id}` : ""}`,
            payload
        );
    }
    for (let i = 0; i < 12; i++) {
        const email = `gui.koalicjant.${i + 1}@koala.test`;
        const content = JSON.stringify({
            profilePicture: asset(images[i % images.length]),
            description: i % 3 ? longText : `${longText} ${longText} ${longWord}`,
        });
        // No creation endpoint exists for koalicjants, so only their fixture rows are inserted directly.
        if (!db(`SELECT id FROM cms.koalicjants WHERE email=${q(email)};`)) {
            db(
                `INSERT INTO cms.koalicjants (id,user_id,name_first,name_last,email,content_json,updated_at,version,is_visiable) VALUES (${q(randomUUID())},${q(staff[i].id)},${q(firstNames[i])},${q(i % 3 ? "Testowa" : "Nowak-Kowalska-Wiśniewska")},${q(email)},${q(content)}::jsonb,now(),1,${i < 10});`
            );
        }
    }

    console.log("5/7 Regulamin, historia, sekcje dokumentów");
    const staticPages = await api("admin", "GET", "/cms/static-pages");
    const history = `# Konkurs KOALA na przestrzeni lat\n\n**To fikcyjne dane do testów interfejsu, nie oficjalna historia konkursu.**\n\n${longText}\n\n![Zdjęcie historyczne](${asset(images[0])})\n\n## Zestawienie przykładowych edycji\n\n| Edycja | Drużyny | Szkoły | Miasta | Uczestnicy | Etap szkolny | Finał |\n|:--|--:|--:|--:|--:|:--|:--|\n${Array.from({ length: 14 }, (_, i) => `| ${2012 + i}/${2013 + i} | ${40 + i * 5} | ${12 + i} | ${6 + i} | ${120 + i * 15} | Październik — grudzień | Maj — czerwiec |`).join("\n")}\n\n## Jak zmieniał się konkurs\n\n${longText}\n\n### Współpraca szkół\n\n${longText}\n\n> ${longText}\n\n![Wysoka ilustracja do testu proporcji](${asset(images[1])})\n\n## Wspomnienia uczestników\n\n${longText}\n\n${longText}`;
    const rules = `# Przykładowy regulamin konkursu KOALA\n\n**Materiał testowy GUI — nie jest oficjalnym regulaminem ani podstawą udzielania zgód.**\n\n${Array.from({ length: 9 }, (_, i) => `## § ${i + 1}. ${["Postanowienia ogólne", "Uczestnicy i drużyny", "Obowiązki kapitana", "Rola opiekuna", "Przebieg konkursu", "Ocena rozwiązań", "Wyniki i odwołania", "Dane osobowe", "Postanowienia końcowe"][i]}\n\n1. ${longText}\n2. ${longText}\n3. Każda drużyna powinna zapoznać się z terminami i opublikowanymi materiałami.\n`).join("\n")}\n## Załącznik — przykładowe terminy\n\n| Działanie | Odpowiedzialna osoba | Termin |\n|:--|:--|:--|\n| Zgłoszenie drużyny | Kapitan drużyny | Przed zamknięciem zapisów |\n| Przesłanie dokumentów | Opiekun drużyny | Zgodnie z harmonogramem |\n| Ocena prac | Zespół organizatorów i recenzentów | Po zakończeniu etapu |`;
    const problems = {};
    for (const edition of fixtureEditions) {
        problems[edition.id] = {};
        const sectionNames = [
            "Etap szkolny — zadania i odpowiedzi",
            "Finał konkursu oraz materiały przygotowawcze dla uczestników",
            "Dodatkowe materiały, instrukcje i załączniki organizacyjne",
        ];
        documentsByEdition.get(edition.id).forEach((file, i) => {
            const section = sectionNames[Math.floor(i / 2)];
            (problems[edition.id][section] ??= []).push({
                id: file.id,
                title: file.name,
                fileName: file.path,
                url: asset(file),
            });
        });
    }
    for (const [name, contentJson] of [
        ["RULES_PAGE", { markdownBody: rules }],
        ["HISTORY_PAGE", { markdownBody: history }],
        ["TASKS_PAGE", { markdownBody: JSON.stringify(problems) }],
    ]) {
        const page = staticPages.find((item) => item.name === name);
        assert.ok(page, `Missing ${name}`);
        await api("admin", "PUT", `/cms/static-pages/${page.id}`, {
            contentJson: JSON.stringify(contentJson),
            version: page.version,
        });
    }

    console.log("6/7 Drużyny, opiekunowie i wszystkie stany RODO");
    const guardianEmails = [
        "guardian.pelny@koala.test",
        "gui.guardian.02@koala.test",
        "gui.guardian.03@koala.test",
    ];
    for (const email of guardianEmails)
        await account(email, ["TEAM_GUARDIAN"], "Aleksandra", "Opiekunka-Wiśniewska");
    const rodoFile = pdf("FICTIONAL CONSENT - GUI FIXTURE", 2);
    for (let i = 0; i < 12; i++) {
        const email =
            i === 0
                ? "kapitan.pelny@koala.test"
                : i === 1
                  ? "kapitan.bez-zgod@koala.test"
                  : `gui.kapitan.${String(i + 1).padStart(2, "0")}@koala.test`;
        await account(
            email,
            ["TEAM_CAPTAIN"],
            i % 2 ? "Michał" : "Aleksandra Magdalena",
            "Brzęczyszczykiewicz-Kowalska"
        );
        let team = (await list("/account/teams/me", email)).find(
            (item) => item.editionId === active.id
        );
        if (!team)
            team = await api(email, "POST", "/account/teams", {
                name:
                    i % 3
                        ? `[GUI] Dzielne Koale ${i + 1}`
                        : `[GUI] Mistrzowie Kombinatoryki i Logiki ${i + 1}`,
                schoolId: schools[i].id,
            });
        if (i !== 2) {
            const names = [
                ["Aleksandra Magdalena", "Nowak-Kowalska-Wiśniewska"],
                ["Jan", "Brzęczyszczykiewicz"],
            ];
            if (team.teamMembers.length === 0) {
                for (const [nameFirst, nameLast] of names)
                    await api(email, "POST", `/account/teams/${team.id}/member`, {
                        nameFirst,
                        nameLast,
                        version: null,
                    });
            }
        }
        if (i !== 2 && !team.teamGuardian) {
            const code = await api(email, "POST", `/account/teams/${team.id}/new-join-code`);
            await api(guardianEmails[i % 3], "POST", `/account/teams/join/${code.joinCode}`);
        }
        // Fixture acceptance and long-lived join codes are data setup, not changes to application logic.
        db(
            `UPDATE account.teams SET name_accepted=${i % 2 === 0} WHERE id=${q(team.id)}; UPDATE account."TeamJoinCodes" SET expires_at=now()+interval '30 days' WHERE team_id=${q(team.id)};`
        );
        team = await api("admin", "GET", `/account/teams/${team.id}`);
        if (i !== 1 && i !== 2) {
            for (let j = 0; j < team.teamMembers.length; j++) {
                let member = team.teamMembers[j];
                if (member.rodoVersion === null) {
                    const form = new FormData();
                    form.append("File", rodoFile, "GUI-fikcyjna-zgoda-RODO.pdf");
                    member = await api(
                        guardianEmails[i % 3],
                        "POST",
                        `/account/teams/${team.id}/rodos/${member.teamMemberId}`,
                        form
                    );
                }
                const state = ["RODO_UNVERIFIED", "RODO_ACCEPTED", "RODO_REJECTED"][(i + j) % 3];
                if (member.rodoState !== state)
                    await api(
                        "admin",
                        "PUT",
                        `/account/teams/${team.id}/rodos/${member.teamMemberId}/state`,
                        { state, version: member.version }
                    );
            }
        }
    }

    console.log("7/7 Weryfikacja danych widocznych w API");
    const teams = await list("/account/teams");
    const fixtureTeams = teams.filter((team) => team.name.startsWith("[GUI]"));
    assert.ok(fixtureTeams.length >= 12);
    const states = Object.fromEntries(
        ["RODO_NONE", "RODO_UNVERIFIED", "RODO_ACCEPTED", "RODO_REJECTED"].map((state) => [
            state,
            fixtureTeams
                .flatMap((team) => team.teamMembers)
                .filter((member) => member.rodoState === state).length,
        ])
    );
    for (const count of Object.values(states)) assert.ok(count > 0);
    for (const file of [...images, ...documents]) {
        const response = await fetch(absoluteAsset(file));
        assert.equal(response.status, 200);
        const bytes = await response.arrayBuffer();
        assert.ok(bytes.byteLength > 0);
    }
    const koalicjants = await list("/cms/koalicjants");
    assert.ok(
        koalicjants.filter((person) => person.email.startsWith("gui.koalicjant.")).length >= 12
    );
    const summary = {
        schools: schools.length,
        editions: fixtureEditions.length,
        images: images.length,
        pdfs: documents.length,
        posts: 24,
        sponsors: 10,
        koalicjants: 12,
        teams: fixtureTeams.length,
        rodoStates: states,
        activeEdition: active.name,
    };
    console.log(JSON.stringify(summary, null, 2));
    console.log("Dane gotowe. Hasło kont testowych: " + password);
    assert.ok(admin.id);
}

async function refreshImages() {
    const environment = JSON.parse(
        execFileSync(
            "docker",
            ["inspect", "koala_backend_container", "--format", "{{json .Config.Env}}"],
            { encoding: "utf8" }
        )
    );
    const setting = environment.find((value) => value.startsWith("FileSettings__PublicFilesPath="));
    assert.ok(setting, "Missing public file directory configuration.");
    const directory = posix.resolve(setting.slice(setting.indexOf("=") + 1));
    assert.ok(directory.startsWith("/app/backend_data/"), "Unexpected public file directory.");
    const files = JSON.parse(
        db(
            "SELECT coalesce(json_agg(json_build_object('name',name,'path',path)), '[]'::json) FROM cms.public_files WHERE name LIKE '[GUI] %x%.png';"
        )
    );
    const temporaryDirectory = mkdtempSync(join(tmpdir(), "koala-gui-images-"));
    try {
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            assert.match(file.path, /^[a-f0-9-]+\.png$/i);
            const dimensions = file.name.match(/ (\d+)x(\d+)\.png$/);
            assert.ok(dimensions);
            const width = Number(dimensions[1]);
            const height = Number(dimensions[2]);
            assert.ok(width > 0 && width <= 2048 && height > 0 && height <= 2048);
            const blob = png(width, height, file.name.includes("przezroczyste") ? 11 : i % 11);
            const temporaryFile = join(temporaryDirectory, file.path);
            writeFileSync(temporaryFile, Buffer.from(await blob.arrayBuffer()));
            try {
                execFileSync("docker", [
                    "cp",
                    temporaryFile,
                    `koala_backend_container:${posix.join(directory, file.path)}`,
                ]);
            } finally {
                unlinkSync(temporaryFile);
            }
        }
    } finally {
        rmdirSync(temporaryDirectory);
    }
    console.log(`Odświeżono ${files.length} obrazów GUI; identyfikatory i odnośniki zachowane.`);
}

try {
    if (process.argv.includes("--refresh-images")) await refreshImages();
    else await main();
} finally {
    for (const who of sessions.keys()) {
        try {
            await api(who, "DELETE", "/account/sessions");
        } catch {
            /* Keep the original seed error. */
        }
    }
}
