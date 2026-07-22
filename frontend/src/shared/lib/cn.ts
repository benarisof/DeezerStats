/** Fusionne des classes conditionnelles sans dépendance externe (clsx/tailwind-merge) : suffisant
 * tant que les primitives shared/ui restent simples et ne composent pas de classes conflictuelles. */
export function cn(
  ...classes: Array<string | false | null | undefined>
): string {
  return classes.filter(Boolean).join(" ");
}
