import { useId, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { FaChevronLeft, FaChevronRight } from "react-icons/fa";

const FooterLogoStrip = ({ title, children }: { title: string; children: ReactNode }) => {
    const viewportRef = useRef<HTMLDivElement>(null);
    const stripRef = useRef<HTMLDivElement>(null);
    const contentRef = useRef<HTMLDivElement>(null);
    const id = useId();
    const [directions, setDirections] = useState({ left: false, right: false });

    useLayoutEffect(() => {
        const viewport = viewportRef.current;
        const content = contentRef.current;
        const strip = stripRef.current;
        if (!viewport || !content || !strip) return;
        const measure = () => {
            const left = viewport.scrollLeft > 1;
            const right = viewport.scrollWidth - viewport.clientWidth - viewport.scrollLeft > 1;
            setDirections((current) =>
                current.left === left && current.right === right ? current : { left, right }
            );
        };
        const onWheel = (event: WheelEvent) => {
            // Preserve browser zoom and native horizontal touchpad gestures.
            if (event.ctrlKey || event.metaKey || Math.abs(event.deltaX) >= Math.abs(event.deltaY))
                return;
            const max = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
            const delta =
                event.deltaY *
                (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? viewport.clientWidth : 1);
            const position = Math.max(0, Math.min(max, viewport.scrollLeft));
            if ((delta > 0 && position < max - 1) || (delta < 0 && position > 1)) {
                event.preventDefault();
                viewport.scrollLeft = Math.max(0, Math.min(max, position + delta));
            }
        };
        measure();
        viewport.addEventListener("scroll", measure, { passive: true });
        strip.addEventListener("wheel", onWheel, { passive: false });
        window.addEventListener("resize", measure);
        const observer = new ResizeObserver(measure);
        observer.observe(viewport);
        observer.observe(content);
        return () => {
            viewport.removeEventListener("scroll", measure);
            strip.removeEventListener("wheel", onWheel);
            window.removeEventListener("resize", measure);
            observer.disconnect();
        };
    }, [children]);

    const move = (direction: number) => {
        const viewport = viewportRef.current;
        if (!viewport) return;
        viewport.scrollBy({
            left: direction * Math.max(100, viewport.clientWidth * 0.8),
            behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches
                ? "auto"
                : "smooth",
        });
    };
    const buttonClass =
        "grid h-9 w-8 shrink-0 place-items-center rounded-lg text-ink-muted transition hover:bg-surface-muted hover:text-brand-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-focus";

    return (
        <div ref={stripRef} className="mt-1.5 flex min-w-0 items-center gap-1.5">
            {directions.left ? (
                <button
                    type="button"
                    className={buttonClass}
                    aria-label={`Przewiń w lewo: ${title}`}
                    aria-controls={id}
                    onClick={() => move(-1)}
                >
                    <FaChevronLeft aria-hidden="true" className="text-xs" />
                </button>
            ) : (
                <span aria-hidden="true" className="w-8 shrink-0" />
            )}
            <div
                ref={viewportRef}
                id={id}
                role="region"
                aria-label={`Logotypy: ${title}`}
                tabIndex={0}
                className="app-footer-logos min-w-0 flex-1 overflow-x-auto overflow-y-hidden overscroll-x-contain rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-focus"
            >
                <div ref={contentRef} className="flex w-max min-w-full items-center gap-3">
                    {children}
                </div>
            </div>
            {directions.right ? (
                <button
                    type="button"
                    className={buttonClass}
                    aria-label={`Przewiń w prawo: ${title}`}
                    aria-controls={id}
                    onClick={() => move(1)}
                >
                    <FaChevronRight aria-hidden="true" className="text-xs" />
                </button>
            ) : (
                <span aria-hidden="true" className="w-8 shrink-0" />
            )}
        </div>
    );
};

export default FooterLogoStrip;
