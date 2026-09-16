using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    [SerializeField] private float lifetime = 40f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
