import { useLoading } from "../contexts/LoadingContext";

export default function TopLoadingBar() {
    const { progress } = useLoading();

    const barStyle = {
        width: `${progress}%`,
        opacity: progress === 0 || progress === 100 ? 0 : 1,
    };

    return <div className="topLoadingBar" style={barStyle} />;
}
