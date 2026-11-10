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
    taskAnswers: (taskId: string) => `${API_ROOT}/core/tasks/${taskId}/corr`,
    taskSubmissions: (taskId: string) => `${API_ROOT}/core/tasks/submissions/${taskId}`,
    assignReviewers: (subeditionId: string) =>
        `${API_ROOT}/core/tasks/${subeditionId}/assign-reviewers`,
    reviewerSubmissions: `${API_ROOT}/core/tasks/review/me`,
    reviewerScore: (taskId: string, teamId: string, subeditionId: string) =>
        `${API_ROOT}/core/tasks/review/me/submission/${taskId}/team/${teamId}/subedition/${subeditionId}`,
    schools: `${API_ROOT}/core/schools`,
    posts: `${API_ROOT}/cms/posts`,
    sponsors: `${API_ROOT}/cms/sponsors`,
    koalicjants: `${API_ROOT}/cms/koalicjants`,
    staticPages: `${API_ROOT}/cms/static-pages`,
    publicFiles: `${API_ROOT}/cms/public-files`,
} as const;

export const firstPage = "PageNumber=0&PageSize=100";
