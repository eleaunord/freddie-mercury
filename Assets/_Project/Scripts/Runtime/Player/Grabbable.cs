using UnityEngine;

namespace FreddieMercury
{
    public class Grabbable : MonoBehaviour
    {
        Rigidbody m_Rigidbody;

        // exposer m_Rigidbody en lecture seule pour PinchGrab
        public Rigidbody Body => m_Rigidbody;
        //          PinchGrab en a besoin pour isKinematic/linearVelocity au grab/release,
        //          sans avoir à refaire un GetComponent à chaque fois.

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void TryMoveTo(Vector3 targetPosition)
        {
            if (this.TryGetComponent(out AxisConstraint component)) // Si le gameobj contient un componenet axisconstraint alors corriger le deplacement grace a clamp sinon mouvement classique
                m_Rigidbody.MovePosition(component.Clamp(targetPosition));
            else 
                m_Rigidbody.MovePosition(targetPosition);
        }
    }
}
