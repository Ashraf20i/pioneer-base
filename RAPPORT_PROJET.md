# Pioneer Base : rapport de projet (métier et technique)

**Équipe** : Novapollo Hexad, NASA Space Apps Challenge 2026, événement de Casablanca
**Défi** : *Build a Junior Astronaut Mission Trainer*
**Dépôt** : https://github.com/Ashraf20i/pioneer-base (branches `main`, `occupy-style-3d`, `submission-fixes`)
**Date** : 15 novembre 2026

---

## Partie 1 : volet métier

### 1.1 Le problème

Le défi part d'un constat : le contenu spatial pour enfants est soit trop abstrait (affiches, faits généraux), soit trop technique. Peu d'outils font **vivre** les compromis d'une vraie mission : l'énergie qu'on garde aujourd'hui peut coûter de l'oxygène demain. Le défi demande un jeu ou une application pour les **7 à 12 ans** où l'on gère un avant-poste lunaire ou martien : survie, blindage contre les radiations, budget d'énergie, production de nourriture, délai de communication.

### 1.2 Notre réponse

**Pioneer Base** : le joueur dirige un avant-poste pendant 30 jours avec quatre astronautes. Chaque jour, il répartit une énergie limitée, choisit les rations et l'activité de l'équipage, et dépense 14 caisses de ravitaillement qui ne seront jamais renouvelées. À la fin, le jeu montre **la chaîne exacte de décisions** qui a mené au succès ou à l'évacuation.

Trois versions, une seule logique de simulation :

| Version | Pour qui | Statut |
|---|---|---|
| **Web** (HTML/CSS/JS, sans installation) | Soumission principale : classe, téléphone, jury | Terminée, testée |
| **Unity 2D** (vue de dessus, style néon) | Étape intermédiaire | Sur `main` |
| **Unity 3D** (troisième personne, mode Difficile) | Démo bonus | Sur `occupy-style-3d` et `submission-fixes` |

### 1.3 Les quatre thèmes du défi devenus mécaniques

| Thème du défi | Mécanique | Compromis enseigné |
|---|---|---|
| Survie (oxygène, eau, nourriture) | Consommation quotidienne réelle de 4 astronautes ; usine d'oxygène, foreuses à glace, recyclage de l'eau à 90 % | Faire de l'oxygène coûte de l'énergie et (sur la Lune) de l'eau |
| Blindage contre les radiations | Dose cumulée, couches de blindage, activité « Abri » pendant les tempêtes | Blindage contre modules utiles ; s'abriter contre faire de la science |
| Budget d'énergie | Panneaux, batteries, réacteur ; délestage en cascade (labo, foreuses, serres, oxygène, habitat) | Panneaux bon marché contre réacteur qui marche la nuit |
| Production de nourriture | Serres (cycle de 10 jours), rations pleines, réduites ou demi | Nourrir maintenant contre investir pour plus tard ; rationner coûte de la santé |
| Délai de communication | Valeurs calculées : 1,3 s (Lune), 3 à 22 min (Mars) ; message de Mission Control, défi technique en 3D | Sur Mars, en urgence, l'équipage décide seul |

Six dilemmes sont construits dans le jeu : panneaux ou réacteur, blindage ou autres modules, abri ou science, serre ou rationnement, eau à boire ou pour l'oxygène, nettoyer les panneaux ou faire de la science. **Aucun choix n'est gratuit**, comme le demande le défi.

### 1.4 Des données NASA au jeu

