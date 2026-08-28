// Anime l'ouverture de briefcase_upper (partie mobile de la mallette) une
// fois la combinaison trouvée.
//
// Ne connaît rien du grab, de la main, ni du système de rotation des
// molettes : expose uniquement Open(), à appeler par CombinationLock.OnUnlocked
// (câblé dans l'Inspector, ou en code ci-dessous si la référence est
// assignée). Aucune dépendance au grab générique - reste autonome.

using System.Collections;
using UnityEngine;

public class BriefcaseOpener : MonoBehaviour
{
    [Header("Référence")]
    [Tooltip("Optionnel : si assigné, Open() est automatiquement abonné à son événement OnUnlocked. Peut aussi être câblé à la main dans l'Inspector (OnUnlocked -> BriefcaseOpener.Open), auquel cas laisser ce champ vide pour éviter un double appel.")]
    [SerializeField] private CombinationLock combinationLock;

    [Header("Pose de départ (fermée)")]
    [SerializeField] private Vector3 startLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 startLocalEulerAngles = new Vector3(-90f, 0f, 0f);

    [Header("Pose d'arrivée (ouverte)")]
    [SerializeField] private Vector3 endLocalPosition = new Vector3(0f, 0.355f, -0.261f);
    [SerializeField] private Vector3 endLocalEulerAngles = new Vector3(6.464f, 0f, 0f);

    [Header("Timing")]
    [Tooltip("Durée de l'animation d'ouverture, en secondes.")]
    [SerializeField] private float openDuration = 1f;

    [Tooltip("Courbe d'easing appliquée à t (0 à 1) avant l'interpolation. Par défaut un ease-out (démarre vite, ralentit en fin de course) ; à peaufiner en playtest.")]
    [SerializeField] private AnimationCurve easeCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Son (optionnel)")]
    [Tooltip("Joué au tout début de l'animation, si assigné. Peut rester vide en attendant le choix de l'asset son.")]
    [SerializeField] private AudioSource openSound;

    private bool _isOpen;

    private void OnEnable()
    {
        if (combinationLock != null)
            combinationLock.OnUnlocked.AddListener(Open);
    }

    private void OnDisable()
    {
        if (combinationLock != null)
            combinationLock.OnUnlocked.RemoveListener(Open);
    }

    /// <summary>
    /// Lance l'animation d'ouverture. Sans effet si déjà ouvert (évite un
    /// double déclenchement si OnUnlocked est invoqué/câblé plusieurs fois).
    /// </summary>
    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        if (openSound != null) openSound.Play();

        Quaternion startRotation = Quaternion.Euler(startLocalEulerAngles);
        Quaternion endRotation = Quaternion.Euler(endLocalEulerAngles);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float eased = easeCurve.Evaluate(t);

            // Slerp sur les Quaternion plutôt qu'un Lerp brut sur les angles
            // d'Euler, pour éviter une trajectoire de rotation bizarre.
            transform.localPosition = Vector3.LerpUnclamped(startLocalPosition, endLocalPosition, eased);
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, eased);

            yield return null;
        }

        transform.localPosition = endLocalPosition;
        transform.localRotation = endRotation;
    }
}
