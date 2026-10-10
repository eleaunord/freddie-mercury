using System;
using UnityEngine;

namespace FreddieMercury
{
    // Posé à côté de Grabbable sur un objet du type tiroir/levier. Grabbable.TryMoveTo
    // appelle Clamp() avant de déplacer le Rigidbody, pour brider le mouvement sur un
    // seul axe plutôt que de suivre la main librement 
    public class AxisConstraint : MonoBehaviour
    {
        [SerializeField, Tooltip("Axe local le long duquel l'objet peut glisser (ex: Vector3.forward).")]
        Vector3 m_LocalAxis = Vector3.forward;

        [SerializeField, Tooltip("Distance minimale autorisée le long de l'axe, par rapport à la position de repos.")]
        float m_MinDistance = 0f;

        [SerializeField, Tooltip("Distance maximale autorisée le long de l'axe, par rapport à la position de repos.")]
        float m_MaxDistance = 0.3f;

        Vector3 m_RestPosition;

        void Awake()
        {
            m_RestPosition = transform.position; // Capture la position de depart de l'objet
            // TODO H — capture la position de départ de l'objet dans m_RestPosition,
            //          une seule fois (transform.position). C'est le point "distance 0"
            //          à partir duquel m_MinDistance/m_MaxDistance sont mesurées.
        }

        public Vector3 Clamp(Vector3 desiredWorldPosition)
        {
            float clampedValue = 0;
            Vector3 m_movement = desiredWorldPosition - m_RestPosition; //Distance du mouvement
            m_LocalAxis = transform.TransformDirection(m_LocalAxis); // normalise la direction autorisé
            clampedValue = Math.Clamp(Vector3.Dot(m_movement, m_LocalAxis), m_MinDistance, m_MaxDistance); // Projette le vector sur l'axe autorisé (dot), et clamp cette valeur entre min et max pré-defini
            desiredWorldPosition = m_RestPosition + (m_LocalAxis * clampedValue); // Enfin renvoyer le vector du deplacement (corrigé) à effectué (on prend le vector puis on additionne par l'axe autorisé multiplié par la valeur de laxe autorisé)
            return desiredWorldPosition;
        }
    }
}
