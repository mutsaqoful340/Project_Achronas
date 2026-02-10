using UnityEngine;

/// <summary>
/// Component yang dipasang pada pointer GameObject untuk mendeteksi collision dengan success zone.
/// Attach script ini pada GameObject pointer yang memiliki Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class _RadialQTE_PointerCollider : MonoBehaviour
{
    [Tooltip("Reference ke _RadialQTE parent")]
    public _RadialQTE radialQTE;

    private void Awake()
    {
        // Auto-find parent jika tidak di-assign
        if (radialQTE == null)
        {
            radialQTE = GetComponentInParent<_RadialQTE>();
        }

        // Pastikan collider adalah trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah collision dengan success zone
        if (other.CompareTag("QTE_SuccessZone") && radialQTE != null)
        {
            radialQTE.OnPointerEnterSuccessZone();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Cek apakah keluar dari success zone
        if (other.CompareTag("QTE_SuccessZone") && radialQTE != null)
        {
            radialQTE.OnPointerExitSuccessZone();
        }
    }
}
