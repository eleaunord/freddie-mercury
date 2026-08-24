# Freddie Mercury — Plan d'attaque (2 semaines)

Escape Game en VR — projet 42.
Objectif : terminer le sujet en 2 semaines, en binôme, avec les bonus 1, 2, 3 (et 4 en stretch-goal).

## Répartition des rôles

Chaque tâche a un seul propriétaire du début à la fin — personne ne reprend le travail de l'autre en cours de route. Le pôle assigné (A ou B) bascule d'une tâche à l'autre pour chaque personne au fil du planning, de sorte que les deux touchent aux deux pôles sur les 2 semaines.

- **Pôle A — Systems / Core VR**
  Setup OpenXR, input, physique des interactions (grab, rotation, push/pull, téléportation), events, save/load, performance.

- **Pôle B — Level Design / Contenu / Narration**
  Greybox des niveaux, puzzles, logique de jeu (interactions objet-objet), son, mise en scène narrative, cohérence visuelle.

## Choix techniques

- **OpenXR** (via Unity XR Plugin Management + Input System) plutôt qu'OVRInput :
  - Standard officiel cross-vendor (Khronos), compatible Quest / Vive / PSVR2...
  - Répond directement à la contrainte "compatible with at least one VR headset... or any other"
  - Aucune interaction "prête à l'emploi" fournie → colle à l'exigence "rawest API" / "no advance packages"
- **Locomotion** : téléportation instantanée (raycast + snap de position), sans arc de visée ni transition/fade, sans vignetting de confort associé. À surveiller en playtest (J13) pour vérifier que le confort reste correct malgré l'absence de transition.

### Setup en place

- Unity 6000.4.4f1, Built-in Render Pipeline, color space **Linear**.
- Packages VR : `com.unity.xr.openxr` + `com.unity.inputsystem` **uniquement** (pas de XR Interaction Toolkit, contrainte « rawest API »).
- Loader OpenXR activé pour **Android** (casque standalone) et **Standalone** (Play mode PC VR, Windows uniquement, cf. ci-dessous).
- Profils d'interaction : Oculus Touch + Khronos Simple (fallback Lynx et autres runtimes OpenXR), Meta Quest Support côté Android, Vive + Index côté Standalone.
- Rendu : Single Pass Instanced, Vulkan seul, ARM64 / IL2CPP, minSdk 29 (Lynx R1 = Android 10), MSAA 4x, vsync laissé au casque.

### Arborescence

| Chemin | Rôle |
|---|---|
| `Assets/_Project/Scenes/Lobby.unity` | Scène de départ (greybox 6x6 m) |
| `Assets/_Project/Prefabs/XR Rig.prefab` | Rig joueur : tête + 2 mains, à réutiliser dans chaque niveau |
| `Assets/_Project/Settings/XRControls.inputactions` | Actions XR : pose tête/mains, grip, trigger, thumbstick |
| `Assets/_Project/Scripts/Runtime/Player/XRTrackingOrigin.cs` | Passe le runtime en tracking origin `Floor` (remplace `XROrigin`) |
| `Assets/_Project/Scripts/Runtime/Core/InputActionAssetEnabler.cs` | Active l'asset d'actions pour tout le rig |
| `Assets/_Project/Scripts/Editor/HeadsetBuild.cs` | Menu `Freddie Mercury > Build and Run on Headset` |

Le rig n'applique **aucun** offset de caméra : en mode `Floor` le runtime renvoie déjà la taille réelle du joueur.

### Lancer sur le casque

1. Casque en mode développeur, branché en USB (`adb devices` doit le lister).
2. Menu Unity : `Freddie Mercury > Build and Run on Headset` (build dev, APK dans `Builds/`).

**Pas besoin de Quest Link** : le jeu tourne en standalone dans le casque.

Le Play mode de l'éditeur n'affiche de la VR que si un runtime PC VR tourne (Quest Link / Air Link / SteamVR), ce qui est **Windows uniquement** : sur macOS il n'existe aucun runtime Oculus PC. Sur Mac, chaque test casque passe donc par un build APK. La config OpenXR Standalone reste en place pour le poste Windows du binôme.

