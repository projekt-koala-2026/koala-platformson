import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useRef,
    useState,
    type ReactNode,
} from "react";

interface LoadingContextValue {
    progress: number;
    startLoading: () => void;
    stopLoading: () => void;
}

const LoadingContext = createContext<LoadingContextValue | undefined>(undefined);

export const LoadingProvider = ({ children }: { children: ReactNode }) => {
    const [progress, setProgress] = useState(0);
    const activeRequests = useRef(0);
    const advanceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
    const hideTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    const clearTimer = (timer: typeof advanceTimer) => {
        if (timer.current !== null) {
            clearTimeout(timer.current);
            timer.current = null;
        }
    };

    const startLoading = useCallback(() => {
        activeRequests.current += 1;
        clearTimer(hideTimer);
        if (activeRequests.current > 1) return;

        setProgress(15);
        clearTimer(advanceTimer);
        advanceTimer.current = setTimeout(() => {
            setProgress((current) => (current > 0 && current < 70 ? 70 : current));
            advanceTimer.current = null;
        }, 250);
    }, []);

    const stopLoading = useCallback(() => {
        if (activeRequests.current === 0) return;
        activeRequests.current -= 1;
        if (activeRequests.current > 0) return;

        clearTimer(advanceTimer);
        setProgress(100);
        clearTimer(hideTimer);
        hideTimer.current = setTimeout(() => {
            setProgress(0);
            hideTimer.current = null;
        }, 250);
    }, []);

    useEffect(
        () => () => {
            clearTimer(advanceTimer);
            clearTimer(hideTimer);
        },
        []
    );

    const value = useMemo(
        () => ({ progress, startLoading, stopLoading }),
        [progress, startLoading, stopLoading]
    );

    return <LoadingContext.Provider value={value}>{children}</LoadingContext.Provider>;
};

export const useLoading = (): LoadingContextValue => {
    const context = useContext(LoadingContext);
    if (!context) throw new Error("useLoading must be used within LoadingProvider");
    return context;
};
