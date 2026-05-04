using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Room Bounds")]
    public bool constrainToRoom = true;
    public float roomBoundaryPadding = 0.45f;

    Rigidbody body;
    FloorController floorController;
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();



    void Awake()
    {
        // Get the rigidbody on this.
        body = GetComponent<Rigidbody>();
        floorController = FindFirstObjectByType<FloorController>();
    }

    void FixedUpdate()
    {
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity =new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        body.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, body.linearVelocity.y, targetVelocity.y);
        ApplyRoomBounds();
    }

    void ApplyRoomBounds()
    {
        if (!constrainToRoom)
        {
            return;
        }

        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        if (floorController == null)
        {
            return;
        }

        Vector3 currentPosition = body.position;
        Vector3 clampedPosition = floorController.ClampPointToRoom(currentPosition, roomBoundaryPadding);

        if ((clampedPosition - currentPosition).sqrMagnitude < 0.0001f)
        {
            return;
        }

        body.position = clampedPosition;
        body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }
}
