using UnityEngine;
using UnityEngine.InputSystem;

namespace FreddieMercury.Core
{
    // Enables every action map of the asset while this component is active.
    // Components reading actions through an InputActionReference (Tracked Pose
    // Driver, and later the interaction scripts) never enable the asset
    // themselves, so a single owner has to do it for the whole rig.
    [DisallowMultipleComponent]
    public sealed class InputActionAssetEnabler : MonoBehaviour
    {
        [SerializeField, Tooltip("Action asset enabled for as long as this component is active.")]
        InputActionAsset m_Actions;

        public InputActionAsset actions => m_Actions;

        void OnEnable()
        {
            if (m_Actions == null)
            {
                Debug.LogError($"[{nameof(InputActionAssetEnabler)}] No action asset assigned: nothing will be tracked.", this);
                return;
            }

            m_Actions.Enable();
        }

        void OnDisable()
        {
            if (m_Actions != null)
                m_Actions.Disable();
        }
    }
}
