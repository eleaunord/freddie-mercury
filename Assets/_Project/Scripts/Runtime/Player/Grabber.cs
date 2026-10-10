using System.Drawing;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace FreddieMercury.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PinchGrab : MonoBehaviour
    {
        [Header("Hand")]
        [SerializeField, Tooltip("Source of joint updates. One instance exists per hand.")]
        XRHandTrackingEvents m_HandEvents;

        [SerializeField, Tooltip("Transform poses are reported relative to. Usually the XR Origin.")]
        Transform m_TrackingSpace;

        [Header("Pinch")]
        [SerializeField, Tooltip("Thumb-index distance that closes the pinch, in meters.")]
        float m_CloseThreshold = 0.02f;

        [SerializeField, Tooltip("Thumb-index distance that opens it again. Must exceed the close threshold.")]
        float m_OpenThreshold = 0.035f;

        [Header("Grab")]
        [SerializeField, Tooltip("Search radius around the pinch point, in meters.")]
        float m_GrabRadius = 0.05f;

        [SerializeField, Tooltip("Layers considered grabbable.")]
        LayerMask m_GrabbableLayers = ~0;

        Vector3 oldPoint = Vector3.zero;

        Vector3 velocityPoint = Vector3.zero;

        readonly Collider[] m_Hits = new Collider[8];

        bool m_Pinching;
        Grabbable m_Held;

        void OnValidate()
        {
            // To prevend if values are incorrect
            if (m_OpenThreshold <= m_CloseThreshold)
            {
                m_OpenThreshold = m_CloseThreshold + 0.005f;
            }
        }

        void OnEnable()
        {
            if (!m_HandEvents)
            {
                Debug.LogError("Grabber need a XRHandTracking reference.", this);
                enabled = false;
            }
            else // Subscribe our function to the handevent list of ref function
            {
                m_HandEvents.jointsUpdated.AddListener(OnJointsUpdated);
                m_HandEvents.trackingLost.AddListener(OnTrackingLost);
            }
        }

        void OnDisable()
        {
            if (!m_HandEvents)
            {
                Debug.LogError("Grabber need a XRHandTracking reference.", this);
                enabled = false;
            }
            else
            {
                Release();
                m_HandEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
                m_HandEvents.trackingLost.RemoveListener(OnTrackingLost);
            }
        }

        void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            if (!args.hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumbPose) || !args.hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indexPose))
                return;
            var dist = Vector3.Distance(thumbPose.position, indexPose.position);
            var point = ToWorld((thumbPose.position + indexPose.position) / 2f); // Le point est la position du princement (entre le pouce et l'index), plus tard egalement la position de l'objet princé
            if (dist <= m_CloseThreshold && !m_Pinching)
            {
                Grab(point);
                m_Pinching = true;
            }
            if (dist >= m_OpenThreshold && m_Pinching)
            {
                Release();
                m_Pinching = false;
            }
            if (m_Held)
            {
                m_Held.TryMoveTo(point);
            }
            if (m_Pinching && m_Held) // TODO: Cas d'erreur premier point ?
            {
                velocityPoint = (point - oldPoint) / Time.deltaTime; //vitesse = distance divisé par temps d'une frame
            }
            oldPoint = point;
        }

        void OnTrackingLost()
        {
            if (m_Held)
                Release();
        }

        Vector3 ToWorld(Vector3 trackingSpacePosition)
        {
            // Le casque te donne les positions du pouce/index relatives au rig, on a besoin des valeurs par apport a notre espace a nous ex: notre scene. TransformPoint fait la convertion des element d'espace rig a espace scene en tenant compte des position ET rotation.
            if (m_TrackingSpace)
                return m_TrackingSpace.TransformPoint(trackingSpacePosition);
            else
                return trackingSpacePosition;
        }

        void Grab(Vector3 point)
        {
            // Les hits sont les elements intercepté par le grabs ils sont au nombre maximum de 8, ils vont tous etre flitrés jusqu'a trouvé le meilleur condidat (l'element le plus proche du point de grab)
            int count = Physics.OverlapSphereNonAlloc(point, m_GrabRadius, m_Hits, m_GrabbableLayers); // (retutilise le tableau) Elle dessine une sphère invisible dans la scène et dit quels colliders la touchent
            Grabbable finalComp = null;
            float smallest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (m_Hits[i].attachedRigidbody)
                {
                    if (m_Hits[i].TryGetComponent(out Grabbable component))
                    {
                        if (Vector3.Distance(component.transform.position, point) < smallest)
                        {
                            finalComp = component;
                            smallest = Vector3.Distance(component.transform.position, point);
                        }
                    }
                }
            }
            if (finalComp == null)
            {
                return ;
            }
            else // Si trouvé on stock le candidat a grab et on enleve la gravité afin de pouvoir le manipuler sans contrainte
            {
                m_Held = finalComp;
                m_Held.Body.isKinematic = true;
            }
        }

        void Release()
        {
            if (m_Held == null)
            {
                return;
            }
            m_Held.Body.linearVelocity = velocityPoint; //Donner la velocité
            m_Held.Body.isKinematic = false;
            m_Held = null;
        }
    }
}
