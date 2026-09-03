// Vérifie l'ordre dans lequel les 3 livres entrent dans le carton
// ("Cardboard", scène LivingRoom). À attacher sur le GameObject "Cardboard".
//
// Seul compte l'ORDRE de premier contact avec la zone de la boîte : les
// livres peuvent être posés n'importe où À L'INTÉRIEUR, il n'y a pas de
// slot/position à respecter. Dès que les 3 livres sont simultanément dans
// la zone, on compare l'ordre d'insertion courant à l'ordre attendu :
//   Livro_dos_Espiritos (1er) -> A_Genese (2e) -> Nosso_Lar (3e)
//   - correspond exactement  -> CreepyDollAudioController.NotifyBooksSolved()
//     puis le puzzle est verrouillé (tout OnTrigger... suivant est ignoré).
//   - ne correspond pas       -> rien ne se passe, simple Debug.Log. Le
//     joueur peut ressortir un livre (OnTriggerExit le retire de la liste)
//     et recommencer dans un autre ordre.
//
// Ne connaît RIEN du grab ni du clavier : la détection se fait uniquement
// via colliders/triggers physiques. Ne connaît de CreepyDollAudioController
// que sa méthode publique NotifyBooksSolved() - aucune dépendance dans
// l'autre sens (même découplage que CombinationLock / KeyPadlockPuzzle).
//
// PRÉREQUIS SCÈNE (à vérifier dans l'Inspector, NE PAS casser si déjà en
// place) :
//   - "Cardboard" doit porter un Collider avec Is Trigger = true, couvrant
//     le volume INTÉRIEUR de la boîte. Un warning est loggué au démarrage si
//     ce n'est pas le cas.
//   - Chaque livre doit porter un Rigidbody (kinematic tant qu'il n'y a pas
//     de physique de grab) ET son propre Collider individuel : voir le
//     commentaire d'en-tête de BookIdentity. Sans Rigidbody sur le livre OU
//     sur le carton, aucun OnTriggerEnter/Exit ne se déclenche.

using System.Collections.Generic;
using UnityEngine;

public class BookOrderPuzzle : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Poupée du salon : NotifyBooksSolved() est appelée dessus quand l'ordre des 3 livres est correct.")]
    [SerializeField] private CreepyDollAudioController creepyDoll;

    // Ordre d'insertion attendu, défini en dur (pas exposé à l'Inspector :
    // c'est une règle du puzzle, pas un réglage).
    private readonly BookIdentity.BookType[] _expectedOrder =
    {
        BookIdentity.BookType.LivroDosEspiritos,
        BookIdentity.BookType.AGenese,
        BookIdentity.BookType.NossoLar
    };

    // Ordre d'insertion courant : un type est ajouté à la fin quand le livre
    // entre dans la zone, retiré quand il en sort. Sert aussi de garde
    // anti-doublon (un livre à plusieurs colliders ne compte qu'une fois).
    private readonly List<BookIdentity.BookType> _insertionOrder = new List<BookIdentity.BookType>();

    private bool _solved;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning("BookOrderPuzzle sur '" + name + "' : ce GameObject doit porter un Collider avec 'Is Trigger' coché, couvrant l'intérieur de la boîte. Sans ça, aucun livre ne sera détecté.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_solved) return;

        var book = other.GetComponentInParent<BookIdentity>();
        if (book == null) return;

        if (_insertionOrder.Contains(book.Type))
        {
            // Déjà compté (autre collider du même livre, ou re-entrée
            // parasite) : on ignore sans toucher à l'ordre.
            return;
        }

        _insertionOrder.Add(book.Type);
        Debug.Log("BookOrderPuzzle sur '" + name + "' : livre ENTRÉ '" + book.Type + "' (" + _insertionOrder.Count + "/" + _expectedOrder.Length + "). Ordre courant : " + DescribeCurrentOrder());

        if (_insertionOrder.Count < _expectedOrder.Length) return;

        // Les 3 livres sont dans la boîte : on vérifie.
        if (MatchesExpectedOrder())
        {
            _solved = true;
            Debug.Log("BookOrderPuzzle sur '" + name + "' : ORDRE CORRECT ! Puzzle des livres résolu, vérifications suivantes désactivées.");

            if (creepyDoll != null)
            {
                creepyDoll.NotifyBooksSolved();
            }
            else
            {
                Debug.LogWarning("BookOrderPuzzle sur '" + name + "' : référence 'Creepy Doll' non assignée dans l'Inspector, NotifyBooksSolved() n'a pas pu être appelée.");
            }
        }
        else
        {
            Debug.Log("BookOrderPuzzle sur '" + name + "' : 3 livres présents mais ordre INCORRECT (" + DescribeCurrentOrder() + "), rien ne se passe. Attendu : " + DescribeExpectedOrder() + ". Ressors un livre et recommence.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_solved) return;

        var book = other.GetComponentInParent<BookIdentity>();
        if (book == null) return;

        if (!_insertionOrder.Remove(book.Type))
        {
            // Pas dans la liste (livre à plusieurs colliders déjà retiré,
            // ou sortie parasite) : rien à faire.
            return;
        }

        Debug.Log("BookOrderPuzzle sur '" + name + "' : livre SORTI '" + book.Type + "' (" + _insertionOrder.Count + "/" + _expectedOrder.Length + "). Ordre courant : " + DescribeCurrentOrder());
    }

    private bool MatchesExpectedOrder()
    {
        if (_insertionOrder.Count != _expectedOrder.Length) return false;

        for (int i = 0; i < _expectedOrder.Length; i++)
        {
            if (_insertionOrder[i] != _expectedOrder[i]) return false;
        }
        return true;
    }

    private string DescribeCurrentOrder()
    {
        return _insertionOrder.Count == 0 ? "(vide)" : string.Join(" -> ", _insertionOrder);
    }

    private string DescribeExpectedOrder()
    {
        return string.Join(" -> ", _expectedOrder);
    }
}
