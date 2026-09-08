using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;              // has the game ended?
    public float timeToWin;                     // time a player needs to hold the hat to win
    public float invincibleDuration;            // how long after a player gets the hat, are they invincible
    private float hatPickupTime;                // the time the hat was picked up by the current holder

    [Header("Players")]
    public string playerPrefabLocation;         // path in Resources folder to the Player prefab
    public Transform[] spawnPoints;             // array of all available spawn points
    public PlayerController[] players;          // array of all the players
    public int playerWithHat;                   // id of the player with the hat
    private int playersInGame;                  // number of players in the game

    // instance
    public static GameManager instance;
    void Awake()
    {
        // instance
        instance = this;
    }

    void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.All);
    }

    [PunRPC]
    void ImInGame()
    {
        playersInGame++;

        if (playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }

    // spawns a player and initializes it
    void SpawnPlayer()
    {
        // instantiate the player across the network
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.Identity);
        
        // get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();
    }


}
