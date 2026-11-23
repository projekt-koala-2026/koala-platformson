import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaSave, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import Modal from "../../components/Modal";
import RecordDates from "../../components/RecordDates";
import type { ApiEdition, ApiPost, Edition, Post } from "../../types/models";
import { adaptEdition, adaptPost } from "../../utils/apiAdapters";
import { apiEndpoints, firstPage } from "../../utils/apiEndpoints";
import { apiListRequest, apiRequestResult } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

const POSTS_ENDPOINT = apiEndpoints.posts;

type Feedback = { tone: "success" | "error"; message: string } | null;
type SidePanel = "preview" | "posts";

const emptyForm = { name: "", title: "", markdownBody: "", editionId: "", isVisible: false };

const pickDefaultEdition = (editions: Edition[]) => {
    return (
        editions.find((edition) => edition.state === "ACTIVE")?.id ??
        editions.find((edition) => edition.state !== "CLOSED")?.id ??
        ""
    );
};

const EditPostScreen = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [posts, setPosts] = useState<Post[]>([]);
    const [postNames, setPostNames] = useState<Record<string, string>>({});
    const [postVersions, setPostVersions] = useState<Record<string, number>>({});
    const [editions, setEditions] = useState<Edition[]>([]);
    const [form, setForm] = useState(emptyForm);
    const [editForm, setEditForm] = useState(emptyForm);
    const [initialEditForm, setInitialEditForm] = useState(emptyForm);
    const [editingPostId, setEditingPostId] = useState<string | null>(null);
    const [editorRevision, setEditorRevision] = useState(0);
    const [sidePanel, setSidePanel] = useState<SidePanel>("preview");
    const [expandedPosts, setExpandedPosts] = useState<Set<string>>(() => new Set());
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiListRequest<ApiPost>(`${POSTS_ENDPOINT}?${firstPage}`, null, "GET", navigate),
            apiListRequest<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}&States=CLOSED`,
                null,
                "GET",
                navigate
            ),
            apiListRequest<ApiEdition>(
                `${apiEndpoints.editions}?${firstPage}&States=ACTIVE&States=INACTIVE`,
                null,
                "GET",
                navigate
            ),
        ]).then(([postsData, pastEditionsData, activeEditionData]) => {
            if (!active) return;
            const editionsData = [...(activeEditionData ?? []), ...(pastEditionsData ?? [])].map(
                adaptEdition
            );
            setPosts((postsData ?? []).map(adaptPost));
            setPostNames(Object.fromEntries((postsData ?? []).map((post) => [post.id, post.name])));
            setPostVersions(
                Object.fromEntries((postsData ?? []).map((post) => [post.id, post.version]))
            );
            setEditions(editionsData);
            setForm((current) => ({
                ...current,
                editionId: current.editionId || pickDefaultEdition(editionsData ?? []),
            }));
            if (!postsData || !pastEditionsData || !activeEditionData) {
                setFeedback({ tone: "error", message: "Nie udało się pobrać wszystkich danych." });
            }
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    const resetForm = () => {
        setEditingPostId(null);
        setForm({ ...emptyForm, editionId: pickDefaultEdition(editions) });
        setEditorRevision((revision) => revision + 1);
    };

    const startEditing = (post: Post) => {
        const value = {
            name: postNames[post.id] ?? post.title,
            title: post.title,
            markdownBody: post.markdownBody,
            editionId: post.editionId,
            isVisible: post.isVisible,
        };
        setEditForm(value);
        setInitialEditForm(value);
        setEditingPostId(post.id);
        setFeedback(null);
    };
    const closeEditing = () => {
        if (saving) return;
        if (
            JSON.stringify(editForm) !== JSON.stringify(initialEditForm) &&
            !window.confirm("Porzucić niezapisane zmiany?")
        )
            return;
        setEditingPostId(null);
        setFeedback(null);
    };

    const savePost = async (markdownBody: string, editing = false) => {
        const value = editing ? editForm : form;
        const postId = editing ? editingPostId : null;
        const title = value.title.trim();
        if (title.length < 3 || title.length > 200) {
            setFeedback({ tone: "error", message: "Tytuł musi mieć od 3 do 200 znaków." });
            return;
        }
        if (title.length > 50 || !/^[\p{L}\s-]+$/u.test(title)) {
            setFeedback({
                tone: "error",
                message:
                    "Tytuł może mieć maksymalnie 50 znaków i zawierać tylko litery, spacje lub łączniki.",
            });
            return;
        }
        if (!markdownBody.trim()) {
            setFeedback({ tone: "error", message: "Treść wpisu nie może być pusta." });
            return;
        }
        if (!value.editionId) {
            setFeedback({ tone: "error", message: "Wybierz edycję konkursu." });
            return;
        }

        const suppliedName = value.name.trim();
        const name =
            suppliedName ||
            title
                .replace(/[^\p{L}\s-]/gu, " ")
                .replace(/\s+/g, " ")
                .trim()
                .slice(0, 50) ||
            "Wpis";
        if (suppliedName && (suppliedName.length > 50 || !/^[\p{L}\s-]+$/u.test(suppliedName))) {
            setFeedback({
                tone: "error",
                message:
                    "Nazwa robocza może mieć maksymalnie 50 znaków i zawierać tylko litery, spacje lub łączniki.",
            });
            return;
        }
        const edition = editions.find((item) => item.id === value.editionId);
        if (!edition || (!postId && edition.state === "CLOSED")) {
            setFeedback({
                tone: "error",
                message: "Wybierz edycję dostępną dla tego wpisu.",
            });
            return;
        }
        setSaving(true);
        setFeedback(null);
        const payload = {
            name,
            contentJson: JSON.stringify({ version: "1", title_pl: title, body_pl: markdownBody }),
            editionId: value.editionId,
            isVisible: edition.state === "INACTIVE" ? false : value.isVisible,
            ...(postId ? { version: postVersions[postId] ?? 0 } : {}),
        };
        const endpoint = postId ? `${POSTS_ENDPOINT}/${postId}` : POSTS_ENDPOINT;
        const method = postId ? "PUT" : "POST";
        const { data, error } = await apiRequestResult<ApiPost>(
            endpoint,
            payload,
            method,
            navigate
        );

        if (!data) {
            setFeedback({
                tone: "error",
                message:
                    error?.message ?? "Nie udało się zapisać wpisu. Formularz został zachowany.",
            });
            setSaving(false);
            return;
        }

        const adapted = adaptPost(data);
        setPostNames((current) => ({ ...current, [data.id]: data.name }));
        setPostVersions((current) => ({ ...current, [data.id]: data.version }));
        setPosts((current) =>
            postId
                ? current.map((post) => (post.id === postId ? adapted : post))
                : [adapted, ...current]
        );
        setFeedback({
            tone: "success",
            message: postId ? "Zmiany zostały zapisane." : "Wpis został utworzony.",
        });
        if (editing) setEditingPostId(null);
        else resetForm();
        setSaving(false);
    };

    const deletePost = async (post: Post) => {
        if (!window.confirm(`Czy na pewno chcesz usunąć wpis „${post.title}”?`)) return;
        setDeletingId(post.id);
        setFeedback(null);
        const { status } = await apiRequestResult(
            `${POSTS_ENDPOINT}/${post.id}`,
            null,
            "DELETE",
            navigate
        );
        if (status !== 200 && status !== 204) {
            setFeedback({ tone: "error", message: "Nie udało się usunąć wpisu." });
        } else {
            setPosts((current) => current.filter((item) => item.id !== post.id));
            if (editingPostId === post.id) resetForm();
            setFeedback({ tone: "success", message: "Wpis został usunięty." });
        }
        setDeletingId(null);
    };

    const editionTitle = (editionId: string) =>
        editions.find((edition) => edition.id === editionId)?.title ?? "Nieprzypisana";
    const postsNewestFirst = useMemo(
        () =>
            [...posts].sort((first, second) => {
                const difference = Date.parse(second.createdAt) - Date.parse(first.createdAt);
                return Number.isFinite(difference) ? difference : 0;
            }),
        [posts]
    );

    const renderFields = (
        value: typeof emptyForm,
        onChange: React.Dispatch<React.SetStateAction<typeof emptyForm>>,
        sourceId: string | null
    ) => (
        <fieldset disabled={saving} className="min-w-0">
            <div className="space-y-5">
                <label className="block text-sm font-medium text-ink">
                    Nazwa robocza (opcjonalna)
                    <input
                        type="text"
                        value={value.name}
                        maxLength={50}
                        onChange={(event) =>
                            onChange((current) => ({
                                ...current,
                                name: event.target.value,
                            }))
                        }
                        className="mt-2 w-full rounded-xl border border-line-strong px-3 py-2.5"
                    />
                    <span className="mt-1 block text-xs text-ink-muted">
                        Jeśli pozostawisz to pole puste, nazwa zostanie utworzona z tytułu wpisu.
                    </span>
                </label>
                <label className="block text-sm font-medium text-ink">
                    Tytuł *
                    <input
                        type="text"
                        value={value.title}
                        maxLength={200}
                        onChange={(event) =>
                            onChange((current) => ({
                                ...current,
                                title: event.target.value,
                            }))
                        }
                        className="mt-2 w-full rounded-xl border border-line-strong px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                        placeholder="Tytuł wpisu"
                    />
                </label>
                <label className="block text-sm font-medium text-ink">
                    Edycja *
                    <select
                        value={value.editionId}
                        onChange={(event) =>
                            onChange((current) => ({
                                ...current,
                                editionId: event.target.value,
                                isVisible:
                                    editions.find((edition) => edition.id === event.target.value)
                                        ?.state === "INACTIVE"
                                        ? false
                                        : current.isVisible,
                            }))
                        }
                        className="app-select mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                    >
                        <option value="">Wybierz edycję konkursu</option>
                        {editions
                            .filter(
                                (edition) =>
                                    edition.state !== "CLOSED" ||
                                    (sourceId &&
                                        posts.find((post) => post.id === sourceId)?.editionId ===
                                            edition.id)
                            )
                            .map((edition) => (
                                <option key={edition.id} value={edition.id}>
                                    {edition.title}
                                </option>
                            ))}
                    </select>
                </label>
                <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-line bg-canvas p-4">
                    <input
                        type="checkbox"
                        checked={
                            editions.find((edition) => edition.id === value.editionId)?.state !==
                                "INACTIVE" && value.isVisible
                        }
                        disabled={
                            editions.find((edition) => edition.id === value.editionId)?.state ===
                            "INACTIVE"
                        }
                        onChange={(event) =>
                            onChange((current) => ({
                                ...current,
                                isVisible: event.target.checked,
                            }))
                        }
                        className="mt-0.5 h-4 w-4 rounded border-line-strong text-brand-text focus:ring-brand-focus"
                    />
                    <span>
                        <span className="block text-sm font-medium text-ink">
                            Widoczny na stronie publicznej
                        </span>
                        <span className="mt-1 block text-xs leading-5 text-ink-muted">
                            Dla nieaktywnej edycji wpis pozostaje ukryty. W pozostałych przypadkach
                            możesz zmienić jego widoczność.
                        </span>
                    </span>
                </label>
                <div>
                    <span className="mb-2 block text-sm font-medium text-ink">Treść *</span>
                    <MarkdownEditor
                        key={`${sourceId ?? "new"}-${editorRevision}`}
                        initialValue={value.markdownBody}
                        onChange={(markdownBody) =>
                            onChange((current) => ({ ...current, markdownBody }))
                        }
                        disabled={saving}
                    />
                </div>
            </div>
        </fieldset>
    );

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="app-admin-page">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Treści
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">Zarządzanie wpisami</h1>
                    <p className="mt-2 max-w-3xl text-sm text-ink-secondary">
                        Twórz komunikaty przypisane do konkretnej edycji i sprawdzaj ich wygląd
                        przed publikacją.
                    </p>
                </div>

                {feedback && !editingPostId && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${feedback.tone === "success" ? "app-status-success" : "app-status-danger"}`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-line bg-surface p-8 text-center text-ink-muted shadow-sm">
                        Ładowanie wpisów…
                    </div>
                ) : (
                    <div className="grid items-start gap-8 xl:grid-cols-2">
                        <section className="min-w-0 rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6">
                            <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
                                <div>
                                    <h2 className="text-xl font-semibold">Dodaj wpis</h2>
                                    <p className="mt-1 text-sm text-ink-muted">
                                        Pola oznaczone gwiazdką są wymagane.
                                    </p>
                                </div>
                            </div>

                            <div className="space-y-5">
                                <label className="block text-sm font-medium text-ink">
                                    Tytuł *
                                    <input
                                        type="text"
                                        value={form.title}
                                        maxLength={50}
                                        onChange={(event) =>
                                            setForm((current) => ({
                                                ...current,
                                                title: event.target.value,
                                            }))
                                        }
                                        className="mt-2 w-full rounded-xl border border-line-strong px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                                        placeholder="Tytuł wpisu"
                                    />
                                </label>
                                <label className="block text-sm font-medium text-ink">
                                    Edycja *
                                    <select
                                        value={form.editionId}
                                        onChange={(event) =>
                                            setForm((current) => ({
                                                ...current,
                                                editionId: event.target.value,
                                            }))
                                        }
                                        className="app-select mt-2 w-full rounded-xl border border-line-strong bg-surface px-3 py-2.5 shadow-sm outline-none transition focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
                                    >
                                        <option value="">Wybierz edycję konkursu</option>
                                        {editions.map((edition) => (
                                            <option key={edition.id} value={edition.id}>
                                                {edition.title}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                                <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-line bg-canvas p-4">
                                    <input
                                        type="checkbox"
                                        checked={form.isVisible}
                                        onChange={(event) =>
                                            setForm((current) => ({
                                                ...current,
                                                isVisible: event.target.checked,
                                            }))
                                        }
                                        className="mt-0.5 h-4 w-4 rounded border-line-strong text-brand-text focus:ring-brand-focus"
                                    />
                                    <span>
                                        <span className="block text-sm font-medium text-ink">
                                            Widoczny na stronie publicznej
                                        </span>
                                        <span className="mt-1 block text-xs leading-5 text-ink-muted">
                                            Wyłącz, aby zachować wpis jako ukryty szkic.
                                        </span>
                                    </span>
                                </label>
                                <div>
                                    <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
                                        <span className="mb-2 block text-sm font-medium text-ink">
                                            Treść *
                                        </span>
                                        <Button
                                            text={saving ? "Zapisywanie…" : "Zapisz zmiany"}
                                            icon={<FaSave />}
                                            onClick={() => void savePost(form.markdownBody)}
                                            disabled={saving}
                                        />
                                    </div>
                                    <MarkdownEditor
                                        key={`${editingPostId ?? "new"}-${editorRevision}`}
                                        initialValue={form.markdownBody}
                                        onChange={(markdownBody) =>
                                            setForm((current) => ({ ...current, markdownBody }))
                                        }
                                        disabled={saving}
                                    />
                                </div>
                            </div>
                        </section>

                        <section className="min-w-0 xl:sticky xl:top-24">
                            <div className="mb-4 flex rounded-xl bg-surface-subtle p-1">
                                {(["preview", "posts"] as SidePanel[]).map((panel) => (
                                    <button
                                        key={panel}
                                        type="button"
                                        onClick={() => setSidePanel(panel)}
                                        className={`flex-1 rounded-lg px-3 py-2 text-sm font-medium transition ${sidePanel === panel ? "bg-surface text-ink-heading shadow-sm" : "text-ink-secondary hover:text-ink-heading"}`}
                                    >
                                        {panel === "preview"
                                            ? "Podgląd"
                                            : `Wpisy (${posts.length})`}
                                    </button>
                                ))}
                            </div>

                            {sidePanel === "preview" ? (
                                <article className="max-h-[70dvh] overflow-y-auto overscroll-contain rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6 xl:max-h-[calc(100dvh-10rem)]">
                                    <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                        {editionTitle(form.editionId)}
                                    </p>
                                    <h2 className="mt-2 break-words text-2xl font-bold">
                                        {form.title.trim() || "Tytuł wpisu"}
                                    </h2>
                                    <p className="mt-2 text-xs text-ink-muted">
                                        {new Date().toLocaleString("pl-PL", {
                                            dateStyle: "long",
                                            timeStyle: "short",
                                        })}
                                    </p>
                                    <div className="mt-6 border-t border-line-subtle pt-5">
                                        {form.markdownBody.trim() ? (
                                            <MarkdownRenderer content={form.markdownBody} />
                                        ) : (
                                            <p className="text-sm text-ink-subtle">
                                                Podgląd treści pojawi się tutaj.
                                            </p>
                                        )}
                                    </div>
                                </article>
                            ) : posts.length === 0 ? (
                                <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center text-sm text-ink-muted">
                                    Brak wpisów.
                                </div>
                            ) : (
                                <div
                                    role="region"
                                    aria-label="Lista wpisów"
                                    tabIndex={0}
                                    className="max-h-[70dvh] space-y-4 overflow-y-auto overscroll-contain rounded-2xl border border-line bg-canvas p-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus xl:max-h-[calc(100dvh-10rem)]"
                                >
                                    {postsNewestFirst.map((post) => (
                                        <article
                                            key={post.id}
                                            className="rounded-2xl border border-line bg-surface p-5 shadow-sm"
                                        >
                                            <div className="flex items-start justify-between gap-4">
                                                <div className="min-w-0">
                                                    <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                                        {editionTitle(post.editionId)}
                                                    </p>
                                                    <h3 className="mt-1 break-words text-lg font-semibold">
                                                        {post.title}
                                                    </h3>
                                                    <span
                                                        className={`mt-2 inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${post.isVisible ? "bg-brand-soft text-brand-text" : "bg-surface-muted text-ink-secondary"}`}
                                                    >
                                                        {post.isVisible ? "Widoczny" : "Ukryty"}
                                                    </span>
                                                    <RecordDates
                                                        createdAt={post.createdAt}
                                                        updatedAt={post.updatedAt}
                                                        className="mt-1"
                                                    />
                                                </div>
                                                <div className="flex shrink-0 gap-2">
                                                    <Button
                                                        text={
                                                            <>
                                                                <FaEdit />
                                                                <span className="sr-only">
                                                                    Edytuj
                                                                </span>
                                                            </>
                                                        }
                                                        onClick={() => startEditing(post)}
                                                        className="px-3"
                                                    />
                                                    {isAdminUser && (
                                                        <Button
                                                            text={
                                                                <>
                                                                    <FaTrash />
                                                                    <span className="sr-only">
                                                                        Usuń
                                                                    </span>
                                                                </>
                                                            }
                                                            disabled={deletingId === post.id}
                                                            onClick={() => void deletePost(post)}
                                                            className="bg-danger px-3 hover:bg-danger-hover"
                                                        />
                                                    )}
                                                </div>
                                            </div>
                                            <div className="mt-4 border-t border-line-subtle pt-4 text-sm">
                                                <div
                                                    id={`post-content-${post.id}`}
                                                    className={
                                                        expandedPosts.has(post.id)
                                                            ? ""
                                                            : "max-h-40 overflow-hidden"
                                                    }
                                                >
                                                    <MarkdownRenderer content={post.markdownBody} />
                                                </div>
                                                <button
                                                    type="button"
                                                    aria-expanded={expandedPosts.has(post.id)}
                                                    aria-controls={`post-content-${post.id}`}
                                                    onClick={() =>
                                                        setExpandedPosts((current) => {
                                                            const next = new Set(current);
                                                            if (next.has(post.id))
                                                                next.delete(post.id);
                                                            else next.add(post.id);
                                                            return next;
                                                        })
                                                    }
                                                    className="mt-3 rounded-lg px-2 py-1.5 text-sm font-medium text-brand-text hover:bg-brand-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
                                                >
                                                    {expandedPosts.has(post.id)
                                                        ? "Zwiń treść"
                                                        : "Rozwiń treść"}
                                                </button>
                                            </div>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    </div>
                )}
            </main>
            <Modal
                isOpen={editingPostId !== null}
                onClose={closeEditing}
                title="Edytuj wpis"
                maxWidth="xl"
                footer={
                    <div className="flex justify-end gap-3">
                        <button
                            type="button"
                            disabled={saving}
                            onClick={closeEditing}
                            className="rounded-xl px-4 py-2 text-ink-secondary hover:bg-surface-muted disabled:opacity-50"
                        >
                            Anuluj
                        </button>
                        <Button
                            text={saving ? "Zapisywanie…" : "Zapisz zmiany"}
                            disabled={saving}
                            onClick={() => void savePost(editForm.markdownBody, true)}
                        />
                    </div>
                }
            >
                {feedback?.tone === "error" && (
                    <p role="alert" className="app-status-danger mb-5 rounded-xl border p-3">
                        {feedback.message}
                    </p>
                )}
                {renderFields(editForm, setEditForm, editingPostId)}
            </Modal>
        </div>
    );
};

export default EditPostScreen;
