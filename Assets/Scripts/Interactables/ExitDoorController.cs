// Porte de sortie automatique : générique et réutilisable (Bedroom,
// Living room, futures salles). Une fois la condition de déverrouillage
// remplie (unlockCondition.IsLocked == false) ET le joueur suffisamment
// proche (distance à Camera.main), la porte pivote seule vers sa position
// ouverte via Quaternion.Slerp.
//
// Ne pilote PAS ConstrainedRotation : ce dernier est réservé à une porte
// actionnée à la main par le futur système de grab (cf. son propre
// commentaire d'en-tête). ExitDoorController pivote directement le
// Transform, pour une porte qui s'ouvre seule une fois débloquée - les deux
// composants peuvent coexister sur le même Door_Panel tant que rien
// n'appelle BeginInteraction/UpdateInteraction dessus (à revoir le jour où
// le grab est branché sur une porte qui a aussi un ExitDoorController).
//
// Ne connaît rien de la salle ni de la scène suivante : c'est
// LevelExitTrigger qui consulte IsOpen pour décider quand déclencher la
// transition de sortie.

using UnityEngine;
using UnityEngine.Events;

public class ExitDoorController : MonoBehaviour
{
    // Même convention d'axe que ConstrainedRotation, pour rester cohérent
    // avec l'Inspector d'un teammate déjà habitué à ce composant.
    public enum RotationAxis { X, Y, Z }

    [Header("Condition de déverrouillage")]
    [Tooltip("Verrou consulté avant d'ouvrir : la porte ne bouge que si IsLocked est false. Ex: le même Lockable que celui gaté par le puzzle de la salle.")]
    [SerializeField] private Lockable unlockCondition;

    [Header("Détection de proximité")]
    [Tooltip("Distance (mètres) entre la caméra XR (Camera.main) et la porte, en dessous de laquelle la porte commence à s'ouvrir une fois déverrouillée.")]
    [SerializeField] private float proximityDistance = 2f;

    [Header("Animation d'ouverture")]
    [Tooltip("Axe local de rotation de la porte.")]
    [SerializeField] private RotationAxis axis = RotationAxis.Z;
    [Tooltip("Angle d'ouverture (degrés), relatif à la position fermée telle qu'orientée dans l'éditeur.")]
    [SerializeField] private float openAngle = 90f;
    [Tooltip("Vitesse du Slerp vers la position ouverte (plus haut = plus rapide).")]
    [SerializeField] private float openSpeed = 2f;
    [Tooltip("Écart angulaire (degrés) sous lequel la porte est considérée complètement ouverte.")]
    [SerializeField] private float openThreshold = 1f;

    [Header("Événement")]
    [Tooltip("Invoqué une seule fois, dès que la porte atteint sa position ouverte.")]
    public UnityEvent OnDoorOpened;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _isOpen;

    /// <summary>Vrai une fois que la porte a fini son animation d'ouverture (ne redevient jamais faux).</summary>
    public bool IsOpen => _isOpen;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.AngleAxis(openAngle, AxisVector());
    }

    // Anime + vérifie la proximité à chaque frame : nécessaire pour un
    // Slerp continu, et il n'existe pas d'event "joueur approche" à
    // écouter ici (contrairement à CombinationLock qui s'abonne à
    // OnNormalizedAngleChanged) - Camera.main n'expose rien de tel.
    private void Update()
    {
        if (_isOpen) return;
        if (unlockCondition == null || unlockCondition.IsLocked) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        if (distance > proximityDistance) return;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, _openRotation, openSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.localRotation, _openRotation) <= openThreshold)
        {
            transform.localRotation = _openRotation;
            _isOpen = true;
            OnDoorOpened?.Invoke();
        }
    }

    private Vector3 AxisVector()
    {
        switch (axis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }
}
