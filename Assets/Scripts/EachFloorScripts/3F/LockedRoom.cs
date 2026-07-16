using UnityEngine;

public class LockedRoom : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "Spot";
    [SerializeField] GameObject roomContent;
    public bool m_IsLocked = true;
    public Vector3 Enter(PlayerMove player)
    {
        if(m_IsLocked!=true )
        {
            player.EnterRoom(Room.LockedRoom, Floor.F3);
            roomContent.SetActive(true);
            return roomContent.transform.Find(spotName).position;
        }
        return Vector3.zero;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
