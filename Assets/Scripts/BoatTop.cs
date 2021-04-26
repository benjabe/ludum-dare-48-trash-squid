using System;
using UnityEngine;

public class BoatTop : MonoBehaviour
{
    public static event Action<BoatTop, Trash> OnTrashHit;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("trigger");
        HandleTrashCollision(collision);
    }

    private void HandleTrashCollision(Collision2D collision)
    {
        var trash = collision.gameObject.GetComponent<Trash>();
        if (trash == null || !trash.GetComponent<Rigidbody2D>().simulated) return;
        OnTrashHit?.Invoke(this, trash);
    }
}
