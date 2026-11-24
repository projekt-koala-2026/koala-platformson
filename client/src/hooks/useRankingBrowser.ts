import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiEdition, ApiRanking, ApiRankingData } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";

export const rankingPageSize = 20;

export function useRankingBrowser(publicOnly = false) {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [rankings, setRankings] = useState<ApiRanking[]>([]);
    const [rankingId, setRankingId] = useState("");
    const [page, setPage] = useState(0);
    const [revision, setRevision] = useState(0);
    const [error, setError] = useState("");
    const [editionsLoaded, setEditionsLoaded] = useState(false);
    const [catalogKey, setCatalogKey] = useState("");
    const [data, setData] = useState({ key: "", rows: [] as ApiRankingData[], total: 0 });
    const key = `${editionId}/${rankingId}/${page}/${revision}`;

    useEffect(() => {
        let active = true;
        void apiListRequestResult<ApiEdition>(
            `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            setEditionsLoaded(true);
            if (!result.data) {
                setError("Nie udało się pobrać edycji.");
                return;
            }
            setEditions(result.data);
            setEditionId((current) =>
                result.data!.some((e) => e.id === current)
                    ? current
                    : (result.data!.find((e) => e.state === "ACTIVE")?.id ??
                      result.data![0]?.id ??
                      "")
            );
        });
        return () => {
            active = false;
        };
    }, [navigate, revision]);

    useEffect(() => {
        if (!editionId) return;
        let active = true;
        void apiRequestResult<ApiRanking[]>(
            apiEndpoints.editionRankings(editionId),
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            setCatalogKey(`${editionId}/${revision}`);
            if (!result.data) {
                setRankings([]);
                setRankingId("");
                setError("Nie udało się pobrać rankingów.");
                return;
            }
            const items = result.data
                .filter((r) => !publicOnly || r.isPublic)
                .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt));
            setRankings(items);
            setRankingId((current) =>
                items.some((r) => r.rankingId === current) ? current : (items[0]?.rankingId ?? "")
            );
        });
        return () => {
            active = false;
        };
    }, [editionId, revision, publicOnly, navigate]);

    useEffect(() => {
        if (!editionId || !rankingId || catalogKey !== `${editionId}/${revision}`) return;
        let active = true;
        void apiRequestResult<ApiRankingData[]>(
            `${apiEndpoints.ranking(editionId, rankingId)}?PageNumber=${page}&PageSize=${rankingPageSize}`,
            null,
            "GET",
            navigate
        ).then((result) => {
            if (!active) return;
            if (!result.data) {
                setError("Nie udało się pobrać wyników rankingu.");
                setData({ key, rows: [], total: 0 });
                return;
            }
            const total = result.pagination?.totalNumber ?? result.data.length;
            const last = Math.max(0, Math.ceil(total / rankingPageSize) - 1);
            if (page > last) {
                setPage(last);
                return;
            }
            setData({ key, rows: result.data, total });
        });
        return () => {
            active = false;
        };
    }, [editionId, rankingId, page, revision, navigate, key, catalogKey]);

    const chooseEdition = (id: string) => {
        setError("");
        setEditionId(id);
        setRankingId("");
        setRankings([]);
        setPage(0);
    };
    const chooseRanking = (id: string) => {
        setError("");
        setRankingId(id);
        setPage(0);
    };
    const refresh = () => {
        setError("");
        setRevision((value) => value + 1);
    };
    const catalogLoading = Boolean(editionId) && catalogKey !== `${editionId}/${revision}`;
    return {
        editions,
        editionId,
        chooseEdition,
        rankings,
        rankingId,
        chooseRanking,
        edition: editions.find((e) => e.id === editionId),
        ranking: rankings.find((r) => r.rankingId === rankingId),
        rows: data.key === key ? data.rows : [],
        total: data.key === key ? data.total : 0,
        loading: !editionsLoaded || catalogLoading || (Boolean(rankingId) && data.key !== key),
        page,
        setPage,
        refresh,
        error,
        setError,
    };
}
