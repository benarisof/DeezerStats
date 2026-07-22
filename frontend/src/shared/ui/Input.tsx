import type { InputHTMLAttributes } from "react";
import { cn } from "@/shared/lib/cn";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export function Input({ label, error, id, className, ...props }: InputProps) {
  return (
    <label className="flex flex-col gap-1 text-sm" htmlFor={id}>
      {label && <span className="font-medium text-foreground">{label}</span>}
      <input
        id={id}
        className={cn(
          "rounded-md border border-border bg-background px-3 py-2 text-foreground",
          "outline-none focus:border-accent-border focus:ring-2 focus:ring-accent-bg",
          error && "border-danger",
          className,
        )}
        aria-invalid={Boolean(error)}
        {...props}
      />
      {error && <span className="text-danger text-xs">{error}</span>}
    </label>
  );
}
