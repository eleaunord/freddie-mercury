// Détecte l'insertion de la clé (INTERACT_Cle) dans la zone serrure d'un
// cadenas (RotationConstraint) : la clé doit être à l'intérieur du trigger
// collider qui définit la zone serrure ET son axe avant doit être aligné
// avec l'axe de rotation du cadenas (le même axe sert à l'insertion et à la
// rotation, comme un vrai barillet de serrure), à une tolérance d'angle
// configurable près.
//
// Ne connaît rien du grab, de la main, ni du clavier : expose uniquement
// IsInserted (et CurrentLock), à consulter par RotationConstraint
// (ApplyRotationDelta) et par n'importe quelle source de rotation.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KeyInsertable : MonoBehaviour
{
    [Header("Alignement")]
    [Tooltip("Axe \"avant\" de la clé en local : direction de la lame/tige qui doit pointer dans la serrure.")]
    [SerializeField] private Vector3 localForwardAxis = Vector3.forward;

    [Tooltip("Tolérance d'angle (en degrés) entre l'axe avant de la clé et l'axe de la serrure pour considérer la clé insérée.")]
    [SerializeField] private float alignmentToleranceDegrees = 15f;

    private bool _inZone;
    private RotationConstraint _currentLock;

    /// <summary>Vrai quand la clé est dans la zone serrure ET correctement alignée avec l'axe de rotation du cadenas occupé.</summary>
    public bool IsInserted { get; private set; }

    /// <summary>Le cadenas dans la zone serrure duquel la clé se trouve actuellement, ou null si hors de toute zone.</summary>
    public RotationConstraint CurrentLock => _currentLock;

    private void Awake()
    {
        // Rigidbody cinématique : nécessaire pour que les événements
        // OnTrigger... se déclenchent (la clé est déplacée à la main via
        // transform, jamais par la physique), sans jamais tomber ni pousser
        // quoi que ce soit.
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var lockConstraint = other.GetComponentInParent<RotationConstraint>();
        if (lockConstraint == null) return;

        _currentLock = lockConstraint;
        _inZone = true;
        UpdateInsertedState();
    }

    private void OnTriggerStay(Collider other)
    {
        // Ré-évalue l'alignement en continu : la clé peut tourner (une fois
        // insérée) ou être ré-orientée pendant qu'elle est déjà dans la zone.
        var lockConstraint = other.GetComponentInParent<RotationConstraint>();
        if (lockConstraint == null || lockConstraint != _currentLock) return;

        UpdateInsertedState();
    }

    private void OnTriggerExit(Collider other)
    {
        var lockConstraint = other.GetComponentInParent<RotationConstraint>();
        if (lockConstraint == null || lockConstraint != _currentLock) return;

        _inZone = false;
        _currentLock = null;
        IsInserted = false;
    }

    private void UpdateInsertedState()
    {
        if (!_inZone || _currentLock == null)
        {
            IsInserted = false;
            return;
        }

        Vector3 keyForwardWorld = transform.TransformDirection(localForwardAxis.normalized);
        Vector3 lockAxisWorld = _currentLock.transform.TransformDirection(_currentLock.RotationAxis.normalized);

        // L'axe de la serrure est une ligne, pas une direction : la clé doit
        // pouvoir s'insérer "dans un sens ou dans l'autre" selon son
        // orientation de modélisation.
        float angle = Vector3.Angle(keyForwardWorld, lockAxisWorld);
        float alignedAngle = Mathf.Min(angle, 180f - angle);

        IsInserted = alignedAngle <= alignmentToleranceDegrees;
    }
}
