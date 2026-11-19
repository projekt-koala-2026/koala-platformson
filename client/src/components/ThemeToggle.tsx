import { useEffect, useState } from "react";
import { FaMoon, FaSun } from "react-icons/fa";
import {
    applyTheme,
    getAppliedTheme,
    THEME_STORAGE_KEY,
    type AppTheme,
} from "../utils/themeService";

const ThemeToggle = () => {
    const [theme, setTheme] = useState<AppTheme>(getAppliedTheme);
    const isDark = theme === "dark";

    useEffect(() => {
        const syncTheme = (event: StorageEvent) => {
            if (event.key !== THEME_STORAGE_KEY) return;
            if (event.newValue !== "light" && event.newValue !== "dark") return;
            applyTheme(event.newValue, false);
            setTheme(event.newValue);
        };

        window.addEventListener("storage", syncTheme);
        return () => window.removeEventListener("storage", syncTheme);
    }, []);

    const toggleTheme = () => {
        const nextTheme: AppTheme = isDark ? "light" : "dark";
        applyTheme(nextTheme);
        setTheme(nextTheme);
    };

    const label = isDark ? "Włącz jasny motyw" : "Włącz ciemny motyw";

    return (
        <button
            type="button"
            aria-label={label}
            title={label}
            onClick={toggleTheme}
            className="grid h-10 w-10 shrink-0 place-items-center rounded-xl border border-line bg-surface text-ink-strong shadow-sm transition hover:border-brand-border-strong hover:bg-brand-soft hover:text-brand-text-strong active:bg-brand-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus"
        >
            {isDark ? <FaSun aria-hidden="true" /> : <FaMoon aria-hidden="true" />}
        </button>
    );
};

export default ThemeToggle;
