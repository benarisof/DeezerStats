import { createBrowserRouter, Navigate } from "react-router-dom";
import { ProtectedRoute } from "@/features/auth/ui/ProtectedRoute";
import { LoginPage } from "@/pages/Login/ui/LoginPage";
import { RegisterPage } from "@/pages/Register/ui/RegisterPage";
import { HomePage } from "@/pages/Home/ui/HomePage";
import { TopAlbumsPage } from "@/pages/TopAlbums/ui/TopAlbumsPage";
import { TopArtistsPage } from "@/pages/TopArtists/ui/TopArtistsPage";
import { TopTracksPage } from "@/pages/TopTracks/ui/TopTracksPage";
import { HistoryPage } from "@/pages/History/ui/HistoryPage";
import { AlbumItemPage } from "@/pages/Item/ui/AlbumItemPage";
import { ArtistItemPage } from "@/pages/Item/ui/ArtistItemPage";
import { SearchResultsPage } from "@/pages/Search/ui/SearchResultsPage";
import { RootLayout } from "../layout/RootLayout";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <RootLayout />,
        children: [
          { index: true, element: <HomePage /> },
          { path: "albums/top", element: <TopAlbumsPage /> },
          { path: "albums/:albumId", element: <AlbumItemPage /> },
          { path: "artists/top", element: <TopArtistsPage /> },
          { path: "artists/:artistId", element: <ArtistItemPage /> },
          { path: "tracks/top", element: <TopTracksPage /> },
          { path: "history", element: <HistoryPage /> },
          { path: "search", element: <SearchResultsPage /> },
        ],
      },
    ],
  },
  { path: "*", element: <Navigate to="/" replace /> },
]);
