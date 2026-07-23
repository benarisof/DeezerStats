import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/shared/lib/cn";

type ButtonVariant = "primary" | "secondary" | "ghost";
type ButtonShape = "rectangle" | "pill";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  /** "pill" (rounded-full, plus de padding horizontal) pour un bouton plus allongé, cohérent avec
   * les autres éléments arrondis du header (recherche, nav) -- voir UploadButton. */
  shape?: ButtonShape;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: "bg-accent text-white hover:opacity-90",
  secondary: "border border-border text-foreground hover:bg-surface",
  ghost: "text-muted-foreground hover:text-foreground hover:bg-surface",
};

/* Radius et padding horizontal varient ensemble (jamais deux classes rounded-* concurrentes dans le
   même composant, voir shared/lib/cn.ts) : le shape choisi fixe l'un ET l'autre. */
const shapeClasses: Record<ButtonShape, string> = {
  rectangle: "rounded-md px-4",
  pill: "rounded-full px-5",
};

export function Button({
  variant = "primary",
  shape = "rectangle",
  className,
  disabled,
  ...props
}: ButtonProps) {
  return (
    <button
      className={cn(
        "inline-flex shrink-0 items-center justify-center gap-2 py-2 text-sm font-medium whitespace-nowrap",
        "cursor-pointer transition-colors disabled:cursor-not-allowed disabled:opacity-50",
        shapeClasses[shape],
        variantClasses[variant],
        className,
      )}
      disabled={disabled}
      {...props}
    />
  );
}
