// Fait tourner Handle librement (sans limite d'angle) autour d'un axe local
// fixe, en réponse au grab direct de PinchGrab (Grabber.cs, lecture seule).
//
// Architecture (v3) : l'angle est piloté DIRECTEMENT depuis la pose de la
// main (XR Hands, même package que PinchGrab, lu indépendamment via nos
// propres XRHandTrackingEvents), jamais depuis la position de Handle.
// Les deux versions précédentes dérivaient l'angle de la position du
// Rigidbody, que PinchGrab tire librement en 3D (MovePosition) puis que ce
// script re-snappait à l'ancre - deux écritures concurrentes sur le même
// Transform, intrinsèquement instable pour extraire une rotation 1DOF
// propre. Ici, plus aucune lecture de la position de Handle n'entre dans le
// calcul de rotation : la main est la seule source, la position de Handle
// n'est plus qu'un résultat verrouillé en LateUpdate.
//
// Détection "tenu" : PinchGrab n'expose aucun event ni propriété publique
// sur son état de grab (vérifié dans PinchGrab.cs). Seul son effet de bord
// sur Rigidbody.isKinematic (true au grab, false au relâchement - sur le
// MÊME Rigidbody que celui-ci, Handle portant Collider + Grabbable +
// Rigidbody sur un seul GameObject) est observable de l'extérieur, et ce
// script lit isKinematic DIRECTEMENT (pas de flag intermédiaire) : tenu si
// et seulement si isKinematicNow est vrai.
//
// (Historique : une version précédente forçait isKinematic à true après
// chaque relâchement pour annuler le throw, avec un flag _isHeld séparé
// pour ne pas confondre ce true forcé avec un vrai grab. Ça cassait le
// second grab - une fois forcé à true, plus aucune transition false->true
// n'était observable pour PinchGrab.Grab(), donc _isHeld ne repassait
// jamais à true. Le throw est en réalité déjà annulé par le seul zeroing de
// linearVelocity/angularVelocity ci-dessous : sans gravité et sans autre
// force sur ce Rigidbody, une vélocité nulle suffit à ce qu'il ne bouge pas
// tout seul, et LateUpdate re-verrouille de toute façon la position/rotation
// visibles à chaque frame quoi qu'il arrive côté physique. isKinematic peut
// donc redevenir un signal "tenu" propre et non trafiqué.)
//
// Vérifié dans PinchGrab.cs : aucune écriture de rotation nulle part (ni
// MoveRotation, ni assignation de .rotation/.localRotation) - seul
// isKinematic et MovePosition (position) sont touchés. Le verrouillage de
// Y/Z ci-dessous n'a donc besoin d'aucun re-snap de rotation explicite :
// _baseLocalRotation * Quaternion.AngleAxis(angle, axisLocal) ne fait
// varier QUE la composante autour de l'axe choisi, par construction
// quaternion - jamais via transform.localEulerAngles, pour éviter tout
// risque de wrap 0-360.
//
// Choix de la main tenante : au premier FixedUpdate où isKinematicNow devient
// vrai, on ne verrouille PAS immédiatement une main par défaut - les events
// jointsUpdated ne sont pas synchronisés avec le passage isKinematic
// false->true, donc aucun cache de point de pince n'est garanti disponible
// cette frame précise. Tant qu'aucune main n'a de point de pince valide en
// cache, _holdingHand reste None et aucune rotation n'est calculée; dès
// qu'une main a un point valide (potentiellement plusieurs frames plus
// tard), elle est verrouillée pour le reste de la tenue (la plus proche du
// pivot si les deux sont valides ce jour-là).
//
// [DefaultExecutionOrder] : garde-fou pour s'exécuter après PinchGrab si son
// ordre d'exécution venait à changer un jour.

using UnityEngine;
using UnityEngine.XR.Hands;

