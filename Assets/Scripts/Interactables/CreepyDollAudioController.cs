// Poupée flippante du salon (creepyDoll) : machine à états à 3 phases
// (NotMet -> Met -> Solved) qui pilote tous les sons de la poupée en
// fonction de la proximité du joueur, plus le déclenchement du tiroir une
// fois le puzzle des livres résolu.
//
// Ne connaît RIEN du puzzle des livres lui-même : le futur script qui vérifie
// l'ordre des livres dans le carton appellera simplement NotifyBooksSolved()
// (ex: câblé dans l'Inspector, ou en code) - aucune dépendance dans l'autre
// sens. Reste autonome, comme RotationConstraint/CombinationLock.
//
// Détection de proximité : même pattern qu'ExitDoorController (distance à
// Camera.main.transform.position, vérifiée en Update()), avec un état
// wasInRange pour ne détecter que la transition hors-zone -> dans-zone et
// éviter de rejouer ComeBack en boucle tant que le joueur reste dans la zone.

using System.Collections;
using UnityEngine;

public class CreepyDollAudioController : MonoBehaviour
{
    private enum DollState { NotMet, Met, Solved }

    [Header("Détection de proximité")]
    [Tooltip("Distance (mètres) entre la caméra XR (Camera.main) et la poupée, en dessous de laquelle le joueur est considéré 'dans la zone'.")]
    [SerializeField] private float triggerDistance = 2f;

