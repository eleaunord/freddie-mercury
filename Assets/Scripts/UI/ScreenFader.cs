// Fondu plein écran générique et réutilisable pour n'importe quelle
// transition de scène (Bedroom -> Living room, Living room -> Outside,
// lobby -> niveau 1...) : ne connaît aucune scène ni aucun contexte en
// particulier.
//
// Prévu pour être posé sur une Image plein cadre d'un Canvas World Space
// attaché à la caméra XR - PAS un Canvas Screen Space, incompatible VR
// (ne suit pas la tête du joueur).

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    [Tooltip("Durée du fondu (secondes), à l'aller comme au retour.")]
    [SerializeField] private float fadeDuration = 1f;

    private Image _image;
    private Coroutine _routine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        SetAlpha(0f);
    }

    /// <summary>Fondu vers le noir opaque ; invoque onComplete une fois terminé.</summary>
    public void FadeToBlack(Action onComplete = null) => StartFade(1f, onComplete);

    /// <summary>Fondu depuis le noir vers transparent ; invoque onComplete une fois terminé.</summary>
    public void FadeFromBlack(Action onComplete = null) => StartFade(0f, onComplete);

    private void StartFade(float targetAlpha, Action onComplete)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        float startAlpha = _image.color.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / fadeDuration)));
            yield return null;
        }

        SetAlpha(targetAlpha);
        _routine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        Color c = _image.color;
        c.a = alpha;
        _image.color = c;
    }
}
