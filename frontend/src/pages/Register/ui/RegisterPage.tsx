import { Link } from "react-router-dom";
import { RegisterForm } from "@/features/auth/ui/RegisterForm";

export function RegisterPage() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-6 px-6">
      <h1 className="text-2xl font-semibold text-foreground">Deezer Stats</h1>
      <RegisterForm />
      <p className="text-sm text-muted-foreground">
        Déjà un compte ?{" "}
        <Link to="/login" className="text-accent hover:underline">
          Se connecter
        </Link>
      </p>
    </div>
  );
}
