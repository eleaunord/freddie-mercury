# Parcours du hand tracking — du casque à la main affichée

À l'arrache, même format que [grab-trace.md](grab-trace.md). Ici c'est le rendu **visuel** de la main (le squelette qui bouge à l'écran), pas la logique de grab — les deux partagent les 5 premières étapes (le subsystem), puis divergent.
Chemins `Library/PackageCache/...` = package Unity, pas dans le repo, hash qui change à chaque réimport, pas de lien direct possible.

---

## 1. Runtime → subsystem (identique au parcours du grab)

1. Casque → runtime OpenXR → extension `XR_EXT_hand_tracking`
2. La feature crée le subsystem : `OpenXR/HandTracking.cs:166` → `CreateSubsystem<...>()`
3. Le subsystem démarre : `OpenXR/HandTracking.cs:303` → `OnSubsystemStart()`

## 2. Le `XRHandTrackingEvents` du visuel (pas le nôtre)

4. Celui posé par le sample `HandVisualizer`, déjà présent dans `Left/Right Hand Tracking.prefab` — réglé sur `BeforeRender` (`m_UpdateType: 2`), pas `Dynamic`, pour la latence minimale à l'affichage
5. Il s'abonne au subsystem, même mécanisme qu'avant : `XRHandTrackingEvents.cs:218-220`
6. Le subsystem invoque le délégué en passe `BeforeRender` : `XRHandSubsystem.cs:519`
7. `XRHandTrackingEvents.OnUpdatedHands` republie en `UnityEvent` : `XRHandTrackingEvents.cs:248` → `XRHandTrackingEvents.cs:289`

## 3. Le squelette lit les articulations

8. `XRHandSkeletonDriver` est abonné à ce `jointsUpdated` (dans le prefab, câblé par le sample) : `XRHandSkeletonDriver.cs:234` → `jointsUpdated.AddListener(OnJointsUpdated);`
9. Il lit chacune des 26 articulations : `XRHand.cs:55` → `GetJoint(id)` puis `XRHandJoint.cs:106` → `TryGetPose(out pose)`
10. Il applique chaque pose à l'os correspondant : `XRHandSkeletonDriver.cs:326` → `m_JointTransforms[i].SetLocalPose(...)`

## 4. Le rendu

11. Le `SkinnedMeshRenderer` déforme les ~1616 sommets du mesh selon la position des os (skinning) — automatique, aucun script à nous, géré par Unity/le GPU
12. → image affichée dans le casque

## 5. Cas particulier — perte de tracking

13. Si la main sort du champ : `XRHandMeshController.cs:157` → `m_HandMeshRenderer.enabled = true/false;` (cache/réaffiche le mesh selon que la main est trackée ou non)

---

## Les 26 joints, pour référence

```
Wrist, Palm                                                →  2
Thumb  : Metacarpal, Proximal, Distal, Tip                 →  4   (pas d'Intermediate)
Index  : Metacarpal, Proximal, Intermediate, Distal, Tip   →  5
Middle, Ring, Little : idem                                → 15
                                                            ────
                                                              26
```

## Où c'est posé dans la hiérarchie

```
Left Hand Tracking                     (prefab sample com.unity.xr.hands)
 ├── XRHandTrackingEvents (BeforeRender)  ← celui-ci, sujet de ce fichier
 ├── XRHandSkeletonDriver                 ← lit les 26 joints, bouge les os
 ├── SkinnedMeshRenderer + mesh de main   ← rendu, automatique
 ├── XRHandTrackingEvents (Dynamic)       ← PAS lui, c'est celui de grab-trace.md
 └── PinchGrab                            ← PAS lui non plus
```

**Pourquoi deux `XRHandTrackingEvents` séparés sur le même GameObject** : celui-ci (`BeforeRender`) sert uniquement à l'affichage, déjà câblé par le sample Unity — n'y touche pas. Celui du grab (`Dynamic`) est le nôtre, ajouté à côté, pour ne pas mélanger logique de jeu et timing de rendu. Détail complet dans [grab-trace.md](grab-trace.md).
