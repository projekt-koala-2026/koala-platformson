import { forwardRef, useEffect, useImperativeHandle, useState } from "react";
import { apiRequest, apiUrl } from "../utils/apiFetcher";

const ImagePicker = forwardRef(({ onSelect, navigate }, ref) => {
    const [images, setImages] = useState([]);

    const fetchImages = async () => {
        const data = await apiRequest(
            "/api/admin/file/public/files?Folder=images",
            null,
            "GET",
            navigate
        );
        if (data) {
            setImages(data);
            console.log(data);
        }
    };

    useImperativeHandle(ref, () => ({
        refresh: () => {
            fetchImages();
        },
    }));

    useEffect(() => {
        fetchImages();
    }, []);

    return (
        <div
            style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fill, minmax(100px, 1fr))",
                gap: "10px",
                padding: "10px",
                background: "#333",
                borderRadius: "8px",
                maxHeight: "300px",
                overflowY: "auto",
            }}
        >
            {images.map((img) => (
                <div
                    key={img.id}
                    onClick={() => onSelect(img)}
                    style={{ cursor: "pointer", border: "2px solid transparent" }}
                    onMouseOver={(e) => (e.currentTarget.style.borderColor = "#ffcc00")}
                    onMouseOut={(e) => (e.currentTarget.style.borderColor = "transparent")}
                >
                    <img
                        src={apiUrl + img.filePath}
                        alt={img.title}
                        style={{
                            width: "100%",
                            height: "80px",
                            objectFit: "cover",
                            borderRadius: "4px",
                        }}
                    />
                    <p
                        style={{
                            fontSize: "10px",
                            color: "white",
                            margin: "2px 0",
                            overflow: "hidden",
                        }}
                    >
                        {img.title}
                    </p>
                </div>
            ))}
        </div>
    );
});

export default ImagePicker;
