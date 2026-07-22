/**
 * Point d'accès unique aux variables d'environnement Vite : évite de disperser des
 * `import.meta.env.VITE_*` dans tout le code et fait échouer vite si une variable requise manque,
 * plutôt que de laisser un `undefined` silencieux se propager jusqu'à un appel réseau.
 */
function readRequiredEnv(key: string): string {
  const value = import.meta.env[key];

  if (!value) {
    throw new Error(
      `Variable d'environnement manquante : ${key}. Voir .env.example à la racine du frontend.`,
    );
  }

  return value;
}

export const env = {
  apiUrl: readRequiredEnv("VITE_API_URL"),
};
