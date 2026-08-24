// Rotation contrainte générique et réutilisable pour tout objet interactif
// tournant autour d'un axe unique dans une plage [MinAngle, MaxAngle]
// (poignée, cadenas, porte...).
//
// Ce component ne détecte AUCUNE entrée (clavier, souris, main VR...) : il
// expose uniquement BeginInteraction / UpdateInteraction / EndInteraction,
// destinées à être pilotées par un futur système de grab générique.
// Rien ici ne dépend d'XR Interaction Toolkit ni d'un autre package
// d'interaction prêt à l'emploi.

using System.Collections;
using UnityEngine;

public class ConstrainedRotation : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("Contrainte de rotation")]
    [Tooltip("Axe local autour duquel l'objet tourne.")]
    [SerializeField] private RotationAxis axis = RotationAxis.Y;

    [Tooltip("Angle minimum atteignable, en degrés.")]
    [SerializeField] private float minAngle = 0f;

    [Tooltip("Angle maximum atteignable, en degrés.")]
    [SerializeField] private float maxAngle = 90f;

    [Tooltip("Angle de repos initial (position au démarrage, et cible du spring-back si actif).")]
    [SerializeField] private float restAngle = 0f;

    [Header("Comportement au relâchement")]
    [Tooltip("Si actif, l'objet revient automatiquement à l'angle de repos quand l'interaction se termine (ex: poignée). Si inactif, il reste où on l'a laissé (ex: porte, cadenas).")]
    [SerializeField] private bool springBackToRest = false;

    [Tooltip("Durée du retour à l'angle de repos, en secondes.")]
    [SerializeField] private float springBackDuration = 0.3f;

    private Quaternion _baseRotation;
    private float _currentAngle;
    private Coroutine _springBackRoutine;

    /// <summary>Angle courant, en degrés, toujours compris dans [MinAngle, MaxAngle].</summary>
    public float CurrentAngle => _currentAngle;

    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public float RestAngle => restAngle;
    public bool IsAtRest => Mathf.Approximately(_currentAngle, restAngle);

    private void Awake()
    {
        // La rotation au moment du chargement fait office de "zéro" : tout
        // CurrentAngle s'applique en plus de cette pose de base, pour que
        // l'objet puisse être orienté librement dans l'éditeur au préalable.
        _baseRotation = transform.localRotation;
        _currentAngle = Mathf.Clamp(restAngle, minAngle, maxAngle);
        ApplyRotation();
    }

    /// <summary>
    /// À appeler par le système de grab quand la saisie commence.
    /// Annule un éventuel retour à l'angle de repos en cours.
    /// </summary>
    public void BeginInteraction()
    {
        if (_springBackRoutine != null)
        {
            StopCoroutine(_springBackRoutine);
            _springBackRoutine = null;
        }
    }

    /// <summary>
    /// À appeler à chaque frame pendant l'interaction, avec la variation
    /// d'angle (en degrés) à appliquer depuis la dernière frame. Le résultat
    /// est automatiquement clampé entre MinAngle et MaxAngle.
    /// </summary>
    public void UpdateInteraction(float deltaAngle)
    {
        _currentAngle = Mathf.Clamp(_currentAngle + deltaAngle, minAngle, maxAngle);
        ApplyRotation();
    }

    /// <summary>
    /// À appeler par le système de grab quand la saisie se termine. Si
    /// SpringBackToRest est actif, ramène progressivement l'objet à son
    /// angle de repos ; sinon l'objet reste exactement où on l'a laissé.
    /// </summary>
    public void EndInteraction()
    {
        if (!springBackToRest) return;

        if (_springBackRoutine != null) StopCoroutine(_springBackRoutine);
        _springBackRoutine = StartCoroutine(SpringBackRoutine());
    }

    private IEnumerator SpringBackRoutine()
    {
        float startAngle = _currentAngle;
        float t = 0f;

        while (t < springBackDuration)
        {
            t += Time.deltaTime;
            _currentAngle = Mathf.Lerp(startAngle, restAngle, Mathf.Clamp01(t / springBackDuration));
            ApplyRotation();
            yield return null;
        }

        _currentAngle = restAngle;
        ApplyRotation();
        _springBackRoutine = null;
    }

    private void ApplyRotation()
    {
        transform.localRotation = _baseRotation * Quaternion.AngleAxis(_currentAngle, AxisVector());
    }

    private Vector3 AxisVector()
    {
        switch (axis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }
}
