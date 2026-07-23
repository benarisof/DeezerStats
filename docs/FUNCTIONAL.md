# Deezer Stats — Documentation fonctionnelle

> Décrit ce que fait la solution du point de vue de l'utilisateur : fonctionnalités, écrans,
> parcours et règles métier. Pour l'architecture et les choix techniques, voir
> [`TECHNICAL.md`](./TECHNICAL.md). Pour le contrat d'API détaillé, voir
> [`api/openapi.yaml`](./api/openapi.yaml).

## 1. Présentation

Deezer Stats est une application web personnelle qui reconstitue des statistiques d'écoute à
partir des exports Excel mensuels fournis par Deezer (l'historique d'écoute n'étant pas
disponible via une API publique officielle). Une fois l'historique importé, l'utilisateur peut
consulter ses classements (albums / artistes / morceaux les plus écoutés), son historique
d'écoute détaillé, rechercher dans son catalogue personnel, et filtrer toutes ces vues par plage
de dates.

Chaque compte est cloisonné : les statistiques d'un utilisateur ne sont jamais visibles par un
autre (toutes les requêtes de consultation sont scopées à l'utilisateur authentifié).

## 2. Fonctionnalités

### 2.1 Compte et authentification

- **Inscription** (email, mot de passe ≥ 8 caractères, nom affiché) — connecte automatiquement
  l'utilisateur après création du compte.
- **Connexion** (email + mot de passe).
- **Session persistante** : la session survit à la fermeture de l'onglet/du navigateur (elle est
  restaurée automatiquement au rechargement de la page), sans nécessiter de reconnexion tant que
  la session n'a pas expiré (30 jours d'inactivité).
- **Déconnexion** explicite.
- Toutes les pages de consultation sont protégées : un utilisateur non connecté est redirigé vers
  l'écran de connexion.

### 2.2 Import de l'historique d'écoute

- Bouton "Importer historique" dans l'en-tête, disponible sur toutes les pages authentifiées.
- Accepte le fichier Excel (`.xlsx`) mensuel exporté depuis Deezer.
- À l'issue de l'import, un rapport s'affiche : nombre de morceaux importés, nombre de lignes déjà
  connues (ignorées silencieusement, pas une erreur — permet de réimporter un fichier qui
  chevauche un import précédent sans créer de doublons), et nombre de lignes en erreur (ex. durée
  d'écoute négative dans le fichier source) avec le détail par ligne.
- Les imports sont cumulatifs : chaque nouvel import s'ajoute à l'historique déjà connu, sans
  jamais dupliquer une écoute déjà enregistrée.
- Après un import réussi, tous les classements et l'historique se mettent à jour automatiquement
  (pas besoin de recharger la page).

### 2.3 Page d'accueil

Vue d'ensemble sous forme de trois carrousels défilants (au doigt/trackpad, ou via les flèches
qui n'apparaissent que s'il reste du contenu à faire défiler) :

- **Top albums**, **Top artistes**, **Top morceaux** — les 10 éléments les plus écoutés sur la
  période sélectionnée, chacun affiché avec sa pochette (ou un visuel de remplacement si Deezer
  n'a pas encore été interrogé pour cet élément), son titre/nom, et son nombre d'écoutes.
- Les albums et artistes sont cliquables (renvoient vers leur page détail) ; les morceaux ne le
  sont pas (pas de page dédiée à un morceau seul).
- Si aucune écoute n'est enregistrée sur la période choisie, un message l'indique à la place du
  carrousel plutôt que d'afficher une zone vide.

### 2.4 Classements (Top Albums / Top Artistes / Top Morceaux)

- Trois pages dédiées, chacune présentant jusqu'à 100 éléments classés par nombre d'écoutes
  décroissant, sous forme de mosaïque de cartes (pochette + titre + sous-titre + nombre
  d'écoutes + badge de rang), paginées (25 éléments par page).
- Les albums et morceaux sont affichés en carré, les artistes en cercle (photo de profil).
- Cliquer sur un album ou un artiste ouvre sa page détail.

### 2.5 Page détail (album / artiste)

- **Album** : pochette, titre, nom de l'artiste, nombre total d'écoutes, durée cumulée d'écoute,
  date de sortie (si connue), et la liste de tous les morceaux de l'album triés par nombre
  d'écoutes décroissant.
- **Artiste** : photo, nom, nombre total d'écoutes, nombre d'albums distincts écoutés, nombre de
  morceaux distincts écoutés, durée cumulée d'écoute, et la liste de tous les morceaux de
  l'artiste triés par nombre d'écoutes décroissant (avec l'album d'origine de chaque morceau).
- Un artiste n'a pas de "durée" ou de "date de sortie" propres (ça n'a de sens que pour un
  album) : ces champs sont remplacés par les agrégats ci-dessus, propres à un artiste.

### 2.6 Historique d'écoute

- Liste chronologique (la plus récente en premier) des morceaux écoutés : titre, artiste, album,
  date/heure d'écoute — paginée, jusqu'à 100 dernières entrées.

### 2.7 Recherche

- Barre de recherche dans l'en-tête, disponible sur toutes les pages.
- **Suggestions d'autocomplétion** dès 4 caractères saisis (avec un léger délai pour éviter de
  spammer les requêtes à chaque frappe) : liste déroulante mêlant albums, artistes et morceaux
  correspondants, tolérante aux fautes de frappe (moteur Meilisearch).
