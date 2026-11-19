import { HttpResponse, http } from "msw";
import { describe, expect, it, vi } from "vitest";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult, apiUrl } from "../../../src/utils/apiFetcher";
import { server } from "../../mocks/server";

const editionsUrl = `${apiUrl}${apiEndpoints.editions}`;

describe("apiRequestResult", () => {
    it("unwraps API data envelopes and includes session credentials", async () => {
        const fetchSpy = vi.spyOn(globalThis, "fetch");
        server.use(
            http.get(editionsUrl, () => {
                return HttpResponse.json({ data: [{ id: "edition-1", name: "2026" }] });
            })
        );

        const result = await apiRequestResult<Array<{ id: string; name: string }>>(
            apiEndpoints.editions,
            null,
            "GET"
        );

        expect(result).toEqual({
            data: [{ id: "edition-1", name: "2026" }],
            status: 200,
        });
        expect(fetchSpy).toHaveBeenCalledWith(
            editionsUrl,
            expect.objectContaining({ credentials: "include", cache: "no-store" })
        );
    });

    it("serializes JSON bodies for modifying requests", async () => {
        const payload = { name: "Edycja testowa" };

        server.use(
            http.post(editionsUrl, async ({ request }) => {
                expect(request.headers.get("content-type")).toBe("application/json");
                expect(await request.json()).toEqual(payload);
                return HttpResponse.json(
                    { data: { id: "edition-2", ...payload } },
                    { status: 201 }
                );
            })
        );

        const result = await apiRequestResult<{ id: string; name: string }>(
            apiEndpoints.editions,
            payload,
            "POST"
        );

        expect(result.status).toBe(201);
        expect(result.data).toEqual({ id: "edition-2", name: "Edycja testowa" });
    });

    it("clears a stale session and redirects an unauthorized admin request", async () => {
        const navigate = vi.fn();
        localStorage.setItem("userId", "stale-user");
        localStorage.setItem("userRoles", JSON.stringify({ isAdmin: true }));
        window.history.replaceState({}, "", "/admin/posts");
        server.use(http.get(editionsUrl, () => new HttpResponse(null, { status: 401 })));

        const result = await apiRequestResult(apiEndpoints.editions, null, "GET", navigate);

        expect(result).toMatchObject({ data: null, status: 401 });
        expect(localStorage.getItem("userId")).toBeNull();
        expect(localStorage.getItem("userRoles")).toBeNull();
        expect(navigate).toHaveBeenCalledWith("/admin/login");
    });

    it("returns a null status when the network request fails", async () => {
        server.use(http.get(editionsUrl, () => HttpResponse.error()));

        await expect(apiRequestResult(apiEndpoints.editions, null, "GET")).resolves.toEqual({
            data: null,
            status: null,
        });
    });

    it("keeps a valid session on forbidden requests and preserves the server error", async () => {
        const navigate = vi.fn();
        localStorage.setItem("userId", "editor-1");
        window.history.replaceState({}, "", "/admin/posts");
        server.use(
            http.get(editionsUrl, () =>
                HttpResponse.json(
                    { errorCode: "KOALA_FORBIDDEN", detail: "Insufficient permissions" },
                    { status: 403 }
                )
            )
        );
        const result = await apiRequestResult(apiEndpoints.editions, null, "GET", navigate);
        expect(result.error).toEqual({
            code: "KOALA_FORBIDDEN",
            message: "Insufficient permissions",
        });
        expect(localStorage.getItem("userId")).toBe("editor-1");
        expect(navigate).not.toHaveBeenCalled();
    });

    it("loads subsequent pages while retaining repeated filters", async () => {
        server.use(
            http.get(editionsUrl, ({ request }) => {
                const url = new URL(request.url);
                expect(url.searchParams.getAll("States")).toEqual(["ACTIVE", "INACTIVE"]);
                const page = Number(url.searchParams.get("PageNumber"));
                return HttpResponse.json({
                    data: [{ id: `edition-${page}` }],
                    pagination: { pageNumber: page, pageSize: 1, totalNumber: 2 },
                });
            })
        );
        const result = await apiListRequestResult<{ id: string }>(
            `${apiEndpoints.editions}?PageSize=1&States=ACTIVE&States=INACTIVE`,
            null,
            "GET"
        );
        expect(result.data).toEqual([{ id: "edition-0" }, { id: "edition-1" }]);
    });

    it("does not report an incomplete list as success when a later page fails", async () => {
        server.use(
            http.get(editionsUrl, ({ request }) => {
                const page = Number(new URL(request.url).searchParams.get("PageNumber"));
                return page === 0
                    ? HttpResponse.json({
                          data: [{ id: "edition-0" }],
                          pagination: { pageNumber: 0, pageSize: 1, totalNumber: 2 },
                      })
                    : new HttpResponse(null, { status: 500 });
            })
        );
        const result = await apiListRequestResult(
            `${apiEndpoints.editions}?PageSize=1`,
            null,
            "GET"
        );
        expect(result).toMatchObject({ data: null, status: 500 });
    });
});
