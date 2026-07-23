import { useState } from "react";
import { cn } from "@/shared/lib/cn";

interface CoverProps {
  src: string | null;
  alt: string;
  shape?: "square" | "circle";
  className?: string;
}

function MusicNoteIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-1/3 w-1/3 text-accent"
      aria-hidden="true"
    >
      <path d="M9 18V5l12-2v13" />
      <circle cx="6" cy="18" r="3" />
      <circle cx="18" cy="16" r="3" />
    </svg>
  );
}

/** Vignette de cover (album/artiste/morceau) : affiche l'image si `src` est fourni et se charge
 * correctement, sinon un dégradé de secours avec une icône note de musique -- jamais de case vide,
 * que la cover soit absente en base (`src === null`) ou que l'URL Deezer soit devenue invalide
 * (`onError`). */
export function Cover({ src, alt, shape = "square", className }: CoverProps) {
  const [failed, setFailed] = useState(false);

  return (
    <div
      className={cn(
        "relative aspect-square w-full shrink-0 overflow-hidden bg-surface",
        shape === "circle" ? "rounded-full" : "rounded-lg",
        className,
      )}
    >
      {src && !failed ? (
        <img
          src={src}
          alt={alt}
          loading="lazy"
          decoding="async"
          onError={() => setFailed(true)}
          className="h-full w-full object-cover"
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-accent-bg to-accent-border">
          <MusicNoteIcon />
        </div>
      )}
    </div>
  );
}
