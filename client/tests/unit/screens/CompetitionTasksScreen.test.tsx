import { screen, waitFor, within } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import CompetitionTasksScreen from "../../../src/screens/admin/CompetitionTasksScreen";
import { apiEndpoints } from "../../../src/utils/apiEndpoints";
import { apiUrl } from "../../../src/utils/apiFetcher";
import { renderWithRouter } from "../../helpers/renderWithRouter";
import { server } from "../../mocks/server";

vi.mock("../../../src/components/AdminHeader", () => ({ default: () => null }));
vi.mock("../../../src/components/MarkdownEditor", () => ({
    default: ({
        initialValue,
        onChange,
    }: {
        initialValue: string;
        onChange: (value: string) => void;
    }) => (
        <textarea
            aria-label="Treść Markdown"
            defaultValue={initialValue}
            onChange={(event) => onChange(event.target.value)}
        />
    ),
}));

const edition = { id: "edition-1", name: "Edycja", state: "ACTIVE" };
const subedition = {
    id: "sub-1",
    editionId: edition.id,
    name: "Etap",
    state: "ACTIVE",
    answerOpen: "2026-01-01T00:00:00Z",
    answerClose: "2027-01-01T00:00:00Z",
    version: 1,
};
const task = {
    id: "task-1",
    subEditionId: subedition.id,
    name: "Zadanie pierwsze",
    tier: "TIER_1",
    contentJson: JSON.stringify({ version: 1, body_pl: "Treść", extra: "zachowaj" }),
    version: 2,
};
const base = `${apiUrl}${apiEndpoints.competitionTasks(edition.id, subedition.id)}`;
const answerUrl = `${apiUrl}${apiEndpoints.taskAnswers(task.id)}`;
const common = () => [
    http.get(`${apiUrl}${apiEndpoints.editions}`, () => HttpResponse.json({ data: [edition] })),
    http.get(`${apiUrl}${apiEndpoints.subeditions(edition.id)}`, () =>
        HttpResponse.json({ data: [subedition] })
    ),
    http.get(base, () => HttpResponse.json({ data: [task] })),
];

describe("zadania konkursowe", () => {
    it("zapisuje edytowany Markdown, zachowując pozostałe pola i wersję zadania", async () => {
        const bodies: Record<string, unknown>[] = [];
        server.use(
            ...common(),
            http.get(answerUrl, () => HttpResponse.json({ data: [] })),
            http.put(`${base}/${task.id}`, async ({ request }) => {
                const body = (await request.json()) as Record<string, unknown>;
                bodies.push(body);
                return HttpResponse.json({ data: { ...task, ...body, version: 3 } });
            })
        );
        const { user } = renderWithRouter(<CompetitionTasksScreen />);
        await user.click(await screen.findByRole("button", { name: "Edytuj" }));
        const markdown = screen.getByLabelText("Treść Markdown");
        await user.clear(markdown);
        await user.type(markdown, "Nowa treść");
        await user.click(screen.getByRole("button", { name: "Typ odpowiedzi" }));
        await user.click(screen.getByRole("option", { name: "Jednokrotny wybór" }));
        await user.click(screen.getByRole("button", { name: "Dodaj wariant" }));
        await user.click(screen.getByRole("button", { name: "Dodaj wariant" }));
        for (const option of ["A", "B", "C", "D"]) {
            await user.type(screen.getByLabelText(`Wariant ${option}`), `Odpowiedź ${option}`);
        }
        await user.click(screen.getByRole("button", { name: "Zapisz" }));
        await waitFor(() => expect(bodies).toHaveLength(1));
        expect(bodies[0].version).toBe(2);
        expect(JSON.parse(bodies[0].contentJson as string)).toEqual({
            version: 1,
            body_pl: "Nowa treść",
            extra: "zachowaj",
            answer_type: "single_choice",
            options: ["A", "B", "C", "D"].map((id) => ({ id, text: `Odpowiedź ${id}` })),
        });
        expect(await screen.findByRole("status")).toHaveTextContent("Zapisano zadanie");
    });

    it("nie wysyła niepoprawnego JSON i nie usuwa wpisanej treści", async () => {
        const post = vi.fn();
        server.use(...common(), http.post(base, post));
        const { user } = renderWithRouter(<CompetitionTasksScreen />);
        await user.click(await screen.findByRole("button", { name: "Dodaj zadanie" }));
        await user.type(screen.getByLabelText("Nazwa zadania"), "Test");
        await user.click(screen.getByRole("button", { name: "Dane zaawansowane" }));
        const input = screen.getByLabelText("Treść zadania (JSON)");
        await user.clear(input);
        await user.type(input, "błędny json");
        await user.click(screen.getByRole("button", { name: "Zapisz" }));
        expect(screen.getByRole("alert")).toHaveTextContent("poprawnym obiektem JSON");
        expect(input).toHaveValue("błędny json");
        expect(post).not.toHaveBeenCalled();
    });

    it("dodaje poprawną odpowiedź i pokazuje ją w oknie zadania", async () => {
        const bodies: unknown[] = [];
        server.use(
            ...common(),
            http.get(answerUrl, () => HttpResponse.json({ data: [] })),
            http.post(answerUrl, async ({ request }) => {
                const body = await request.json();
                bodies.push(body);
                return HttpResponse.json({
                    data: { id: "answer-1", taskItemId: task.id, answer: "A", version: 1 },
                });
            })
        );
        const { user } = renderWithRouter(<CompetitionTasksScreen />);
        await user.click(await screen.findByRole("button", { name: "Poprawne odpowiedzi" }));
        const dialog = screen.getByRole("dialog", { name: "Poprawne odpowiedzi" });
        await screen.findByText("Brak poprawnych odpowiedzi.");
        await user.type(within(dialog).getByLabelText("Nowa poprawna odpowiedź"), "A");
        await user.click(within(dialog).getByRole("button", { name: "Dodaj odpowiedź" }));
        expect(await within(dialog).findByText("A")).toBeInTheDocument();
        expect(bodies).toEqual([{ answer: "A" }]);
    });
});
