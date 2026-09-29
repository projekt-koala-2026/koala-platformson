import type {
    ApiEdition,
    ApiKoalicjant,
    ApiPost,
    ApiPublicFile,
    ApiSponsor,
    ApiSchool,
    ApiStaticPage,
    Edition,
    Koalicjant,
    ManagedFile,
    ProblemsByEdition,
    Post,
    Sponsor,
    School,
} from "../types/models";

const parseContent = (contentJson: string): Record<string, unknown> => {
    try {
        const value: unknown = JSON.parse(contentJson);
        return typeof value === "object" && value !== null
            ? (value as Record<string, unknown>)
            : {};
    } catch {
        return {};
    }
};

const stringField = (value: unknown) => (typeof value === "string" ? value : "");

export const staticPageMarkdown = (page: ApiStaticPage | undefined) => {
    if (!page) return "";
    const content = parseContent(page.contentJson);
    return stringField(content.markdownBody) || stringField(content.content);
};

export const adaptPublicFile = (file: ApiPublicFile): ManagedFile => ({
    id: file.id,
    title: file.name,
    filePath: `/api/koala/content/${file.path}`,
});

export const staticPageProblems = (page: ApiStaticPage | undefined): ProblemsByEdition => {
    if (!page) return {};
    const content = parseContent(page.contentJson);
    const nested = content.problemsByEdition;
    if (typeof nested === "object" && nested !== null) return nested as ProblemsByEdition;
    const serialized = stringField(content.markdownBody) || stringField(content.content);
    if (!serialized) return {};
    try {
        const parsed: unknown = JSON.parse(serialized);
        return typeof parsed === "object" && parsed !== null ? (parsed as ProblemsByEdition) : {};
    } catch {
        return {};
    }
};

export const adaptEdition = (edition: ApiEdition): Edition => ({
    id: edition.id,
    title: edition.name,
    startDate: edition.createdAt,
    endDate: edition.expiredAt ?? "9999-12-31T23:59:59Z",
});

export const adaptSchool = (school: ApiSchool): School => ({
    id: school.id,
    rspo: Number(school.rspo),
    name: school.nameFull,
    nameShort: school.nameShort,
    state: school.state,
    city: school.city,
    type: school.type,
    addres: [school.road, school.building].filter(Boolean).join(" "),
});

export const adaptPost = (post: ApiPost): Post => {
    const content = parseContent(post.contentJson);
    return {
        id: post.id,
        editionId: post.editionId,
        title: post.name,
        markdownBody:
            stringField(content.markdownBody) || stringField(content.content) || post.contentJson,
        createdAt: post.createdAt,
    };
};

export const adaptSponsor = (sponsor: ApiSponsor): Sponsor => {
    const content = parseContent(sponsor.contentJson);
    return {
        id: sponsor.id,
        name: sponsor.name,
        websiteUrl: stringField(content.websiteUrl) || "#",
        logoUrl: stringField(content.logoUrl) || undefined,
        description: stringField(content.description) || undefined,
    };
};

export const adaptKoalicjant = (person: ApiKoalicjant): Koalicjant => {
    const content = parseContent(person.contentJson);
    return {
        id: person.id,
        name: `${person.nameFirst} ${person.nameLast}`.trim(),
        profilePicture: stringField(content.profilePicture),
        description: stringField(content.description) || stringField(content.markdownBody),
    };
};
