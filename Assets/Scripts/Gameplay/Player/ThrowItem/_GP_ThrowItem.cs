using UnityEngine;
using UnityEngine.InputSystem;

public class _GP_ThrowItem : MonoBehaviour
{
    [Header("Item to throw")]
    [Tooltip("GameObject yang akan di-pickup.")]
    [SerializeField] private GameObject _itemToPickup;
    [Tooltip("GameObject yang akan di-throw.")]
    [SerializeField] private GameObject _itemToThrow;

    [Header("Throwing References")]
    [SerializeField] private Transform _throwItemSlot;

    [Header("Throwing Settings")]
    [SerializeField] private float _throwForce = 10f;

    private InputActions _inputActions;

    void Start()
    {
        _itemToThrow = null;
    }

    private void OnEnable()
    {
        if (_inputActions == null)
            _inputActions = new InputActions();

        _inputActions.Player.Interact.performed += OnInteractPerformed;
        _inputActions.Player.Throw.performed += OnThrowPerformed;
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (_inputActions != null)
        {
            _inputActions.Player.Interact.performed -= OnInteractPerformed;
            _inputActions.Player.Throw.performed -= OnThrowPerformed;
            _inputActions.Player.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        OnPickUpItem();
    }

    private void OnThrowPerformed(InputAction.CallbackContext context)
    {
        ThrowItem();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ThrowableItem"))
        {
            _itemToPickup = other.gameObject;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ThrowableItem") && other.gameObject == _itemToPickup)
        {
            _itemToPickup = null;
        }
    }

    private void OnPickUpItem()
    {
        if (_itemToPickup != null)
        {
            // Drop currently held item before picking up the new one
            if (_itemToThrow != null)
            {
                _itemToThrow.transform.SetParent(null);
                _itemToThrow.GetComponent<Rigidbody>().isKinematic = false;
                _itemToThrow = null;
            }

            _itemToPickup.transform.SetParent(_throwItemSlot);
            _itemToPickup.GetComponent<Rigidbody>().isKinematic = true;
            _itemToPickup.transform.localPosition = Vector3.zero;
            _itemToPickup.transform.localRotation = Quaternion.identity;
            _itemToThrow = _itemToPickup;
            _itemToPickup = null;
        }
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
}
