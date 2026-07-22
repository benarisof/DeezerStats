import { afterEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { useAuthStore } from "../model/authStore";
import { ProtectedRoute } from "./ProtectedRoute";

function renderWithRouter() {
  return render(
    <MemoryRouter initialEntries={["/protected"]}>
      <Routes>
        <Route path="/login" element={<p>Login Page</p>} />
        <Route element={<ProtectedRoute />}>
          <Route path="/protected" element={<p>Protected Content</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe("ProtectedRoute", () => {
  afterEach(() => {
    useAuthStore.setState({ user: null, status: "idle" });
  });

  it("shows a spinner while the session status is still idle", () => {
    useAuthStore.setState({ status: "idle" });

    renderWithRouter();

    expect(screen.getByRole("status")).toBeInTheDocument();
    expect(screen.queryByText("Protected Content")).not.toBeInTheDocument();
  });

  it("shows a spinner while bootstrap is loading", () => {
    useAuthStore.setState({ status: "loading" });

    renderWithRouter();

    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("redirects to /login when unauthenticated", () => {
    useAuthStore.setState({ status: "unauthenticated" });

    renderWithRouter();

    expect(screen.getByText("Login Page")).toBeInTheDocument();
    expect(screen.queryByText("Protected Content")).not.toBeInTheDocument();
  });

  it("renders the protected content when authenticated", () => {
    useAuthStore.setState({ status: "authenticated" });

    renderWithRouter();

    expect(screen.getByText("Protected Content")).toBeInTheDocument();
  });
});
