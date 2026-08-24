// TEMPORAIRE - pilote ConstrainedRotation au clavier/souris en attendant le
// vrai système de grab. À supprimer une fois le grab réel branché.
//
// Vise le centre de l'écran depuis la Camera portée par ce GameObject
// (TEMP_TestCamera). Maintenir la touche E et bouger la souris
// horizontalement fait tourner l'objet visé, s'il expose un
// ConstrainedRotation, via sa seule API publique (Begin/Update/End) —
// ce script ne connaît rien d'autre de ConstrainedRotation.
//
// Note : TempTestCameraController lit la souris en permanence (curseur
// verrouillé) pour le regard, ce qui entrerait en conflit avec la lecture
// de la souris ci-dessous. C'est pourquoi l'interaction est déclenchée par
// la touche E (et non un clic) : pendant que E est maintenu, ce script
// désactive temporairement TempTestCameraController pour libérer l'axe
// souris horizontal, puis le réactive au relâchement. Le curseur reste
// verrouillé/invisible en permanence, aucune libération n'est nécessaire.

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TEMP_KeyboardRotationTester : MonoBehaviour
{
    [Header("Visée (TEMP)")]
    [Tooltip("Caméra depuis laquelle on vise le centre de l'écran. Par défaut, la Camera portée par ce GameObject.")]
    [SerializeField] private Camera sourceCamera;
    [Tooltip("Portée max du raycast de visée, en mètres.")]
    [SerializeField] private float maxRayDistance = 3f;

    [Header("Sensibilité (TEMP)")]
    [Tooltip("Degrés de rotation appliqués par unité de mouvement souris horizontal.")]
    [SerializeField] private float mouseSensitivity = 90f;

    private ConstrainedRotation _activeRotation;
    private TempTestCameraController _cameraLookController;

    private void Awake()
    {
        if (sourceCamera == null) sourceCamera = GetComponent<Camera>();

        _cameraLookController = GetComponent<TempTestCameraController>();
        if (_cameraLookController == null)
        {
            Debug.LogWarning("TEMP_KeyboardRotationTester: aucun TempTestCameraController trouvé sur '" + name +
                "' - la caméra ne sera pas mise en pause pendant l'interaction (E), la souris pilotera les deux à la fois.");
        }
    }

    private void Update()
    {
        if (sourceCamera == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryBeginInteraction();
        }

        if (_activeRotation != null && Input.GetKey(KeyCode.E))
        {
            float deltaAngle = Input.GetAxis("Mouse X") * mouseSensitivity;
            _activeRotation.UpdateInteraction(deltaAngle);
        }

        if (Input.GetKeyUp(KeyCode.E) && _activeRotation != null)
        {
            EndInteraction();
        }
    }

    private void OnDisable()
    {
        // Ne laisse jamais un objet "accroché" ni la caméra désactivée si
        // le tester est lui-même désactivé en pleine interaction.
        if (_activeRotation != null)
        {
            EndInteraction();
        }
    }

    private void TryBeginInteraction()
    {
        Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            // GetComponentInParent car le BoxCollider vit sur l'enfant
            // (Corps/Panel) alors que ConstrainedRotation vit sur le pivot
            // (le parent, à la charnière) pour les objets à pivot déporté.
            var rotation = hit.collider.GetComponentInParent<ConstrainedRotation>();
            if (rotation != null)
            {
                // Un éventuel Lockable sur le même objet (ex: la porte)
                // conditionne l'interaction : verrouillé = on ignore l'input.
                var lockable = rotation.GetComponent<Lockable>();
                if (lockable != null && lockable.IsLocked)
                {
                    Debug.Log("'" + rotation.name + "' est verrouillé - interaction ignorée.");
                    return;
                }

                _activeRotation = rotation;
                _activeRotation.BeginInteraction();

                if (_cameraLookController != null) _cameraLookController.enabled = false;
            }
        }
    }

    private void EndInteraction()
    {
        _activeRotation.EndInteraction();
        _activeRotation = null;

        if (_cameraLookController != null) _cameraLookController.enabled = true;
    }
}
