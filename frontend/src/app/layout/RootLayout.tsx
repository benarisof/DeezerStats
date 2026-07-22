import { Outlet } from "react-router-dom";
import { Header } from "@/widgets/header/ui/Header";

export function RootLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex flex-1 flex-col px-6 py-6">
        <Outlet />
      </main>
    </div>
  );
}