## Bonus visés

| # | Bonus | Approche |
|---|---|---|
| 1 | Multiple VR headsets | Conséquence directe du choix OpenXR — pas de tâche dédiée, juste validé aux tests casque (J6, J11, J13). |
| 2 | Saving/loading system | Sérialisation JSON (`JsonUtility` + `System.IO`) de la progression et de l'état des objets. Tâche dédiée J9-J10. |
| 3 | Coherent and beautiful world | Passe lighting (baked lighting), palette cohérente, post-process léger. Intégré à la passe polish J12. |
| 4 | Interaction with in-game characters | Perso scripté (Animator + triggers), pas d'IA/dialogue complet. **Stretch-goal** J12 — premier élément sacrifié en cas de retard. |

## Planning détaillé

### Semaine 1

| Jours | Personne 1 | Personne 2 |
|---|---|---|
| J1 | **Commun** : design doc, découpe des 2 niveaux + puzzles, setup repo Git, setup Unity + OpenXR + XR Origin (archi qui garantit d'office la compat multi-headset → bonus 1). | |
| J2-J3 | **Pôle A** — Grab générique (détection, attache, lancer) + téléportation instantanée (raycast, snap position, sans transition). | **Pôle B** — Greybox lobby + niveau 1 (layout, volumes, placement objets). |
| J4-J5 | **Pôle B** — Greybox niveau 2 + éléments narratifs d'environnement. | **Pôle A** — Rotation contrainte (poignées, cadenas, limites d'angle). |
| J6 | **Commun — Intégration #1** : merge grab + teleport + rotation dans les greybox, test croisé casque (+ test sur un 2e headset si dispo → valide bonus 1), fix collisions. | |
| J7-J8 | **Pôle A** — Push/pull (translation contrainte, min/max). | **Pôle B** — Logique niveau 1 (interactions objet-objet type clé/cadenas, condition de sortie). |

### Semaine 2

| Jours | Personne 1 | Personne 2 |
|---|---|---|
| J9-J10 | **Pôle B** — Logique niveau 2 + écran de fin. | **Pôle A — bonus 2** : système save/load (JsonUtility + System.IO, sérialisation progression/état des objets). |
| J11 | **Commun — Intégration #2** : parcours complet lobby → niveau 1 → niveau 2 → fin, playtest croisé (test du save/load inclus, re-test multi-headset si possible). | |
| J12 | **Pôle A** — Système d'events + hooks audio dans le framework, puis **bonus 4** (stretch) : interaction avec un perso scripté si le temps le permet. | **Pôle B** — Sound design/ambiance + contenu tuto du lobby, puis **bonus 3** : passe lighting/cohérence visuelle. |
| J13 | **Commun** — polish confort VR (vérifier le résiduel de motion sickness du teleport instant même sans vignette) + stabilité FPS (profiling), test casque final (2e headset si dispo). | |
| J14 | **Commun** — buffer : nettoyage repo, hash md5 des assets si > 1Go, dernière repasse avant rendu. | |

## Répartition finale par personne

- **Personne 1** : Grab+teleport (A) → Greybox niveau 2 (B) → Push/pull (A) → Logique niveau 2 (B) → Events/audio + bonus 4 (A) → 3× Pôle A, 2× Pôle B
- **Personne 2** : Greybox lobby/niveau 1 (B) → Rotation (A) → Logique niveau 1 (B) → Save/load bonus 2 (A) → Sound design + bonus 3 (B) → 2× Pôle A, 3× Pôle B

## Points de vigilance

- Ne pas sacrifier les intégrations (J6, J11) pour gagner du temps ailleurs — c'est là qu'on découvre si l'archi partagée tient.
- Bonus 4 = premier candidat à couper en cas de retard sur J9-J11 ; ça ne remet rien d'autre en cause.
- Vérifier auprès d'un staff/référent 42 que l'usage précis des packages Meta/OpenXR rentre bien dans la contrainte "rawest API" / "no advance packages" avant d'investir trop de temps dans une archi qui pourrait être recalée.
- Assets > 1Go : ne pas les pousser directement dans le repo, remplacer par un hash md5 recursif du dossier.
