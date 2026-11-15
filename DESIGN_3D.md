# Pioneer Base 3D : document de conception (branche `occupy-style-3d`)

Objectif : passer d'une vue 2D à une base 3D explorable, dans l'esprit d'Occupy Mars, sans copier ce jeu, et en gardant ce qui fait notre force : un simulateur exact, sourcé, pour apprendre.

## 1. Éléments métier (ce que le jury attend d'un « Junior Astronaut Mission Trainer »)

| Élément | Dans le jeu |
|---|---|
| Vraies contraintes d'une mission | Oxygène, eau, nourriture, énergie, radiation, santé de 4 astronautes pendant 30 jours |
| Vraies données | Chaque constante porte un tag Real / Derived / Simplified (`GameData.cs`, identique à `js/data.js`) |
| Apprendre par les conséquences | Débrief avec chaîne de causes datée, compromis, et un conseil |
| Deux environnements | Lune (nuit de 15 jours, pas d'air, 1,37 mSv/jour) et Mars (poussière, 43 % du soleil, 0,67 mSv/jour) |
| Compromis | 14 caisses, pas de ravitaillement : réacteur ou serres ou boucliers |
| Esprit critique | 6 cartes Myth or Reality avec source |
| Sécurité du public jeune | Jamais de « mort » : seulement « medical evacuation » ou « emergency evacuation » |
| Indépendance | Aucun logo NASA, mention « not affiliated » sur l'écran titre |

Le moteur (`Assets/Scripts/Core`) ne change pas. Les tests d'équilibrage (`csharp-tests`) doivent rester verts.

## 2. Éléments de design (3D, tout généré par code, aucun fichier d'art)

- **Monde** : terrain en relief (dunes et mesas sur Mars, cratères sur la Lune), rochers, zone plate pour la base.
- **Base extérieure** : habitat à dôme, usine d'oxygène, laboratoire avec parabole, serres en verre avec lampes magenta, foreuses à glace, champ solaire qui suit le soleil, batteries, réacteur à l'écart, dépôt de caisses, véhicule de remontée sur son aire, rover.
- **Ce qu'on voit = l'état de la simulation** :
  - les 14 caisses sont posées au dépôt et disparaissent quand on construit ;
  - les plantes grandissent pendant les 10 jours du cycle ;
  - la poussière ternit les panneaux sur Mars, un jour de Maintenance les nettoie ;
  - les fenêtres s'éteignent dans l'ordre de délestage (labo, foreuses, serres, oxygène, habitat) ;
  - les couches de bouclier sont des murs de régolithe autour de l'habitat.
- **Caméra** : troisième personne, orbite libre (glisser), zoom (molette), clic ou toucher pour marcher.
- **HUD** : jauges, compteur SOL/DAY, frise des 30 jours, carte contextuelle du module proche, bandes cinéma pendant les séquences.

## 3. Éléments de distinction (pourquoi ce n'est ni Among Us ni Occupy Mars)

1. **Le temps comme mécanique** : « END SOL » lance un accéléré du jour ; le soleil traverse le ciel, la nuit tombe, les résultats arrivent.
2. **Le ciel est juste** : sur la Lune le ciel reste noir en plein jour, il n'y a pas de brouillard, la Terre reste presque fixe ; sur Mars le ciel est couleur caramel et la poussière voile tout.
3. **Démarche réelle** : petits bonds en faible gravité, plus hauts sur la Lune.
4. **PIP, le robot compagnon** : il annonce les alertes, explique les chiffres réels, et change d'humeur (œil cyan, ambre, rouge, vert).
5. **Chaque module enseigne** : en s'approchant, une carte donne le chiffre réel et sa source.
6. **Fins sans violence** : arrivée du vaisseau de relève (succès) ou décollage d'évacuation.
7. **Son généré par code** : bips, alarme, vent de tempête, tics de compteur Geiger pendant une tempête solaire.

## 4. Inspirations cinéma (ambiance, pas de copie)

| Film | Ce qu'on en retient |
|---|---|
| The Martian | Compteur de SOL, serre, tempête de poussière, résoudre un problème à la fois |
| Interstellar | Robot compagnon utile et un peu drôle, poussière qui avale la lumière |
| Moon (2009) | Solitude d'une base lunaire, robot au visage simple qui exprime une humeur |
| 2001 : l'Odyssée de l'espace | Plans larges et lents, typographie blanche sobre, œil rond du robot |
| Apollo 13 | Réparer avec ce qu'on a (pannes, réparation par l'équipage) |
| Gravity | Événement spatial soudain : la tempête solaire |
| WALL-E | Robot attachant pour un public jeune |