    [Header("Audio")]
    [Tooltip("Unique AudioSource de la poupée, utilisé pour tous les clips (jamais plus d'un à la fois).")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Joué une seule fois, à la toute première approche du joueur.")]
    [SerializeField] private AudioClip whoAreU;
    [Tooltip("Joué une seule fois, 5s après la fin de WhoAreU. Ne sera plus jamais rejoué ensuite.")]
    [SerializeField] private AudioClip presentation;
    [Tooltip("Joué à chaque entrée du joueur dans la zone de proximité, tant que l'état est 'Met'.")]
    [SerializeField] private AudioClip comeBack;
    [Tooltip("Joué une seule fois quand NotifyBooksSolved() est appelée.")]
    [SerializeField] private AudioClip booksSolved;
    [Tooltip("Joué juste après BooksSolved, en même temps que le début de l'animation du tiroir.")]
    [SerializeField] private AudioClip drawerOpen;

    [Tooltip("Délai (secondes) entre la fin de WhoAreU et le début de Presentation.")]
    [SerializeField] private float delayBeforePresentation = 5f;

    [Header("Tiroir (DrawerOne)")]
    [Tooltip("GameObject du tiroir à animer une fois le puzzle des livres résolu.")]
    [SerializeField] private Transform drawerOne;
    [Tooltip("Position locale Z de départ (fermé) du tiroir.")]
    [SerializeField] private float drawerClosedZ = 0.01999927f;
    [Tooltip("Position locale Z d'arrivée (ouvert) du tiroir.")]
    [SerializeField] private float drawerOpenZ = 0.365f;
    [Tooltip("Durée de l'animation d'ouverture du tiroir, en secondes.")]
    [SerializeField] private float drawerOpenDuration = 1.5f;

    [Tooltip("Clé (INTERACT_Cle) posée dans le tiroir, enfant de DrawerOne. Désactivée au démarrage (invisible et non interactible tant que le tiroir n'est pas ouvert) puis réactivée au même instant que l'animation du tiroir - il n'existe pas de vraie cavité creuse sur le mesh du tiroir pour la cacher visuellement, donc on la cache via SetActive plutôt que par occlusion géométrique. Optionnel : laisser vide si la clé n'est pas encore placée en scène.")]
    [SerializeField] private GameObject keyInDrawer;

    private DollState _state = DollState.NotMet;
    private bool _wasInRange;

    private void Awake()
    {
        if (keyInDrawer != null) keyInDrawer.SetActive(false);
    }

    // Anime + vérifie la proximité à chaque frame : nécessaire pour détecter
    // la transition hors-zone -> dans-zone, et il n'existe pas d'event
    // "joueur approche" à écouter ici (contrairement à CombinationLock qui
    // s'abonne à OnNormalizedAngleChanged) - Camera.main n'expose rien de
    // tel, exactement comme ExitDoorController.
    private void Update()
    {
        if (_state == DollState.Solved) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        bool isInRange = distance <= triggerDistance;

        if (isInRange && !_wasInRange)
        {
            OnPlayerEnteredRange();
        }

        _wasInRange = isInRange;
    }

    private void OnPlayerEnteredRange()
    {
        switch (_state)
        {
            case DollState.NotMet:
                StartCoroutine(FirstMeetingSequence());
                break;

            case DollState.Met:
                PlayClip(comeBack);
                break;
        }
    }

    // NotMet -> Met : WhoAreU, puis 5s de silence, puis Presentation (qui ne
    // sera plus jamais rejouée après ça).
    private IEnumerator FirstMeetingSequence()
    {
        // Passe l'état tout de suite pour ne pas redéclencher cette
        // coroutine si le joueur ressort/rentre dans la zone pendant la
        // séquence (ex: pendant les 5s de silence).
        SetState(DollState.Met);

        yield return PlayClipAndWait(whoAreU);
        yield return new WaitForSeconds(delayBeforePresentation);
        yield return PlayClipAndWait(presentation);
    }

    /// <summary>
    /// À appeler par le futur script de puzzle des livres, une fois l'ordre
    /// correct trouvé. Joue BooksSolved, puis déclenche DrawerOpen et
    /// démarre l'animation du tiroir exactement au même instant. Sans effet
    /// si déjà appelée, ou si l'état n'est pas encore 'Met' (le puzzle des
    /// livres ne devrait de toute façon pas être résolvable avant la
    /// première rencontre, mais on protège quand même contre un appel
    /// précoce ou en double).
    /// </summary>
    public void NotifyBooksSolved()
    {
        if (_state != DollState.Met) return;
        StartCoroutine(SolvedSequence());
    }

    private IEnumerator SolvedSequence()
    {
        yield return PlayClipAndWait(booksSolved);

        // Même frame, même appel : le son du tiroir, son animation, et la
        // réapparition de la clé démarrent ensemble.
        PlayClip(drawerOpen);
        StartCoroutine(AnimateDrawerOpen());
        if (keyInDrawer != null) keyInDrawer.SetActive(true);

        SetState(DollState.Solved);
    }

    private IEnumerator AnimateDrawerOpen()
    {
        if (drawerOne == null)
        {
            Debug.LogWarning("CreepyDollAudioController sur '" + name + "' : 'Drawer One' n'est pas assigné dans l'Inspector, impossible d'animer le tiroir.");
            yield break;
        }

        Vector3 startPos = drawerOne.localPosition;
        startPos.z = drawerClosedZ;
        Vector3 endPos = startPos;
        endPos.z = drawerOpenZ;

        drawerOne.localPosition = startPos;

        float elapsed = 0f;
        while (elapsed < drawerOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drawerOpenDuration);
            drawerOne.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        drawerOne.localPosition = endPos;
    }

    // Empêche tout chevauchement : ne joue pas si un clip est déjà en cours.
    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        if (audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    // Comme PlayClip, mais attend la fin de la lecture avant de continuer -
    // utilisé pour enchaîner des clips sans les superposer (WhoAreU ->
    // Presentation, BooksSolved -> DrawerOpen).
    private IEnumerator PlayClipAndWait(AudioClip clip)
    {
        if (audioSource == null || clip == null) yield break;
        if (audioSource.isPlaying) yield break;

        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitForSeconds(clip.length);
    }

    private void SetState(DollState newState)
    {
        if (_state == newState) return;
        Debug.Log("CreepyDollAudioController sur '" + name + "' : " + _state + " -> " + newState);
        _state = newState;
    }
}
