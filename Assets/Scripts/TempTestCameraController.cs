// TEMPORAIRE - à supprimer une fois le XR Origin intégré, ne pas committer avec le reste du projet final
//
// Contrôleur de caméra de test non-VR pour valider les proportions de la scène
// (Lobby_Greybox) en Play Mode, en attendant l'intégration du XR Origin (OpenXR)
// par le reste de l'équipe.
//
// - Déplacement ZQSD / WASD au sol, vitesse de marche réaliste (~2 m/s)
// - Rotation de la caméra à la souris (regarder autour)
// - Pas de saut, pas de vol : la hauteur (Y) reste verrouillée en permanence
//
// A retirer de la scène et supprimer ce fichier dès que le vrai XR Origin est en place.

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TempTestCameraController : MonoBehaviour
{
    [Header("Déplacement (TEMP)")]
    [Tooltip("Vitesse de marche en m/s.")]
    public float moveSpeed = 2f;

    [Header("Regard souris (TEMP)")]
    [Tooltip("Sensibilité de la rotation caméra à la souris.")]
    public float mouseSensitivity = 2f;
    [Tooltip("Limite de rotation verticale (haut/bas) en degrés.")]
    public float pitchLimit = 80f;

    private float _yaw;
    private float _pitch;
    private float _fixedHeight;

    void Start()
    {
        // Verrouille la hauteur de départ (yeux à 1.7m) : on ne monte/descend jamais.
        _fixedHeight = transform.position.y;

        Vector3 startEuler = transform.eulerAngles;
        _yaw = startEuler.y;
        _pitch = startEuler.x;

        // Souris capturée par défaut pour pouvoir regarder autour immédiatement.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();

        // Echap pour libérer la souris (pratique en test), clic pour la reprendre.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    void HandleMovement()
    {
        // Supporte ZQSD (AZERTY) et WASD (QWERTY) simultanément.
        float forwardInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) forwardInput += 1f;
        if (Input.GetKey(KeyCode.S)) forwardInput -= 1f;

        float rightInput = 0f;
        if (Input.GetKey(KeyCode.D)) rightInput += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) rightInput -= 1f;

        // Déplacement à plat (on ignore le pitch de la caméra) : pas de vol, pas de saut.
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normzalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move = (forward * forwardInput + right * rightInput);
        if (move.sqrMagnitude > 1f) move.Normalize();

        Vector3 newPos = transform.position + move * moveSpeed * Time.deltaTime;
        newPos.y = _fixedHeight; // reste au sol en permanence
        transform.position = newPos;
    }
}
