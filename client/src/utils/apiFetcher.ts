import type { NavigateFunction } from "react-router-dom";
import type { ApiPublicFile, ManagedFile } from "../types/models";
import { adaptPublicFile } from "./apiAdapters";
import { apiEndpoints } from "./apiEndpoints";

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
    error?: { code?: string; message: string };
    pagination?: { pageNumber: number; pageSize: number; totalNumber: number };
}

const parseError = async (response: Response): Promise<ApiResult<never>["error"]> => {
    try {
        const body = await response.json();
        return {
            code: body.errorCode ?? body.error?.code,
            message: body.detail ?? body.error?.message ?? body.title ?? `HTTP ${response.status}`,
        };
    } catch {
        return { message: `HTTP ${response.status}` };
    }
};

const handleAuthFailure = (response: Response, navigate?: NavigateFunction) => {
    if (response.status === 401) {
        const path = window.location.pathname;
        localStorage.removeItem("userRoles");
        localStorage.removeItem("userId");
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
        const isMultipart = options instanceof FormData;
        const fetchConfig: RequestInit = {
            method: method,
            cache: method === "GET" || method === "HEAD" ? "no-store" : undefined,
            headers: isMultipart ? undefined : { "Content-Type": "application/json" },
            credentials: "include",
        };

        if (options !== null && options !== undefined && method !== "GET" && method !== "HEAD") {
            fetchConfig.body = isMultipart ? options : JSON.stringify(options);
        }

        const response = await fetch(apiUrl + url, fetchConfig);

        if (!response.ok) {
            handleAuthFailure(response, navigate);
            return { data: null, status: response.status, error: await parseError(response) };
        }
        if (
            method === "GET" &&
            response.headers.get("content-type")?.includes("application/json")
        ) {
            const body = await response.json();
            if (typeof body === "object" && body !== null && "data" in body) {
                return {
                    data: body.data,
                    status: response.status,
                    ...(body.pagination ? { pagination: body.pagination } : {}),
                };
            }
            return { data: body, status: response.status };
        }
        return { data: await parseResponse<T>(response), status: response.status };
    } catch {
        return { data: null, status: null };
    }
};

// Fetch all pages for screens that display a complete list or filter it locally.
export const apiListRequestResult = async <T>(
    url: string,
    _options: null,
    _method: "GET",
    navigate?: NavigateFunction
): Promise<ApiResult<T[]>> => {
    const requestUrl = new URL(url, window.location.origin);
    requestUrl.searchParams.set("PageNumber", "0");
    if (!requestUrl.searchParams.has("PageSize")) requestUrl.searchParams.set("PageSize", "100");
    const items: T[] = [];
    for (let page = 0; page < 1000; page += 1) {
        requestUrl.searchParams.set("PageNumber", String(page));
        const result = await apiRequestResult<T[]>(
            `${requestUrl.pathname}${requestUrl.search}`,
            null,
            "GET",
            navigate
        );
        if (!result.data) return result;
        items.push(...result.data);
        if (!result.pagination || items.length >= result.pagination.totalNumber) {
            return { data: items, status: result.status };
        }
        if (result.data.length === 0) break;
    }
    return {
        data: null,
        status: null,
        error: { message: "Nie udało się pobrać kompletnej listy." },
    };
};

export const apiListRequest = async <T>(
    url: string,
    options: null,
    method: "GET",
    navigate?: NavigateFunction
): Promise<T[] | null> => (await apiListRequestResult<T>(url, options, method, navigate)).data;

// Successful responses use the shared API envelope; the helper returns its data field.
export const apiRequest = async <T = unknown>(
    url: string,
    options: unknown,
    method: HttpMethod,
    navigate?: NavigateFunction
): Promise<T | null> => (await apiRequestResult<T>(url, options, method, navigate)).data;

export const uploadFile = async (
    file: File,
    title: string,
    _folder: string,
    navigate?: NavigateFunction
): Promise<ManagedFile | null> => {
    const formData = new FormData();
    formData.append("File", file);
    formData.append("Name", title);

    try {
        const response = await fetch(apiUrl + apiEndpoints.publicFiles, {
            method: "POST",
            body: formData,
            credentials: "include",
        });
        if (!response.ok) {
            handleAuthFailure(response, navigate);
            return null;
        }
        return adaptPublicFile(await parseResponse<ApiPublicFile>(response));
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