## 5. Créativité : séquences clés

- **Ouverture** : la caméra descend du ciel vers la base, bandes cinéma, « SOL 1 ».
- **Accéléré du jour** à chaque fin de journée.
- **Tempête de poussière** : brouillard orange, particules, soleil faible, vent.
- **Nuit lunaire** : étoiles, clair de Terre, lampe du casque, fenêtres de la base.
- **Tempête solaire** : ciel rouge pulsé, tics Geiger, bulle d'abri si l'équipage s'abrite.
- **Fin** : vaisseau de relève qui se pose, ou véhicule de remontée qui décolle.

## 6. Contrôles

- Marcher : ZQSD ou WASD ou flèches, ou clic/toucher sur le sol.
- Utiliser un module : clic dessus, ou E / Espace quand on est proche.
- Caméra : glisser pour tourner, molette pour zoomer.
- Tout le plan du jour est aussi accessible par les boutons du HUD (utile sur téléphone).

## 7. Vérification automatique

- `dotnet run -c Release` dans `csharp-tests` : équilibrage et débriefs.
- `Shots` : Unity lance le jeu en mode automatique (`-pbshots`), joue des parties complètes et enregistre des captures dans `unity-pioneer-base/Shots/`.
- Build WebGL en ligne de commande, puis test dans le navigateur.

## 8. À vérifier par l'équipe avant soumission

- La phrase de PIP « sunsets on Mars look blue » est vraie (images des rovers), mais sa source n'est pas encore dans `SOURCES.md`.
- Tester sur un vrai téléphone (la 3D WebGL demande un appareil récent).
- Décision go/no-go : si la 3D pose problème, `main` (2D) et la version web restent les soumissions de secours.

## 9. Mode Difficile : « Crash Landing » (ajout du 15 novembre)

Le mode Normal ne change pas. Le mode Difficile ajoute un voyage avant la base (Mars seulement).

- **Départ** : le vaisseau s'écrase à 40 km de l'avant-poste. Oxygène pour moins de 5 sols, pas de recycleur d'eau dans le rover.
- **Conduite** : le joueur conduit le rover dans un canyon vers une balise verte. Une batterie pleine fait 10,5 km ; le sable mou double la consommation.
- **Camp** : quand la batterie est vide, on campe. La nuit recharge la batterie et coûte un sol d'oxygène, d'eau et de nourriture. Tempête de poussière au sol 3 : recharge de 35 % seulement.
- **Capsules** : sept capsules de ravitaillement sont tombées le long du canyon (oxygène, eau, batterie, caisses, nourriture). Chacune est un détour.
- **Défis techniques** : douze questions de chimie et de physique à trois choix (électrolyse, combustion de l'hydrogène, énergie = puissance x temps, distance / vitesse, loi en carré inverse, radiation, blindage, recyclage). Une bonne réponse ouvre la capsule en entier ou donne +15 % de batterie le matin ; une mauvaise donne la moitié, avec l'explication.
- **Arrivée** : la mission de 30 sols commence avec ce qui reste, plus les réserves réduites de l'avant-poste, et 9 caisses plus celles des capsules.
- **Échec** : si la santé tombe à zéro, une navette de secours récupère l'équipage (« medical evacuation »), puis débrief.

Règles : `Assets/Scripts/Core/Journey.cs`. Monde, conduite et écrans : `Assets/Scripts/PioneerGame.Journey.cs`.

Tests (`csharp-tests`) : un trajet prudent arrive en pleine santé ; les capsules rapportent des caisses ; une conduite négligente coûte la santé ; la base se gagne dans 55 % des cas sans capsule et 100 % avec quatre capsules (joueur expert, 200 parties).

Toutes les valeurs du voyage sont « Simplified », sauf les besoins de l'équipage qui reprennent les valeurs Real/Derived. À vérifier par l'équipe : les énoncés des douze questions (arithmétique vérifiée ; la question sur l'air de Mars cite MOXIE, déjà dans les sources).
