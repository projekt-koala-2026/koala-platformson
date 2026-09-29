import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiStaticPage } from "../../types/models";
import { staticPageMarkdown } from "../../utils/apiAdapters";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiRequest } from "../../utils/apiFetcher";

const RuleScreen = () => {
    const navigate = useNavigate();
    const [rules, setRules] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void apiRequest<ApiStaticPage[]>(apiEndpoints.staticPages, null, "GET", navigate).then((data) => {
            if (!active) return;
            const page = data?.find((item) => item.name === "RULES_PAGE");
            if (page) setRules(staticPageMarkdown(page));
            else setHasError(true);
            setLoading(false);
        });
        return () => {
            active = false;
        };
    }, [navigate]);

    return (
        <div className="flex min-h-screen flex-col bg-slate-50">
            <PublicHeader navigate={navigate} />
            <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
                <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-8">
                    <p className="text-sm font-semibold uppercase tracking-wider text-emerald-700">
                        Dokumentacja konkursu
                    </p>
                    <h1 className="mt-2 text-3xl font-bold text-slate-900">Regulamin</h1>
                    <div className="mt-8">
                        {loading ? (
                            <p className="text-sm text-slate-500">Ładowanie regulaminu…</p>
                        ) : hasError ? (
                            <p
                                role="alert"
                                className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
                            >
                                Nie udało się pobrać regulaminu. Spróbuj ponownie później.
                            </p>
                        ) : rules.trim() ? (
                            <MarkdownRenderer content={rules} />
                        ) : (
                            <p className="text-sm text-slate-500">
                                Regulamin nie został jeszcze opublikowany.
                            </p>
                        )}
                    </div>
                </article>
            </main>
            <PublicFooter />
        </div>
    );
};

export default RuleScreen;
