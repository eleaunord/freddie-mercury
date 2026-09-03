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

        readonly Collider[] m_Hits = new Collider[8];

        bool m_Pinching;
        Rigidbody m_Held;

        void OnValidate()
        {
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
            else
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
            var point = ToWorld((thumbPose.position + indexPose.position) / 2f);

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
                m_Held.MovePosition(point);
            }
        }

        void OnTrackingLost()
        {
        }

        Vector3 ToWorld(Vector3 trackingSpacePosition)
        {
            if (m_TrackingSpace)
                return m_TrackingSpace.TransformPoint(trackingSpacePosition);
            else
                return trackingSpacePosition;
        }

        void Grab(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, m_GrabRadius, m_Hits, m_GrabbableLayers);
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
            else
            {
                m_Held = finalComp.GetComponent<Rigidbody>();
                m_Held.isKinematic = true;
            }
        }

        void Release()
        {
            if (m_Held == null)
            {
                return;
            }
            m_Held.isKinematic = false;
            m_Held = null;
        }
    }
}
