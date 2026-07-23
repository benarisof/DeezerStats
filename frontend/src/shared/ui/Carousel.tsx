import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";

interface CarouselProps {
  children: ReactNode;
  ariaLabel: string;
}

function ChevronIcon({ direction }: { direction: "left" | "right" }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d={direction === "left" ? "M15 18l-6-6 6-6" : "M9 18l6-6-6-6"} />
    </svg>
  );
}

const arrowButtonClasses =
  "absolute top-1/2 z-10 -translate-y-1/2 rounded-full border border-border bg-background p-1.5 text-foreground shadow-lg transition hover:bg-surface";

/** Marge de tolérance pour ignorer les écarts d'arrondi flottant entre `scrollLeft`/`scrollWidth` et
 * la position réelle de bord, qui empêcheraient sinon les flèches de disparaître en fin de course. */
const SCROLL_EDGE_TOLERANCE_PX = 1;

/** Bande défilante horizontalement (scroll-snap natif, au doigt/trackpad) avec flèches
 * précédent/suivant qui n'apparaissent que quand il reste du contenu à faire défiler dans cette
 * direction. Pas d'auto-play (voir la demande initiale : défilement manuel uniquement). */
export function Carousel({ children, ariaLabel }: CarouselProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const updateScrollState = useCallback(() => {
    const el = scrollRef.current;

    if (!el) {
      return;
    }

    setCanScrollLeft(el.scrollLeft > SCROLL_EDGE_TOLERANCE_PX);
    setCanScrollRight(
      el.scrollLeft + el.clientWidth <
        el.scrollWidth - SCROLL_EDGE_TOLERANCE_PX,
    );
  }, []);

  useEffect(() => {
    updateScrollState();
    const el = scrollRef.current;

    if (!el) {
      return;
    }

    const observer = new ResizeObserver(updateScrollState);
    observer.observe(el);
    return () => observer.disconnect();
  }, [updateScrollState, children]);

  function scroll(direction: -1 | 1) {
    scrollRef.current?.scrollBy({
      left: direction * scrollRef.current.clientWidth * 0.8,
      behavior: "smooth",
    });
  }

  return (
    <div className="relative">
      {canScrollLeft && (
        <button
          type="button"
          aria-label="Précédent"
          onClick={() => scroll(-1)}
          className={`${arrowButtonClasses} -left-3`}
        >
          <ChevronIcon direction="left" />
        </button>
      )}

      <div
        ref={scrollRef}
        role="list"
        aria-label={ariaLabel}
        onScroll={updateScrollState}
        className="no-scrollbar flex gap-4 overflow-x-auto scroll-smooth snap-x snap-mandatory pb-1"
      >
        {children}
      </div>

      {canScrollRight && (
        <button
          type="button"
          aria-label="Suivant"
          onClick={() => scroll(1)}
          className={`${arrowButtonClasses} -right-3`}
        >
          <ChevronIcon direction="right" />
        </button>
      )}
    </div>
  );
}
