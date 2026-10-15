import { useLoading } from "../contexts/LoadingContext";

const TopLoadingBar = () => {
    const { progress } = useLoading();
    const visible = progress > 0;

    return (
        <div
            role="progressbar"
            aria-label="Ładowanie"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={progress}
            aria-hidden={!visible}
            className="pointer-events-none fixed inset-x-0 top-0 z-[60] h-1 overflow-hidden bg-transparent"
        >
            <div
                className="h-full origin-left bg-emerald-600 shadow-[0_0_8px_rgb(5_150_105_/_0.45)] transition-[transform,opacity] duration-300 ease-out"
                style={{
                    transform: `scaleX(${progress / 100})`,
                    opacity: visible ? 1 : 0,
                }}
            />
        </div>
    );
};

export default TopLoadingBar;