namespace FreddieMercury
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody))]
    public class HandleCrankRotator : MonoBehaviour
    {
        private enum RotationAxisLocal { X, Y, Z }
        private enum HoldingHand { None, Left, Right }

        [Header("Axe de rotation")]
        [Tooltip("Axe local (au moment du Start, avant toute rotation appliquée par ce script) autour duquel Handle tourne librement, sans limite d'angle.")]
        [SerializeField] private RotationAxisLocal axis = RotationAxisLocal.Z;

        [Header("Détection de rotation")]
        [Tooltip("Delta angulaire (en degrés) en dessous duquel un pas physique n'est pas considéré comme une rotation active - filtre le bruit numérique du hand tracking.")]
        [SerializeField] private float rotationEpsilonDegrees = 0.02f;

        [Header("Mains (lecture directe XR Hands, indépendante de PinchGrab)")]
        [Tooltip("Source de joints de la main gauche. Assigner le même composant XRHandTrackingEvents que celui référencé par le PinchGrab de la main gauche.")]
        [SerializeField] private XRHandTrackingEvents leftHandEvents;
        [Tooltip("Source de joints de la main droite. Assigner le même composant XRHandTrackingEvents que celui référencé par le PinchGrab de la main droite.")]
        [SerializeField] private XRHandTrackingEvents rightHandEvents;
        [Tooltip("Transform relatif auquel les poses de joints sont rapportées (généralement le XR Origin). Même référence que celle utilisée par PinchGrab.")]
        [SerializeField] private Transform trackingSpace;

        private Rigidbody _rigidbody;
        private Vector3 _anchorLocalPosition;
        private Quaternion _baseLocalRotation;
        private float _accumulatedAngleDegrees;
        private Vector3 _previousProjectedDirection;

        private bool _wasKinematic;
        private HoldingHand _holdingHand = HoldingHand.None;

        private Vector3 _leftPinchWorld;
        private bool _leftPinchValid;
        private Vector3 _rightPinchWorld;
        private bool _rightPinchValid;

        /// <summary>Delta angulaire, en degrés, mesuré au dernier pas physique (0 hors grab actif).</summary>
        public float CurrentDeltaAngle { get; private set; }

        /// <summary>Vrai si Handle a tourné au dernier pas physique (|CurrentDeltaAngle| au-dessus du seuil de bruit).</summary>
        public bool IsBeingRotated => Mathf.Abs(CurrentDeltaAngle) > rotationEpsilonDegrees;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _anchorLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _wasKinematic = _rigidbody.isKinematic;
        }

        private void OnEnable()
        {
            if (leftHandEvents != null)
            {
                leftHandEvents.jointsUpdated.AddListener(OnLeftJointsUpdated);
                leftHandEvents.trackingLost.AddListener(OnLeftTrackingLost);
            }
            if (rightHandEvents != null)
            {
                rightHandEvents.jointsUpdated.AddListener(OnRightJointsUpdated);
                rightHandEvents.trackingLost.AddListener(OnRightTrackingLost);
            }
        }

        private void OnDisable()
        {
            if (leftHandEvents != null)
            {
                leftHandEvents.jointsUpdated.RemoveListener(OnLeftJointsUpdated);
                leftHandEvents.trackingLost.RemoveListener(OnLeftTrackingLost);
            }
            if (rightHandEvents != null)
            {
                rightHandEvents.jointsUpdated.RemoveListener(OnRightJointsUpdated);
                rightHandEvents.trackingLost.RemoveListener(OnRightTrackingLost);
            }
        }

        private void OnLeftJointsUpdated(XRHandJointsUpdatedEventArgs args) => CachePinchPoint(args, ref _leftPinchWorld, ref _leftPinchValid);
        private void OnRightJointsUpdated(XRHandJointsUpdatedEventArgs args) => CachePinchPoint(args, ref _rightPinchWorld, ref _rightPinchValid);
        private void OnLeftTrackingLost() => _leftPinchValid = false;
        private void OnRightTrackingLost() => _rightPinchValid = false;

        private void CachePinchPoint(XRHandJointsUpdatedEventArgs args, ref Vector3 cachedWorldPoint, ref bool cacheValid)
        {
            if (args.hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumbPose) &&
                args.hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indexPose))
            {
                Vector3 localPoint = (thumbPose.position + indexPose.position) / 2f;
                cachedWorldPoint = trackingSpace != null ? trackingSpace.TransformPoint(localPoint) : localPoint;
                cacheValid = true;
            }
            else
            {
                cacheValid = false;
            }
        }

        private void FixedUpdate()
        {
            bool isKinematicNow = _rigidbody.isKinematic;

            if (isKinematicNow && !_wasKinematic)
            {
                // Front de grab authentique : PinchGrab.Grab() vient de passer isKinematic
                // à true. On ne verrouille pas encore de main : ça se résout ci-dessous,
                // dès qu'un point de pince valide existe.
                _holdingHand = HoldingHand.None;
            }
            else if (!isKinematicNow && _wasKinematic)
            {
                // Front de relâchement authentique : PinchGrab.Release() vient de passer
                // isKinematic à false. On annule toute vélocité résiduelle pour ne jamais
                // laisser Unity simuler le "throw" implicite du passage en Rigidbody
                // dynamique - suffisant en soi (pas de gravité, pas d'autre force sur ce
                // Rigidbody, et LateUpdate reverrouille de toute façon la position/rotation
                // visibles à chaque frame). isKinematic reste donc false, intact, prêt à
                // signaler le PROCHAIN grab par un vrai front false->true.
                _holdingHand = HoldingHand.None;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (isKinematicNow)
            {
                if (_holdingHand == HoldingHand.None)
                {
                    _holdingHand = ChooseClosestValidHand();
                    if (_holdingHand != HoldingHand.None && TryGetHandWorldPoint(_holdingHand, out Vector3 startWorld))
                    {
                        _previousProjectedDirection = ProjectedDirectionFromPivot(startWorld);
                    }
                }

                if (_holdingHand != HoldingHand.None && TryGetHandWorldPoint(_holdingHand, out Vector3 handWorld))
                {
                    Vector3 currentProjected = ProjectedDirectionFromPivot(handWorld);

                    // currentProjected vaut (quasi) zéro si la main tenante se trouve
                    // exactement sur l'axe de rotation (cas dégénéré, division par zéro
                    // interne à Vector3.SignedAngle). On ignore ce pas plutôt que
                    // d'écrire une référence nulle qui casserait le calcul du pas suivant.
                    const float MinProjectedSqrMagnitude = 1e-8f; // ~0.1 mm

                    if (currentProjected.sqrMagnitude >= MinProjectedSqrMagnitude)
                    {
                        CurrentDeltaAngle = Vector3.SignedAngle(_previousProjectedDirection, currentProjected, AxisWorld());
                        _previousProjectedDirection = currentProjected;
                        _accumulatedAngleDegrees += CurrentDeltaAngle;
                    }
                    else
                    {
                        CurrentDeltaAngle = 0f;
                    }
                }
                else
                {
                    // Tenu, mais aucune main résolue pour l'instant (aucun point de pince
                    // en cache encore) : pas de rotation ce pas, on retentera au suivant.
                    CurrentDeltaAngle = 0f;
                }
            }
            else
            {
                CurrentDeltaAngle = 0f;
            }

            _wasKinematic = _rigidbody.isKinematic;
        }

        private void LateUpdate()
        {
            // Verrouillage visuel : à CHAQUE frame rendue, Handle doit apparaître ancré
            // dans son logement (aucune translation possible) et à sa rotation
            // accumulée sur le seul axe configuré (Y et Z de _baseLocalRotation restent
            // structurellement inchangés par cette composition quaternion - PinchGrab ne
            // touchant jamais la rotation, cette seule ligne suffit à les verrouiller,
            // pas besoin de les réassigner séparément).
            transform.localPosition = _anchorLocalPosition;
            transform.localRotation = _baseLocalRotation * Quaternion.AngleAxis(_accumulatedAngleDegrees, AxisVector(axis));
        }

        private HoldingHand ChooseClosestValidHand()
        {
            bool leftOk = TryGetHandWorldPoint(HoldingHand.Left, out Vector3 leftPoint);
            bool rightOk = TryGetHandWorldPoint(HoldingHand.Right, out Vector3 rightPoint);

            if (!leftOk && !rightOk) return HoldingHand.None;
            if (leftOk && !rightOk) return HoldingHand.Left;
            if (rightOk && !leftOk) return HoldingHand.Right;

            Vector3 pivot = PivotWorld();
            float leftDist = Vector3.Distance(leftPoint, pivot);
            float rightDist = Vector3.Distance(rightPoint, pivot);
            return leftDist <= rightDist ? HoldingHand.Left : HoldingHand.Right;
        }

        private bool TryGetHandWorldPoint(HoldingHand hand, out Vector3 point)
        {
            switch (hand)
            {
                case HoldingHand.Left: point = _leftPinchWorld; return _leftPinchValid;
                case HoldingHand.Right: point = _rightPinchWorld; return _rightPinchValid;
                default: point = default; return false;
            }
        }

        /// <summary>Position de repos de Handle (son centre de rotation), en espace monde.</summary>
        private Vector3 PivotWorld()
        {
            return transform.parent != null
                ? transform.parent.TransformPoint(_anchorLocalPosition)
                : _anchorLocalPosition;
        }

        /// <summary>
        /// Vecteur (position monde donnée - pivot), projeté sur le plan perpendiculaire
        /// à l'axe de rotation. Mesure l'écart angulaire de la main tenante par rapport
        /// au pivot, sans jamais lire la position ou l'orientation de Handle lui-même.
        /// </summary>
        private Vector3 ProjectedDirectionFromPivot(Vector3 worldPosition)
        {
            Vector3 toPoint = worldPosition - PivotWorld();
            return Vector3.ProjectOnPlane(toPoint, AxisWorld());
        }

        /// <summary>
        /// Axe de rotation en espace monde, figé sur l'orientation de Handle au
        /// Start() (avant toute rotation appliquée par ce script) : tourner autour de
        /// son propre axe local ne fait pas dériver cet axe, donc pas besoin de le
        /// recalculer à partir de la rotation courante - seul un déplacement du
        /// parent le ferait, d'où la lecture de transform.parent à chaque appel.
        /// </summary>
        private Vector3 AxisWorld()
        {
            Vector3 localAxis = _baseLocalRotation * AxisVector(axis);
            return transform.parent != null ? transform.parent.TransformDirection(localAxis) : localAxis;
        }

        private static Vector3 AxisVector(RotationAxisLocal a)
        {
            switch (a)
            {
                case RotationAxisLocal.X: return Vector3.right;
                case RotationAxisLocal.Y: return Vector3.up;
                default: return Vector3.forward;
            }
        }

        private void OnDrawGizmos()
        {
            // Hors Play Mode, _baseLocalRotation n'a jamais été capturée (Start() pas
            // encore appelé) : on retombe sur la rotation locale actuelle de l'éditeur.
            Quaternion baseRotation = Application.isPlaying ? _baseLocalRotation : transform.localRotation;
            Vector3 localAxis = baseRotation * AxisVector(axis);
            Vector3 axisWorld = (transform.parent != null ? transform.parent.TransformDirection(localAxis) : localAxis).normalized;

            Vector3 center = transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center - axisWorld * 0.075f, center + axisWorld * 0.075f);
            Gizmos.DrawSphere(center, 0.005f);
        }
    }
}