1. **Collecte** des valeurs publiées, uniquement de sources NASA : NASA TSIS-1 (lumière solaire), Mars Fact Sheet et Moon Fact Sheet (distances), Moon Facts (nuit lunaire), Curiosity RAD (radiation sur Mars), valeurs de référence ECLSS (oxygène et nourriture), record de recyclage de l'eau sur l'ISS, NASA-STD-3001 (limite de radiation), DONKI (dates de deux vraies tempêtes solaires), MOXIE (oxygène tiré de l'air martien), InSight (poussière sur les panneaux).
2. **Calcul** des valeurs dérivées : lumière sur Mars = 1 / 1,524² = 43 % ; électrolyse 2 H₂O → 2 H₂ + O₂ = 4,5 L d'eau pour 4 kg d'oxygène ; délai radio = distance / vitesse de la lumière.
3. **Étiquetage** de chaque chiffre : *Real* (mesuré, jamais modifié), *Derived* (calculé), *Simplified* (valeur de jeu assumée). Un panneau « Where do these numbers come from? » liste tout, avec les sources.
4. **Transformation en règles** : nuit lunaire de 15 tours sans soleil ; 1,37 mSv/jour sur 30 jours dépasse la limite de 40 mSv, donc le blindage est indispensable sur la Lune ; tempêtes DONKI aux jours 7 et 20 ; poussière martienne à 3 % par jour.
5. **Équilibrage honnête** : seules les valeurs *Simplified* ont été réglées (nourriture de départ 150 kg, tempête de poussière à 15 %).

Toutes les sources sont désormais NASA. La dose lunaire (1,37 mSv/jour) est une valeur de jeu *Simplified*, présentée comme environ deux fois la dose mesurée par Curiosity, car la Lune n'a pas d'air.

### 1.5 Ce qui distingue le projet

- **Débrief causal** : la chaîne de causes est reconstruite depuis le vrai journal de la partie (ex. : nuit lunaire, pas de réacteur, batterie vide, usine d'oxygène coupée, évacuation), avec le compromis nommé et un conseil.
- **Données honnêtes et traçables** : chaque chiffre a son étiquette et sa source.
- **Équilibrage prouvé** : des joueurs simulés jouent 200 parties par site.
- **Public jeune** : jamais de « mort », seulement des évacuations ; aucun logo NASA ; mention « Not affiliated with or endorsed by NASA ».
- **Version 3D** : base explorable, ciels justes (noir sur la Lune, caramel sur Mars), robot compagnon PIP, accéléré du jour, mode Difficile « Crash Landing » avec 13 défis de chimie et de physique.

### 1.6 Impact visé

- **Élèves de 7 à 12 ans** : comprendre par l'échec qu'une décision d'aujourd'hui a un coût demain.
- **Enseignants** : une séance de 45 minutes, guide intégré, aucune installation.
- **Rejouabilité** : événements décalés par une graine aléatoire, deux sites, mode Difficile.

### 1.7 Critères du jury

| Critère | Notre argument |
|---|---|
| Impact | Jouable en classe en une séance, sans compte ni installation (version web) |
| Créativité | Débrief causal, étiquettes de données, Myth or Reality, 3D avec rover et défis techniques |
| Validité | Valeurs NASA verrouillées par des tests, simplifications avouées |
| Pertinence | Les cinq thèmes du défi sont des mécaniques |
| Présentation | Vidéo de 30 s centrée sur une décision et sa conséquence (`DEMO_SCRIPT.md`) |

---

## Partie 2 : volet technique

### 2.1 Architecture

```
Données (js/data.js | GameData.cs)  ->  Moteur de simulation (sim.js | Sim.cs)  ->  Débrief (debrief.js | Debrief.cs)
                                                    |
                         Interfaces : web (ui.js) | Unity 2D | Unity 3D (PioneerGame.*.cs)
```

Le moteur est **pur** : aucune dépendance à l'affichage. On peut donc le tester hors navigateur (Node) et hors Unity (dotnet).

### 2.2 Le moteur (un appel = un jour)

Ordre fixe de résolution : constructions payées, modules de la veille mis en service, urgence et pannes, environnement (nuit, poussière, tempêtes), énergie et délestage, eau, oxygène et nourriture, radiation, santé et science, conditions de fin. Un générateur aléatoire déterministe (mulberry32) rend chaque partie reproductible à partir d'une graine. Un aperçu (`Preview`) joue le jour sur une copie de l'état, pour montrer les conséquences avant de valider.

### 2.3 Version web

- HTML, CSS et JavaScript, **aucune dépendance**, aucun appel réseau, fonctionne ouvert depuis le disque.
- Fichiers : `js/data.js` (77 valeurs étiquetées), `js/sim.js`, `js/debrief.js`, `js/ui.js`, `index.html` (guide enseignant inclus).
- Accessible au clavier, adapté au téléphone, respecte « reduced motion ».

### 2.4 Version Unity 3D

- **Unity 2021.3.23f1**, export **WebGL**. Tout est généré par code : terrain, ciel, modules, astronautes, rover, textures, sons. Aucun fichier d'art externe.
- Fichiers (`unity-pioneer-base/Assets/Scripts`) :

| Fichier | Rôle |
|---|---|
| `Core/GameData.cs`, `Sim.cs`, `Debrief.cs`, `Players.cs` | Port C# du moteur web |
| `Core/Journey.cs` | Règles du mode Difficile et les 13 défis techniques |
| `PioneerGame.cs` | Déroulement, entrées, menus, menu pause, cinématiques |
| `PioneerGame.World.cs` | Terrain, ciel, modules, emplacements à construire |
| `PioneerGame.Actors.cs` | Astronaute, équipage, PIP, caméra |
| `PioneerGame.Rover.cs` | Rover pressurisé détaillé (pneus générés, textures) |
| `PioneerGame.Journey.cs` | Canyon, conduite, camp, capsules, arrivée |
| `PioneerGame.Fx.cs` | Particules, météo, sons générés, réactions aux événements |
| `PioneerGame.UI.cs` | Interface « centre de contrôle », polices Inter et Roboto Mono |
| `PioneerGame.Shots.cs` | Joueur automatique et captures d'écran |

- **Commandes** : marcher (ZQSD, WASD, clic), utiliser un module (clic ou E), caméra (glisser, molette), finir la journée (END SOL), pause (Échap ou bouton MENU : reprendre, recommencer, menu principal, sources).

### 2.5 Tests et vérification

| Test | Contenu | Résultat |
|---|---|---|
| `npm test` (web) | 147 648 contrôles du moteur, équilibrage, 202 contrôles des données | Tout passe |
| `csharp-tests` (dotnet) | Mêmes bandes d'équilibrage que le web, plans impossibles, débriefs, mode Difficile, 13 défis | Tout passe |
| Joueur automatique Unity | Missions complètes avec captures (`-executeMethod BuildWebGL.Shots -pbshots`) | Sans erreur |
| Navigateur | Démarrage WebGL, console | Sans erreur |

Résultats d'équilibrage (200 parties par site, web et C# identiques) :

| Joueur simulé | Lune | Mars |
|---|---|---|
| Ne fait rien | 100 % d'évacuations | 100 % |
| Naïf | 100 % d'évacuations | 100 % |
| Correct mais imparfait | 58,5 % de réussite | 85 % |
| Expert | 100 % de réussite (92 % à 3 étoiles) | 100 % (100 % à 3 étoiles) |

Mode Difficile : la base se gagne dans 55 % des cas sans capsule ramassée, 100 % avec quatre capsules.

### 2.6 Problèmes rencontrés et solutions

| Problème | Solution |
|---|---|
| Module WebGL d'Unity incomplet | Réinstallation par la ligne de commande d'Unity Hub |
| Matériaux créés par code absents du build | Shaders ajoutés aux « Always Included Shaders » par script |
| Erreurs « SphereCollider doesn't exist » en WebGL | `link.xml` et désactivation du nettoyage du code moteur |
| Navigateur qui mélange deux builds en cache | Noms de fichiers uniques par build |
| API DONKI en erreur 404 | Dates de tempêtes intégrées, sans prétendre à des données en direct |

### 2.7 Usage de l'IA

Le code, les tests et les textes ont été écrits avec l'aide de Claude (Anthropic). L'équipe a relu, vérifié les sources et testé. Le jeu n'utilise aucune IA à l'exécution et ne fait aucun appel réseau. Aucune image ni vidéo n'a été générée par un outil d'IA.

---

## Partie 3 : limites et suite

**Limites connues**

- Environ sept ressources à suivre, alors que le défi en conseille deux ou trois pour les jeunes joueurs.
- Le délai de communication est expliqué et testé par un défi, mais ce n'est pas encore une vraie mécanique de décision.
- Pas de météo spatiale en direct (DONKI) ; les dates sont figées.
- La version 3D n'a pas été testée sur téléphone, ni jouée à la main en entier dans un navigateur.
- Le test humain avec trois personnes, dont un enfant, reste à faire.

**À vérifier par l'équipe avant la soumission** (détail dans `SOURCES.md`)

- Les liens vers les sources, dont la NASA Moon Fact Sheet ajoutée en dernier.
- La valeur de 0,84 kg d'oxygène et 1,8 kg de nourriture par jour dans un document NASA.
- Les énoncés des 13 défis techniques.

**Pistes**

- Brancher DONKI en direct quand l'API répond.
- Faire du délai radio une vraie décision (agir tout de suite ou attendre la réponse de la Terre).
- Un mode « classe » avec moins de ressources pour les plus jeunes.
- Publication sur GitHub Pages pour le jury.
