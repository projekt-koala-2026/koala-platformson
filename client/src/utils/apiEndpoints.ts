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
    autoCheckProgress: (subeditionId: string) =>
        `${API_ROOT}/core/tasks/subedition/${subeditionId}/autocheck-progress`,
    autoCheck: (subeditionId: string) =>
        `${API_ROOT}/core/tasks/subedition/${subeditionId}/autocheck`,
    competitionTasks: (editionId: string, subeditionId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/tasks`,
    competitionTask: (editionId: string, subeditionId: string, taskId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/tasks/${taskId}`,
    activeTeamTasks: (editionId: string, subeditionId: string, teamId: string) =>
        `${API_ROOT}/core/editions/${editionId}/subeditions/${subeditionId}/task/active/${teamId}`,
    teamTaskSubmission: (taskId: string, teamId: string, subeditionId: string) =>
        `${API_ROOT}/core/tasks/${taskId}/team/${teamId}/subedition/${subeditionId}`,
    rankings: `${API_ROOT}/core/ranking`,
    editionRankings: (editionId: string) => `${API_ROOT}/core/ranking/edition/${editionId}`,
    ranking: (editionId: string, rankingId: string) =>
        `${API_ROOT}/core/ranking/edition/${editionId}/ranking/${rankingId}`,
    rankingSeries: (editionId: string) => `${API_ROOT}/core/editions/${editionId}/Etap1Series`,
    seriesRanking: (seriesId: string, rankingId: string) =>
        `${API_ROOT}/core/ranking/etap1series/${seriesId}/ranking/${rankingId}`,
    generateRanking: (editionId: string, stageId: string) =>
        `${API_ROOT}/core/ranking/edition/${editionId}/generete/${stageId}`,
    generateTotalRanking: (editionId: string) =>
        `${API_ROOT}/core/ranking/edition/${editionId}/etap1-total-ranking`,
    totalRankingPublication: (editionId: string, visible: boolean) =>
        `${API_ROOT}/core/ranking/edition/${editionId}/total-ranking-make-${visible ? "public" : "not-public"}`,
    seriesRankingPublication: (seriesId: string, visible: boolean) =>
        `${API_ROOT}/core/ranking/etap1series/${seriesId}/ranking-make-${visible ? "public" : "not-public"}`,
    rankingData: (rankingId: string, rowId: string) =>
        `${API_ROOT}/core/ranking/ranking/${rankingId}/ranking-data/${rowId}`,
    taskAnswers: (taskId: string) => `${API_ROOT}/core/tasks/${taskId}/corr`,
    taskSubmissions: (taskId: string) => `${API_ROOT}/core/tasks/submissions/${taskId}`,
    taskReviewProgress: (taskId: string) => `${API_ROOT}/core/tasks/${taskId}/review-progress`,
    taskPendingReviewers: (taskId: string) => `${API_ROOT}/core/tasks/${taskId}/pending-reviewers`,
    assignReviewers: (subeditionId: string, taskId: string) =>
        `${API_ROOT}/core/tasks/${subeditionId}/assign-reviewers/task/${taskId}`,
    updateAssignedReviewer: (subeditionId: string, taskId: string) =>
        `${API_ROOT}/core/tasks/${subeditionId}/updated-assign-reviewer/task/${taskId}`,
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
