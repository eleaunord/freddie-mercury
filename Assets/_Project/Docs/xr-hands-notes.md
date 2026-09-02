# XR Hands — pense-bête

Chemin des sources : `Library/PackageCache/com.unity.xr.hands@<hash>/Runtime/`
(le hash change à chaque réimport du package, les numéros de ligne valent pour la 1.9.0)

---

## Le principe en une ligne

**Le signal ne transporte pas les données.** On te prévient que ça a changé, puis tu vas lire ce dont tu as besoin.

---

## Au démarrage — une seule fois

| # | Ce qui se passe | Référence |
|---|---|---|
| 1 | Unity initialise le XR avant la 1re scène | [Assets/XR/XRGeneralSettings.asset](../../XR/XRGeneralSettings.asset) → `m_InitManagerOnStart: 1` |
| 2 | La feature OpenXR **crée** le subsystem | `OpenXR/HandTracking.cs:166` → `CreateSubsystem<XRHandSubsystemDescriptor, XRHandSubsystem>(...)` |
| 3 | Il **démarre** quand la session OpenXR démarre → `running = true` | `OpenXR/HandTracking.cs:303` → `OnSubsystemStart()` |
| 4 | `XRHandTrackingEvents` le cherche en boucle jusqu'à le trouver | `XRHandTrackingEvents.cs:166` → `Update()` |
| 5 | Il s'abonne au délégué | `XRHandTrackingEvents.cs:218-220` → `m_Subsystem.updatedHands += OnUpdatedHands;` |
| 6 | Skeleton et Mesh s'abonnent, eux, aux UnityEvents | `XRHandSkeletonDriver.cs:234` → `jointsUpdated.AddListener(OnJointsUpdated);` |

> Créé ≠ démarré. Et `running` peut repasser à `false` en cours de partie (casque en veille) — d'où le test `!= null && running`, jamais `!= null` seul.

---

## À chaque frame

| # | Ce qui se passe | Référence |
|---|---|---|
| 1 | Les caméras mesurent, le runtime calcule les 26 poses | — (hors Unity) |
| 2 | Le subsystem **stocke** les poses dans `leftHand` / `rightHand` | `XRHandSubsystem.cs:75` → `public XRHand leftHand => GetHand(Handedness.Left);` |
| 3 | Le subsystem **invoque** le délégué (2× par frame) | `XRHandSubsystem.cs:519` → `updatedHands.Invoke(this, updateSuccessFlags, updateType);` |
| 4 | Les 2 fonctions abonnées sont appelées (main G + main D) | `XRHandTrackingEvents.cs:248` → `OnUpdatedHands(subsystem, flags, updateType)` |
| 5 | Chacune vérifie **si c'est SA main** | `XRHandTrackingEvents.cs:279` → `(flags & UpdateSuccessFlags.LeftHandJoints) != None` |
| 6 | Si oui, elle republie en UnityEvent | `XRHandTrackingEvents.cs:289` → `m_JointsUpdated?.Invoke(args);` |
| 7 | Le skeleton driver **va lire** les joints | `XRHand.cs:55` → `GetJoint(id)` puis `XRHandJoint.cs:106` → `TryGetPose(out pose)` |
| 8 | Il applique les poses aux 26 os | `XRHandSkeletonDriver.cs:326` → `m_JointTransforms[i].SetLocalPose(...)` |
| 9 | Le GPU déforme les 1616 sommets (skinning) → image | automatique, `SkinnedMeshRenderer` |

En parallèle, sur les **transitions** seulement (main perdue / retrouvée) :
`XRHandMeshController.cs:157` → `m_HandMeshRenderer.enabled = true/false;`

---

## Les 3 pièges à ne pas réoublier

**1. `Invoke()` ne transporte aucune pose.**
```csharp
OnUpdatedHands(XRHandSubsystem subsystem,   // une référence vers le "tableau"
               UpdateSuccessFlags flags,     // ce qui a changé, au niveau MAIN
               UpdateType updateType)        // quel moment de la frame
```
Les positions restent dans le subsystem. Le 1er paramètre sert justement à aller les y chercher.

**2. `flags` ne dit jamais quel doigt a bougé.** Il n'a que 4 valeurs possibles :
```csharp
LeftHandRootPose = 1<<0,  LeftHandJoints = 1<<1,
RightHandRootPose = 1<<2, RightHandJoints = 1<<3
```
→ granularité **main entière**, pas articulation.

**3. L'ID passé à `GetJoint()` est TON choix.**
```csharp
hand.GetJoint(XRHandJointID.IndexTip);   // personne ne te l'impose
```
C'est ce qui permet à ton script de grab d'en lire 2 quand le skeleton driver en lit 26.

---

## Vocabulaire

| Terme | Définition | Exemple |
|---|---|---|
| **Subsystem** | module Unity qui expose une capacité matérielle sans dire quel casque | `XRHandSubsystem` |
| **Délégué** | variable qui stocke une **liste de fonctions** | `updatedHands` |
| **Fonction abonnée** | fonction ajoutée à cette liste avec `+=`, appelée **par** le subsystem | `OnUpdatedHands` |
| **UnityEvent** | même idée, mais sérialisable → visible dans l'Inspector | `jointsUpdated` |

Pourquoi une **liste** et pas une seule fonction : parce qu'il y a 2 abonnés — un `XRHandTrackingEvents` par main. Le subsystem sonne une fois, les deux sont appelés, et chacun filtre pour sa main.

---

## Les 26 joints

```
Wrist, Palm                                    →  2
Thumb  : Metacarpal, Proximal, Distal, Tip     →  4   (pas d'Intermediate)
Index  : Metacarpal, Proximal, Intermediate, Distal, Tip  →  5
Middle, Ring, Little : idem                    → 15
                                               ────
                                                 26
```

Parcours complet :
```csharp
for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); ++i)
    var joint = hand.GetJoint(XRHandJointIDUtility.FromIndex(i));
```

---

## Le point de départ du grab

Le script réel du projet est [Grabber.cs](../Scripts/Runtime/Player/Grabber.cs) — il suit exactement ce squelette
(`OnEnable`/`OnDisable` symétriques, `OnJointsUpdated` qui lit pouce + index, distance → geste), avec en plus
l'hystérésis ouverture/fermeture, la conversion en espace monde, et la gestion de la perte de tracking.

> **À vérifier avant de tester** : le fichier s'appelle `Grabber.cs` mais définit `class PinchGrab`. Unity a besoin
> que le nom du fichier corresponde au nom de la classe MonoBehaviour pour pouvoir l'ajouter à un GameObject —
> soit renommer le fichier en `PinchGrab.cs`, soit renommer la classe en `Grabber`.

Le geste « pincer » n'existe pas dans l'API : le subsystem ne donne que des positions.
C'est toi qui l'inventes — et c'est exactement ce que demande le sujet avec la *rawest API*.
