// Relie une cle (CollectibleItem) a un objet verrouille (Lockable) : si la
// cle a ete ramassee, deverrouille la cible. Ne connait rien d'un systeme
// de grab/pickup - expose uniquement TryUnlock(), destinee a etre appelee
// par un futur systeme de grab VR ou par un testeur clavier.

using UnityEngine;

public class KeyPadlockPuzzle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("La cle requise pour deverrouiller la cible.")]
    [SerializeField] private CollectibleItem key;
    [Tooltip("L'objet verrouille a deverrouiller une fois la cle en poche (ex: la porte).")]
    [SerializeField] private Lockable lockTarget;

    /// <summary>
    /// A appeler par le systeme de grab/pickup (ou un testeur) quand le
    /// joueur interagit avec le cadenas. Deverrouille la cible si la cle a
    /// ete collectee ; sinon ne fait rien (juste un message de debug).
    /// </summary>
    public void TryUnlock()
    {
        if (key == null || lockTarget == null)
        {
            Debug.LogWarning("KeyPadlockPuzzle sur '" + name + "' : reference 'key' ou 'lockTarget' manquante dans l'Inspector.");
            return;
        }

        if (key.IsCollected)
        {
            lockTarget.Unlock();
        }
        else
        {
            Debug.Log("Il faut d'abord trouver la cle.");
        }
    }
}
