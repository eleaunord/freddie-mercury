// Component générique pour un objet ramassable (ex: la clé). Ne dépend
// d'aucun système de grab/pickup : expose uniquement Collect(), destinée à
// être appelée par un futur système de grab VR ou par un testeur clavier.
//
// "Collecté" est simulé en désactivant le rendu et le collider de l'objet
// (donne l'impression qu'il a rejoint un inventaire), sans nécessiter de
// vrai système d'inventaire pour l'instant.

using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    /// <summary>Vrai une fois l'objet ramassé. Ne redevient jamais faux.</summary>
    public bool IsCollected { get; private set; }

    private Renderer _renderer;
    private Collider _collider;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
    }

    /// <summary>
    /// À appeler par le système de grab/pickup (ou un testeur) pour ramasser
    /// l'objet. Sans effet si déjà collecté.
    /// </summary>
    public void Collect()
    {
        if (IsCollected) return;

        IsCollected = true;
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
    }
}
