using UnityEngine;

public class _GP_ThrowItem : MonoBehaviour
{
    [Header("Item to throw")]
    [Tooltip("GameObject yang akan di-pickup.")]
    public GameObject _itemToPickup;
    [Tooltip("GameObject yang akan di-throw.")]
    public GameObject _itemToThrow;

    [Header("Throwing References")]
    [SerializeField] private Transform _throwItemSlot;

    [Header("Throwing Settings")]
    [SerializeField] private float _throwForce = 10f;

    [Header("Pickup Validation")]
    [SerializeField] private float _maxPickupDistance = 2.5f;

    // Cached pickup target so animation events still work if trigger exit happens before the event frame.
    private GameObject _pendingPickup;

    void Start()
    {
        _itemToThrow = null;
        _pendingPickup = null;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ThrowableItem"))
        {
            _itemToPickup = other.gameObject;
            _pendingPickup = other.gameObject;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ThrowableItem") && other.gameObject == _itemToPickup)
        {
            _itemToPickup = null;
        }

        if (other.CompareTag("ThrowableItem") && other.gameObject == _pendingPickup)
        {
            _pendingPickup = null;
        }
    }

    public bool HasPickupCandidate()
    {
        return GetValidPickupTarget() != null;
    }

    public void OnPickUpItem()
    {
        GameObject targetPickup = GetValidPickupTarget();
        if (targetPickup != null)
        {
            // Drop currently held item before picking up the new one
            if (_itemToThrow != null)
            {
                _itemToThrow.transform.SetParent(null);
                Rigidbody heldRb = _itemToThrow.GetComponent<Rigidbody>();
                if (heldRb != null)
                    heldRb.isKinematic = false;
                _itemToThrow = null;
            }

            if (_throwItemSlot == null)
            {
                Debug.LogWarning("_GP_ThrowItem: Throw item slot is not assigned.");
                return;
            }

            targetPickup.transform.SetParent(_throwItemSlot);
            Rigidbody pickupRb = targetPickup.GetComponent<Rigidbody>();
            if (pickupRb != null)
                pickupRb.isKinematic = true;
            targetPickup.transform.localPosition = Vector3.zero;
            targetPickup.transform.localRotation = Quaternion.identity;
            _itemToThrow = targetPickup;
            _itemToPickup = null;
            _pendingPickup = null;
        }
    }

    private GameObject GetValidPickupTarget()
    {
        GameObject candidate = _itemToPickup != null ? _itemToPickup : _pendingPickup;
        if (candidate == null)
            return null;

        if (!candidate.activeInHierarchy)
        {
            ClearCandidate(candidate);
            return null;
        }

        float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
        if (sqrDistance > _maxPickupDistance * _maxPickupDistance)
        {
            ClearCandidate(candidate);
            return null;
        }

        return candidate;
    }

    private void ClearCandidate(GameObject candidate)
    {
        if (_itemToPickup == candidate)
            _itemToPickup = null;
        if (_pendingPickup == candidate)
            _pendingPickup = null;
    }

    public void ThrowItem()
    {
        if (_itemToThrow != null)
        {
            _itemToThrow.transform.SetParent(null);
            Rigidbody rb = _itemToThrow.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(_throwItemSlot.forward * _throwForce, ForceMode.Impulse);
            _itemToThrow = null;
        }
    }

    public void DropItem()
    {
        if (_itemToThrow != null)
        {
            _itemToThrow.transform.SetParent(null);
            Rigidbody rb = _itemToThrow.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;
            _itemToThrow = null;
        }
    }
}
