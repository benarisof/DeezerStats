import { Link } from "react-router-dom";
import { RegisterForm } from "@/features/auth/ui/RegisterForm";

export function RegisterPage() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-6 px-6 py-10">
      <div className="w-full max-w-sm overflow-hidden rounded-lg border border-border bg-background shadow-lg">
        <div className="bg-gradient-to-br from-accent to-accent/60 px-6 py-8 text-center">
          <h1 className="text-2xl font-semibold text-white">Deezer Stats</h1>
          <p className="mt-1 text-sm text-white/80">
            Créez votre compte pour suivre vos écoutes.
          </p>
        </div>
        <div className="p-6">
          <RegisterForm />
        </div>
      </div>
      <p className="text-sm text-muted-foreground">
        Déjà un compte ?{" "}
        <Link to="/login" className="text-accent hover:underline">
          Se connecter
        </Link>
      </p>
    </div>
  );
}
