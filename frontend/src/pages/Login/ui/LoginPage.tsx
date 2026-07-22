import { Link } from "react-router-dom";
import { LoginForm } from "@/features/auth/ui/LoginForm";

export function LoginPage() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-6 px-6">
      <h1 className="text-2xl font-semibold text-foreground">Deezer Stats</h1>
      <LoginForm />
      <p className="text-sm text-muted-foreground">
        Pas encore de compte ?{" "}
        <Link to="/register" className="text-accent hover:underline">
          Créer un compte
        </Link>
      </p>
    </div>
  );
}
