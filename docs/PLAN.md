# Plan opérationnel — Deezer Stats

Ce document découpe la réalisation de la solution en phases, chaque phase en tickets.
La progression suit l'architecture hexagonale : Domain → Application → Infrastructure →
API → Frontend → Qualité/Perf → Déploiement. Statut au 2026-07-21, basé sur l'historique
Git et le code existant.

Légende : ✅ fait · 🔶 partiellement fait · ⬜ à faire

## Phase 1 — Fondations & conventions transverses ✅

| # | Ticket | Statut |
|---|--------|--------|
| 1.1 | Scaffold monorepo (backend hexagonal + frontend feature-based) | ✅ |
| 1.2 | Conventions de code (.editorconfig, analyzers .NET + StyleCop, Prettier, oxlint) | ✅ |
| 1.3 | CI GitHub Actions backend (build/format/test) et frontend (lint/format/build) | ✅ |
| 1.4 | docker-compose de dev (postgres, meilisearch, api hot-reload, frontend hot-reload) | ✅ |
| 1.5 | Contrat OpenAPI v0.1.0 (auth, stats, tops, historique, item, recherche, import) | ✅ |

## Phase 2 — Domain Layer (DDD) ✅

| # | Ticket | Statut |
|---|--------|--------|
| 2.1 | Aggregates `Track`, `User` + entités `Album`, `Artist`, `ListeningEvent` | ✅ |
| 2.2 | Value Objects (`DateRange`, `Duration`, `Email`, `Isrc`, `PlayCount`) + invariants | ✅ |
| 2.3 | Décision ADR : la page "item artiste" n'a pas de durée/date de sortie propres → remplacées par des agrégats (nb d'albums/morceaux distincts) | ✅ |
| 2.4 | Tests unitaires domaine (TDD) | ✅ |

## Phase 3 — Application Layer : Ports & premiers Use Cases ✅

