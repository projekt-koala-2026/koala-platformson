import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import SelectListbox from "../../components/SelectListbox";
import TaskAssignmentStatus from "../../components/TaskAssignmentStatus";
import type { ApiCompetitionTask, ApiEdition, ApiSubedition, User } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult } from "../../utils/apiFetcher";

export default function AssignReviewersScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [tasks, setTasks] = useState<ApiCompetitionTask[]>([]);
    const [taskId, setTaskId] = useState("");
    const [reviewers, setReviewers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    useEffect(() => {
        let active = true;
        void Promise.all([
            apiListRequestResult<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE&States=CLOSED`,
                null,
                "GET",
                navigate
            ),
            apiListRequestResult<User>(
                `${apiEndpoints.users}?${firstPage}&UserRoles=ORGANIZATION_REVIEWER`,
                null,
                "GET",
                navigate
            ),
        ]).then(([e, r]) => {
            if (!active) return;
            if (!e.data || !r.data) setError("Nie udało się pobrać edycji lub recenzentów.");
            else {
                setEditions(e.data);
                setEditionId(
                    e.data.find((item) => item.state === "ACTIVE")?.id ?? e.data[0]?.id ?? ""
                );
                setReviewers(r.data);
            }
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);
    useEffect(() => {
        if (!editionId) return;
        let active = true;
        void apiListRequestResult<ApiSubedition>(
            `${apiEndpoints.subeditions(editionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) {
                // The API permits assigning reviewers only after the answer deadline.
                const eligible = data.filter((item) => {
                    const deadline = Date.parse(item.answerClose);
                    return Number.isFinite(deadline) && deadline < Date.now();
                });
                setSubeditions(eligible);
                setSubeditionId(eligible[0]?.id ?? "");
            } else setError("Nie udało się pobrać podedycji.");
        });
        return () => {
            active = false;
        };
    }, [editionId, navigate]);
    useEffect(() => {
        if (!editionId || !subeditionId) return;
        let active = true;
        void apiListRequestResult<ApiCompetitionTask>(
            `${apiEndpoints.competitionTasks(editionId, subeditionId)}?${firstPage}`,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            if (data) {
                setTasks(data);
                setTaskId(data[0]?.id ?? "");
            } else setError("Nie udało się pobrać zadań.");
        });
        return () => {
            active = false;
        };
    }, [editionId, subeditionId, navigate]);
    return (
        <div className="min-h-screen bg-canvas">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                    Ocenianie
                </p>
                <h1 className="mt-2 text-3xl font-bold text-ink-heading">Przydzielanie recenzji</h1>
                <p className="mt-3 text-ink-secondary">
                    Wybierz zadanie i utwórz pary recenzentów. Odpowiedzi zostaną rozdzielone
                    pomiędzy pary.
                </p>
                {error && (
                    <p role="alert" className="app-status-danger mt-6 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                {notice && (
                    <p role="status" className="app-status-success mt-6 rounded-xl border p-3">
                        {notice}
                    </p>
                )}
                {loading ? (
                    <p className="mt-8">Ładowanie…</p>
                ) : editions.length === 0 ? (
                    <p className="mt-8">Brak edycji.</p>
                ) : (
                    <>
                        <div className="mt-8 grid gap-4 rounded-2xl border border-line bg-surface p-5 md:grid-cols-3">
                            <div>
                                <span id="assign-edition" className="mb-2 block text-sm text-ink">
                                    Edycja
                                </span>
                                <SelectListbox
                                    ariaLabelledBy="assign-edition"
                                    value={editionId}
                                    options={editions.map((e) => ({ value: e.id, label: e.name }))}
                                    onChange={(id) => {
                                        if (id === editionId) return;
                                        setEditionId(id);
                                        setSubeditions([]);
                                        setSubeditionId("");
                                        setTasks([]);
                                        setTaskId("");
                                        setNotice("");
                                        setError("");
                                    }}
                                />
                            </div>
                            <div>
                                <span
                                    id="assign-subedition"
                                    className="mb-2 block text-sm text-ink"
                                >
                                    Podedycja
                                </span>
                                {subeditionId ? (
                                    <SelectListbox
                                        ariaLabelledBy="assign-subedition"
                                        value={subeditionId}
                                        options={subeditions.map((s) => ({
                                            value: s.id,
                                            label: s.name,
                                        }))}
                                        onChange={(id) => {
                                            if (id === subeditionId) return;
                                            setSubeditionId(id);
                                            setTasks([]);
                                            setTaskId("");
                                            setNotice("");
                                            setError("");
                                        }}
                                    />
                                ) : (
                                    <p className="py-3 text-sm text-ink-secondary">
                                        Brak podedycji po terminie odpowiadania.
                                    </p>
                                )}
                            </div>
                            <div>
                                <span id="assign-task" className="mb-2 block text-sm text-ink">
                                    Zadanie
                                </span>
                                {taskId ? (
                                    <SelectListbox
                                        ariaLabelledBy="assign-task"
                                        value={taskId}
                                        options={tasks.map((t) => ({ value: t.id, label: t.name }))}
                                        onChange={(id) => {
                                            setTaskId(id);
                                            setNotice("");
                                            setError("");
                                        }}
                                    />
                                ) : (
                                    <p className="py-3 text-ink-secondary">Brak zadań</p>
                                )}
                            </div>
                        </div>
                        <div className="mt-6 rounded-2xl border border-line bg-surface p-5">
                            <h2 className="text-lg font-semibold text-ink-heading">
                                Przydziały wybranego zadania
                            </h2>
                            {taskId && (
                                <TaskAssignmentStatus
                                    key={taskId}
                                    taskId={taskId}
                                    subeditionId={subeditionId}
                                    reviewers={reviewers}
                                />
                            )}
                        </div>
                    </>
                )}
            </main>
        </div>
    );
}
