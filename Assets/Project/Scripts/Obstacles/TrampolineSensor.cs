using UnityEngine;


namespace NonameGame
{
    public class TrampolineSensor : MonoBehaviour
    {
        private Trampoline _trampoline;

        private void Awake()
        {
            _trampoline = transform.parent.GetComponent<Trampoline>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_trampoline == null)
                return;

            var rb = other.attachedRigidbody;
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();

            if (rb == null)
                return;

            // Не регистрируем сам батут
            if (rb.transform == _trampoline.transform)
                return;

            _trampoline.Add(rb, rb.linearVelocity.y);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_trampoline == null)
                return;

            var rb = other.attachedRigidbody;
            if (rb == null)
                rb = other.GetComponentInParent<Rigidbody>();

            if (rb == null)
                return;

            _trampoline.Remove(rb);
        }
    }
}