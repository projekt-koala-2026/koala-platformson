// The backend compares answers as strings, so both editor and participant must use this format.
export const encodeMultipleChoice = (ids: readonly string[]): string =>
    JSON.stringify([...new Set(ids)].sort());

export const decodeMultipleChoice = (value: string): string[] | null => {
    try {
        const parsed: unknown = JSON.parse(value);
        if (!Array.isArray(parsed) || !parsed.every((item) => typeof item === "string"))
            return null;
        if (new Set(parsed).size !== parsed.length) return null;
        return parsed;
    } catch {
        return null;
    }
};
