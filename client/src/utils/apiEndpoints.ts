const API_ROOT = "/api/koala";

export const apiEndpoints = {
    sessions: `${API_ROOT}/account/sessions`,
    links: `${API_ROOT}/account/links`,
    users: `${API_ROOT}/account/users`,
    teams: `${API_ROOT}/account/teams`,
    teamRodos: (teamId: string) => `${API_ROOT}/account/teams/${teamId}/rodos`,
    editions: `${API_ROOT}/core/editions`,
    activeEdition: `${API_ROOT}/core/editions/active-edition`,
    subeditions: (editionId: string) => `${API_ROOT}/core/editions/${editionId}/subeditions`,
    subedition: (editionId: string, id: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${id}`,
    subeditionAction: (editionId: string, id: string, action: "activate" | "close") =>
        `${API_ROOT}/core/editions/${editionId}/${action}_subedition/${id}`,
    competitionTasks: (editionId: string, subeditionId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/tasks`,
    competitionTask: (editionId: string, subeditionId: string, taskId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/tasks/${taskId}`,
    activeTeamTasks: (editionId: string, subeditionId: string, teamId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/task/active/${teamId}`,
    teamTaskSubmission: (taskId: string, teamId: string, subeditionId: string) =>
        `${API_ROOT}/core/tasks/${taskId}/team/${teamId}/subedition/${subeditionId}`,
    rankings: `${API_ROOT}/core/ranking`,
    rankingDraft: (subeditionId: string) =>
        `${API_ROOT}/core/ranking/subedition/${subeditionId}/draft`,
    generateRanking: (subeditionId: string) =>
        `${API_ROOT}/core/ranking/generate/subedition/${subeditionId}`,
    publishRanking: (subeditionId: string) =>
        `${API_ROOT}/core/ranking/subedition/${subeditionId}/publish`,
    taskAnswers: (taskId: string) => `${API_ROOT}/core/tasks/${taskId}/corr`,
    schools: `${API_ROOT}/core/schools`,
    posts: `${API_ROOT}/cms/posts`,
    sponsors: `${API_ROOT}/cms/sponsors`,
    koalicjants: `${API_ROOT}/cms/koalicjants`,
    staticPages: `${API_ROOT}/cms/static-pages`,
    publicFiles: `${API_ROOT}/cms/public-files`,
} as const;

export const firstPage = "PageNumber=0&PageSize=100";
