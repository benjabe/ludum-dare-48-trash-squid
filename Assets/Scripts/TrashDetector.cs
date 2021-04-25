using System;
using UnityEngine;

public class TrashDetector : MonoBehaviour
{
    public static event Action<TrashDetector, Trash> OnTrashEntersTrigger;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTrashTriggerEnter(collision);
    }

    private void HandleTrashTriggerEnter(Collider2D collision)
    {
        var trash = collision.gameObject.GetComponent<Trash>();
        if (trash == null) return;
        OnTrashEntersTrigger?.Invoke(this, trash);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
    }
}
