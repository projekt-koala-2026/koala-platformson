import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiEdition, ApiRanking, ApiRankingData, ApiSubedition } from "../types/models";
import { apiEndpoints, firstPage } from "../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../utils/apiFetcher";

export const rankingPageSize = 20;

export function useRankingBrowser(publicOnly = false) {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [rankings, setRankings] = useState<ApiRanking[]>([]);
    const [series, setSeries] = useState<ApiSubedition[]>([]);
    const [seriesReady, setSeriesReady] = useState(false);
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
        void Promise.all([
            apiRequestResult<ApiRanking[]>(
                apiEndpoints.editionRankings(editionId),
                null,
                "GET",
                navigate
            ),
            publicOnly
                ? Promise.resolve(null)
                : apiListRequestResult<ApiSubedition>(
                      `${apiEndpoints.rankingSeries(editionId)}?${firstPage}`,
                      null,
                      "GET",
                      navigate
                  ),
        ]).then(([result, seriesResult]) => {
            if (!active) return;
            setCatalogKey(`${editionId}/${revision}`);
            setSeries(seriesResult?.data ?? []);
            setSeriesReady(Boolean(seriesResult?.data));
            if (!publicOnly && !seriesResult?.data)
                setError(
                    "Nie udało się pobrać serii I etapu. Generowanie wyników jest niedostępne."
                );
            if (!result.data) {
                setRankings([]);
                setRankingId("");
                setError("Nie udało się pobrać rankingów.");
                return;
            }
            const items = result.data
                .filter((r) => !publicOnly || r.isPublic)
                .sort(
                    (a, b) =>
                        Number(Boolean(a.etap1SeriesId)) - Number(Boolean(b.etap1SeriesId)) ||
                        a.name.localeCompare(b.name, "pl") ||
                        a.tier.localeCompare(b.tier)
                );
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
        const selected = rankings.find((r) => r.rankingId === rankingId);
        if (!selected) return;
        const url = selected.etap1SeriesId
            ? apiEndpoints.seriesRanking(selected.etap1SeriesId, rankingId)
            : apiEndpoints.ranking(editionId, rankingId);
        void apiRequestResult<ApiRankingData[]>(
            `${url}?PageNumber=${page}&PageSize=${rankingPageSize}`,
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
    }, [editionId, rankingId, page, revision, navigate, key, catalogKey, rankings]);

    const chooseEdition = (id: string) => {
        setError("");
        setEditionId(id);
        setRankingId("");
        setRankings([]);
        setSeries([]);
        setSeriesReady(false);
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
        series,
        seriesReady,
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
