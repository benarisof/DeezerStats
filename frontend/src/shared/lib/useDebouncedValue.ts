import { useEffect, useState } from "react";

/** Retourne `value`, mais mis à jour au plus une fois toutes les `delayMs` millisecondes — utilisé
 * par la recherche (features/search) pour éviter un appel réseau à chaque frappe. */
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timeout = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timeout);
  }, [value, delayMs]);

  return debounced;
}
