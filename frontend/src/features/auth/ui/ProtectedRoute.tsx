import { Navigate, Outlet, useLocation } from "react-router-dom";
import { Spinner } from "@/shared/ui/Spinner";
import { useAuthStore } from "../model/authStore";

/** Garde de route : rend les routes enfants si authentifié, redirige vers /login sinon (en
 * conservant la page visée pour y revenir après connexion). Le statut "idle"/"loading" couvre la
 * fenêtre de bootstrap (voir app/providers/AuthBootstrap) : on affiche un spinner plutôt que de
 * rediriger prématurément vers /login avant même d'avoir tenté de restaurer la session. */
export function ProtectedRoute() {
  const status = useAuthStore((state) => state.status);
  const location = useLocation();

  if (status === "idle" || status === "loading") {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner className="h-6 w-6" />
      </div>
    );
  }

  if (status === "unauthenticated") {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
