import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ApiStaticPage } from "../types/models";
import { staticPageMarkdown } from "../utils/apiAdapters";
import { apiEndpoints } from "../utils/apiEndpoints";
import { apiRequestResult } from "../utils/apiFetcher";
import AdminHeader from "./AdminHeader";
import MarkdownEditor from "./MarkdownEditor";
import MarkdownRenderer from "./MarkdownRenderer";

interface StaticPageEditorProps {
    title: string;
    description: string;
    pageName: string;
}

type Feedback = { tone: "success" | "error"; message: string } | null;

const StaticPageEditor = ({ title, description, pageName }: StaticPageEditorProps) => {
    const navigate = useNavigate();
    const [markdownBody, setMarkdownBody] = useState("");
    const [page, setPage] = useState<ApiStaticPage | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;
        void apiRequestResult<ApiStaticPage[]>(
            apiEndpoints.staticPages,
            null,
            "GET",
            navigate
        ).then(({ data }) => {
            if (!active) return;
            const matchingPage = data?.find((item) => item.name === pageName);
            if (matchingPage) {
                setPage(matchingPage);
                setMarkdownBody(staticPageMarkdown(matchingPage));
            } else {
                setFeedback({
                    tone: "error",
                    message: "Nie udało się pobrać aktualnej treści strony.",
                });
            }
            setLoading(false);
        });

        return () => {
            active = false;
        };
    }, [navigate, pageName]);

    const save = async (content: string) => {
        setSaving(true);
        setFeedback(null);
        if (!page) {
            setFeedback({ tone: "error", message: "Nie znaleziono strony do zapisania." });
            setSaving(false);
            return;
        }
        const { data } = await apiRequestResult<ApiStaticPage>(
            `${apiEndpoints.staticPages}/${page.id}`,
            { contentJson: JSON.stringify({ markdownBody: content }), version: page.version },
            "PUT",
            navigate
        );

        if (data) setPage(data);

        setFeedback(
            data === null
                ? {
                      tone: "error",
                      message:
                          "Nie udało się zapisać treści. Wprowadzone zmiany pozostały w edytorze.",
                  }
                : { tone: "success", message: "Treść została zapisana." }
        );
        setSaving(false);
    };

    return (
        <div className="min-h-screen bg-canvas text-ink-heading">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-brand-text">
                        Strona statyczna
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">{title}</h1>
                    <p className="mt-2 max-w-3xl text-sm text-ink-secondary">{description}</p>
                </div>

                {feedback && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success" ? "app-status-success" : "app-status-danger"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-line bg-surface p-8 text-center text-sm text-ink-muted shadow-sm">
                        Ładowanie treści…
                    </div>
                ) : (
                    <div className="grid gap-8 xl:grid-cols-2">
                        <section className="min-w-0 rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6">
                            <h2 className="text-xl font-semibold">Edytor</h2>
                            <p className="mb-5 mt-1 text-sm text-ink-muted">
                                Zmiany są widoczne w podglądzie po prawej stronie.
                            </p>
                            <MarkdownEditor
                                initialValue={markdownBody}
                                onChange={setMarkdownBody}
                                onSave={save}
                                disabled={saving}
                                label={saving ? "Zapisywanie…" : "Zapisz zmiany"}
                            />
                        </section>

                        <section className="min-w-0 rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-6">
                            <div className="mb-5 border-b border-line-subtle pb-4">
                                <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">
                                    Podgląd strony
                                </p>
                                <h2 className="mt-1 text-xl font-semibold">{title}</h2>
                            </div>
                            {markdownBody.trim() ? (
                                <MarkdownRenderer content={markdownBody} />
                            ) : (
                                <p className="text-sm text-ink-subtle">
                                    Strona nie zawiera jeszcze treści.
                                </p>
                            )}
                        </section>
                    </div>
                )}
            </main>
        </div>
    );
};

export default StaticPageEditor;
