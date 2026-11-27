interface RecordDatesProps {
    createdAt?: string | null;
    updatedAt?: string | null;
    className?: string;
    dateOnly?: boolean;
}

const formatDateTime = (value: string, dateOnly: boolean) => {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
        ? null
        : {
              dateTime: date.toISOString(),
              label: dateOnly ? date.toLocaleDateString("pl-PL") : date.toLocaleString("pl-PL"),
          };
};

const RecordDates = ({
    createdAt,
    updatedAt,
    className = "",
    dateOnly = false,
}: RecordDatesProps) => {
    const updated = updatedAt ? formatDateTime(updatedAt, dateOnly) : null;
    if (!updated) return null;
    const createdTime = createdAt ? Date.parse(createdAt) : Number.NaN;
    const updatedTime = Date.parse(updated.dateTime);
    const label =
        Number.isFinite(createdTime) && createdTime === updatedTime ? "Utworzono" : "Edytowano";

    return (
        <div className={`space-y-0.5 text-xs text-ink-muted ${className}`.trim()}>
            <p>
                {label}: <time dateTime={updated.dateTime}>{updated.label}</time>
            </p>
        </div>
    );
};

export default RecordDates;
