using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;  // Kecepatan gerakan pemain

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Ambil komponen Rigidbody
    }

    void Update()
    {
        // Input gerakan horizontal (A dan D atau tombol kiri dan kanan)
        float moveHorizontal = Input.GetAxis("Horizontal"); // A = -1, D = 1
        // Input gerakan vertikal (W dan S atau tombol atas dan bawah)
        float moveVertical = Input.GetAxis("Vertical"); // W = 1, S = -1

        // Buat vektor pergerakan berdasarkan input
        Vector3 movement = new Vector3(moveHorizontal, 0f, moveVertical);

        // Gerakkan pemain dengan menambah gaya pada Rigidbody
        rb.AddForce(movement * moveSpeed);
    }
}

