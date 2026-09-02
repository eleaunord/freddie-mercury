    using System.Collections; //necessary pour coroutine
    using System.Collections.Generic; //necessary pour List<T>
    using UnityEngine;
    using UnityEngine.XR; //bring XRInputSubsystem

    namespace FreddieMercury.Player
    {
        // Replaces the tracking origin handling an XROrigin would provide.
        // In Floor mode the runtime already reports the player's real height,
        // so the camera keeps no manual offset. Polling is needed because the
        // subsystem only exists once the XR loader has started.
        // with the headset firmware ground captation gives the real height of the ground which permit that the player head is well placed without calculate
        [DisallowMultipleComponent] //disable to being able to add 2 times this script on the same game object for obv problems
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

                    if (subsystem.TrySetTrackingOriginMode(m_Mode))
                    {
                        applied = true;
                    }
                }
                return applied;
            }
        }
    }
