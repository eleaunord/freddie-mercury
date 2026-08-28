// Cadenas à combinaison de la mallette (BriefcaseLock) : surveille 3
// molettes (RotationConstraint) et déclenche OnUnlocked dès que les trois
// sont simultanément sur leur angle cible, à la tolérance près.
//
// Ne connaît RIEN du grab, de la main, ni du clavier : les molettes sont
// tournées par ailleurs (pincement main sur les joints, ou testeur clavier)
// via RotationConstraint.ApplyRotationDelta, exactement comme le cadenas à
// clé. Ce script se contente de LIRE RotationConstraint.CurrentAngle - il
// n'appelle jamais ApplyRotationDelta lui-même, et ne dépend d'aucun système
// de grab générique (autonome, pour éviter tout conflit de merge).
//
// Important : RotationConstraint.CurrentAngle est un float accumulé en
// interne (jamais lu depuis transform.localEulerAngles), donc PAS wrappé
// entre 0 et 360 - contrairement à transform.localEulerAngles.x. On compare
// quand même via Mathf.DeltaAngle plutôt que par égalité/soustraction
// directe : ça reste correct quel que soit le nombre de tours effectués, et
// ça protège si un jour CurrentAngle est relu autrement.

using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class WheelCombinationEntry
{
    [Tooltip("Molette à vérifier. Référence directe au RotationConstraint (il expose déjà CurrentAngle) : pas besoin de lire le Transform.")]
    public RotationConstraint wheel;

    [Tooltip("Angle cible, en degrés, tel qu'attendu par RotationConstraint.CurrentAngle (PAS transform.localEulerAngles.x, qui est wrappé 0-360).")]
    public float targetAngle;
}

public class CombinationLock : MonoBehaviour
{
    [Header("Molettes")]
    [Tooltip("Une entrée par molette : wheel1_low, wheel2_low, wheel3_low.")]
    [SerializeField] private WheelCombinationEntry[] wheels = new WheelCombinationEntry[3];

    [Header("Tolérance")]
    [Tooltip("Écart maximum toléré (en degrés) entre l'angle courant d'une molette et sa cible pour la considérer correcte. Le pincement main est moins précis qu'un contrôleur : à affiner en playtest.")]
    [SerializeField] private float tolerance = 5f;

    [Header("Après déverrouillage")]
    [Tooltip("Si actif, désactive les 3 RotationConstraint une fois déverrouillé, pour empêcher de retourner les molettes ensuite. Ne bloque que les sources de rotation qui vérifient elles-mêmes MonoBehaviour.enabled avant d'appeler ApplyRotationDelta.")]
    [SerializeField] private bool lockWheelsOnUnlock = true;

    [Header("Événement")]
    [Tooltip("Invoqué une seule fois, dès que les 3 molettes sont simultanément dans la tolérance.")]
    public UnityEvent OnUnlocked;

    private bool _isUnlocked;

    /// <summary>Vrai dès que la combinaison a été trouvée (une seule fois, ne redevient jamais faux).</summary>
    public bool IsUnlocked => _isUnlocked;

    private void OnEnable()
    {
        foreach (var entry in wheels)
        {
            if (entry?.wheel != null)
                entry.wheel.OnNormalizedAngleChanged.AddListener(OnWheelAngleChanged);
        }
    }

    private void OnDisable()
    {
        foreach (var entry in wheels)
        {
            if (entry?.wheel != null)
                entry.wheel.OnNormalizedAngleChanged.RemoveListener(OnWheelAngleChanged);
        }
    }

    private void Start()
    {
        // Au cas où une molette démarrerait déjà sur son angle cible.
        CheckCombination();
    }

    // RotationConstraint invoque déjà un event à chaque changement d'angle
    // (OnNormalizedAngleChanged) : on s'y abonne plutôt que de poller les 3
    // molettes en Update(). La valeur normalisée elle-même ne nous sert pas
    // ici, on relit CurrentAngle sur chaque molette pour la comparaison.
    private void OnWheelAngleChanged(float _)
    {
        CheckCombination();
    }

    private void CheckCombination()
    {
        if (_isUnlocked) return;

        foreach (var entry in wheels)
        {
            if (entry == null || entry.wheel == null)
            {
                Debug.LogWarning("CombinationLock sur '" + name + "' : une entrée de 'Wheels' n'a pas de molette assignée dans l'Inspector.");
                return;
            }

            float diff = Mathf.DeltaAngle(entry.wheel.CurrentAngle, entry.targetAngle);
            if (Mathf.Abs(diff) > tolerance) return;
        }

        _isUnlocked = true;
        if (lockWheelsOnUnlock) SetWheelsLocked(true);
        OnUnlocked?.Invoke();
    }

    private void SetWheelsLocked(bool locked)
    {
        foreach (var entry in wheels)
        {
            if (entry?.wheel != null)
                entry.wheel.enabled = !locked;
        }
    }
}
