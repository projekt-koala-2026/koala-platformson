import { useEffect } from "react";

const useKeyboardShortcuts = (shortcuts) => {
    useEffect(() => {
        const handleKeyDown = (event) => {
            shortcuts.forEach((shortcut) => {
                const [[key, callback]] = Object.entries(shortcut);
                if (event.key === key) {
                    callback();
                }
            });
        };

        window.addEventListener("keydown", handleKeyDown);

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
        };
    }, [shortcuts]);
};

export default useKeyboardShortcuts;
