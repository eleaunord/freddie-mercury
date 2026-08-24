// TEMPORAIRE - simule le ramassage d'objets (CollectibleItem) et le
// déverrouillage de puzzle (KeyPadlockPuzzle) au clavier, en attendant le
// vrai système de pickup/grab en place. À supprimer une fois le vrai
// système de pickup/grab en place.
//
// Vise le centre de l'écran depuis la Camera portée par ce GameObject
// (TEMP_TestCamera). Touche F : si l'objet visé a un CollectibleItem non
// encore collecté, l'appel Collect() dessus ; si l'objet visé a un
// KeyPadlockPuzzle, appelle TryUnlock() dessus. Ce script ne connaît rien
// d'autre de ces components que leur API publique.

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TEMP_ItemInteractionTester : MonoBehaviour
{
    [Header("Visée (TEMP)")]
    [Tooltip("Caméra depuis laquelle on vise le centre de l'écran. Par défaut, la Camera portée par ce GameObject.")]
    [SerializeField] private Camera sourceCamera;
    [Tooltip("Portée max du raycast de visée, en mètres.")]
    [SerializeField] private float maxRayDistance = 3f;

    private void Awake()
    {
        if (sourceCamera == null) sourceCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (sourceCamera == null) return;
        if (!Input.GetKeyDown(KeyCode.F)) return;

        Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)) return;

        // GetComponentInParent : le collider peut vivre sur un enfant
        // (ex: "Corps") alors que le component visé est sur le parent.
        var collectible = hit.collider.GetComponentInParent<CollectibleItem>();
        if (collectible != null && !collectible.IsCollected)
        {
            collectible.Collect();
        }

        var puzzle = hit.collider.GetComponentInParent<KeyPadlockPuzzle>();
        if (puzzle != null)
        {
            puzzle.TryUnlock();
        }
    }
}
