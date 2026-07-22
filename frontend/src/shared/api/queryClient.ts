import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "./apiError";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) => {
        // Un 401 est déjà géré par httpClient (tentative de refresh) ; un 404/400 ne deviendra
        // jamais valide en réessayant. Seules les erreurs réseau/5xx méritent une nouvelle tentative.
        if (error instanceof ApiError && error.status < 500) {
          return false;
        }

        return failureCount < 2;
      },
    },
  },
});
