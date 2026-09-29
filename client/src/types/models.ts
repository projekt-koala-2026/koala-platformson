export type Role =
    | "ADMIN"
    | "EDITOR"
    | "REVIEWER"
    | "GUARDIAN"
    | "CAPTAIN"
    | "ORGANIZATION_ADMIN"
    | "ORGANIZATION_EDITOR"
    | "ORGANIZATION_REVIEWER"
    | "TEAM_ADMIN"
    | "TEAM_PLAYER";

export interface User {
    id: string;
    email: string;
    roles: Role[];
    nameFirst?: string | null;
    nameLast?: string | null;
    censored?: boolean;
}
export interface SessionUser {
    id: string;
    nameFirst?: string | null;
    nameLast?: string | null;
    email?: string;
    censored?: boolean;
    roles: Role[];
}

export interface ApiEdition {
    id: string;
    name: string;
    createdAt: string;
    expiredAt: string | null;
}

export interface ApiPost {
    id: string;
    editionId: string;
    name: string;
    contentJson: string;
    createdAt: string;
    updatedAt: string;
    isVisible: boolean;
    version: number;
}

export interface ApiSponsor {
    id: string;
    name: string;
    contentJson: string;
    isVisible: boolean;
    version: number;
}

export interface ApiKoalicjant {
    id: string;
    nameFirst: string;
    nameLast: string;
    email: string;
    contentJson: string;
    isVisible: boolean;
    version: number;
}

export interface ApiStaticPage {
    id: string;
    name: string;
    contentJson: string;
    updatedAt: string;
    version: number;
}

export interface ApiTeamMember {
    id: string;
    position: "CAPTAIN" | "ADMIN" | "PLAYER" | string;
}

export interface ApiTeamJoinCode {
    joinCode: string;
    createdAt: string;
    expiresAt: string;
}

export interface ApiTeam {
    id: string;
    editionId: string;
    schoolId: string;
    name: string;
    isCensored: boolean;
    createdAt: string;
    teamMembers: ApiTeamMember[];
    joinCode: ApiTeamJoinCode | null;
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
export interface ApiSchool {
    id: string;
    nameFull: string;
    nameShort: string;
    state: string;
    city: string;
    road: string;
    building: string;
    rspo: string;
    type: string;
    email: string;
    createdAt: string;
    updatedAt: string;
}
export interface School {
    id?: string;
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
    isVisible: boolean;
}
export interface Sponsor {
    id: string;
    name: string;
    websiteUrl: string;
    logoUrl?: string;
    description?: string;
    isVisible: boolean;
}
export interface Koalicjant {
    id: string;
    name: string;
    profilePicture: string;
    description?: string;
    isVisible: boolean;
}
export interface ManagedFile {
    id: string;
    title: string;
    filePath: string;
    url?: string;
}
export interface ApiPublicFile {
    id: string;
    name: string;
    path: string;
    type: string;
    createdAt: string;
    version: number;
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
