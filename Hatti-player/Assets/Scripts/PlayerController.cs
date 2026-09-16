using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [Header("Appearance")]
    public Renderer playerRenderer;

    private readonly Color[] playerColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    [Header("Effects")]
    public string explosionPrefabLocation = "Explosion";

    [HideInInspector]
    public float curHatTime;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;

    // called when the player object is instantiated
    [PunRPC]
    public void Initialize(Player player)
    {
        photonPlayer = player;
        id = player.ActorNumber;

        GameManager.instance.players[id - 1] = this;

        // Assign this player a color based on their ActorNumber
        SetPlayerColor();

        // give the first player the hat
        if (id == 1)
            GameManager.instance.GiveHat(id, true);

        // if this isn't our local player, disable physics
        if (!photonView.IsMine)
            rig.isKinematic = true;
    }

    private void SetPlayerColor()
    {
        int colorIndex = (id - 1) % playerColors.Length;

        playerRenderer.material.color = playerColors[colorIndex];
    }


    private void Update()
    {
        if (photonView.IsMine)
        {
            Move();

            if (Input.GetKeyDown(KeyCode.Space))
                TryJump();

            // track the amount of time we're wearing the hat
            if (hatObject.activeInHierarchy)
            {
                curHatTime += Time.deltaTime;
            }
        }

        // The Master Client checks if this player has reached the time limit
        if (PhotonNetwork.IsMasterClient)
        {
            if (curHatTime >= GameManager.instance.timeToWin &&
                !GameManager.instance.gameEnded)
            {
                GameManager.instance.EliminatePlayer(id);
            }
        }
    }

    [PunRPC]
    public void Eliminate()
    {
        if (!photonView.IsMine)
            return;

        // Remember where the player was
        Vector3 explosionPosition = transform.position;

        // Spawn the explosion across the network
        PhotonNetwork.Instantiate(
            explosionPrefabLocation,
            explosionPosition,
            Quaternion.identity
        );

        PhotonNetwork.Destroy(gameObject);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rig.linearVelocity = new Vector3(x, rig.linearVelocity.y, z);
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.7f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void SetHat(bool hasHat)
    {
        hatObject.SetActive(hasHat);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.instance.GetPlayer(collision.gameObject).id == GameManager.instance.playerWithHat)
            {
                if (GameManager.instance.CanGetHat())
                {
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }

    // from IPunObservable - allows us to send and receive data
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curHatTime); // send info about this playercontroller to others
        }
        else if (stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext(); // receive info about this playercontroller from others
        }
    }
}