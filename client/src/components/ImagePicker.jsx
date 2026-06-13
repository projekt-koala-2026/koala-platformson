import { forwardRef, useEffect, useImperativeHandle, useState } from "react";
import { apiRequest, apiUrl } from "../utils/apiFetcher";
import styles from "./ImagePicker.module.css";

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
        <div className={styles.grid}>
            {images.map((img) => (
                <div key={img.id} onClick={() => onSelect(img)} className={styles.item}>
                    <img src={apiUrl + img.filePath} alt={img.title} className={styles.image} />
                    <p className={styles.title}>{img.title}</p>
                </div>
            ))}
        </div>
    );
});

export default ImagePicker;
