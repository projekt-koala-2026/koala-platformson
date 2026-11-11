export type Role =
    | "ORGANIZATION_ADMIN"
    | "ORGANIZATION_EDITOR"
    | "ORGANIZATION_REVIEWER"
    | "TEAM_CAPTAIN"
    | "TEAM_GUARDIAN";

export interface User {
    id: string;
    email: string;
    roles: Role[];
    nameFirst?: string | null;
    nameLast?: string | null;
    censored?: boolean;
    createdAt: string;
    updatedAt: string;
    version: number;
}
export interface SessionUser {
    id: string;
    nameFirst?: string | null;
    nameLast?: string | null;
    email?: string;
    censored?: boolean;
    roles: Role[];
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface ApiEdition {
    id: string;
    name: string;
    state: "ACTIVE" | "INACTIVE" | "CLOSED" | string;
    teamRegisterOpen: string;
    teamRegisterClose: string;
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface ApiSubedition {
    id: string;
    editionId: string;
    name: string;
    answerOpen: string;
    answerClose: string;
    state: "ACTIVE" | "INACTIVE" | "CLOSED";
    version: number;
}

export interface ApiCompetitionTask {
    id: string;
    subEditionId: string;
    name: string;
    tier: "TIER_1" | "TIER_2";
    contentJson: string;
    version: number;
}

export interface ApiTaskAnswer {
    id: string;
    taskItemId: string;
    answer: string;
    version: number;
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
    teamMemberId: string;
    teamId: string;
    nameFist: string;
    nameLast: string;
    rodoState: RodoState;
    rodoVersion: number | null;
    rodoCreatedAt: string | null;
    rodoUpdatedAt: string | null;
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface ApiTeamJoinCode {
    joinCode: string;
    createdAt: string;
    updatedAt: string;
    expiresAt: string;
}

export type ApiTeamCaptain = {
    id: string;
    nameFirst: string;
    nameLast: string;
    email: string;
    censored: boolean;
    roles: Role[];
    createdAt: string;
    updatedAt: string;
    version: number;
};

export type ApiTeam = {
    id: string;
    editionId: string;
    schoolId: string;
    name: string;
    tier: string;
    isCensored: boolean;
    createdAt: string;
    updatedAt: string;
    version: number;
    teamCaptain: ApiTeamCaptain | null;
    teamGuardian: ApiTeamCaptain | null;
    teamMembers: ApiTeamMember[];
    joinCode: ApiTeamJoinCode | null;
};

export interface ApiTeamUser {
    id: string;
    nameFirst: string;
    nameLast: string;
    email: string;
    censored: boolean;
    roles: Role[];
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface ApiNamedTeamMember {
    teamMemberId: string;
    teamId: string;
    // Backend currently exposes the property with this typo.
    nameFist: string;
    nameLast: string;
    rodoState: RodoState;
    rodoVersion: number | null;
    rodoCreatedAt: string | null;
    rodoUpdatedAt: string | null;
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface ApiTeamDetails {
    id: string;
    editionId: string;
    schoolId: string;
    name: string;
    tier: string;
    isCensored: boolean;
    createdAt: string;
    updatedAt: string;
    version: number;
    teamCaptain: ApiTeamUser | null;
    teamGuardian: ApiTeamUser | null;
    teamMembers: ApiNamedTeamMember[];
    joinCode: ApiTeamJoinCode | null;
}

export type RodoState = "RODO_NONE" | "RODO_UNVERIFIED" | "RODO_ACCEPTED" | "RODO_REJECTED";

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
    tier: string;
    email: string;
    createdAt: string;
    updatedAt: string;
    version: number;
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
    state: ApiEdition["state"];
    version: number;
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
