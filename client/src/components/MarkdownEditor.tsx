import type EasyMDE from "easymde";
import "easymde/dist/easymde.min.css";
import { useEffect, useMemo, useRef, useState } from "react";
import SimpleMDE from "react-simplemde-editor";
import type { ManagedFile } from "../types/models";
import Button from "./Button";
import ImagePicker from "./ImagePicker";
import Modal from "./Modal";

interface MarkdownEditorProps {
    initialValue?: string;
    label?: string;
    disabled?: boolean;
    onChange?: ((value: string) => void) | null;
    onSave: (value: string) => void | Promise<void>;
}

const MarkdownEditor = ({
    initialValue = "",
    label = "Zapisz zmiany",
    disabled = false,
    onChange,
    onSave,
}: MarkdownEditorProps) => {
    const [content, setContent] = useState(initialValue);
    const [showPicker, setShowPicker] = useState(false);
    const mdeInstance = useRef<EasyMDE | null>(null);
    const disabledRef = useRef(disabled);

    useEffect(() => {
        disabledRef.current = disabled;
        mdeInstance.current?.codemirror.setOption("readOnly", disabled ? "nocursor" : false);
    }, [disabled]);

    const handleImageSelect = (file: ManagedFile) => {
        const editor = mdeInstance.current;
        if (!editor) return;
        const cursor = editor.codemirror.getCursor();
        const alt = file.title.replace(/[[\]]/g, "").trim() || "Obraz";
        editor.codemirror.replaceRange(`![${alt}](${file.filePath})`, cursor);
        editor.codemirror.focus();
        setShowPicker(false);
    };

    const handleTextChange = (value: string) => {
        setContent(value);
        onChange?.(value);
    };

    const options = useMemo<EasyMDE.Options>(
        () => ({
            spellChecker: false,
            placeholder: "Wpisz treść…",
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
                        if (disabledRef.current) return;
                        mdeInstance.current = editor;
                        setShowPicker(true);
                    },
                    className: "fa fa-picture-o",
                    title: "Wstaw obraz z serwera",
                },
                "|",
                "preview",
                "guide",
            ],
        }),
        []
    );

    return (
        <div className="w-full space-y-4">
            <SimpleMDE
                value={content}
                onChange={handleTextChange}
                options={options}
                getMdeInstance={(editor) => {
                    mdeInstance.current = editor;
                    editor.codemirror.setOption(
                        "readOnly",
                        disabledRef.current ? "nocursor" : false
                    );
                }}
            />
            <Button text={label} disabled={disabled} onClick={() => void onSave(content)} />
            <Modal
                isOpen={showPicker}
                onClose={() => setShowPicker(false)}
                title="Wybierz obraz"
                maxWidth="xl"
            >
                <ImagePicker onSelect={handleImageSelect} />
            </Modal>
        </div>
    );
};

export default MarkdownEditor;
