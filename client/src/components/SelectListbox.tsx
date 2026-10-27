import { useEffect, useId, useRef, useState } from "react";
import { FaCheck, FaChevronDown } from "react-icons/fa";

export interface SelectListboxOption<T extends string> {
    value: T;
    label: string;
}

interface SelectListboxProps<T extends string> {
    value: T;
    options: readonly SelectListboxOption<T>[];
    onChange: (value: T) => void;
    ariaLabelledBy: string;
    className?: string;
}

function SelectListbox<T extends string>({
    value,
    options,
    onChange,
    ariaLabelledBy,
    className = "",
}: SelectListboxProps<T>) {
    const [isOpen, setIsOpen] = useState(false);
    const selectedIndex = Math.max(
        0,
        options.findIndex((option) => option.value === value)
    );
    const [activeIndex, setActiveIndex] = useState(selectedIndex);
    const rootRef = useRef<HTMLDivElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);
    const optionRefs = useRef<Array<HTMLButtonElement | null>>([]);
    const listboxId = useId();
    const selectedOption = options[selectedIndex];

    useEffect(() => {
        if (!isOpen) return;

        const closeOnOutsideClick = (event: PointerEvent) => {
            if (!rootRef.current?.contains(event.target as Node)) setIsOpen(false);
        };

        document.addEventListener("pointerdown", closeOnOutsideClick);
        return () => document.removeEventListener("pointerdown", closeOnOutsideClick);
    }, [isOpen]);

    const focusOption = (index: number) => {
        const nextIndex = (index + options.length) % options.length;
        setActiveIndex(nextIndex);
        optionRefs.current[nextIndex]?.focus();
    };

    const openListbox = (index = selectedIndex) => {
        setActiveIndex(index);
        setIsOpen(true);
        requestAnimationFrame(() => optionRefs.current[index]?.focus());
    };

    const closeListbox = () => {
        setIsOpen(false);
        triggerRef.current?.focus();
    };

    const selectOption = (option: SelectListboxOption<T>) => {
        onChange(option.value);
        closeListbox();
    };

    return (
        <div ref={rootRef} className={`relative ${className}`}>
            <button
                ref={triggerRef}
                type="button"
                aria-haspopup="listbox"
                aria-expanded={isOpen}
                aria-controls={listboxId}
                aria-labelledby={ariaLabelledBy}
                onClick={() => (isOpen ? setIsOpen(false) : openListbox())}
                onKeyDown={(event) => {
                    if (event.key === "ArrowDown") {
                        event.preventDefault();
                        openListbox(selectedIndex);
                    } else if (event.key === "ArrowUp") {
                        event.preventDefault();
                        openListbox(selectedIndex);
                    }
                }}
                className="flex w-full items-center justify-between gap-3 rounded-xl border border-line-strong bg-surface px-3 py-2.5 text-left text-ink-heading outline-none transition hover:border-brand-border focus:border-brand-focus focus:ring-2 focus:ring-brand-muted"
            >
                <span>{selectedOption?.label}</span>
                <FaChevronDown
                    aria-hidden="true"
                    className={`shrink-0 text-xs text-brand-text transition-transform ${
                        isOpen ? "rotate-180" : ""
                    }`}
                />
            </button>

            {isOpen && (
                <div
                    id={listboxId}
                    role="listbox"
                    aria-labelledby={ariaLabelledBy}
                    className="absolute z-30 mt-2 max-h-64 w-full overflow-auto rounded-xl border border-line bg-surface p-1.5 shadow-panel"
                >
                    {options.map((option, index) => {
                        const isSelected = option.value === value;
                        const isActive = index === activeIndex;

                        return (
                            <button
                                key={option.value}
                                ref={(element) => {
                                    optionRefs.current[index] = element;
                                }}
                                type="button"
                                role="option"
                                aria-selected={isSelected}
                                tabIndex={isActive ? 0 : -1}
                                onFocus={() => setActiveIndex(index)}
                                onMouseMove={() => setActiveIndex(index)}
                                onClick={() => selectOption(option)}
                                onKeyDown={(event) => {
                                    if (event.key === "ArrowDown") {
                                        event.preventDefault();
                                        focusOption(index + 1);
                                    } else if (event.key === "ArrowUp") {
                                        event.preventDefault();
                                        focusOption(index - 1);
                                    } else if (event.key === "Home") {
                                        event.preventDefault();
                                        focusOption(0);
                                    } else if (event.key === "End") {
                                        event.preventDefault();
                                        focusOption(options.length - 1);
                                    } else if (event.key === "Escape") {
                                        event.preventDefault();
                                        closeListbox();
                                    } else if (event.key === "Tab") {
                                        setIsOpen(false);
                                    }
                                }}
                                className={`flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2.5 text-left text-sm outline-none transition-colors ${
                                    isSelected
                                        ? "bg-brand-soft font-semibold text-brand-text-strong"
                                        : isActive
                                          ? "bg-surface-muted text-ink-heading"
                                          : "text-ink-secondary"
                                }`}
                            >
                                <span>{option.label}</span>
                                {isSelected && (
                                    <FaCheck
                                        aria-hidden="true"
                                        className="shrink-0 text-xs text-brand-text"
                                    />
                                )}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
}

export default SelectListbox;
