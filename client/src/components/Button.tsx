import type { MouseEventHandler, ReactNode } from "react";

interface ButtonProps {
    text: ReactNode;
    onClick?: MouseEventHandler<HTMLButtonElement>;
    type?: "button" | "submit";
    disabled?: boolean;
    className?: string;
    title?: string;
}
const Button = ({
    text,
    onClick,
    type = "button",
    disabled = false,
    className = "",
    title,
}: ButtonProps) => (
    <button
        type={type}
        disabled={disabled}
        onClick={onClick}
        title={title}
        className={`inline-flex min-h-10 appearance-none items-center justify-center gap-2 overflow-hidden rounded-xl border-0 bg-brand bg-clip-border px-4 py-2.5 text-sm font-medium text-ink-inverse shadow-sm transition hover:bg-brand-hover active:bg-brand-active focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-border-strong disabled:cursor-not-allowed disabled:bg-surface-disabled disabled:text-ink-secondary disabled:shadow-none ${className}`}
    >
        {text}
    </button>
);

export default Button;
