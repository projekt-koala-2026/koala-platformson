export type AppTheme = "light" | "dark";

export const THEME_STORAGE_KEY = "koala-theme";

const isAppTheme = (value: string | null): value is AppTheme =>
    value === "light" || value === "dark";

export const getAppliedTheme = (): AppTheme => {
    const appliedTheme = document.documentElement.dataset.theme ?? null;
    if (isAppTheme(appliedTheme)) return appliedTheme;

    try {
        const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);
        if (isAppTheme(storedTheme)) return storedTheme;
    } catch {
        // Storage may be unavailable in a restricted browser context.
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
};

export const applyTheme = (theme: AppTheme, persist = true) => {
    document.documentElement.dataset.theme = theme;
    if (!persist) return;

    try {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch {
        // The selected theme still applies for the current page.
    }
};