| # | Ticket | Statut |
|---|--------|--------|
| 3.1 | Ports repositories (Album, Artist, Track, ListeningEvent, User) | ✅ |
| 3.2 | Ports services externes : `IDeezerEnrichmentPort`, `IExcelParserPort` | ✅ |
| 3.3 | Use case *Import listening history* (avec logique d'ignorance des doublons) | ✅ |
| 3.4 | Use case *Get-or-enrich track* (cache DB → fallback API Deezer) | ✅ |
| 3.5 | Use cases Auth (`RegisterUser`, `AuthenticateUser`) | ✅ |
| 3.6 | Tests unitaires Application (Moq sur les ports) | ✅ |

## Phase 4 — Infrastructure : Persistence PostgreSQL ✅

| # | Ticket | Statut |
|---|--------|--------|
| 4.1 | `ApplicationDbContext` + configurations EF Core (`Track`, `ListeningEvent`) | ✅ |
| 4.2 | Migration initiale (`InitialCreate`) | ✅ |
| 4.3 | Repositories EF (Album, Artist, Track, ListeningEvent, User) | ✅ |
| 4.4 | Adapter Excel (`ClosedXmlExcelParser`) | ✅ |
| 4.5 | Adapters sécurité (`BCryptPasswordHasher`, `JwtAccessTokenGenerator`) | ✅ |
| 4.6 | Tests infra (repos + adapters) | ✅ |

## Phase 5 — Validation & robustesse API ✅

| # | Ticket | Statut |
|---|--------|--------|
| 5.1 | Pipeline FluentValidation (validators par commande) | ✅ |
| 5.2 | Middleware global de gestion des exceptions → `ProblemDetails` (RFC 7807) | ✅ |

## Phase 6 — Authentification complète ✅

| # | Ticket | Statut |
|---|--------|--------|
| 6.1 | `POST /auth/register`, `POST /auth/login` | ✅ |
| 6.2 | Entité refresh token (rotation) + `POST /auth/refresh` | ✅ |
| 6.3 | `POST /auth/logout` (révocation du refresh token courant) | ✅ |
| 6.4 | `GET /auth/me` + middleware `JwtBearer` + `[Authorize]` global par défaut | ✅ |
| 6.5 | Tests d'intégration du parcours register → login → refresh → logout | ✅ |

## Phase 7 — Import Excel bout-en-bout ✅

| # | Ticket |
|---|--------|
| 7.1 | `POST /imports` (upload `multipart/form-data`, branchement sur le use case existant) |
| 7.2 | Vérification déduplication au niveau repository (contrainte unique sur les colonnes clés de la ligne d'écoute) |
| 7.3 | Mapping du rapport d'import (`ImportReport` : importedCount/skippedCount/errorCount/errors) |
| 7.4 | Tests d'intégration avec fichier Excel réel contenant des doublons et des lignes invalides |

## Phase 8 — Enrichissement Deezer ✅

| # | Ticket |
|---|--------|
| 8.1 | Adapter `IDeezerEnrichmentPort` : `HttpClient` typé vers l'API publique Deezer + résilience (Polly : retry/timeout) |
| 8.2 | Persistance des métadonnées enrichies (cover, durée, date de sortie) — stratégie cache-first déjà actée par le use case `GetOrEnrichTrackUseCase` |
| 8.3 | Déclenchement de l'enrichissement en tâche de fond après un import (`IHostedService`/`Channel<T>`, pour ne pas bloquer la réponse HTTP) |
| 8.4 | Tests avec `HttpClient` mocké (cache hit / cache miss / échec API Deezer) |

## Phase 9 — Endpoints de consultation (stats, tops, historique, item) ✅

| # | Ticket |
|---|--------|
| 9.1 | `GET /stats/home` (top 10 albums/artistes/morceaux) |
| 9.2 | `GET /albums/top`, `/artists/top`, `/tracks/top` (top 100, paginé) |
| 9.3 | `GET /history` (100 derniers morceaux écoutés, paginé) |
| 9.4 | `GET /albums/{id}`, `GET /artists/{id}` (page item) |
| 9.5 | Filtrage par plage de dates (`from`/`to`) sur toutes ces routes |
| 9.6 | Index SQL sur `PlayCount`/`ListenedAt` + tests d'intégration et de perf |

## Phase 10 — Moteur de recherche (Meilisearch) ⬜

| # | Ticket |
|---|--------|
| 10.1 | Indexation albums/artistes/morceaux dans Meilisearch, synchronisée à l'import/enrichissement |
| 10.2 | `GET /search/suggestions` (autocomplétion, déclenchée côté front à partir de 4 caractères) |
| 10.3 | `GET /search` (recherche complète, clic suggestion ou touche Entrée, paginée) |
| 10.4 | Tests d'intégration recherche : tolérance aux fautes de frappe, perf sur ~50 000 lignes |

*Le contrat OpenAPI sert de source de vérité pendant les phases 1 à 10 : le frontend peut être développé contre un mock généré à partir de `docs/api/openapi.yaml` pendant que le backend implémente chaque endpoint.*

## Phase 11 — Frontend : fondations & authentification ⬜

| # | Ticket |
|---|--------|
| 11.1 | Setup React 19 + Vite + TS + Tailwind v4, charte graphique inspirée de Deezer |
| 11.2 | Router (react-router) + layout header (nav, recherche, bouton upload, sélecteur de plage de dates) |
| 11.3 | State global : Zustand (session/auth) + TanStack Query (data fetching) + intercepteur JWT avec refresh automatique |
| 11.4 | Pages Login/Register + garde de routes (redirection si non connecté) |

## Phase 12 — Frontend : pages de consultation ⬜

| # | Ticket |
|---|--------|
| 12.1 | Page d'accueil (top 10 albums/artistes/morceaux, covers, compteurs) |
| 12.2 | Pages Top Albums / Top Artistes / Top Morceaux (top 100, pagination) |
| 12.3 | Page Historique (100 derniers morceaux) |
| 12.4 | Page Item (détail album ou artiste, liste des tracks triée par écoutes) |
| 12.5 | Composant recherche : suggestions dès 4 caractères, navigation clavier, clic ou Entrée |
| 12.6 | Composant upload Excel + affichage du rapport d'import |
| 12.7 | Sélecteur de plage de dates connecté aux query params (répercuté sur toutes les pages) |

## Phase 13 — Qualité, performance, tests end-to-end ⬜

| # | Ticket |
|---|--------|
| 13.1 | Tests frontend (Vitest + Testing Library) — à activer dans `frontend-ci.yml` (étape déjà prévue en commentaire) |
| 13.2 | Tests e2e (Playwright) sur les parcours principaux (connexion, upload, navigation tops, recherche) |
| 13.3 | Audit performance (indices DB, pagination/infinite scroll, lazy loading des covers, cache HTTP) |
| 13.4 | Accessibilité (a11y) et responsive |

## Phase 14 — Conteneurisation & déploiement ⬜

| # | Ticket |
|---|--------|
| 14.1 | Dockerfiles de production multi-stage (backend .NET, frontend build servi par nginx) |
| 14.2 | `docker-compose.prod.yml` (variables d'environnement, secrets, réseaux dédiés) |
| 14.3 | Documentation de déploiement + README complet |
| 14.4 | (Optionnel) pipeline CD |

---

## Prochaine étape suggérée

La Phase 6 (authentification) est terminée : elle débloquait la Phase 9 (endpoints protégés) et la
Phase 11 (frontend auth). Les phases 7/8/9/10 sont également faites côté backend (import →
enrichissement → consultation → recherche) ; il reste à démarrer le frontend (phases 11/12) contre le
contrat OpenAPI, puis la phase 13 (qualité/perf/e2e) et la phase 14 (déploiement).
