import { NavLink } from "react-router-dom";
import { useAuthStore } from "@/features/auth/model/authStore";
import { DateRangeFilter } from "@/features/date-range-filter/ui/DateRangeFilter";
import { SearchBox } from "@/features/search/ui/SearchBox";
import { UploadButton } from "@/features/upload/ui/UploadButton";
import { Button } from "@/shared/ui/Button";
import { cn } from "@/shared/lib/cn";

const navItems = [
  { to: "/", label: "Accueil", end: true },
  { to: "/albums/top", label: "Albums" },
  { to: "/artists/top", label: "Artistes" },
  { to: "/tracks/top", label: "Morceaux" },
  { to: "/history", label: "Historique" },
];

export function Header() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  return (
    <header className="flex flex-col gap-3 border-b border-border bg-background px-6 py-3">
      <div className="flex flex-wrap items-center gap-4">
        <span className="text-lg font-semibold text-foreground">
          Deezer Stats
        </span>

        <nav className="flex items-center gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "rounded-md px-3 py-1.5 text-sm font-medium text-muted-foreground hover:text-foreground",
                  isActive && "bg-accent-bg text-accent",
                )
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          <SearchBox />
          <UploadButton />

          {user && (
            <div className="flex shrink-0 items-center gap-2 text-sm">
              <span className="whitespace-nowrap text-muted-foreground">
                {user.displayName}
              </span>
              <Button variant="ghost" onClick={() => void logout()}>
                Déconnexion
              </Button>
            </div>
          )}
        </div>
      </div>

      <DateRangeFilter />
    </header>
  );
}
