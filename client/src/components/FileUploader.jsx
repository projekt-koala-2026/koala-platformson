import { forwardRef, useImperativeHandle, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { uploadFile } from "../utils/apiFetcher";

const FileUploader = forwardRef(({ onUploadSuccess, onFileSelect }, ref) => {
    const navigate = useNavigate();
    const fileInputRef = useRef(null);

    useImperativeHandle(ref, () => ({
        open: () => {
            fileInputRef.current.click();
        },
    }));

    const handleFileChange = async (e) => {
        const selectedFile = e.target.files[0];
        if (!selectedFile) return;

        if (onFileSelect) {
            onFileSelect(selectedFile);
            return;
        }

        const success = await uploadFile(selectedFile, selectedFile.name, navigate);
        if (success && onUploadSuccess) {
            onUploadSuccess();
        }
    };

    return (
        <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            style={{ display: "none" }}
        />
    );
});

export default FileUploader;
