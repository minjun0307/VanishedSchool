using UnityEngine;

public class Utillity : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "Spot3";
    [SerializeField] GameObject roomContent;

    public Vector3 Enter(PlayerMove player)
    {
        player.EnterRoom(Room.Utillity, Floor.F1);
        roomContent.SetActive(true);
        return roomContent.transform.Find(spotName).position;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
