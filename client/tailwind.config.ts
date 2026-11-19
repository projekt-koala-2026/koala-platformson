import type { Config } from "tailwindcss";

const color = (name: string) => `rgb(var(--color-${name}) / <alpha-value>)`;

export default {
    content: ["./index.html", "./src/**/*.{ts,tsx}"],
    theme: {
        extend: {
            colors: {
                canvas: color("canvas"),
                surface: {
                    DEFAULT: color("surface"),
                    muted: color("surface-muted"),
                    subtle: color("surface-subtle"),
                    disabled: color("surface-disabled"),
                    active: color("surface-active"),
                },
                line: {
                    subtle: color("line-subtle"),
                    DEFAULT: color("line"),
                    strong: color("line-strong"),
                },
                ink: {
                    subtle: color("ink-subtle"),
                    muted: color("ink-muted"),
                    secondary: color("ink-secondary"),
                    DEFAULT: color("ink"),
                    strong: color("ink-strong"),
                    heading: color("ink-heading"),
                    inverse: color("ink-inverse"),
                    "inverse-muted": color("ink-inverse-muted"),
                },
                brand: {
                    soft: color("brand-soft"),
                    muted: color("brand-muted"),
                    border: color("brand-border"),
                    "border-strong": color("brand-border-strong"),
                    emphasis: color("brand-emphasis"),
                    focus: color("brand-focus"),
                    DEFAULT: color("brand"),
                    hover: color("brand-hover"),
                    active: color("brand-active"),
                    strong: color("brand-strong"),
                    deep: color("brand-deep"),
                    text: color("brand-text"),
                    "text-strong": color("brand-text-strong"),
                    "text-deep": color("brand-text-deep"),
                },
                success: {
                    soft: color("success-soft"),
                    border: color("success-border"),
                    text: color("success-text"),
                },
                warning: {
                    soft: color("warning-soft"),
                    muted: color("warning-muted"),
                    border: color("warning-border"),
                    DEFAULT: color("warning"),
                    hover: color("warning-hover"),
                    text: color("warning-text"),
                },
                danger: {
                    soft: color("danger-soft"),
                    muted: color("danger-muted"),
                    border: color("danger-border"),
                    focus: color("danger-focus"),
                    "border-strong": color("danger-border-strong"),
                    emphasis: color("danger-emphasis"),
                    DEFAULT: color("danger"),
                    hover: color("danger-hover"),
                    active: color("danger-active"),
                    text: color("danger-text"),
                    icon: color("danger-icon"),
                    deep: color("danger-deep"),
                },
                backdrop: color("backdrop"),
            },
            boxShadow: {
                panel: "0 12px 30px rgb(var(--color-shadow) / 0.06)",
                loading: "0 0 8px rgb(var(--color-brand) / 0.45)",
                "table-header": "0 1px 0 rgb(var(--color-line-strong))",
            },
        },
    },
    plugins: [],
} satisfies Config;
