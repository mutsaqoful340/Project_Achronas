[System.Serializable]
public class SaveData
{
    public string checkpointID;

    public float posX, posY, posZ;

    // 🔥 TAMBAHAN (biar gak spawn di depan lagi)
    public float rightX, rightY, rightZ;
}