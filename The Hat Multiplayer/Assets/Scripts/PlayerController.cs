using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviour
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [HideInInspector]
    public float curHatTime;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;

    void Update()
    {
        Move();
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
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
}
