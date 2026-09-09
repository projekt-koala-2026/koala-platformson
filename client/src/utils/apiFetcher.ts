import type { NavigateFunction } from "react-router-dom";
import type { ManagedFile } from "../types/models";

export const apiUrl =
    (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, "") ?? "";
export const resolveApiAssetUrl = (path?: string | null) => {
    if (!path || /^(https?:|data:|blob:)/i.test(path)) return path ?? "";
    return `${apiUrl}/${path.replace(/^\//, "")}`;
};
type HttpMethod = "GET" | "POST" | "PUT" | "DELETE" | "HEAD";
export interface ApiResult<T> {
    data: T | null;
    status: number | null;
}

const handleAuthFailure = (response: Response, navigate?: NavigateFunction) => {
    if (response.status === 401 || response.status === 403) {
        const path = window.location.pathname;
        if (response.status === 401) {
            localStorage.removeItem("userRoles");
            localStorage.removeItem("userId");
        }
        if (path.startsWith("/admin")) navigate?.("/admin/login");
        else navigate?.("/");
    }
};

const parseResponse = async <T>(response: Response): Promise<T> => {
    if (response.status === 204) return true as T;
    const contentType = response.headers.get("content-type");
    if (contentType?.includes("application/json")) {
        const json: unknown = await response.json();
        if (typeof json === "object" && json !== null && "data" in json)
            return (json as { data: T }).data;
        return json as T;
    }
    return ((await response.text()) || true) as T;
};

export const apiRequestResult = async <T = unknown>(
    url: string,
    options: unknown,
    method: HttpMethod,
    navigate?: NavigateFunction
): Promise<ApiResult<T>> => {
    try {
        const fetchConfig: RequestInit = {
            method: method,
            cache: method === "GET" || method === "HEAD" ? "no-store" : undefined,
            headers: {
                "Content-Type": "application/json",
            },
            credentials: "include",
        };

        if (options !== null && options !== undefined && method !== "GET" && method !== "HEAD") {
            fetchConfig.body = JSON.stringify(options);
        }

        const response = await fetch(apiUrl + url, fetchConfig);

        if (!response.ok) {
            handleAuthFailure(response, navigate);
            return { data: null, status: response.status };
        }
        return { data: await parseResponse<T>(response), status: response.status };
    } catch {
        return { data: null, status: null };
    }
};

// The API returns heterogeneous legacy response envelopes; callers may narrow this generic during the incremental API migration.
export const apiRequest = async <T = unknown>(
    url: string,
    options: unknown,
    method: HttpMethod,
    navigate?: NavigateFunction
): Promise<T | null> => (await apiRequestResult<T>(url, options, method, navigate)).data;

export const uploadFile = async (
    file: File,
    title: string,
    folder: string,
    navigate?: NavigateFunction
): Promise<ManagedFile | null> => {
    const formData = new FormData();
    formData.append("File", file);
    formData.append("Title", title);
    formData.append("Folder", folder);

    try {
        const response = await fetch(apiUrl + "/api/admin/file/public/file", {
            method: "POST",
            body: formData,
            credentials: "include",
        });
        if (!response.ok) {
            handleAuthFailure(response, navigate);
            return null;
        }
        return await parseResponse<ManagedFile>(response);
    } catch {
        return null;
    }
};

export const apiFormRequest = async <T = unknown>(
    url: string,
    formData: FormData,
    navigate?: NavigateFunction
): Promise<T | null> => {
    try {
        const response = await fetch(apiUrl + url, {
            method: "POST",
            body: formData,
            credentials: "include",
        });
        if (!response.ok) {
            handleAuthFailure(response, navigate);
            return null;
        }
        return await parseResponse<T>(response);
    } catch {
        return null;
    }
};
