/**
 * Petit pub/sub découplant `httpClient` (qui détecte qu'une session n'est plus valable, sur un 401
 * non récupérable) de `features/auth` (qui détient l'état réactif de session). `shared` ne doit rien
 * importer de `features` — voir le commentaire de session.ts — donc httpClient ne peut pas appeler
 * directement le store d'auth ; il émet un événement, et authStore s'y abonne.
 */

type Listener = () => void;

const listeners = new Set<Listener>();

export function onSessionExpired(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function emitSessionExpired(): void {
  for (const listener of listeners) {
    listener();
  }
}
