// Component générique pour un objet dont l'interaction est conditionnée par
// un verrou (ex: la porte, tant que le cadenas n'est pas ouvert).
//
// Ne touche PAS à ConstrainedRotation ni à quoi que ce soit d'autre : c'est
// à l'appelant (futur système de grab, ou un testeur clavier) de consulter
// IsLocked avant de déclencher une interaction sur l'objet verrouillé.

using UnityEngine;

public class Lockable : MonoBehaviour
{
    [Tooltip("Vrai tant que l'objet est verrouillé. À true par défaut.")]
    [SerializeField] private bool isLocked = true;

    public bool IsLocked => isLocked;

    /// <summary>À appeler quand la condition de déverrouillage est remplie.</summary>
    public void Unlock()
    {
        isLocked = false;
    }
}
