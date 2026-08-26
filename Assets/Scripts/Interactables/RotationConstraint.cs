// Rotation contrainte pour un objet dont la rotation ne doit être possible
// que serrure/clé insérée (ex: le cadenas, INTERACT_Cadenas). Tourne autour
// d'un axe local unique, clampé strictement entre un angle min et un angle
// max.
//
// Ce component ne connaît RIEN du grab, de la main, ni du clavier : il
// expose uniquement ApplyRotationDelta(degrees), destinée à être appelée par
// n'importe quelle source de rotation (testeur clavier aujourd'hui, futur
// système de grab de Personne 1 demain, sans aucune modification ici). La
// seule condition qu'il vérifie lui-même est l'insertion de la clé associée
// (KeyInsertable.IsInserted) : si elle n'est pas insérée (ou mal alignée),
// l'appel est ignoré silencieusement.

using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UnityEvent&lt;float&gt; sérialisable dans l'Inspector, invoqué avec
/// l'angle courant du cadenas normalisé entre 0 (MinAngle) et 1 (MaxAngle).
/// </summary>
[Serializable]
public class NormalizedAngleEvent : UnityEvent<float> { }

public class RotationConstraint : MonoBehaviour
{
    [Header("Contrainte de rotation")]
    [Tooltip("Axe local autour duquel l'objet tourne. Sert aussi d'axe de la serrure pour l'alignement de la clé (voir KeyInsertable) : comme un vrai barillet, on insère et on tourne autour du même axe.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Tooltip("Angle minimum atteignable, en degrés.")]
    [SerializeField] private float minAngle = 0f;

    [Tooltip("Angle maximum atteignable, en degrés.")]
    [SerializeField] private float maxAngle = 90f;

    [Header("Condition d'insertion")]
    [Tooltip("La clé qui doit être insérée (IsInserted == true) pour que ApplyRotationDelta ait un effet. Laissée vide, toute rotation est bloquée par sécurité (avec un avertissement dans la Console).")]
    [SerializeField] private KeyInsertable requiredKey;

    [Header("Événement")]
    [Tooltip("Invoqué à chaque changement d'angle réel, avec l'angle courant normalisé entre 0 et 1 (1 = MaxAngle atteint, ex: déverrouillage).")]
    public NormalizedAngleEvent OnNormalizedAngleChanged;

    private Quaternion _baseRotation;
    private float _currentAngle;
    private bool _warnedMissingKey;

    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;

    /// <summary>Angle courant, en degrés, toujours compris dans [MinAngle, MaxAngle].</summary>
    public float CurrentAngle => _currentAngle;

    /// <summary>Angle courant normalisé entre 0 (MinAngle) et 1 (MaxAngle).</summary>
    public float NormalizedAngle => Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);

    /// <summary>Axe local de rotation, aussi utilisé par KeyInsertable comme axe de la serrure.</summary>
    public Vector3 RotationAxis => rotationAxis;

    private void Awake()
    {
        // La rotation au moment du chargement fait office de "zéro" : le
        // pivot est déjà placé correctement dans l'éditeur, on tourne en
        // plus de cette pose de base.
        _baseRotation = transform.localRotation;
        _currentAngle = Mathf.Clamp(0f, minAngle, maxAngle);
        ApplyRotation();
    }

    /// <summary>
    /// Fait tourner l'objet de <paramref name="degrees"/> degrés autour de
    /// RotationAxis, clampé strictement entre MinAngle et MaxAngle.
    ///
    /// Sans effet, ignoré silencieusement, si RequiredKey n'est pas
    /// assignée ou que RequiredKey.IsInserted vaut false : c'est la SEULE
    /// condition que ce component vérifie lui-même, il ne sait rien d'autre
    /// du grab, de la main, ou de la source de l'appel (clavier de test ou
    /// futur système de grab réel).
    /// </summary>
    public void ApplyRotationDelta(float degrees)
    {
        if (requiredKey == null)
        {
            if (!_warnedMissingKey)
            {
                Debug.LogWarning("RotationConstraint sur '" + name + "' : aucune 'Required Key' assignée dans l'Inspector - rotation bloquée par sécurité.");
                _warnedMissingKey = true;
            }
            return;
        }

        if (!requiredKey.IsInserted) return;

        float newAngle = Mathf.Clamp(_currentAngle + degrees, minAngle, maxAngle);
        if (Mathf.Approximately(newAngle, _currentAngle)) return;

        _currentAngle = newAngle;
        ApplyRotation();
        OnNormalizedAngleChanged?.Invoke(NormalizedAngle);
    }

    private void ApplyRotation()
    {
        transform.localRotation = _baseRotation * Quaternion.AngleAxis(_currentAngle, rotationAxis.normalized);
    }
}
