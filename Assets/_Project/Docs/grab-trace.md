# Parcours du grab — du casque à `Grab()`

À l'arrache, pour retracer vite en soutenance. Détails/vocabulaire complet dans [xr-hands-notes.md](xr-hands-notes.md).
Voir aussi [handtracking-trace.md](handtracking-trace.md) pour le parcours du rendu visuel de la main (squelette/mesh), pas traité ici.
Chemins `Library/PackageCache/...` = package Unity, pas dans le repo, le hash change à chaque réimport, pas de lien direct possible.

---

## 1. Runtime → subsystem (une fois, au démarrage)

1. Casque → runtime OpenXR → extension `XR_EXT_hand_tracking`
2. La feature crée le subsystem : `OpenXR/HandTracking.cs:166` → `CreateSubsystem<...>()`
3. Le subsystem démarre : `OpenXR/HandTracking.cs:303` → `OnSubsystemStart()`
4. `XRHandTrackingEvents` (posé sur `Left/Right Hand Tracking`) le cherche en boucle : `XRHandTrackingEvents.cs:166` → `Update()`
5. Il s'abonne au délégué : `XRHandTrackingEvents.cs:218-220` → `m_Subsystem.updatedHands += OnUpdatedHands;`

## 2. Chaque frame, jusqu'à notre event

6. Le subsystem invoque le délégué (2×/frame, Dynamic + BeforeRender) : `XRHandSubsystem.cs:519` → `updatedHands.Invoke(...)`
7. `XRHandTrackingEvents.OnUpdatedHands` reçoit l'appel : `XRHandTrackingEvents.cs:248`
8. Filtre côté main + côté `UpdateType` (Dynamic pour nous, réglé dans l'Inspector) : `XRHandTrackingEvents.cs:279`
9. Republie en `UnityEvent` : `XRHandTrackingEvents.cs:289` → `m_JointsUpdated?.Invoke(args)`

## 3. Notre fichier — [Grabber.cs](../Scripts/Runtime/Player/Grabber.cs)

10. Abonnement fait une fois, au `OnEnable` : [Grabber.cs#L54](../Scripts/Runtime/Player/Grabber.cs#L54) → `m_HandEvents.jointsUpdated.AddListener(OnJointsUpdated);`
11. `OnJointsUpdated` reçoit l'event à chaque frame Dynamic : [Grabber.cs#L74](../Scripts/Runtime/Player/Grabber.cs#L74)
12. Lecture ThumbTip/IndexTip, sortie si pose invalide : [Grabber.cs#L76](../Scripts/Runtime/Player/Grabber.cs#L76)
13. Distance (pince ?) + point milieu en espace monde : [Grabber.cs#L78](../Scripts/Runtime/Player/Grabber.cs#L78)
14. Transition fermeture → `Grab(point)` : [Grabber.cs#L81](../Scripts/Runtime/Player/Grabber.cs#L81)
15. Transition ouverture → `Release()` : [Grabber.cs#L86](../Scripts/Runtime/Player/Grabber.cs#L86)
16. Suivi continu tant que tenu → `MovePosition` : [Grabber.cs#L92](../Scripts/Runtime/Player/Grabber.cs#L92)

## 4. Dans `Grab()` / `Release()`

17. Recherche physique autour du point de pince : [Grabber.cs#L112](../Scripts/Runtime/Player/Grabber.cs#L112) → `OverlapSphereNonAlloc`
18. Filtre `attachedRigidbody` + composant `Grabbable` : [Grabber.cs#L117](../Scripts/Runtime/Player/Grabber.cs#L117)
19. Garde le plus proche (`finalComp`/`smallest`) : [Grabber.cs#L121](../Scripts/Runtime/Player/Grabber.cs#L121)
20. Stocke le Rigidbody, passe kinematic : [Grabber.cs#L135](../Scripts/Runtime/Player/Grabber.cs#L135)
21. `Release()` : rend la main à la physique, vide `m_Held` : [Grabber.cs#L140](../Scripts/Runtime/Player/Grabber.cs#L140)

**Trou connu, pas encore fait** : `OnTrackingLost()` ([Grabber.cs#L98](../Scripts/Runtime/Player/Grabber.cs#L98)) est vide — si la main sort du tracking en pleine prise, rien ne relâche l'objet.

---

## Chemin dans la hiérarchie (Inspector)

```
XR Rig
 ├── Main Camera
 ├── Left Hand Tracking          ← prefab sample com.unity.xr.hands
 │    ├── XRHandTrackingEvents (visuel, BeforeRender)   ← déjà présent, sert au squelette affiché, PAS à nous
 │    ├── XRHandTrackingEvents (le nôtre, Dynamic)       ← Handedness = Left
 │    └── PinchGrab                                       ← m_HandEvents = celui du dessus, m_TrackingSpace = XR Rig
 └── Right Hand Tracking
      ├── XRHandTrackingEvents (visuel, BeforeRender)
      ├── XRHandTrackingEvents (le nôtre, Dynamic)        ← Handedness = Right
      └── PinchGrab
```

Deux `PinchGrab` au total, un par main, chacun avec son propre `XRHandTrackingEvents` dédié (jamais celui du visuel).

**Note liens** : les `#L54` etc. suivent la convention GitHub/VS Code (`fichier#Lxx`) pour sauter à la ligne — si ton lecteur markdown ne la supporte pas, le lien ouvre quand même le bon fichier, juste pas à la bonne ligne.
