import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import type { Edition, Post } from "../../types/models";
import { apiRequest, apiRequestResult } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";

const POSTS_ENDPOINT = "/api/admin/post";
const EDITIONS_ENDPOINT = "/api/edition";

type Feedback = { tone: "success" | "error"; message: string } | null;
type SidePanel = "preview" | "posts";

const emptyForm = { title: "", markdownBody: "", editionId: "" };

const pickDefaultEdition = (editions: Edition[]) => {
    const now = Date.now();
    return (
        editions.find((edition) => {
            const start = new Date(edition.startDate).getTime();
            const end = new Date(edition.endDate).getTime();
            return start <= now && now <= end;
        })?.id ??
        editions[0]?.id ??
        ""
    );
};

const EditPostScreen = () => {
    const navigate = useNavigate();
    const isAdminUser = useMemo(() => isAdmin(), []);
    const [posts, setPosts] = useState<Post[]>([]);
    const [editions, setEditions] = useState<Edition[]>([]);
    const [form, setForm] = useState(emptyForm);
    const [editingPostId, setEditingPostId] = useState<string | null>(null);
    const [editorRevision, setEditorRevision] = useState(0);
    const [sidePanel, setSidePanel] = useState<SidePanel>("preview");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;
        void Promise.all([
            apiRequest<Post[]>(POSTS_ENDPOINT, null, "GET", navigate),
            apiRequest<Edition[]>(EDITIONS_ENDPOINT, null, "GET", navigate),
        ]).then(([postsData, editionsData]) => {
            if (!active) return;
            setPosts(postsData ?? []);
            setEditions(editionsData ?? []);
            setForm((current) => ({
                ...current,
                editionId: current.editionId || pickDefaultEdition(editionsData ?? []),
            }));
            if (!postsData || !editionsData) {
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
        setEditingPostId(post.id);
        setForm({ title: post.title, markdownBody: post.markdownBody, editionId: post.editionId });
        setEditorRevision((revision) => revision + 1);
        setFeedback(null);
        setSidePanel("preview");
        window.scrollTo({ top: 0, behavior: "smooth" });
    };

    const savePost = async (markdownBody: string) => {
        const title = form.title.trim();
        if (title.length < 3 || title.length > 200) {
            setFeedback({ tone: "error", message: "Tytuł musi mieć od 3 do 200 znaków." });
            return;
        }
        if (!markdownBody.trim()) {
            setFeedback({ tone: "error", message: "Treść wpisu nie może być pusta." });
            return;
        }
        if (!form.editionId) {
            setFeedback({ tone: "error", message: "Wybierz edycję konkursu." });
            return;
        }

        setSaving(true);
        setFeedback(null);
        const payload = { title, markdownBody, editionId: form.editionId };
        const endpoint = editingPostId ? `${POSTS_ENDPOINT}/${editingPostId}` : POSTS_ENDPOINT;
        const method = editingPostId ? "PUT" : "POST";
        const { data } = await apiRequestResult<Post>(endpoint, payload, method, navigate);

        if (!data) {
            setFeedback({
                tone: "error",
                message: "Nie udało się zapisać wpisu. Formularz nie został wyczyszczony.",
            });
            setSaving(false);
            return;
        }

        setPosts((current) =>
            editingPostId
                ? current.map((post) => (post.id === editingPostId ? data : post))
                : [data, ...current]
        );
        setFeedback({
            tone: "success",
            message: editingPostId ? "Zmiany zostały zapisane." : "Wpis został utworzony.",
        });
        resetForm();
        setSaving(false);
    };

    const deletePost = async (post: Post) => {
        if (!window.confirm(`Czy na pewno chcesz usunąć wpis „${post.title}”?`)) return;
        setDeletingId(post.id);
        setFeedback(null);
        const { data } = await apiRequestResult(
            `${POSTS_ENDPOINT}/${post.id}`,
            null,
            "DELETE",
            navigate
        );
        if (data === null) {
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

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Treści
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">Zarządzanie wpisami</h1>
                    <p className="mt-2 max-w-3xl text-sm text-slate-600">
                        Twórz komunikaty przypisane do konkretnej edycji i sprawdzaj ich wygląd
                        przed publikacją.
                    </p>
                </div>

                {feedback && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${feedback.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-red-200 bg-red-50 text-red-800"}`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-slate-500 shadow-sm">
                        Ładowanie wpisów…
                    </div>
                ) : (
                    <div className="grid gap-8 xl:grid-cols-2">
                        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                            <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
                                <div>
                                    <h2 className="text-xl font-semibold">
                                        {editingPostId ? "Edytuj wpis" : "Dodaj wpis"}
                                    </h2>
                                    <p className="mt-1 text-sm text-slate-500">
                                        Pola oznaczone gwiazdką są wymagane.
                                    </p>
                                </div>
                                {editingPostId && (
                                    <Button
                                        text="Anuluj edycję"
                                        onClick={resetForm}
                                        className="bg-slate-600 hover:bg-slate-700"
                                    />
                                )}
                            </div>

                            <div className="space-y-5">
                                <label className="block text-sm font-medium text-slate-700">
                                    Tytuł *
                                    <input
                                        type="text"
                                        value={form.title}
                                        maxLength={200}
                                        onChange={(event) =>
                                            setForm((current) => ({
                                                ...current,
                                                title: event.target.value,
                                            }))
                                        }
                                        className="mt-2 w-full rounded-xl border border-slate-300 px-3 py-2.5 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                                        placeholder="Tytuł wpisu"
                                    />
                                </label>
                                <label className="block text-sm font-medium text-slate-700">
                                    Edycja *
                                    <select
                                        value={form.editionId}
                                        onChange={(event) =>
                                            setForm((current) => ({
                                                ...current,
                                                editionId: event.target.value,
                                            }))
                                        }
                                        className="app-select mt-2 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 shadow-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100"
                                    >
                                        <option value="">Wybierz edycję konkursu</option>
                                        {editions.map((edition) => (
                                            <option key={edition.id} value={edition.id}>
                                                {edition.title}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                                <div>
                                    <span className="mb-2 block text-sm font-medium text-slate-700">
                                        Treść *
                                    </span>
                                    <MarkdownEditor
                                        key={`${editingPostId ?? "new"}-${editorRevision}`}
                                        initialValue={form.markdownBody}
                                        onChange={(markdownBody) =>
                                            setForm((current) => ({ ...current, markdownBody }))
                                        }
                                        onSave={savePost}
                                        disabled={saving}
                                        label={
                                            saving
                                                ? "Zapisywanie…"
                                                : editingPostId
                                                  ? "Zapisz zmiany"
                                                  : "Utwórz wpis"
                                        }
                                    />
                                </div>
                            </div>
                        </section>

                        <section className="min-w-0">
                            <div className="mb-4 flex rounded-xl bg-slate-200 p-1">
                                {(["preview", "posts"] as SidePanel[]).map((panel) => (
                                    <button
                                        key={panel}
                                        type="button"
                                        onClick={() => setSidePanel(panel)}
                                        className={`flex-1 rounded-lg px-3 py-2 text-sm font-medium transition ${sidePanel === panel ? "bg-white text-slate-900 shadow-sm" : "text-slate-600 hover:text-slate-900"}`}
                                    >
                                        {panel === "preview"
                                            ? "Podgląd"
                                            : `Wpisy (${posts.length})`}
                                    </button>
                                ))}
                            </div>

                            {sidePanel === "preview" ? (
                                <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                                    <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                        {editionTitle(form.editionId)}
                                    </p>
                                    <h2 className="mt-2 break-words text-2xl font-bold">
                                        {form.title.trim() || "Tytuł wpisu"}
                                    </h2>
                                    <p className="mt-2 text-xs text-slate-500">
                                        {new Date().toLocaleString("pl-PL", {
                                            dateStyle: "long",
                                            timeStyle: "short",
                                        })}
                                    </p>
                                    <div className="mt-6 border-t border-slate-100 pt-5">
                                        {form.markdownBody.trim() ? (
                                            <MarkdownRenderer content={form.markdownBody} />
                                        ) : (
                                            <p className="text-sm text-slate-400">
                                                Podgląd treści pojawi się tutaj.
                                            </p>
                                        )}
                                    </div>
                                </article>
                            ) : posts.length === 0 ? (
                                <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
                                    Brak wpisów.
                                </div>
                            ) : (
                                <div className="space-y-4">
                                    {posts.map((post) => (
                                        <article
                                            key={post.id}
                                            className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                                        >
                                            <div className="flex items-start justify-between gap-4">
                                                <div className="min-w-0">
                                                    <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                                        {editionTitle(post.editionId)}
                                                    </p>
                                                    <h3 className="mt-1 break-words text-lg font-semibold">
                                                        {post.title}
                                                    </h3>
                                                    <p className="mt-1 text-xs text-slate-500">
                                                        {new Date(post.createdAt).toLocaleString(
                                                            "pl-PL"
                                                        )}
                                                    </p>
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
                                                            className="bg-red-600 px-3 hover:bg-red-700"
                                                        />
                                                    )}
                                                </div>
                                            </div>
                                            <div className="mt-4 max-h-48 overflow-hidden border-t border-slate-100 pt-4 text-sm">
                                                <MarkdownRenderer content={post.markdownBody} />
                                            </div>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    </div>
                )}
            </main>
        </div>
    );
};

export default EditPostScreen;
