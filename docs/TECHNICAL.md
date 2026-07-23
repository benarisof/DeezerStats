# Deezer Stats — Documentation technique

> Décrit l'architecture, les choix techniques et les conventions de la solution. Pour le
> périmètre fonctionnel, voir [`FUNCTIONAL.md`](./FUNCTIONAL.md). Pour le contrat d'API complet
> (endpoints, schémas, codes d'erreur), voir [`api/openapi.yaml`](./api/openapi.yaml) — c'est la
> source de vérité pour l'intégration HTTP, non dupliquée ici. Pour les schémas visuels (C4,
> ERD, séquences), voir [`diagrammes/DIAGRAMS.md`](./diagrammes/DIAGRAMS.md).

## 1. Vue d'ensemble

Monorepo à deux applications, communiquant uniquement en HTTP/JSON :

```
deezer-stats/
├── backend/    API .NET 10 — architecture hexagonale (Domain/Application/Infrastructure/Api)
├── frontend/   SPA React 19 — Feature-Sliced Design
├── docs/       PLAN.md (suivi de réalisation), FUNCTIONAL.md, TECHNICAL.md, api/openapi.yaml
└── docker-compose.yml   Environnement de dev complet (Postgres, Meilisearch, API, frontend)
```

Le backend expose une API REST versionnée (`/api/v1`) consommée exclusivement par le frontend.
Aucune donnée n'est partagée entre utilisateurs : chaque requête de consultation est scopée à
l'utilisateur authentifié (extrait du JWT).

## 2. Stack technique

