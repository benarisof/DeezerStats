import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { cn } from "@/shared/lib/cn";
import { Cover } from "./Cover";

interface MediaCardProps {
  title: string;
  subtitle?: string;
  coverUrl: string | null;
  meta: string;
  shape?: "square" | "circle";
  /** Absente pour les morceaux : pas de page de détail dédiée (voir SearchBox.suggestionHref). */
  href?: string;
  /** Badge de classement en coin de la cover, affiché uniquement sur les mosaïques des pages Top
   * (volontairement absent des carrousels de la home, pour rester visuellement épuré). */
  rank?: number;
  /** "carousel" : largeur fixe + point d'ancrage du scroll-snap. "grid" : occupe toute la cellule de
   * la mosaïque (voir shared/ui/Carousel et les pages Top*). */
  variant: "carousel" | "grid";
}

const baseClasses =
  "group flex flex-col gap-2 rounded-lg p-2 text-left transition hover:scale-[1.03] hover:bg-surface hover:shadow-lg";

const variantClasses: Record<MediaCardProps["variant"], string> = {
  carousel: "w-36 shrink-0 snap-start sm:w-40",
  grid: "w-full",
};

export function MediaCard({
  title,
  subtitle,
  coverUrl,
  meta,
  shape = "square",
  href,
  rank,
  variant,
}: MediaCardProps) {
  const content: ReactNode = (
    <>
      <div className="relative">
        <Cover src={coverUrl} alt={title} shape={shape} />
        {rank !== undefined && (
          <span className="absolute -top-1 -left-1 rounded-full bg-background px-1.5 py-0.5 text-xs font-semibold text-foreground shadow">
            {rank}
          </span>
        )}
      </div>
      <div
        className={cn(
          "flex min-w-0 flex-col",
          shape === "circle" && "items-center text-center",
        )}
      >
        <span className="truncate text-sm font-medium text-foreground">
          {title}
        </span>
        {subtitle && (
          <span className="truncate text-xs text-muted-foreground">
            {subtitle}
          </span>
        )}
        <span className="truncate text-xs text-muted-foreground">{meta}</span>
      </div>
    </>
  );

  const className = cn(baseClasses, variantClasses[variant]);

  if (href) {
    return (
      <Link to={href} className={className}>
        {content}
      </Link>
    );
  }

  return <div className={className}>{content}</div>;
}
