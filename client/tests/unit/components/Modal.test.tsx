import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import Modal from "../../../src/components/Modal";

interface HarnessProps {
    initiallyOpen?: boolean;
    onClose?: () => void;
}

const ModalHarness = ({ initiallyOpen = true, onClose = () => undefined }: HarnessProps) => {
    const [isOpen, setIsOpen] = useState(initiallyOpen);

    return (
        <>
            <button type="button" onClick={() => setIsOpen(true)}>
                Otwórz modal
            </button>
            <Modal
                isOpen={isOpen}
                title="Testowy modal"
                onClose={() => {
                    onClose();
                    setIsOpen(false);
                }}
            >
                <button type="button">Pierwsza akcja</button>
                <button type="button">Ostatnia akcja</button>
            </Modal>
        </>
    );
};

describe("Modal", () => {
    it("renders an accessible dialog and locks page scrolling while open", async () => {
        render(<ModalHarness />);

        const dialog = screen.getByRole("dialog", { name: "Testowy modal" });
        expect(dialog).toHaveAttribute("aria-modal", "true");
        expect(document.body).toHaveStyle({ overflow: "hidden" });
        await waitFor(() => expect(dialog).toHaveFocus());
    });

    it("closes on Escape and restores focus and body scrolling", async () => {
        const onClose = vi.fn();
        const user = userEvent.setup();
        render(<ModalHarness initiallyOpen={false} onClose={onClose} />);
        const opener = screen.getByRole("button", { name: "Otwórz modal" });

        await user.click(opener);
        await waitFor(() => expect(screen.getByRole("dialog")).toHaveFocus());
        await user.keyboard("{Escape}");

        expect(onClose).toHaveBeenCalledOnce();
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
        expect(document.body).not.toHaveStyle({ overflow: "hidden" });
        expect(opener).toHaveFocus();
    });

    it("keeps keyboard focus inside the dialog", async () => {
        const user = userEvent.setup();
        render(<ModalHarness />);
        const closeButton = screen.getByRole("button", { name: "Zamknij" });
        const lastButton = screen.getByRole("button", { name: "Ostatnia akcja" });

        lastButton.focus();
        await user.tab();
        expect(closeButton).toHaveFocus();

        await user.tab({ shift: true });
        expect(lastButton).toHaveFocus();
    });

    it("closes only when the backdrop itself is pressed", () => {
        const onClose = vi.fn();
        render(<ModalHarness onClose={onClose} />);
        const dialog = screen.getByRole("dialog");
        const backdrop = dialog.parentElement;

        fireEvent.mouseDown(dialog);
        expect(onClose).not.toHaveBeenCalled();

        fireEvent.mouseDown(backdrop!);
        expect(onClose).toHaveBeenCalledOnce();
    });
});
