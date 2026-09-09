import type { MouseEventHandler, ReactNode } from "react";

interface ButtonProps {
    text: ReactNode;
    onClick?: MouseEventHandler<HTMLButtonElement>;
    type?: "button" | "submit";
    disabled?: boolean;
    className?: string;
}
const Button = ({
    text,
    onClick,
    type = "button",
    disabled = false,
    className = "",
}: ButtonProps) => (
    <button
        type={type}
        disabled={disabled}
        onClick={onClick}
        className={`inline-flex appearance-none items-center justify-center gap-2 overflow-hidden rounded-xl border-0 bg-emerald-600 bg-clip-border px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-emerald-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-300 disabled:cursor-not-allowed disabled:bg-slate-300 ${className}`}
    >
        {text}
    </button>
);

export default Button;
