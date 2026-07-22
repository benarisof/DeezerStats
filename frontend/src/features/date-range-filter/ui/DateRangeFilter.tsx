import { useDateRangeParams } from "../model/useDateRangeParams";

export function DateRangeFilter() {
  const { range, setRange } = useDateRangeParams();

  return (
    <div className="flex items-center gap-2 text-sm">
      <label className="flex items-center gap-1" htmlFor="date-range-from">
        <span className="text-muted-foreground">Du</span>
        <input
          id="date-range-from"
          type="date"
          className="rounded-md border border-border bg-background px-2 py-1 text-foreground"
          value={range.from ?? ""}
          max={range.to}
          onChange={(event) =>
            setRange({ ...range, from: event.target.value || undefined })
          }
        />
      </label>
      <label className="flex items-center gap-1" htmlFor="date-range-to">
        <span className="text-muted-foreground">au</span>
        <input
          id="date-range-to"
          type="date"
          className="rounded-md border border-border bg-background px-2 py-1 text-foreground"
          value={range.to ?? ""}
          min={range.from}
          onChange={(event) =>
            setRange({ ...range, to: event.target.value || undefined })
          }
        />
      </label>
      {(range.from ?? range.to) && (
        <button
          type="button"
          className="text-muted-foreground hover:text-foreground text-xs underline"
          onClick={() => setRange({ from: undefined, to: undefined })}
        >
          Réinitialiser
        </button>
      )}
    </div>
  );
}
