import "easymde/dist/easymde.min.css";
import { useMemo, useRef, useState } from "react";
import SimpleMDE from "react-simplemde-editor";
import Button from "./Button";
import ImagePicker from "./ImagePicker";

const MarkdownEditor = ({ onSave, initialValue = "", label = "Zapisz zmiany" }) => {
    const [content, setContent] = useState(initialValue);
    const [showPicker, setShowPicker] = useState(false);
    const mdeInstance = useRef(null);
    const internalPickerRef = useRef(null);

    const handleImageSelect = (file) => {
        const cm = mdeInstance.current.codemirror;
        const cursor = cm.getCursor();
        const textToInsert = `![](${file.filePath})`;

        cm.replaceRange(textToInsert, cursor);
        setShowPicker(false);
    };

    const options = useMemo(
        () => ({
            spellChecker: false,
            placeholder: "Wpisz treść...",
            toolbar: [
                "bold",
                "italic",
                "heading",
                "|",
                "quote",
                "unordered-list",
                "ordered-list",
                "|",
                "link",
                {
                    name: "custom-image",
                    action: (editor) => {
                        mdeInstance.current = editor;
                        setShowPicker(true);
                    },
                    className: "fa fa-picture-o",
                    title: "Insert Image from Server",
                },
                "|",
                "preview",
                "guide",
            ],
        }),
        []
    );

    return (
        <div className="editorContainer">
            {showPicker && (
                <div className="modalOverlay">
                    <div className="modalContent">
                        <button onClick={() => setShowPicker(false)}>X</button>
                        <ImagePicker ref={internalPickerRef} onSelect={handleImageSelect} />
                    </div>
                </div>
            )}

            <SimpleMDE value={content} onChange={(value) => setContent(value)} options={options} />
            <Button text={label} onClick={() => onSave(content)} />
        </div>
    );
};

export default MarkdownEditor;
