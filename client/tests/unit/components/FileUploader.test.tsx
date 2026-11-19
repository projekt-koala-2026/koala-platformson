import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import FileUploader from "../../../src/components/FileUploader";

const testFile = (name = "zdjecie.png", type = "image/png") =>
    new File(["test-content"], name, { type });

describe("FileUploader", () => {
    it("passes a selected file and permits selecting the same file again", async () => {
        const onFileSelect = vi.fn();
        const user = userEvent.setup();
        const { container } = render(
            <FileUploader accept="image/png" hint="Maksymalnie 5 MB" onFileSelect={onFileSelect} />
        );
        const input = container.querySelector<HTMLInputElement>('input[type="file"]')!;
        const file = testFile();

        expect(input).toHaveAttribute("accept", "image/png");
        expect(screen.getByText("Maksymalnie 5 MB")).toBeInTheDocument();

        await user.upload(input, file);
        await user.upload(input, file);

        expect(onFileSelect).toHaveBeenCalledTimes(2);
        expect(onFileSelect).toHaveBeenNthCalledWith(1, file);
        expect(onFileSelect).toHaveBeenNthCalledWith(2, file);
    });

    it("reports validation errors without forwarding an invalid file", async () => {
        const onFileSelect = vi.fn();
        const onValidationError = vi.fn();
        const user = userEvent.setup();
        const { container } = render(
            <FileUploader
                onFileSelect={onFileSelect}
                onValidationError={onValidationError}
                validateFile={(file) =>
                    file.type === "application/pdf" ? null : "Dozwolone są tylko pliki PDF."
                }
            />
        );
        const input = container.querySelector<HTMLInputElement>('input[type="file"]')!;
        const file = testFile();

        await user.upload(input, file);

        expect(onValidationError).toHaveBeenCalledWith("Dozwolone są tylko pliki PDF.");
        expect(onFileSelect).not.toHaveBeenCalled();
    });

    it("shows drag feedback and accepts a dropped file", async () => {
        const onFileSelect = vi.fn();
        render(<FileUploader onFileSelect={onFileSelect} />);
        const dropzone = screen.getByText("Wybierz plik").closest("label")!;
        const file = testFile();
        const dataTransfer = {
            files: [file],
            types: ["Files"],
            dropEffect: "none",
        };

        fireEvent.dragEnter(dropzone, { dataTransfer });
        expect(screen.getByText("Upuść plik tutaj")).toBeInTheDocument();

        fireEvent.drop(dropzone, { dataTransfer });

        await waitFor(() => expect(onFileSelect).toHaveBeenCalledWith(file));
        expect(screen.getByText("Wybierz plik")).toBeInTheDocument();
    });

    it("ignores selections and drops while disabled", async () => {
        const onFileSelect = vi.fn();
        const user = userEvent.setup();
        const { container } = render(
            <FileUploader disabled onFileSelect={onFileSelect} label="Dodaj dokument" />
        );
        const input = container.querySelector<HTMLInputElement>('input[type="file"]')!;
        const dropzone = screen.getByText("Przesyłanie pliku…").closest("label")!;
        const file = testFile();

        expect(input).toBeDisabled();
        expect(dropzone).toHaveAttribute("aria-disabled", "true");
        await user.upload(input, file);
        fireEvent.drop(dropzone, {
            dataTransfer: { files: [file], types: ["Files"], dropEffect: "none" },
        });

        expect(onFileSelect).not.toHaveBeenCalled();
    });
});
