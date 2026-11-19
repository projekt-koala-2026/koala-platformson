import { render, type RenderOptions } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement, ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { LoadingProvider } from "../../src/contexts/LoadingContext";

interface RenderWithRouterOptions extends Omit<RenderOptions, "wrapper"> {
    initialEntries?: string[];
}

export const renderWithRouter = (
    element: ReactElement,
    { initialEntries = ["/"], ...renderOptions }: RenderWithRouterOptions = {}
) => {
    const Wrapper = ({ children }: { children: ReactNode }) => (
        <LoadingProvider>
            <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
        </LoadingProvider>
    );

    return {
        user: userEvent.setup(),
        ...render(element, { wrapper: Wrapper, ...renderOptions }),
    };
};
