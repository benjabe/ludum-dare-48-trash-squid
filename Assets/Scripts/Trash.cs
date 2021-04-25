using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Trash : MonoBehaviour
{
    public static event Action<Trash, TrashDetector> OnOnTrashEntersTriggerHandled;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 5, group = "Trash")] private float _timeAfterDropUntilPickUpPossible = 0.4f;
    [Header("Rigidbody parameters")]
    [SerializeField, TweakableMember(minValue = -5, maxValue = 5, group = "Trash")] private float _airGravityScale = 1.0f;
    [SerializeField, TweakableMember(minValue = -5, maxValue = 5, group = "Trash")] private float _waterGravityScale = 0.001f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 10, group = "Trash")] private float _airLinearDrag = 0.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 10, group = "Trash")] private float _waterLinearDrag = 1.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 25, group = "Trash")] private float _airAngularDrag = 15.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 25, group = "Trash")] private float _waterAngularDrag = 15.0f;

    private bool _inWater = false;
    private bool _canBePickedUp = true;
    private bool _inBoat = false;
    private Rigidbody2D _rigidBody = null;
    private Collider2D _collider = null;
    private Collider2D _pickUpCollider = null;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        SquidController.OnDropTrash += OnSquidDropTrash;
        BoatTop.OnTrashHit += OnTrashHitBoatTop;
        TrashDetector.OnTrashEntersTrigger += OnEnterTrashDetectorTrigger;
    }

    private void OnEnterTrashDetectorTrigger(TrashDetector trashDetector, Trash trash)
    {
        if (trash != this) return;
        if (!_canBePickedUp) return;
        _pickUpCollider = trashDetector.GetComponent<Collider2D>();
        OnOnTrashEntersTriggerHandled?.Invoke(this, trashDetector);
    }

    private void OnDestroy()
    {
        SquidController.OnDropTrash -= OnSquidDropTrash;
        BoatTop.OnTrashHit -= OnTrashHitBoatTop;
    }

    private void OnTrashHitBoatTop(BoatTop boatTop, Trash trash)
    {
        if (trash != this) return;
        _canBePickedUp = false;
        _inBoat = true;
        _rigidBody.simulated = false;
    }

    private void Update()
    {
        _inWater = transform.position.y < 0;
        _rigidBody.gravityScale = _inWater ? _waterGravityScale : _airGravityScale;
        _rigidBody.drag = _inWater ? _waterLinearDrag : _airLinearDrag;
        _rigidBody.angularDrag = _inWater ? _waterAngularDrag : _airAngularDrag;
        if (_pickUpCollider != null)
            Physics2D.IgnoreCollision(_collider, _pickUpCollider, !_canBePickedUp);
    }

    private void OnSquidDropTrash(SquidController squid, Trash trash)
    {
        if (trash != this) return;
        _canBePickedUp = false;
        StartCoroutine(StartPickUpTimer());
    }

    private IEnumerator StartPickUpTimer()
    {
        yield return new WaitForSeconds(_timeAfterDropUntilPickUpPossible);
        _canBePickedUp = !_inBoat;
    }
}