// Contrôleur de la scène de fin de jeu (Outside) : au chargement, fait
// apparaître en fondu le texte de victoire déjà en place dans la scène.
// Ne construit rien dynamiquement - le Canvas World Space est un
// GameObject de scène assigné dans l'Inspector, pour rester réglable
// visuellement par l'équipe (position, taille de police...) sans repasser
// par le code.

using System.Collections;
using UnityEngine;

public class OutsideSceneController : MonoBehaviour
{
    [Header("Texte de victoire")]
    [Tooltip("CanvasGroup du Canvas World Space affichant le message de victoire.")]
    [SerializeField] private CanvasGroup victoryCanvasGroup;

    [Tooltip("Durée du fondu d'apparition du texte (secondes).")]
    [SerializeField] private float fadeInDuration = 1.2f;

    private void Start()
    {
        if (victoryCanvasGroup == null)
        {
            Debug.LogWarning("OutsideSceneController sur '" + name + "' : 'Victory Canvas Group' non assigné dans l'Inspector.");
            return;
        }

        // Invisible au départ : évite que le texte apparaisse brutalement
        // pendant/juste après le fade-in de la transition de scène
        // (ScreenFader, géré séparément par LevelExitTrigger côté Living room).
        victoryCanvasGroup.alpha = 0f;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            victoryCanvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }

        victoryCanvasGroup.alpha = 1f;
    }
}
