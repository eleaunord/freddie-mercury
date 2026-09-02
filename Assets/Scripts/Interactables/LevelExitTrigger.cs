// Trigger de passage de porte : détecte quand le joueur (collider de la
// caméra XR) traverse l'embrasure d'une porte de sortie, et déclenche un
// fondu au noir suivi du chargement d'une nouvelle scène.
//
// Générique et réutilisable pour n'importe quelle porte de sortie du jeu
// (Bedroom -> Living room, Living room -> Outside, ...) : la scène cible
// est un champ assignable, jamais hardcodée. Ne s'arme QUE si la porte
// associée (ExitDoorController) est ouverte : traverser l'embrasure porte
// fermée ne déclenche rien.
//
// À poser sur un GameObject séparé dans l'embrasure du Door_Panel (pas sur
// le battant lui-même, qui pivote) avec un BoxCollider en isTrigger.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Porte associée")]
    [Tooltip("Porte à consulter : le trigger ne réagit que si IsOpen est vrai.")]
    [SerializeField] private ExitDoorController door;

    [Header("Détection du joueur")]
    [Tooltip("Tag du collider considéré comme le joueur (la caméra XR est taguée MainCamera dans ce projet). Laisser vide pour accepter n'importe quel collider.")]
    [SerializeField] private string playerTag = "MainCamera";

    [Header("Transition de scène")]
    [Tooltip("Fondu plein écran à utiliser pour la transition (World Space Canvas sur la caméra XR).")]
    [SerializeField] private ScreenFader screenFader;
    [Tooltip("Nom de la scène à charger une fois le fondu terminé (doit être présente dans les Build Settings).")]
    [SerializeField] private string targetSceneName;

    [Header("Événement")]
    [Tooltip("Invoqué une seule fois, dès que le passage est détecté porte ouverte (avant le fondu).")]
    public UnityEvent OnLevelExitTriggered;

    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (door == null || !door.IsOpen) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        _triggered = true;
        OnLevelExitTriggered?.Invoke();
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        if (screenFader != null)
        {
            bool faded = false;
            screenFader.FadeToBlack(() => faded = true);
            yield return new WaitUntil(() => faded);
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("LevelExitTrigger sur '" + name + "' : 'Target Scene Name' non renseigné dans l'Inspector.");
            yield break;
        }

        // Chargement async avec activation différée : évite un freeze le
        // temps du chargement, la scène n'est activée qu'une fois prête.
        AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        load.allowSceneActivation = true;
    }
}
