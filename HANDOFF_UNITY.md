# Reprise : version Unity de Pioneer Base

Lis ce fichier en premier. Il résume tout ce qui a été fait jusqu'ici.

## Contexte
- Équipe Novapollo Hexad, NASA Space Apps Casablanca 2026, défi "Build a Junior Astronaut Mission Trainer".
- Date limite officielle : dimanche 15 novembre 2026, 23h59. Interne : 22h30. Gel des fonctionnalités : 14h00. Décision go/no-go Unity : dimanche 12h00.
- Cahier des charges : voir `CORRESPONDANCE_CAHIER_DES_CHARGES.txt` (et `README.md`, `DECISIONS.md`).
- Dépôt GitHub (privé) : https://github.com/Ashraf20i/pioneer-base (branche `main`, compte Ashraf20i).

## Ce qui est terminé : la version web (plan B, déjà poussée)
HTML/CSS/JS sans dépendance. `npm test` passe (engine, balance, data).
Fichiers clés : `js/data.js` (toutes les constantes avec tag Real/Derived/Simplified), `js/sim.js` (moteur pur), `js/debrief.js` (chaîne de causes), `js/ui.js`, `tests/` (joueurs simulés).
Valeurs changées pour l'équilibrage : nourriture de départ 150 kg (au lieu de 130), multiplicateur de tempête de poussière 0,15 (au lieu de 0,25).
Résultats (200 parties/site) : rien = 100 % échec, naïf = 100 % échec, correct = 58,5 % Lune / 85 % Mars, expert = 100 % (3 étoiles 92 % / 100 %).
Ne pas casser cette version : elle est la soumission de secours.

## Ce qui a été décidé avec l'utilisateur
- Faire une version **Unity 2D, vue de dessus, style Among Us** (pas de première personne, trop long).
- Un astronaute que le joueur déplace vers des **stations** (systèmes, activité, ration, construction, "Run day").
- Les lumières des salles s'éteignent quand l'énergie manque (scène de la vidéo de 30 s : voir `DEMO_SCRIPT.md`).
- Export **WebGL** pour GitHub Pages.
- L'utilisateur trouve la page web trop chargée : en Unity, garder l'écran très simple (4 choix par jour).
- Ne pas faire de choses non demandées. L'utilisateur parle français ; les textes du jeu restent en anglais.

## État de l'environnement Unity
- Projet créé : `unity-pioneer-base/` (dans ce dossier), éditeur **2021.3.23f1** (`C:\Program Files\Unity\Hub\Editor\2021.3.23f1\Editor\Unity.exe`). Module WebGL installé pour cette version. Licence Personal active.
- Paquet **MCP for Unity** (CoplayDev, v10.0.0) installé dans `Packages/manifest.json`.
- `uv` installé (via pip). Python 3.12.
- Serveur MCP **UnityMCP** enregistré dans Claude Code : `http://127.0.0.1:8080/mcp`, statut connecté. Il faut que **l'éditeur Unity reste ouvert** sur le projet. Si le serveur est déconnecté : dans Unity, `Window > MCP for Unity`, puis `Start Server`.
- Pour lancer Unity depuis la ligne de commande, entourer le chemin du projet de guillemets (il contient une espace) : `Start-Process ... -ArgumentList '-projectPath "C:\Users\Msi\Desktop\EMI 2\Hackathon_Nasa\unity-pioneer-base" ...'`.
- Le dossier Unity n'est pas encore commité. `.gitignore` exclut Library, Temp, Logs, obj, UserSettings, Builds.
- L'accès écran (computer-use) était lié à Unity 2022 et pas à l'éditeur 2021 ouvert. Ne pas l'utiliser pour piloter Unity, utiliser les outils MCP Unity.

## Plan, avec points de contrôle (chacun doit marcher avant le suivant)
1. Tester la connexion MCP : créer un objet dans une scène, lire la console Unity.
2. Porter le moteur en C# (`Sim.cs`, `Debrief.cs`, `GameData.cs`), même logique que `js/sim.js` et `js/debrief.js`, mêmes constantes que `js/data.js`. Refaire les tests d'équilibrage (joueurs simulés de `tests/players.js`) avec Unity Test Framework ou une console C# : mêmes bandes (rien 100 %, naïf ≥ 90 %, correct 40-90 %, expert ≥ 95 % et 3 étoiles ≥ 50 %).
3. Scène de la base en vue de dessus (formes simples : salles, couloirs, astronaute, stations), lumières qui s'allument et s'éteignent. Interface minimale : jauges, "Run day", alertes.
4. Débrief (chaîne de causes, graphique, conseil), cartes Myth or Reality (6, voir `js/data.js`), lien vers le panneau de sources de la version web.
5. Export WebGL (`Build/`), test sur navigateur et téléphone, publication sur GitHub Pages (dossier séparé du jeu web).
Si l'étape 3 n'est pas jouable dimanche à 12h00 : abandonner Unity pour la soumission et garder la version web.

## Règles du projet à respecter dans Unity aussi
- Aucun mot "mort" : seulement "emergency evacuation" ou "medical evacuation".
- Aucun logo NASA ; mention "An independent student project for the NASA Space Apps Challenge 2026. Not affiliated with or endorsed by NASA."
- Les valeurs Real ne changent jamais pour équilibrer. Seules les Simplified se règlent.
- Ne rien pousser sur GitHub, ne rien publier sans l'accord explicite de l'utilisateur.
