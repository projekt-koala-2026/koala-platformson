import { createContext, useContext, useState } from "react";
import { randint } from "../utils/helpers";

const LoadingContext = createContext();

export const LoadingProvider = ({ children }) => {
    const [progress, setProgress] = useState(0);

    const startLoading = () => {
        setProgress(randint(10, 40));
        setTimeout(() => setProgress(randint(50, 80)), 200);
    };

    const stopLoading = () => {
        setProgress(100);
        setTimeout(() => setProgress(0), 200);
    };

    return (
        <LoadingContext.Provider value={{ progress, startLoading, stopLoading }}>
            {children}
        </LoadingContext.Provider>
    );
};

export const useLoading = () => useContext(LoadingContext);
