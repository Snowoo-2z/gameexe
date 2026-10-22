<p align="center">
  <img src="assets/logo.png" width="180" alt="Logo Pousse !">
</p>

# 🌱 Pousse ! — petit platformer (C# / .exe Windows)

Un platformer 2D en **C# + [Raylib](https://www.raylib.com/)**, compilé en **un seul `.exe` autonome** pour PC Windows.
Pour l'instant : un sol d'herbe très stylé (vent, rafales, l'herbe se plie quand tu passes dedans 🌾) et un petit perso avec des animations soignées (squash & stretch, course, écharpe qui flotte, pousse sur la tête qui rebondit…).

![Capture du jeu](assets/screenshot.png)

[![Build](https://github.com/Snowoo-2z/gameexe/actions/workflows/build.yml/badge.svg)](https://github.com/Snowoo-2z/gameexe/actions/workflows/build.yml)
[![Dernière version](https://img.shields.io/github/v/release/Snowoo-2z/gameexe?label=version&color=3da85a)](https://github.com/Snowoo-2z/gameexe/releases/latest)

---

## 🎮 Jouer (Windows) — installation en 1 double-clic

1. Télécharge **[Installer.bat](https://github.com/Snowoo-2z/gameexe/releases/latest/download/Installer.bat)** (depuis la dernière Release)
2. **Double-clique dessus** → il télécharge le jeu depuis GitHub, l'installe, crée un raccourci sur ton **Bureau** et dans le **menu Démarrer**, puis Lance le jeu. Aucun droit administrateur requis.

> 💡 Tu peux aussi télécharger **[GameExe.exe](https://github.com/Snowoo-2z/gameexe/releases/latest/download/GameExe.exe)** directement : c'est un seul fichier autonome, double-clic et ça joue.

### Contrôles

| Action | Touches |
|---|---|
| Bouger | `←` `→` ou `Q` / `D` (ZQSD & WASD acceptés) |
| Sauter | `Espace`, `↑`, `Z` ou `W` — relâche tôt pour un petit saut |
| Courir | `Maj` (Shift) |
| Plein écran | `F` ou `F11` |
| Capture d'écran | `P` (enregistre `capture.png`) |
| Appliquer une mise à jour | `Entrée` (quand le bandeau orange apparaît) |
| Quitter | `Échap` |

---

## 🔄 Mises à jour automatiques (depuis GitHub)

Le système d'update fonctionne de **3 façons complémentaires** :

1. **Dans le jeu** : à chaque démarrage, le jeu demande en arrière-plan à l'API GitHub quelle est la dernière version. Si elle est plus récente, il télécharge le nouvel `.exe` tout seul et affiche un bandeau : **`Entrée`** → l'exe est remplacé et le jeu se relance. Hors-ligne ? Le jeu démarre normalement, sans rien dire.
2. **Via `Installer.bat`** : le relancer met le jeu à jour (il compare les versions). Il récupère toujours la dernière version du script d'installation depuis le repo.
3. **Désinstaller** : `Desinstaller.bat` (supprime le jeu + les raccourcis).

### 📦 Publier une nouvelle version (pour le développeur)

C'est **100 % automatisé par GitHub Actions** :

1. Change le numéro dans **[version.txt](version.txt)** (ex. `0.1.0` → `0.2.0`)
2. Commit + push sur **`main`** (ou merge ta PR)
3. Le workflow **[Release](.github/workflows/release.yml)** fabrique le `.exe` sur un PC Windows chez GitHub et publie la Release `vX.Y.Z` avec `GameExe.exe`, le `.zip`, `Installer.bat` et `version.json`
4. Tous les joueurs reçoivent la mise à jour à leur prochain lancement ✨

*(Un tag `vX.Y.Z` fait la même chose. Le workflow **[Build](.github/workflows/build.yml)**, lui, compile à chaque push pour vérifier que rien n'est cassé, et dépose l'exe en artifact téléchargeable dans l'onglet Actions.)*

---

## 🛠️ Développer

Prérequis : **[.NET SDK 8](https://dotnet.microsoft.com/download)** (Windows, gratuit).

```bat
dev.bat      :: Lance le jeu depuis le code source (mode dev, sans auto-update)
build.bat    :: Fabrique dist\GameExe.exe localement (même chose que GitHub Actions)
```

Équivalent en ligne de commande :

```bash
dotnet run --project src/GameExe                       # lancer en dev
dotnet publish src/GameExe -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist
```

> 🐧🍎 Le code est multi-plateforme (Raylib) : `dotnet run` fonctionne aussi sur Linux/macOS pour développer. Seul l'exe publié est Windows pour l'instant.

### Structure du projet

```
gameexe/
├── version.txt                  ← LA version du jeu (source unique de vérité)
├── Installer.bat                ← installe / met à jour depuis GitHub (joueurs)
├── Desinstaller.bat
├── build.bat  / dev.bat         ← build local / lancement dev
├── tools/installer.ps1          ← le vrai script d'installation (PowerShell)
├── .github/workflows/
│   ├── build.yml                ← compile le .exe à chaque push (vérification)
│   └── release.yml              ← publie la Release GitHub automatiquement
├── src/GameExe/
│   ├── Program.cs               ← fenêtre, boucle de jeu, caméra, HUD
│   ├── Player.cs                ← physique + TOUTES les animations du perso
│   ├── World.cs                 ← ciel, collines, terre, herbe animée, décor
│   ├── Particles.cs             ← poussière, brins d'herbe, onde d'impact
│   ├── Updater.cs               ← vérification GitHub + application de l'update
│   └── GameInfo.cs / Utils.cs
├── preview/index.html           ← aperçu web jouable (jumeau JS du jeu, pour tester vite)
└── assets/                      ← logo + icône
```

**Aucune image n'est utilisée en jeu** : tout (perso, herbe, nuages, papillons…) est dessiné avec des formes et animé en code — c'est pour ça que l'exe tient en un seul fichier sans aucun dossier d'assets.

### Détails d'animation implémentés

- **Perso** : squash & stretch à ressorts (saut/atterrissage dosés selon l'impact), respiration au repos, clignements, regard qui suit le mouvement, bouche « o » en chute, bras qui moulinent, cycle de course, inclinaison du corps, écharpe ondulante à segments, pousse sur ressort, poussière de dérapage/course, onde + herbe arrachée à l'atterrissage.
- **Game feel** : *coyote time*, *jump buffering*, saut à hauteur variable, caméra douce avec anticipation.
- **Herbe** : ~800 brins en 2 couches, vent + rafales voyageuses, **les brins s'écartent au passage du perso** ; fleurs, champignons, pollen, papillons, parallaxe (collines, arbres, nuages, herbe de premier plan).

---

## 🗺️ Idées pour la suite

- [ ] Plateformes (sauter dessus), puis plusieurs niveaux
- [ ] Pièces / graines à ramasser + compteur
- [ ] Ennemis (limaces ?) et rebond sur la tête
- [ ] Sons générés en code (saut, ramassage) + petite musique
- [ ] Double-saut / dash
- [ ] Menu titre et sauvegarde des meilleurs temps

---

*Fait avec C# 12 · .NET 8 · Raylib-cs 7.0.2 · GitHub Actions*
