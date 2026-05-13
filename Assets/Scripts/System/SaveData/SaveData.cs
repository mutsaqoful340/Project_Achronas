using System;
using UnityEngine;

/// <summary>
/// Container untuk semua data yang akan disimpan ke disk.
/// Tambahkan field baru di sini jika ingin menyimpan data lain.
/// </summary>
[Serializable]
public class SaveData
{
    // ── Player Transform ──────────────────────────────────────
    public float posX;
    public float posY;
    public float posZ;

    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;

    // ── Metadata ──────────────────────────────────────────────
    public string saveSlot;
    public string savedAt;   // ISO 8601, contoh: "2025-06-01T14:30:00"
    public int    playTimeSeconds;

    // ── Helper: konversi dari/ke Unity types ──────────────────
    public void SetPosition(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }

    public Vector3 GetPosition() => new Vector3(posX, posY, posZ);

    public void SetRotation(Quaternion rot)
    {
        rotX = rot.x;
        rotY = rot.y;
        rotZ = rot.z;
        rotW = rot.w;
    }

    public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
}
