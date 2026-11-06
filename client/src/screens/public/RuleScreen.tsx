import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import koalaImage from "../../assets/mascots/koala_mis2a.png";
import KoalaIllustration from "../../components/KoalaIllustration";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import PublicFooter from "../../components/PublicFooter";
import PublicHeader from "../../components/PublicHeader";
import type { ApiStaticPage } from "../../types/models";
import { staticPageMarkdown } from "../../utils/apiAdapters";
import { apiEndpoints } from "../../utils/apiEndpoints";
import { apiListRequest } from "../../utils/apiFetcher";

const RuleScreen = () => {
    const navigate = useNavigate();
    const [rules, setRules] = useState("");
    const [loading, setLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        let active = true;
        void apiListRequest<ApiStaticPage>(apiEndpoints.staticPages, null, "GET", navigate).then(
            (data) => {
                if (!active) return;
                const page = data?.find((item) => item.name === "RULES_PAGE");
                if (page) setRules(staticPageMarkdown(page));
                else setHasError(true);
                setLoading(false);
            }
        );
        return () => {
            active = false;
        };
    }, [navigate]);

    return (
        <div className="flex min-h-screen flex-col bg-canvas">
            <PublicHeader navigate={navigate} />
            <main className="app-public-page">
                <header className="mb-8 flex items-center justify-between gap-4 sm:gap-8">
                    <div className="min-w-0">
                        <p className="text-sm font-semibold uppercase tracking-wider text-brand-text">
                            Dokumentacja konkursu
                        </p>
                        <h1 className="mt-2 text-3xl font-bold text-ink-heading">Regulamin</h1>
                        <p className="mt-3 max-w-2xl text-ink-secondary">
                            Zasady uczestnictwa, przebieg konkursu i sposób oceniania drużyn.
                        </p>
                    </div>
                    <KoalaIllustration src={koalaImage} />
                </header>
                <article className="rounded-2xl border border-line bg-surface p-5 shadow-sm sm:p-8">
                    <div>
                        {loading ? (
                            <p className="text-sm text-ink-muted">Ładowanie regulaminu…</p>
                        ) : hasError ? (
                            <p
                                role="alert"
                                className="rounded-xl border p-4 text-sm app-status-danger"
                            >
                                Nie udało się pobrać regulaminu. Spróbuj ponownie później.
                            </p>
                        ) : rules.trim() ? (
                            <MarkdownRenderer content={rules} />
                        ) : (
                            <p className="text-sm text-ink-muted">
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
