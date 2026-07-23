# Deezer Stats

[![Backend CI](https://github.com/benarisof/DeezerStats/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/benarisof/DeezerStats/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/benarisof/DeezerStats/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/benarisof/DeezerStats/actions/workflows/frontend-ci.yml)

Application web personnelle de consultation des statistiques d'écoute Deezer : importez vos
exports Excel mensuels, puis explorez vos tops albums/artistes/morceaux, votre historique
d'écoute et recherchez dans votre catalogue — le tout avec les pochettes récupérées
automatiquement depuis Deezer.

## Fonctionnalités

- Compte utilisateur (inscription/connexion), session persistante avec rotation automatique du
  refresh token.
- Import d'un fichier Excel mensuel Deezer, cumulatif et sans doublons.
- Page d'accueil : carrousels des top 10 albums/artistes/morceaux.
- Classements complets (top 100, paginés, mosaïque de cartes avec pochette).
- Pages détail album/artiste, historique d'écoute, recherche avec autocomplétion (tolérante aux
  fautes de frappe).
- Filtre par plage de dates, appliqué à toutes les pages de consultation.

Détail complet : [`docs/FUNCTIONAL.md`](docs/FUNCTIONAL.md).

## Stack

| | |
|---|---|
| Backend | .NET 10, ASP.NET Core, EF Core / PostgreSQL, architecture hexagonale |
| Frontend | React 19, TypeScript (strict), Vite, Tailwind CSS v4, Feature-Sliced Design |
| Recherche | Meilisearch |
| Auth | JWT (access token) + refresh token en rotation |

Détail complet : [`docs/TECHNICAL.md`](docs/TECHNICAL.md).

## Démarrage rapide

Prérequis : Docker + Docker Compose.

```bash
cp env.example .env
docker compose up
```

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API | http://localhost:5231/api/v1 |
| Meilisearch | http://localhost:7700 |

Les migrations de base de données s'appliquent automatiquement au démarrage de l'API — aucune
commande manuelle nécessaire. Les deux applications tournent en hot-reload (modifications du code
source reflétées immédiatement).

## Structure du repo

```
deezer-stats/
├── backend/    API .NET (Domain / Application / Infrastructure / Api)
├── frontend/   SPA React (app / pages / widgets / features / entities / shared)
├── docs/       Documentation fonctionnelle, technique, contrat d'API, suivi de réalisation
└── docker-compose.yml
```

## Développement

```bash
# Backend (depuis backend/)
dotnet test DeezerStats.slnx                       # 240 tests
dotnet format DeezerStats.slnx --verify-no-changes  # conventions de code

# Frontend (depuis frontend/)
npm test -- --run   # Vitest (51 tests) -- sans --run, lance le mode watch interactif
npm run lint         # oxlint
npm run format:check # Prettier
npm run build        # tsc --build + vite build
```

## Documentation

- [`docs/FUNCTIONAL.md`](docs/FUNCTIONAL.md) — fonctionnalités, écrans, règles métier
- [`docs/TECHNICAL.md`](docs/TECHNICAL.md) — architecture, stack, conventions
- [`docs/diagrammes/DIAGRAMS.md`](docs/diagrammes/DIAGRAMS.md) — schémas C4, ERD, séquences (import, enrichissement, auth), dépendances frontend
- [`docs/api/openapi.yaml`](docs/api/openapi.yaml) — contrat d'API
- [`docs/PLAN.md`](docs/PLAN.md) — plan de réalisation (suivi par phase)