- Cliquer sur une suggestion album/artiste ouvre directement sa page détail ; cliquer sur un
  morceau (ou appuyer sur Entrée) ouvre la page de résultats complets.
- **Page de résultats complets** : liste paginée de tous les éléments du catalogue personnel
  correspondant à la recherche.

### 2.8 Filtre de plage de dates

- Sélecteur dans l'en-tête (préréglages : 30/90/365 derniers jours, depuis le début, ou plage
  personnalisée), qui s'applique à toutes les pages de consultation à la fois (accueil,
  classements, historique, détail album/artiste) sans avoir à le re-sélectionner en changeant de
  page.
- Se réinitialise à "depuis le début" lors d'un rechargement complet de la page (choix assumé, pas
  persisté entre sessions).

## 3. Parcours utilisateur type

1. L'utilisateur crée un compte (ou se connecte s'il en a déjà un).
2. Il importe un ou plusieurs fichiers Excel mensuels exportés depuis Deezer.
3. Il consulte la page d'accueil pour une vue d'ensemble, puis explore ses classements complets,
   filtre par période, ou recherche un artiste/album précis.
4. Au fil de la consultation, les pochettes des albums/artistes/morceaux pas encore vues
   apparaissent automatiquement (récupérées depuis Deezer à la demande, voir §4).
5. Il peut réimporter un nouveau fichier plus tard : les nouvelles écoutes s'ajoutent sans
   dupliquer les précédentes.

## 4. Règles métier notables

- **Dédoublonnage à l'import** : une ligne du fichier Excel déjà présente en base (même
  utilisateur, même morceau, même date/heure d'écoute) est ignorée silencieusement, jamais
  ré-insérée ni signalée comme erreur.
- **Rattachement artiste/album d'un morceau** : seul le premier nom de la colonne "artiste" du
  fichier Excel détermine l'artiste et l'album d'un morceau (les featurings éventuels, ex.
  "The Weeknd, Future", sont conservés en texte libre pour l'affichage mais ne créent jamais
  d'artiste ou d'album séparé) — évite qu'un même album se retrouve fragmenté selon les
  featurings de chaque morceau.
- **Enrichissement des pochettes "à la demande"** : les métadonnées Deezer (pochette, durée,
  date de sortie) ne sont **pas** récupérées automatiquement à l'import (un import réel de
  ~42 000 lignes prenait plus de 10 minutes avec un enrichissement séquentiel) — elles sont
  récupérées la première fois qu'un élément est effectivement affiché (page d'accueil, un
  classement, une page détail), puis mises en cache définitivement en base. Un élément déjà
  enrichi n'est jamais réinterrogé.
- **Classements plafonnés à 100** : par construction, aucun classement (albums/artistes/morceaux)
  ni l'historique ne dépasse 100 éléments au total, quel que soit le volume réel de données.
- **Session** : l'access token (courte durée, 1h) est renouvelé automatiquement et
  silencieusement via le refresh token (30 jours) tant que l'utilisateur reste actif ; une
  tentative de réutilisation d'un refresh token déjà utilisé (signe probable de vol de session)
  révoque immédiatement toutes les sessions actives de l'utilisateur, qui doit alors se
  reconnecter.

## 5. Écrans

| Écran | Route | Accès |
|---|---|---|
| Connexion | `/login` | Public |
| Inscription | `/register` | Public |
| Accueil (carrousels) | `/` | Authentifié |
| Top albums | `/albums/top` | Authentifié |
| Top artistes | `/artists/top` | Authentifié |
| Top morceaux | `/tracks/top` | Authentifié |
| Détail album | `/albums/:albumId` | Authentifié |
| Détail artiste | `/artists/:artistId` | Authentifié |
| Historique | `/history` | Authentifié |
| Résultats de recherche | `/search?q=...` | Authentifié |

## 6. Périmètre non couvert

- Pas de partage de statistiques entre utilisateurs, pas de classement collectif.
- Pas d'édition manuelle de l'historique (uniquement via import de fichier).
- Pas de suppression de compte ni de gestion de profil au-delà du nom affiché fixé à
  l'inscription.
- Pas d'application mobile native (SPA web responsive uniquement).
