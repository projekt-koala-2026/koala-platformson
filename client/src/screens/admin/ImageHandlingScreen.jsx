import { useRef } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import FileUploader from "../../components/FileUploader";
import ImagePicker from "../../components/ImagePicker";
import { apiRequest, uploadFile } from "../../utils/apiFetcher";

const ImageHandlingScreen = () => {
    const navigate = useNavigate();
    const uploaderRef = useRef(null);
    const pickerRef = useRef(null);

    const handleAddFile = async (file) => {
        await uploadFile(file, file.name, navigate);
        pickerRef.current.refresh();
    };

    const handleDeleteFile = async (file) => {
        if (!window.confirm("Usunąć to zdjęcie?")) return;
        await apiRequest("/api/admin/file/public/files", { id: file.id }, "DELETE", navigate);
        pickerRef.current.refresh();
    };

    return (
        <div className="container">
            <h1>Zarządzanie zdjęciami</h1>
            <Button text={"Dodaj zdjęcie"} onClick={() => uploaderRef.current.open()} />
            <FileUploader ref={uploaderRef} onFileSelect={handleAddFile} />
            <h1>Zarządzanie zdjęciami</h1>
            <ImagePicker ref={pickerRef} onSelect={handleDeleteFile} />
        </div>
    );
};

export default ImageHandlingScreen;
