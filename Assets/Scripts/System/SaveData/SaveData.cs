using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    // PLAYER 1 TRANSFORM
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;

    // PLAYER 2 TRANSFORM
    public float pos2X, pos2Y, pos2Z;
    public float rot2X, rot2Y, rot2Z, rot2W;

    // ROOM & SPAWN POINT
    public string lastRoomID;
    public float spawnP1X, spawnP1Y, spawnP1Z;
    public float spawnP2X, spawnP2Y, spawnP2Z;

    // METADATA
    public string saveSlot;
    public string savedAt;
    public int playTimeSeconds;

    // MINIMAP
    public string[] visitedRooms = new string[0];

    // HELPER — PLAYER 1
    public void SetPosition(Vector3 pos)
    {
        posX = pos.x; posY = pos.y; posZ = pos.z;
    }
    public Vector3 GetPosition() => new Vector3(posX, posY, posZ);

    public void SetRotation(Quaternion rot)
    {
        rotX = rot.x; rotY = rot.y; rotZ = rot.z; rotW = rot.w;
    }
    public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);

    // HELPER — PLAYER 2
    public void SetPosition2(Vector3 pos)
    {
        pos2X = pos.x; pos2Y = pos.y; pos2Z = pos.z;
    }
    public Vector3 GetPosition2() => new Vector3(pos2X, pos2Y, pos2Z);

    public void SetRotation2(Quaternion rot)
    {
        rot2X = rot.x; rot2Y = rot.y; rot2Z = rot.z; rot2W = rot.w;
    }
    public Quaternion GetRotation2() => new Quaternion(rot2X, rot2Y, rot2Z, rot2W);

    // HELPER — SPAWN POINTS
    public void SetSpawnP1(Vector3 pos)
    {
        spawnP1X = pos.x; spawnP1Y = pos.y; spawnP1Z = pos.z;
    }
    public Vector3 GetSpawnP1() => new Vector3(spawnP1X, spawnP1Y, spawnP1Z);

    public void SetSpawnP2(Vector3 pos)
    {
        spawnP2X = pos.x; spawnP2Y = pos.y; spawnP2Z = pos.z;
    }
    public Vector3 GetSpawnP2() => new Vector3(spawnP2X, spawnP2Y, spawnP2Z);
}