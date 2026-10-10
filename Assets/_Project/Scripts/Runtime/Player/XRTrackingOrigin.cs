    using System.Collections; //necessary pour coroutine
    using System.Collections.Generic; //necessary pour List<T>
    using UnityEngine;
    using UnityEngine.XR; //bring XRInputSubsystem

    namespace FreddieMercury.Player
    {
        public sealed class XRTrackingOrigin : MonoBehaviour //"sealed" means no one should heritate of this class to extend
        {
            [SerializeField, Tooltip("Reference point the runtime reports poses from.")]
            TrackingOriginModeFlags m_Mode = TrackingOriginModeFlags.Floor; // Floor = the xr runtime (headset) instant gives the floor position as 0.0.0

            [SerializeField, Tooltip("Seconds spent waiting for the XR input subsystem to start before giving up.")]
            float m_StartupTimeout = 10f;

            // Reused every frame so polling doesn't allocate.
            static readonly List<XRInputSubsystem> k_Subsystems = new List<XRInputSubsystem>();

            IEnumerator Start() // start coroutine (waiting) ! permit to not stop the main thread with traditional while () yield return so we dont return the program :)
            {
                var deadline = Time.realtimeSinceStartup + m_StartupTimeout;

                while (Time.realtimeSinceStartup < deadline)
                {
                    if (TryApplyModeToRunningSubsystems())
                    {
                        yield break;
                    }
                    yield return null;
                }
            }
            bool TryApplyModeToRunningSubsystems()
            {
                SubsystemManager.GetSubsystems(k_Subsystems);

                var applied = false;
                foreach (var subsystem in k_Subsystems)
                {
                    if (!subsystem.running || !subsystem.GetSupportedTrackingOriginModes().HasFlag(m_Mode))
                    {
                        continue;
                    }

                    if (subsystem.TrySetTrackingOriginMode(m_Mode)) // Demande au casque de mesurer les positions depuis le sol donc sol = 0 et pas device = 0
                    {
                        applied = true;
                    }
                }
                return applied;
            }
        }
    }
