export type Role = "ADMIN" | "EDITOR" | "REVIEWER" | "GUARDIAN" | "CAPTAIN";

export interface User {
    id: string;
    email: string;
    roles: Role[];
}
export interface SessionUser {
    id: string;
    roles: Role[];
}
export interface Team {
    id: string;
    teamName?: string;
    name1?: string;
    name2?: string;
    name3?: string;
    name4?: string;
    schoolRSPO?: number;
}
export interface TeamPayload {
    id?: string;
    teamName: string;
    name1: string;
    name2: string;
    name3: string;
    name4: string;
    schoolRSPO: number;
}
export interface School {
    rspo: number;
    name: string;
    nameShort?: string;
    state: string;
    city: string;
    type: string;
    addres: string;
}
export interface Edition {
    id: string;
    title: string;
    startDate: string;
    endDate: string;
}
export interface Post {
    id: string;
    title: string;
    markdownBody: string;
    editionId: string;
    createdAt: string;
}
export interface Sponsor {
    id: string;
    name: string;
    websiteUrl: string;
    logoUrl?: string;
    description?: string;
}
export interface Koalicjant {
    id: string;
    name: string;
    profilePicture: string;
    description?: string;
}
export interface ManagedFile {
    id: string;
    title: string;
    filePath: string;
    url?: string;
}
export interface ProblemFile {
    id: string;
    title: string;
    fileName: string;
    url: string;
}
export type ProblemsByEdition = Record<string, Record<string, ProblemFile[]>>;
export interface MarkdownStaticPage {
    markdownBody: string;
}
export interface UserRoles {
    isAdmin: boolean;
    isEditor: boolean;
    isReviewer: boolean;
    isGuardian: boolean;
    isCaptain: boolean;
}
