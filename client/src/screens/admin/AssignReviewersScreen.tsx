import { useEffect, useState } from "react";
import { FaPlus, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import Modal from "../../components/Modal";
import SelectListbox from "../../components/SelectListbox";
import TaskAssignmentStatus from "../../components/TaskAssignmentStatus";
import type { ApiCompetitionTask, ApiEdition, ApiSubedition, User } from "../../types/models";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequestResult, apiRequestResult } from "../../utils/apiFetcher";

type Pair = { first: string; second: string };
const name = (user: User) =>
    [user.nameFirst, user.nameLast].filter(Boolean).join(" ") || user.email;

export default function AssignReviewersScreen() {
    const navigate = useNavigate();
    const [editions, setEditions] = useState<ApiEdition[]>([]);
    const [editionId, setEditionId] = useState("");
    const [subeditions, setSubeditions] = useState<ApiSubedition[]>([]);
    const [subeditionId, setSubeditionId] = useState("");
    const [tasks, setTasks] = useState<ApiCompetitionTask[]>([]);
    const [taskId, setTaskId] = useState("");
    const [reviewers, setReviewers] = useState<User[]>([]);
    const [pairs, setPairs] = useState<Pair[]>([{ first: "", second: "" }]);
    const [modal, setModal] = useState(false);
    const [busy, setBusy] = useState(false);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [assignmentRevision, setAssignmentRevision] = useState(0);
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
    const save = async () => {
        if (!taskId || pairs.some((p) => !p.first || !p.second || p.first === p.second)) {
            setError("W każdej parze wybierz dwie różne osoby.");
            return;
        }
        const normalized = pairs.map((p) => [p.first, p.second].sort().join(":"));
        if (new Set(normalized).size !== normalized.length) {
            setError("Ta sama para została dodana więcej niż raz.");
            return;
        }
        setBusy(true);
        setError("");
        const result = await apiRequestResult(
            apiEndpoints.assignReviewers(subeditionId),
            {
                taskIds: [taskId],
                revierPairIds: pairs.map((p) => ({ reviewer1Id: p.first, reviewer2Id: p.second })),
            },
            "POST",
            navigate
        );
        if (result.status && result.status >= 200 && result.status < 300) {
            setModal(false);
            setNotice("Przydzielono nieprzypisane odpowiedzi do recenzji.");
            setAssignmentRevision((current) => current + 1);
        } else
            setError(
                "Nie udało się przydzielić odpowiedzi. Sprawdź termin, aktywację kont recenzentów i czy zadanie ma nieprzypisane odpowiedzi."
            );
        setBusy(false);
    };
    const options = [
        { value: "", label: "Wybierz recenzenta" },
        ...reviewers.map((u) => ({
            value: u.id,
            label: name(u) === u.email ? u.email : `${name(u)} (${u.email})`,
        })),
    ];
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
                {error && !modal && (
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
                                    key={`${taskId}-${assignmentRevision}`}
                                    taskId={taskId}
                                    reviewers={reviewers}
                                />
                            )}
                            <p className="text-sm text-ink-secondary mt-4">
                                Przydział jest możliwy po zakończeniu czasu odpowiedzi i obejmuje
                                tylko odpowiedzi, którym nie przydzielono jeszcze recenzentów.
                            </p>
                            <Button
                                className="mt-4"
                                text="Utwórz pary i przydziel"
                                icon={<FaPlus />}
                                disabled={!taskId || reviewers.length < 2}
                                onClick={() => {
                                    setPairs([{ first: "", second: "" }]);
                                    setError("");
                                    setModal(true);
                                }}
                            />
                            {reviewers.length < 2 && (
                                <p className="mt-2 text-sm text-ink-secondary">
                                    Potrzebne są co najmniej dwa konta recenzentów.
                                </p>
                            )}
                        </div>
                    </>
                )}
            </main>
            <Modal
                isOpen={modal}
                onClose={() => {
                    if (!busy) {
                        setModal(false);
                        setError("");
                    }
                }}
                title="Pary recenzentów"
                maxWidth="xl"
                footer={
                    <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between">
                        <Button
                            text="Dodaj parę"
                            icon={<FaPlus />}
                            disabled={busy}
                            onClick={() =>
                                setPairs((current) => [...current, { first: "", second: "" }])
                            }
                        />
                        <div className="flex flex-wrap justify-end gap-3">
                            <Button
                                text="Anuluj"
                                disabled={busy}
                                className="bg-surface-subtle text-ink hover:bg-surface-disabled"
                                onClick={() => {
                                    setModal(false);
                                    setError("");
                                }}
                            />
                            <Button
                                text={busy ? "Przydzielanie…" : "Przydziel odpowiedzi"}
                                disabled={busy}
                                onClick={() => void save()}
                            />
                        </div>
                    </div>
                }
            >
                <p className="text-sm text-ink-secondary">
                    Zadanie: {tasks.find((t) => t.id === taskId)?.name}. Parom przydzielane są
                    kolejne nieprzypisane odpowiedzi.
                </p>
                {error && (
                    <p role="alert" className="app-status-danger mt-4 rounded-xl border p-3">
                        {error}
                    </p>
                )}
                <div className="mt-4 space-y-4">
                    {pairs.map((pair, index) => (
                        <div key={index} className="rounded-xl border border-line p-4">
                            <div className="mb-3 flex items-center justify-between">
                                <h3 className="font-semibold text-ink-heading">Para {index + 1}</h3>
                                {pairs.length > 1 && (
                                    <Button
                                        text="Usuń parę"
                                        icon={<FaTrash />}
                                        className="bg-danger hover:bg-danger-hover"
                                        disabled={busy}
                                        onClick={() =>
                                            setPairs((current) =>
                                                current.filter((_, i) => i !== index)
                                            )
                                        }
                                    />
                                )}
                            </div>
                            <div className="grid gap-3 sm:grid-cols-2">
                                {(["first", "second"] as const).map((field, position) => (
                                    <div key={field}>
                                        <span
                                            id={`pair-${index}-${field}`}
                                            className="mb-2 block text-sm text-ink"
                                        >
                                            Recenzent {position + 1}
                                        </span>
                                        <SelectListbox
                                            inline
                                            ariaLabelledBy={`pair-${index}-${field}`}
                                            value={pair[field]}
                                            options={options.filter(
                                                (option) =>
                                                    !option.value ||
                                                    option.value !==
                                                        pair[field === "first" ? "second" : "first"]
                                            )}
                                            onChange={(id) =>
                                                setPairs((current) =>
                                                    current.map((entry, i) =>
                                                        i === index
                                                            ? { ...entry, [field]: id }
                                                            : entry
                                                    )
                                                )
                                            }
                                        />
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            </Modal>
        </div>
    );
}