| Couche | Techno |
|---|---|
| Backend runtime | .NET 10, C# |
| Backend web | ASP.NET Core (contrôleurs MVC, pas de Minimal API) |
| Base de données | PostgreSQL 17, EF Core 10 (Npgsql) |
| Recherche | Meilisearch v1.48 (autocomplétion + recherche complète, tolérante aux fautes) |
| Validation | FluentValidation |
| Résilience HTTP | Polly (retry/timeout sur les appels à l'API publique Deezer) |
| Parsing Excel | ClosedXML |
| Sécurité | JWT (access token) + refresh token en rotation, BCrypt (hash mots de passe) |
| Tests backend | xUnit — NSubstitute + FluentAssertions (convention actuelle) ou Moq + `Assert` natif (fichiers plus anciens, non migrés) |
| Frontend runtime | React 19, TypeScript 6 (strict), Vite 8 |
| Frontend état | Zustand (session/auth, filtre de dates), TanStack Query v5 (data fetching + cache) |
| Frontend routing | react-router v7 |
| Frontend style | Tailwind CSS v4 (`@tailwindcss/vite`, config CSS-first via `@theme inline`, pas de `tailwind.config.*`) |
| Tests frontend | Vitest + Testing Library |
| Qualité frontend | oxlint, Prettier |
| Qualité backend | analyzers .NET + StyleCop, `dotnet format` |
| Conteneurisation dev | docker-compose (hot-reload des deux applications) |

## 3. Backend — architecture hexagonale

Quatre projets, dépendances à sens unique (`Api → Infrastructure → Application → Domain`) :

```
DeezerStats.Domain          Aucune dépendance externe. Entités, Value Objects, règles métier pures.
DeezerStats.Application      Dépend uniquement de Domain (+ FluentValidation, Logging.Abstractions).
                              Ports (interfaces) + Use Cases. Aucune référence à EF Core.
DeezerStats.Infrastructure    Implémente les ports : EF Core/Postgres, adapters HTTP Deezer,
                              Meilisearch, ClosedXML, BCrypt/JWT.
DeezerStats.Api               Contrôleurs, middleware, DI, pipeline HTTP. Point d'entrée du process.
```

Ce découpage a été vérifié empiriquement (graphe de dépendances des `.csproj`) : `Application`
n'a aucune référence à EF Core, ce qui garantit que les use cases restent testables sans base de
données et remplaçables (ex. changer de SGBD) sans toucher à la logique métier.

### 3.1 Domain

Agrégats (`Entity<Guid>` + `IAggregateRoot`), constructeur privé pour EF Core, propriétés en
lecture seule mutées uniquement via des méthodes métier explicites :

| Agrégat | Rôle | Invariants notables |
|---|---|---|
| `User` | Compte utilisateur | Email + hash de mot de passe + nom affiché obligatoires |
| `RefreshToken` | Jeton de rafraîchissement (racine distincte de `User`, pour recherche directe par hash sans charger l'agrégat utilisateur) | Révocation idempotente, trace le jeton de remplacement lors d'une rotation |
| `Artist` | Artiste | Nom + version normalisée (recherche/unicité) ; `IsEnriched` = pochette connue |
| `Album` | Album, rattaché à un `Artist` | Titre + version normalisée ; `IsEnriched` = pochette + date de sortie + durée connues |
| `Track` | Morceau, rattaché à un `Artist` et un `Album` | ISRC, titre, `FeaturedArtists` en texte libre (affichage uniquement, ne crée jamais d'entité) ; `IsEnriched` = durée + pochette connues |
| `ListeningEvent` | Une écoute (utilisateur + morceau + durée + date) | Date d'écoute jamais dans le futur (tolérance 5 min) |

Value Objects : `Email`, `Isrc`, `Duration`, `DateRange`.

### 3.2 Application

- **Ports** (`Ports/`) : interfaces pour tout ce qui est "impur" — repositories
  (`IAlbumRepository`, `IArtistRepository`, `ITrackRepository`, `IListeningEventRepository`,
  `IUserRepository`, `IRefreshTokenRepository`), `IUnitOfWork`, services externes
  (`IDeezerEnrichmentPort`, `IExcelParserPort`, `ISearchEnginePort`), sécurité
  (`IPasswordHasher`, `IAccessTokenGenerator`, `IRefreshTokenGenerator`), et
  `ICatalogEnrichmentCoordinator` (voir §3.4).
- **Use Cases** (`UseCases/`, un dossier par domaine fonctionnel) : `Users/` (register, login,
  refresh, logout, get current user), `Import/` (import du fichier Excel), `Stats/Home`,
  `Stats/TopAlbums|TopArtists|TopTracks`, `Stats/Album|Artist` (détail), `Stats/History`,
  `Albums|Artists|Tracks` (`GetOrEnrichXUseCase`, cache-first vers Deezer), `Search/`
  (suggestions + recherche complète).
- **Validation** : un validator FluentValidation par commande/requête paginée (mutualisé pour la
  pagination via `PagedQueryValidator<T>`, hérité par les quatre validators de listes paginées
  sans rien y ajouter).
- **Unit of Work** : chaque repository ne fait que suivre les changements (`AddAsync`/
  `UpdateAsync` n'appellent jamais `SaveChangesAsync`) ; c'est le use case qui décide du
  périmètre de la transaction en appelant explicitement `IUnitOfWork.SaveChangesAsync()` une
  seule fois, y compris lorsque plusieurs écritures logiquement liées doivent être committées
  ensemble (ex. `RegisterUserUseCase` : création de l'utilisateur + émission du refresh token en
  un seul aller-retour base).

### 3.3 Infrastructure

- **Persistence** (`Persistence/`) : `ApplicationDbContext` (EF Core), une classe de
  configuration par entité (`Configuration/`), repositories concrets, `UnitOfWork`,
  `ListeningStatsQueryService` (requêtes d'agrégation optimisées, `GROUP BY` poussé en SQL plutôt
  qu'une agrégation en mémoire — voir §3.5). Sept migrations à ce jour, dont la création
  initiale, l'ajout des contraintes de dédoublonnage du catalogue, le retrait de l'ISRC de
  `ListeningEvent`, la contrainte d'unicité sur l'email, l'ajout des refresh tokens, et l'ajout
  des featured artists sur `Track`.
- **Adapters** (`Adapters/`) :
  - `Deezer/DeezerHttpEnrichmentAdapter` : `HttpClient` typé vers l'API publique Deezer, avec
    retry/timeout Polly.
  - `Catalog/CatalogEnrichmentCoordinator` : voir §3.4.
  - `Search/MeilisearchAdapter` + `MeilisearchInitializerService` (configuration de l'index au
    démarrage) + `MeilisearchOptionsValidator` (garde-fou de démarrage, voir §6).
  - `Excel/ClosedXmlExcelParser` : lecture du fichier Excel Deezer.
  - `Security/BCryptPasswordHasher`, `JwtAccessTokenGenerator` (+ `JwtSettingsValidator`),
    `RefreshTokenGenerator`.

### 3.4 Enrichissement à la demande (`CatalogEnrichmentCoordinator`)

Les métadonnées Deezer (pochette, durée, date de sortie) ne sont récupérées ni à l'import ni en
tâche de fond planifiée : elles sont enrichies "à la demande", la première fois qu'une liste ou
une page détail les affiche réellement.

- Pour une **page détail** (`GetAlbumDetailUseCase`/`GetArtistDetailUseCase`) : un seul appel
  Deezer si l'élément n'est pas déjà enrichi, puis persistance et ré-indexation Meilisearch
  systématiques.
- Pour une **liste** (accueil, tops) : `CoverEnrichmentHelper` repère les éléments sans
  pochette dans la page déjà obtenue, puis délègue à `CatalogEnrichmentCoordinator` qui les
  enrichit **en parallèle, à concurrence bornée** (10 max — `Parallel.ForEachAsync`), chaque
  élément dans son propre `IServiceScope` (un `DbContext` scoped n'est pas thread-safe, donc pas
  partageable entre les tâches parallèles). Une erreur sur un élément (Deezer indisponible,
  élément supprimé entre-temps) est journalisée et absorbée : elle ne fait jamais échouer les
  autres éléments ni la requête de liste qui a déclenché l'enrichissement. Les documents
  fraîchement enrichis sont ré-indexés dans Meilisearch en une seule passe à la fin.
- Un élément déjà enrichi (`IsEnriched == true`) n'est jamais réinterrogé.

### 3.5 Performance des requêtes de statistiques

`ListeningStatsQueryService` pousse les agrégations (`GROUP BY` + `ORDER BY count(*) DESC` +
`LIMIT`) directement en SQL via une projection en **type anonyme** (EF Core traduit correctement
un `GroupBy` projeté vers un type anonyme composite, mais échoue à traduire la même requête
projetée vers un type nommé — limitation vérifiée empiriquement), puis mappe le résultat vers les
DTOs nommés en mémoire. Les tops home (10 éléments) et les tops complets (jusqu'à 100) partagent
la même implémentation avec un paramètre `take` différent.

### 3.6 Pipeline HTTP (`Program.cs`)

Ordre du middleware : `ExceptionHandlingMiddleware` → `UseHttpsRedirection` → `UseCors` →
`UseAuthentication` → `UseAuthorization` → `MapControllers`. Points notables :

- **Migrations automatiques** au démarrage (`dbContext.Database.MigrateAsync()`), idempotent,
  ignoré par le provider EF Core InMemory utilisé dans les tests d'intégration.
- **`ExceptionHandlingMiddleware`** traduit les exceptions applicatives en `ProblemDetails`
  (RFC 7807) : `ValidationException` (FluentValidation) → 400 avec `errors` par champ,
  `ConflictException` → 409, `AuthenticationFailedException` → 401, `DomainException` → 400.
  À part : un paramètre non-nullable manquant/vide (ex. `q` sur `/search`) est rejeté **avant**
  ce middleware par la validation implicite d'ASP.NET Core sur les types non-nullables (message
  générique en anglais, `type`/`traceId` renseignés — contrairement au format ci-dessus). Détail
  complet des deux formats dans `openapi.yaml` (schéma `ProblemDetails`).
- **Politique d'autorisation par défaut** : `RequireAuthenticatedUser()` en `FallbackPolicy`, donc
  tout endpoint est protégé par JWT sauf marquage explicite `[AllowAnonymous]` (register/login/
  refresh).

## 4. Authentification & sécurité

- **Access token JWT** (1h, `Jwt:ExpirationInMinutes` = 60), signé HMAC (`Jwt:Key`), validé
  (issuer/audience/lifetime/signature) via `AddJwtBearer`.
- **Refresh token** (30 jours, `AuthRules.RefreshTokenExpirationInDays`) : valeur aléatoire
  générée côté serveur, jamais stockée en clair (seul le hash est persisté), rotation à chaque
  utilisation (`POST /auth/refresh` révoque l'ancien token et en émet un nouveau). La réutilisation
  d'un refresh token déjà révoqué déclenche la révocation de **toutes** les sessions actives de
  l'utilisateur (indice de vol de session).
- **Mots de passe** hachés avec BCrypt (`BCryptPasswordHasher`), jamais stockés en clair.
- **Garde-fous de démarrage** : si `Jwt:Key` ou `Meilisearch:MasterKey` valent encore leur
  placeholder de développement (`appsettings.json`) en dehors de `Development`, le process refuse
  de démarrer (`InvalidOperationException` explicite) plutôt que de tourner silencieusement avec
  un secret trivial et public.
- **CORS** : origine autorisée configurée via `Cors:AllowedOrigins` (dev : `http://localhost:5173`).

## 5. Frontend — Feature-Sliced Design

```
src/
├── app/        Bootstrap : router, providers (QueryClientProvider), layout racine
├── pages/      Un dossier par écran (ui/, parfois model/ + api/ propres à la page)
├── widgets/    Composition de plusieurs features (le Header)
├── features/   Une action utilisateur autonome (auth, recherche, upload, filtre de dates)
├── entities/   Types + accès API par entité métier (album, artist, track, user)
└── shared/     Primitives génériques sans connaissance du domaine (ui/, api/, lib/, config/)
```

Règle de dépendance stricte (imposée par convention, pas par un outil, mais vérifiée dans le
code) : une couche ne dépend que des couches en dessous d'elle. En particulier, `shared/` n'importe
jamais depuis `features/` — ex. `shared/api/httpClient` détecte une session expirée et
**émet un événement** plutôt que d'appeler directement le store d'auth (`features/auth`), qui
s'y abonne (`shared/api/authEvents.ts`, pattern pub/sub minimal).

### 5.1 État et données

- **Zustand** pour l'état applicatif transverse : `authStore` (utilisateur courant, statut de
  session, actions login/register/logout/bootstrap) et le store du filtre de plage de dates
  (partagé par toutes les pages de consultation, en mémoire uniquement — se réinitialise à un
  rechargement complet de la page, comportement voulu).
- **TanStack Query** pour toutes les données serveur (`staleTime` 30s, pas de retry sur les
  erreurs 4xx hors réseau/5xx). Invalidation ciblée par préfixe de clé après un import réussi
  (`albums`, `artists`, `tracks`, `history`, `home-stats`).
- **`httpClient`** (`shared/api/httpClient.ts`) : wrapper `fetch` centralisant l'ajout du header
  `Authorization`, le rafraîchissement automatique du token sur un 401 (une seule tentative de
  refresh partagée entre requêtes concurrentes — `refreshPromise` en vol unique), et la
  traduction des réponses non-2xx en `ApiError` (miroir de `ProblemDetails`, expose
  `fieldErrors` pour l'affichage des erreurs de formulaire).
- **Session** : l'access token ne vit qu'en mémoire (perdu au rechargement, réduit la fenêtre
  d'exposition XSS) ; le refresh token est persisté en `localStorage` et sert à restaurer la
  session au chargement de l'app (`authStore.bootstrap`).

### 5.2 UI et thème

- Design system minimal dans `shared/ui/` (`Button`, `Input`, `Pagination`, `Spinner`,
  `ErrorState`, `Cover`, `MediaCard`, `Carousel`) construit sur des tokens Tailwind exposés via
  `@theme inline` dans `index.css`, eux-mêmes pilotés par des custom properties CSS — thème sombre
  unique (fond quasi-noir, texte blanc/mauve, accent violet), pas de bascule clair/sombre.
- `Cover` gère la pochette manquante ou en échec de chargement avec un dégradé + icône de
  remplacement plutôt qu'une case vide. `MediaCard` factorise la carte (pochette + infos + nombre
  d'écoutes) réutilisée à l'identique dans les carrousels de l'accueil (`variant="carousel"`) et
  les mosaïques des pages Top (`variant="grid"`). `Carousel` implémente le défilement au scroll
  natif (CSS scroll-snap) avec flèches conditionnelles, sans dépendance externe.
- `Button` : `shape="pill"` (utilisé par le bouton d'import) vs `rectangle` (défaut) — jamais deux
  classes Tailwind concurrentes sur la même propriété CSS (la petite fonction `cn()` maison ne
  résout pas les conflits de classes, contrairement à `tailwind-merge`).

### 5.3 Routing

`react-router` (mode data router). Racine : `/login` et `/register` publiques ; toutes les autres
routes sous un `ProtectedRoute` (redirige vers `/login` si non authentifié, affiche un spinner
pendant la restauration de session) puis un `RootLayout` commun (header + zone de contenu).

## 6. Contrat d'API

Le contrat HTTP complet (endpoints, schémas de requête/réponse, codes d'erreur, comportements de
validation) est documenté dans [`docs/api/openapi.yaml`](./api/openapi.yaml) — tenu à jour au fil
des évolutions de chaque endpoint et vérifié empiriquement contre l'implémentation réelle
(y compris les comportements non triviaux comme les deux formats de réponse 400 possibles). Ne
pas dupliquer ce contrat ici.

## 7. Tests

| Projet | Nombre de tests | Convention |
|---|---|---|
| `DeezerStats.Domain.UnitTests` | 41 | xUnit |
| `DeezerStats.Application.UnitTests` | 78 | NSubstitute + FluentAssertions (majoritaire) ou Moq + `Assert` (fichiers auth plus anciens) |
| `DeezerStats.Infrastructure.UnitTests` | 84 | xUnit + FluentAssertions, EF Core InMemory |
| `DeezerStats.Api.UnitTests` | 24 | xUnit |
| `DeezerStats.Api.IntegrationTests` | 13 | `CustomWebApplicationFactory`, EF Core InMemory, HTTP réel |
| **Backend total** | **240** | `dotnet test DeezerStats.slnx` |
| Frontend (Vitest) | 51 (9 fichiers) | Testing Library, ciblé sur `shared/` et `features/auth` — aucune page/composant de présentation testé (voir §9) |

Backend : couverture large sur `Domain`/`Application`/`Infrastructure`, y compris les scénarios de
concurrence auth (course d'inscription, réutilisation de refresh token) et l'enrichissement
(cache hit/miss/échec API Deezer mocké). `dotnet format --verify-no-changes` et les analyzers
StyleCop font partie de la boucle de vérification standard.

## 8. Environnement de développement

```bash
cp env.example .env   # ajuster si besoin, valeurs de dev par défaut fonctionnelles telles quelles
docker compose up
```

| Service | Port hôte | Rôle |
|---|---|---|
| `frontend` | 5173 | Vite dev server, hot-reload |
| `api` | 5231 | ASP.NET Core (`dotnet watch`), hot-reload |
| `postgres` | 5432 | Base applicative |
| `meilisearch` | 7700 | Moteur de recherche |

Les migrations EF Core s'appliquent automatiquement au démarrage de `api` (pas de commande
manuelle nécessaire). Le frontend cible l'API via `VITE_API_URL` (`http://localhost:5231/api/v1`
par défaut).

## 9. Limitations connues

- **Pas de tests e2e** (Playwright envisagé, non fait) ni d'audit d'accessibilité formel.
- **Couverture de tests frontend** concentrée sur `shared/`, `features/auth` et
  `features/upload` : aucune page ni composant de présentation (`MediaCard`, `Carousel`,
  `SearchBox`, les pages elles-mêmes) n'a de test dédié.
- **Duplication du bloc chargement/erreur** : `HomePage`, `TopAlbumsPage`, `TopArtistsPage`,
  `TopTracksPage`, `HistoryPage`, `SearchResultsPage` répètent le même bloc
  `isLoading`/`isError`/`ErrorState`, et quatre pages répètent le même `useEffect` de
  réinitialisation de pagination au changement de plage de dates — factorisable dans un hook ou
  un composant `QueryBoundary` partagé.
- **Pas de conteneurisation de production** ni de pipeline CD (docker-compose actuel = dev
  uniquement, avec bind mounts et hot-reload).
- **Recherche** : pas de navigation clavier (flèches) dans le menu de suggestions.

Pour le détail historique de la réalisation (tickets, statut par phase), voir
[`docs/PLAN.md`](./PLAN.md) — ce document liste l'avancement projet dans le temps et peut
comporter un statut légèrement en retard par rapport à l'état réel du code.
