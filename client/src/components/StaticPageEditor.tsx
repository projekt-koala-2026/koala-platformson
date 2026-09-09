import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { MarkdownStaticPage } from "../types/models";
import { apiRequest, apiRequestResult } from "../utils/apiFetcher";
import AdminHeader from "./AdminHeader";
import MarkdownEditor from "./MarkdownEditor";
import MarkdownRenderer from "./MarkdownRenderer";

interface StaticPageEditorProps {
    title: string;
    description: string;
    contentEndpoint: string;
    saveEndpoint: string;
}

type Feedback = { tone: "success" | "error"; message: string } | null;

const StaticPageEditor = ({
    title,
    description,
    contentEndpoint,
    saveEndpoint,
}: StaticPageEditorProps) => {
    const navigate = useNavigate();
    const [markdownBody, setMarkdownBody] = useState("");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [feedback, setFeedback] = useState<Feedback>(null);

    useEffect(() => {
        let active = true;
        void apiRequest<MarkdownStaticPage>(contentEndpoint, null, "GET", navigate).then((data) => {
            if (!active) return;
            if (data && typeof data.markdownBody === "string") {
                setMarkdownBody(data.markdownBody);
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
    }, [contentEndpoint, navigate]);

    const save = async (content: string) => {
        setSaving(true);
        setFeedback(null);
        const legacyPage: MarkdownStaticPage = { markdownBody: content };
        const { data } = await apiRequestResult<string>(
            saveEndpoint,
            { markdownBody: JSON.stringify(legacyPage) },
            "PUT",
            navigate
        );

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
        <div className="min-h-screen bg-slate-50 text-slate-900">
            <AdminHeader navigate={navigate} />
            <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="mb-8">
                    <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">
                        Strona statyczna
                    </p>
                    <h1 className="mt-1 text-3xl font-bold tracking-tight">{title}</h1>
                    <p className="mt-2 max-w-3xl text-sm text-slate-600">{description}</p>
                </div>

                {feedback && (
                    <div
                        role="status"
                        className={`mb-6 rounded-xl border px-4 py-3 text-sm ${
                            feedback.tone === "success"
                                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                                : "border-red-200 bg-red-50 text-red-800"
                        }`}
                    >
                        {feedback.message}
                    </div>
                )}

                {loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 shadow-sm">
                        Ładowanie treści…
                    </div>
                ) : (
                    <div className="grid gap-8 xl:grid-cols-2">
                        <section className="min-w-0 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                            <h2 className="text-xl font-semibold">Edytor</h2>
                            <p className="mb-5 mt-1 text-sm text-slate-500">
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

                        <section className="min-w-0 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
                            <div className="mb-5 border-b border-slate-100 pb-4">
                                <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                                    Podgląd strony
                                </p>
                                <h2 className="mt-1 text-xl font-semibold">{title}</h2>
                            </div>
                            {markdownBody.trim() ? (
                                <MarkdownRenderer content={markdownBody} />
                            ) : (
                                <p className="text-sm text-slate-400">
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
