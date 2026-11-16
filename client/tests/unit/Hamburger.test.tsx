import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Hamburger from "../../src/components/Hamburger";

describe("nawigacja boczna", () => {
    beforeEach(() => sessionStorage.clear());
    it("otwiera panel, oznacza bieżącą stronę i zamyka go po wyborze", async () => {
        const user = userEvent.setup();
        const navigate = vi.fn();
        render(<Hamburger options={[{ label: "Wpisy", onClick: navigate, active: true }]} />);
        const trigger = screen.getByRole("button", { name: "Otwórz nawigację" });
        await user.click(trigger);
        expect(screen.getByRole("dialog", { name: "Nawigacja" })).toBeInTheDocument();
        expect(document.body.style.overflow).toBe("hidden");
        expect(screen.getByRole("button", { name: "Wpisy" })).toHaveAttribute(
            "aria-current",
            "page"
        );
        await user.click(screen.getByRole("button", { name: "Wpisy" }));
        expect(navigate).toHaveBeenCalledOnce();
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
        expect(trigger).toHaveFocus();
        expect(document.body.style.overflow).toBe("");
    });

    it("zamyka panel klawiszem Escape", async () => {
        const user = userEvent.setup();
        render(<Hamburger options={[]} />);
        await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
        await user.keyboard("{Escape}");
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("zamyka i ponownie otwiera panel przyciskiem nawigacji", async () => {
        const user = userEvent.setup();
        render(<Hamburger options={[]} />);
        await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
        await user.click(screen.getByRole("button", { name: "Zamknij nawigację" }));
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
        expect(screen.getByRole("dialog", { name: "Nawigacja" })).toBeInTheDocument();
    });

    it("na szerokim ekranie otwiera panel bez blokowania strony", async () => {
        const user = userEvent.setup();
        vi.stubGlobal(
            "matchMedia",
            vi.fn().mockReturnValue({
                matches: true,
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
            })
        );
        try {
            const navigate = vi.fn();
            const view = render(<Hamburger options={[{ label: "Wpisy", onClick: navigate }]} />);
            await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
            expect(screen.getByRole("region", { name: "Nawigacja" })).toBeInTheDocument();
            expect(document.body).toHaveClass("navigation-panel-open");
            expect(document.body.style.overflow).toBe("");
            await user.click(screen.getByRole("button", { name: "Wpisy" }));
            expect(navigate).toHaveBeenCalledOnce();
            expect(screen.getByRole("region", { name: "Nawigacja" })).toBeInTheDocument();
            view.unmount();
            render(<Hamburger options={[]} />);
            expect(screen.getByRole("region", { name: "Nawigacja" })).toBeInTheDocument();
            await user.keyboard("{Escape}");
            expect(document.body).not.toHaveClass("navigation-panel-open");
        } finally {
            vi.unstubAllGlobals();
        }
    });

    it("zamyka panel przy zwężeniu okna i przywraca preferencję po rozszerzeniu", async () => {
        const user = userEvent.setup();
        let onChange: (() => void) | undefined;
        const query = {
            matches: true,
            addEventListener: vi.fn((_event: string, callback: () => void) => {
                onChange = callback;
            }),
            removeEventListener: vi.fn(),
        };
        vi.stubGlobal("matchMedia", vi.fn().mockReturnValue(query));
        try {
            render(<Hamburger options={[{ label: "Wpisy", onClick: vi.fn() }]} />);
            await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
            query.matches = false;
            act(() => onChange?.());
            expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
            await user.click(screen.getByRole("button", { name: "Otwórz nawigację" }));
            await user.click(screen.getByRole("button", { name: "Wpisy" }));
            expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
            query.matches = true;
            act(() => onChange?.());
            expect(screen.getByRole("region", { name: "Nawigacja" })).toBeInTheDocument();
        } finally {
            vi.unstubAllGlobals();
        }
    });
});
