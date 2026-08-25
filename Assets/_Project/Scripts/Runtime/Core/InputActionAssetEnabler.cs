using UnityEngine;
using UnityEngine.InputSystem; //bring input action

namespace FreddieMercury.Core
{
    // Enables every action map of the asset while this component is active.
    [DisallowMultipleComponent]
    public sealed class InputActionAssetEnabler : MonoBehaviour
    {
        [SerializeField, Tooltip("Action asset enabled for as long as this component is active.")]
        InputActionAsset m_Actions;

        public InputActionAsset actions => m_Actions; // so actions only referes to m_actions cannot modified m_actions (readonly)

        void OnEnable() //activate the map when the game obj is created/inisialize
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
