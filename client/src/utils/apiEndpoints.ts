const API_ROOT = "/api/koala";

export const apiEndpoints = {
    sessions: `${API_ROOT}/account/sessions`,
    links: `${API_ROOT}/account/links`,
    users: `${API_ROOT}/account/users`,
    teams: `${API_ROOT}/account/teams`,
    editions: `${API_ROOT}/core/editions`,
    activeEdition: `${API_ROOT}/core/editions/active-edition`,
    schools: `${API_ROOT}/core/schools`,
    posts: `${API_ROOT}/cms/posts`,
    sponsors: `${API_ROOT}/cms/sponsors`,
    koalicjants: `${API_ROOT}/cms/koalicjants`,
    staticPages: `${API_ROOT}/cms/static-pages`,
    publicFiles: `${API_ROOT}/cms/public-files`,
} as const;

export const firstPage = "PageNumber=0&PageSize=100";
