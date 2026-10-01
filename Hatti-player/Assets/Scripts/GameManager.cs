using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;
    public float timeToWin;             // total time to hold the hat to win
    public float invincibleDuration;    // prevents players from instantly losing hat
    private float hatPickupTime;        // the time that hat was picked up

    [Header("Players")]
    public string playerPrefabLocation; // path in Resources folder
    public Transform[] spawnPoints;     // array of all available spawn points
    public PlayerController[] players;  // array of all the players
    public int playerWithHat;           // id of the player with the hat
    private int playersInGame;          // number of players in the game 

    [Header("Effects")]
    public string explosionPrefabLocation;

    // instance
    public static GameManager instance;

    private void Awake()
    {
        // lazy singleton - see NetworkManager for better implementation of this pattern
        // narrator justifies this based on the idea that we're not persisting this via DontDestroyOnLoad
        instance = this;
    }

    private void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    // called everytime a player finishes its start function
    [PunRPC]
    void ImInGame()
    {
        playersInGame++;

        // when the last player announces they're in game, each player can spawn themselves
        if (playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }

    void SpawnPlayer()
    {
        // instantiate the player across the network
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity); 


        // get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();

        // initialize the player
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer(int playerId)
    {
        return players.FirstOrDefault(x => x != null && x.id == playerId);
    }

    public PlayerController GetPlayer(GameObject playerObject)
    {
        return players.First(x => x != null && x.gameObject == playerObject);
    }

    // called when the player hits the hatted player - giving them the hat
    [PunRPC]
    public void GiveHat(int playerId, bool initialGive)
    {
        // Remove the hat from the previous holder if they still exist.
        if (!initialGive)
        {
            PlayerController previousHolder = null;

            // Only try to find the previous holder if their ID is valid.
            if (playerWithHat > 0 && playerWithHat <= players.Length)
            {
                previousHolder = players[playerWithHat - 1];
            }

            if (previousHolder != null)
            {
                previousHolder.SetHat(false);
            }
        }

        // Update who currently has the hat.
        playerWithHat = playerId;

        // Find the new holder.
        PlayerController newHolder = GetPlayer(playerId);

        if (newHolder != null)
        {
            // Give them the hat.
            newHolder.SetHat(true);
        }

        // Prevent immediate transfer.
        hatPickupTime = Time.time;
    }

    // is the player able to take the hat at this current time?
    public bool CanGetHat()
    {
        if (Time.time > hatPickupTime + invincibleDuration)
            return true;
        else
            return false;
    }
    
    [PunRPC]
    public void PlayExplosion(Vector3 position)
    {
        GameObject explosion = Instantiate(
            Resources.Load<GameObject>(explosionPrefabLocation),
            position,
            Quaternion.identity
        );

        Destroy(explosion, 0.6f);
    }

    public void EliminatePlayer(int playerId)
    {
        if (!PhotonNetwork.IsMasterClient || gameEnded)
            return;

        PlayerController eliminatedPlayer = GetPlayer(playerId);

        if (eliminatedPlayer == null)
            return;

        // Save the player's position BEFORE destroying them
        Vector3 explosionPosition = eliminatedPlayer.transform.position;

        // Tell everyone to spawn the explosion
        photonView.RPC(
            "PlayExplosion",
            RpcTarget.All,
            explosionPosition
        );

        // Tell the player to destroy themselves
        eliminatedPlayer.photonView.RPC(
            "Eliminate",
            eliminatedPlayer.photonPlayer
        );

        // Remove from the Master Client's player list
        players[playerId - 1] = null;

        int playersRemaining = players.Count(p => p != null);

        // If only one player remains, they win
        if (playersRemaining == 1)
        {
            gameEnded = true;

            PlayerController winner = players.First(p => p != null);

            photonView.RPC(
                "WinGame",
                RpcTarget.All,
                winner.id
            );
        }

        // More than one player remains -> find the next player
        if (playersRemaining > 1)
        {
            PlayerController nextPlayer = GetNextPlayer(playerId);

            if (nextPlayer != null)
            {
                photonView.RPC(
                    "GiveHat",
                    RpcTarget.All,
                    nextPlayer.id,
                    false
                );
            }
        }
    }

    private PlayerController GetNextPlayer(int eliminatedPlayerId)
    {
        // Start searching with the player immediately after
        // the eliminated player's ID.
        for (int offset = 1; offset <= players.Length; offset++)
        {
            int nextId = ((eliminatedPlayerId - 1 + offset) % players.Length) + 1;

            PlayerController candidate = players[nextId - 1];

            if (candidate != null)
                return candidate;
        }

        return null;
    }



    [PunRPC]
    void WinGame(int playerId)
    {
        gameEnded = true;
        PlayerController player = GetPlayer(playerId);

        // set the UI to show who's won
        GameUI.instance.SetWinText(player.photonPlayer.NickName);

        Invoke("GoBackToMenu", 3.0f);
    }

    void GoBackToMenu()
    {
        PhotonNetwork.LeaveRoom();
        NetworkManager.instance.ChangeScene("Menu");
    }
}