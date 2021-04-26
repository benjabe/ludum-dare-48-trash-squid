using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SquidController : MonoBehaviour
{
    public static event Action<SquidController, Trash> OnDropTrash;
    public Animator animator;

    [SerializeField, TweakableMember(group = "Squid")] private float _thrustForceMagnitude = 1.0f;
    [SerializeField] private float _relativeForceDirectionInDegrees = 0.0f;
    [SerializeField, TweakableMember(minValue = -100, maxValue = 100, group = "Squid")] private float _torqueForceMagnitude = 1f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 5, group = "Squid")] private float _thrustTime = 0.5f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 5, group = "Squid")] private float _trashThrowDownFactor = 0.5f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 5, group = "Squid")] private float _trashThrowVelocityFactor = 0.5f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 5, group = "Squid")] private float _dropTime = 0.5f;
    [SerializeField] private bool _canSpamThrust = false;
    [Header("Rigidbody parameters")]
    [SerializeField, TweakableMember(minValue = -5, maxValue = 5, group = "Squid")] private float _airGravityScale = 1.0f;
    [SerializeField, TweakableMember(minValue = -5, maxValue = 5, group = "Squid")] private float _waterGravityScale = 0.001f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 10, group = "Squid")] private float _airLinearDrag = 0.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 10, group = "Squid")] private float _waterLinearDrag = 1.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 25, group = "Squid")] private float _airAngularDrag = 15.0f;
    [SerializeField, TweakableMember(minValue = 0, maxValue = 25, group = "Squid")] private float _waterAngularDrag = 15.0f;

    private Rigidbody2D _rigidBody = null;
    private Collider2D _collider = null;
    private TrashDetector _trashCollider = null;
    private bool _isThrustQueued = false;
    private bool _canThrust = true;
    private float _torque = 0.0f;
    private bool _inWater = false;
    private List<Trash> _pickedUpTrash = new List<Trash>();
    private bool _canPickUp = true;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _trashCollider = GetComponentInChildren<TrashDetector>();
        Trash.OnOnTrashEntersTriggerHandled += OnTrashCollideWithSquipPickUpCollider;
    }

    private void OnDestroy()
    {
        Trash.OnOnTrashEntersTriggerHandled -= OnTrashCollideWithSquipPickUpCollider;
    }

    private void Update()
    {
        if (_canSpamThrust && !_isThrustQueued) HandleSpamThrust();
        else if (!_canSpamThrust) HandleHeldThrust();
        _torque = Input.GetAxis("Horizontal") * _torqueForceMagnitude;
        _inWater = transform.position.y < 0;
        if (!_inWater) _isThrustQueued = false;
        _rigidBody.gravityScale = _inWater ? _waterGravityScale : _airGravityScale;
        _rigidBody.drag = _inWater ? _waterLinearDrag : _airLinearDrag;
        _rigidBody.angularDrag = _inWater ? _waterAngularDrag : _airAngularDrag;
        if (Input.GetKeyDown(KeyCode.E)) DropAllTrash();
    }

    private void HandleSpamThrust()
    {
        _isThrustQueued = Input.GetButtonDown("Jump");
        _canThrust = true;
    }

    private void FixedUpdate()
    {
        if (_isThrustQueued)
        {
            Thrust();
            animator.SetBool("Thrusting", true);
        } else if (_canThrust && !_isThrustQueued)
        {
            animator.SetBool("Thrusting", false);
        }
        ApplyTorque();
    }

    private void OnTrashCollideWithSquipPickUpCollider(Trash trash, TrashDetector collider)
    {
        if (collider == _trashCollider && _canPickUp) PickUpTrash(trash);
    }

    private void HandleHeldThrust()
    {
        if (!_canThrust || _isThrustQueued) return;
        _isThrustQueued = _inWater && (Input.GetButton("Jump") || Input.GetAxisRaw("Vertical") == 1);
        if (!_isThrustQueued) return;
        _canThrust = false;
        StartCoroutine(ResetHeldThrust());
    }

    private IEnumerator ResetHeldThrust()
    {
        yield return new WaitForSeconds(_thrustTime);
        _canThrust = true;
    }

    private void ApplyTorque()
    {
        _rigidBody.AddTorque(_torque);
    }

    private void Thrust()
    {
        _rigidBody.AddRelativeForce(CalculateThrustForceVector(), ForceMode2D.Impulse);
        _isThrustQueued = false;
    }

    private Vector2 CalculateThrustForceVector()
    {
        var directionVector = new Vector2(
            Mathf.Cos(_relativeForceDirectionInDegrees * Mathf.Deg2Rad),
            Mathf.Sin(_relativeForceDirectionInDegrees * Mathf.Deg2Rad));
        return directionVector * _thrustForceMagnitude;
    }

    private void PickUpTrash(Trash trash)
    {
        trash.transform.SetParent(transform);
        trash.transform.position = _trashCollider.transform.position;
        trash.GetComponent<Rigidbody2D>().simulated = false;
        _pickedUpTrash.Add(trash);
    }

    private IEnumerator ResetPickUp()
    {
        yield return new WaitForSeconds(_dropTime);
        _canPickUp = true;
    }

    private void DropAllTrash()
    {
        foreach (var trash in _pickedUpTrash.ToList())
            DropTrash(trash);
        _canPickUp = false;
        StartCoroutine(ResetPickUp());
    }

    private void DropTrash(Trash trash)
    {
        _pickedUpTrash.Remove(trash);
        trash.transform.SetParent(null);
        trash.GetComponent<Rigidbody2D>().simulated = true;
        trash.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        var trashImpulse = _rigidBody.velocity * _trashThrowVelocityFactor + Vector2.down * _trashThrowDownFactor;
        trash.GetComponent<Rigidbody2D>().AddForce(trashImpulse, ForceMode2D.Impulse);
        OnDropTrash?.Invoke(this, trash);
    }
}
