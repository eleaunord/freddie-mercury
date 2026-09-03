// Identité d'un livre pour le puzzle du carton (Cardboard) : porte
// uniquement le type du livre, choisi dans l'Inspector. À attacher
// INDIVIDUELLEMENT sur chacun des 3 livres (Livro_dos_Espiritos, A_Genese,
// Nosso_Lar), qui restent enfants du GameObject "Books" (aucune
// réorganisation de hiérarchie nécessaire).
//
// Ne connaît RIEN du puzzle ni de l'ordre attendu : c'est BookOrderPuzzle
// (sur "Cardboard") qui lit BookType via OnTriggerEnter/Exit. Même logique
// de découplage que CollectibleItem ou KeyInsertable - un component passif
// que d'autres interrogent, jamais l'inverse.
//
// PRÉREQUIS SCÈNE (à vérifier dans l'Inspector, NE PAS casser si déjà en
// place, NE PAS modifier la hiérarchie pour autant) :
//   - Chaque livre doit porter son PROPRE Collider individuel (un Box
//     Collider non-trigger suffit), sur ce même GameObject ou sur un de ses
//     enfants. Il ne doit PAS y avoir de collider unique fusionné au niveau
//     du parent "Books" : sinon le trigger du carton ne verrait qu'un seul
//     contact pour les 3 livres et l'ordre d'insertion serait indétectable.
//   - Chaque livre doit porter un Rigidbody pour que les événements
//     OnTriggerEnter/Exit du carton se déclenchent. Tant que la physique de
//     grab de Personne 1 n'est pas là, mettre ce Rigidbody en Is Kinematic
//     (+ décocher Use Gravity) comme le fait KeyInsertable, pour que le
//     livre ne tombe pas et ne pousse rien.

using UnityEngine;

public class BookIdentity : MonoBehaviour
{
    public enum BookType
    {
        LivroDosEspiritos,
        AGenese,
        NossoLar
    }

    [Tooltip("Type de CE livre, à assigner dans l'Inspector pour chacun des 3 livres.")]
    [SerializeField] private BookType bookType;

    /// <summary>Type de ce livre, tel qu'assigné dans l'Inspector.</summary>
    public BookType Type => bookType;
}
