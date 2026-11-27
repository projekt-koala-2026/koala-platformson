import type { ReactNode } from "react";

export interface NavigationOption {
    label: string;
    onClick: () => void | Promise<void>;
    disabled?: boolean;
    indentLevel?: 1 | 2;
}

export interface NavigationLink {
    label: string;
    path: string;
    /** Draw a sidebar separator above this link (except when it is first). */
    separatorBefore?: boolean;
    indentLevel?: 1 | 2;
}

export interface ModalProps {
    children: ReactNode;
    isOpen: boolean;
    onClose: () => void;
    title: string;
    maxWidth?: "md" | "lg" | "xl";
    footer?: ReactNode;
}
