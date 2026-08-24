using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace FreddieMercury.Player
{
    // Replaces the tracking origin handling an XROrigin would provide.
    // In Floor mode the runtime already reports the player's real height,
    // so the camera keeps no manual offset. Polling is needed because the
    // subsystem only exists once the XR loader has started.
    [DisallowMultipleComponent]
    public sealed class XRTrackingOrigin : MonoBehaviour
    {
        [SerializeField, Tooltip("Reference point the runtime reports poses from.")]
        TrackingOriginModeFlags m_Mode = TrackingOriginModeFlags.Floor;

        [SerializeField, Tooltip("Seconds spent waiting for the XR input subsystem to start before giving up.")]
        float m_StartupTimeout = 10f;

        static readonly List<XRInputSubsystem> k_Subsystems = new List<XRInputSubsystem>();

        IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + m_StartupTimeout;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryApplyMode())
                    yield break;

                yield return null;
            }

            Debug.LogWarning(
                $"[{nameof(XRTrackingOrigin)}] No running XR input subsystem accepted the '{m_Mode}' tracking origin. " +
                "The rig keeps the device default, which usually places the player's head at this transform.",
                this);
        }

        bool TryApplyMode()
        {
            SubsystemManager.GetSubsystems(k_Subsystems);

            var applied = false;
            foreach (var subsystem in k_Subsystems)
            {
                if (!subsystem.running || (subsystem.GetSupportedTrackingOriginModes() & m_Mode) == 0)
                    continue;

                applied |= subsystem.TrySetTrackingOriginMode(m_Mode);
            }

            return applied;
        }
    }
}
