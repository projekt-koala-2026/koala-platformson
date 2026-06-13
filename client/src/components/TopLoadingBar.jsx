import { useEffect, useRef } from "react";
import { useLoading } from "../contexts/LoadingContext";
import styles from "./TopLoadingBar.module.css";

const TopLoadingBar = () => {
    const { progress } = useLoading();
    const barRef = useRef(null);

    useEffect(() => {
        if (!barRef.current) return;

        barRef.current.style.width = `${progress}%`;
        barRef.current.style.opacity = progress === 0 || progress === 100 ? 0 : 1;
    }, [progress]);

    return <div ref={barRef} className={styles.topLoadingBar} />;
};

export default TopLoadingBar;
