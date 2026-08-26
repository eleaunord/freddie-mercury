// TEMPORAIRE - pilote INTERACT_Cle au clavier (nouveau Input System) en
// attendant le vrai système de grab/pinch de Personne 1 (Pôle A). À
// supprimer une fois le grab réel branché.
//
// Flèches ou WASD (+ Espace/Maj. gauche pour la hauteur) pour approcher la
// clé de la zone serrure d'un cadenas. Une fois KeyInsertable.IsInserted
// vrai, Q/E appelle ApplyRotationDelta sur le RotationConstraint du cadenas
// actuellement occupé (KeyInsertable.CurrentLock) - exactement l'appel que
// fera le futur système de grab à la place de Q/E, sans rien changer à
// KeyInsertable ni à RotationConstraint.
//
// Ce script ne connaît de RotationConstraint que sa méthode publique
// ApplyRotationDelta, et de KeyInsertable que IsInserted/CurrentLock : il
// n'a aucune connaissance de la logique de clamp ou de la condition
// d'insertion, qui restent entièrement gérées par ces deux components.
//
// Utilise exclusivement les API du nouveau Input System (Keyboard.current),
// jamais Input.GetKey/GetAxis : Active Input Handling est réglé sur "Input
// System Package (New)" uniquement dans ce projet.
//
// À activer uniquement en mode debug : décoche "Debug Actif" dans
// l'Inspector, ou désactive simplement ce component, pour le neutraliser
// sans le retirer du GameObject.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(KeyInsertable))]
public class TEMP_KeyInsertionTester : MonoBehaviour
{
    [Header("Debug (TEMP)")]
    [Tooltip("Décoche pour désactiver totalement ce testeur clavier sans le retirer du GameObject.")]
    [SerializeField] private bool debugActif = true;

    [Header("Déplacement (TEMP)")]
    [Tooltip("Vitesse de déplacement de la clé, en m/s.")]
    [SerializeField] private float moveSpeed = 0.5f;

    [Header("Rotation (TEMP)")]
    [Tooltip("Degrés appliqués par seconde de maintien de Q ou E, une fois la clé insérée.")]
    [SerializeField] private float rotationSpeed = 60f;

    private KeyInsertable _keyInsertable;

    private void Awake()
    {
        _keyInsertable = GetComponent<KeyInsertable>();
    }

    private void Update()
    {
        if (!debugActif) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        HandleMovement(keyboard);
        HandleRotation(keyboard);
    }

    private void HandleMovement(Keyboard keyboard)
    {
        float horizontal = 0f;
        if (keyboard[Key.RightArrow].isPressed || keyboard[Key.D].isPressed) horizontal += 1f;
        if (keyboard[Key.LeftArrow].isPressed || keyboard[Key.A].isPressed) horizontal -= 1f;

        float depth = 0f;
        if (keyboard[Key.UpArrow].isPressed || keyboard[Key.W].isPressed) depth += 1f;
        if (keyboard[Key.DownArrow].isPressed || keyboard[Key.S].isPressed) depth -= 1f;

        float vertical = 0f;
        if (keyboard[Key.Space].isPressed) vertical += 1f;
        if (keyboard[Key.LeftShift].isPressed) vertical -= 1f;

        Vector3 move = new Vector3(horizontal, vertical, depth);
        if (move.sqrMagnitude > 1f) move.Normalize();

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation(Keyboard keyboard)
    {
        // La condition d'insertion est entièrement vérifiée par
        // RotationConstraint.ApplyRotationDelta lui-même ; on ne consulte
        // IsInserted ici que pour éviter d'appeler inutilement (et pour
        // ignorer Q/E tant que rien n'est inséré, comme le ferait le grab).
        if (!_keyInsertable.IsInserted) return;

        RotationConstraint targetLock = _keyInsertable.CurrentLock;
        if (targetLock == null) return;

        float rotationInput = 0f;
        if (keyboard[Key.E].isPressed) rotationInput += 1f;
        if (keyboard[Key.Q].isPressed) rotationInput -= 1f;

        if (Mathf.Approximately(rotationInput, 0f)) return;

        targetLock.ApplyRotationDelta(rotationInput * rotationSpeed * Time.deltaTime);
    }
}
