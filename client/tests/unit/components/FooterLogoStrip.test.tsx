import { act, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, it, vi } from "vitest";
import FooterLogoStrip from "../../../src/components/FooterLogoStrip";

it("pokazuje strzałki wyłącznie w dostępnych kierunkach i reaguje na zmianę szerokości", async () => {
    const user = userEvent.setup();
    render(
        <FooterLogoStrip title="Sponsorzy">
            <span>Logo</span>
        </FooterLogoStrip>
    );
    const viewport = screen.getByRole("region", { name: "Logotypy: Sponsorzy" });
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
    let width = 300;
    let offset = 0;
    Object.defineProperties(viewport, {
        clientWidth: { configurable: true, get: () => width },
        scrollWidth: { configurable: true, value: 900 },
        scrollLeft: { configurable: true, get: () => offset },
    });
    const scrollBy = vi.fn();
    viewport.scrollBy = scrollBy;
    act(() => window.dispatchEvent(new Event("resize")));
    const right = screen.getByRole("button", { name: "Przewiń w prawo: Sponsorzy" });
    expect(
        screen.queryByRole("button", { name: "Przewiń w lewo: Sponsorzy" })
    ).not.toBeInTheDocument();
    await user.click(right);
    expect(scrollBy).toHaveBeenLastCalledWith({ left: 240, behavior: "smooth" });
    offset = 250;
    fireEvent.scroll(viewport);
    await user.click(screen.getByRole("button", { name: "Przewiń w lewo: Sponsorzy" }));
    expect(scrollBy).toHaveBeenLastCalledWith({ left: -240, behavior: "smooth" });
    offset = 600;
    fireEvent.scroll(viewport);
    expect(
        screen.queryByRole("button", { name: "Przewiń w prawo: Sponsorzy" })
    ).not.toBeInTheDocument();
    offset = 0;
    width = 1000;
    act(() => window.dispatchEvent(new Event("resize")));
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
});

it("przesuwa kółkiem myszy, ale oddaje przewijanie stronie na końcach i zachowuje zoom oraz touchpad", () => {
    render(
        <FooterLogoStrip title="Sponsorzy">
            <span>Logo</span>
        </FooterLogoStrip>
    );
    const viewport = screen.getByRole("region", { name: "Logotypy: Sponsorzy" });
    let offset = 0;
    let width = 300;
    Object.defineProperties(viewport, {
        clientWidth: { configurable: true, get: () => width },
        scrollWidth: { configurable: true, value: 900 },
        scrollLeft: {
            configurable: true,
            get: () => offset,
            set: (value: number) => {
                offset = value;
            },
        },
    });
    const wheel = (options: WheelEventInit) => {
        const event = new WheelEvent("wheel", { bubbles: true, cancelable: true, ...options });
        fireEvent(viewport, event);
        return event;
    };
    expect(wheel({ deltaY: -100 }).defaultPrevented).toBe(false);
    expect(offset).toBe(0);
    expect(wheel({ deltaY: 100 }).defaultPrevented).toBe(true);
    expect(offset).toBe(100);
    // The pointer may be over the arrow or the space beside the viewport.
    const overStrip = new WheelEvent("wheel", { bubbles: true, cancelable: true, deltaY: 20 });
    fireEvent(viewport.parentElement!, overStrip);
    expect(overStrip.defaultPrevented).toBe(true);
    expect(offset).toBe(120);
    expect(wheel({ deltaY: -2, deltaMode: 1 }).defaultPrevented).toBe(true);
    expect(offset).toBe(88);
    expect(wheel({ deltaY: 20, ctrlKey: true }).defaultPrevented).toBe(false);
    expect(wheel({ deltaX: 40, deltaY: 2 }).defaultPrevented).toBe(false);
    expect(offset).toBe(88);
    expect(wheel({ deltaY: 1000 }).defaultPrevented).toBe(true);
    expect(offset).toBe(600);
    expect(wheel({ deltaY: 100 }).defaultPrevented).toBe(false);
    width = 1000;
    offset = 0;
    expect(wheel({ deltaY: 100 }).defaultPrevented).toBe(false);
    expect(offset).toBe(0);
});
