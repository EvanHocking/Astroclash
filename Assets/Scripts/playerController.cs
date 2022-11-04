using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class playerController : NetworkBehaviour
{
    
    public float accelDelta;
    public float maxVelocity;
    public float rotationSpeed;
    public float angle;
    public Vector2 acceleration;
    public Vector2 velocity;
    public bool moving;

    private Camera playerCamera;

    void Start()
    {
        GameObject parent = gameObject.transform.parent.gameObject;
        playerCamera = parent.GetComponentInChildren<Camera>();
    }

    void Awake()
    {
        accelDelta = 10;
        maxVelocity = 16;
        rotationSpeed = 120;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner) 
        {
            //update the camera origin
            Vector3 newPosition = playerCamera.transform.position;
            newPosition.x = gameObject.transform.position.x;
            newPosition.y = gameObject.transform.position.y;
            playerCamera.transform.position= newPosition;

            Debug.Log("Current Rotation: " + transform.eulerAngles.z);
            Debug.Log("Current X: " + velocity * (float)Math.Cos(transform.eulerAngles.z));
            Debug.Log("Current Y: " + velocity * (float)Math.Sin(transform.eulerAngles.z));

            //Movement is broken down into: X = Movement_Speed * cos(rotation_angle) Y = Movement_Speed * sin(rotation_angle)
            ProcessInputs();
            velocity += acceleration;
            acceleration *= 0;
            velocity = Vector2.ClampMagnitude(velocity, maxVelocity);
        }
    }

    void FixedUpdate()
    {
        if (IsOwner)
        {
            transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.position = transform.position + new Vector3(velocity.x * Time.deltaTime, velocity.y * Time.deltaTime, 0);
        }
    }

    void ProcessInputs()
    {
        float rotate = Input.GetAxisRaw("Horizontal");
        Debug.Log("Rotate: " + rotate);
        angle = angle + (rotate * rotationSpeed * -1 * Time.deltaTime);
        if (angle < 0)
        {
            angle += 360;
        }
        else if (angle >= 360)
        {
            angle -= 360;
        }
        moving = Input.GetButton("Forward");
        if (moving)
        {
            Vector2 direction = new Vector2(Mathf.Cos((angle) * Mathf.Deg2Rad), Mathf.Sin((angle) * Mathf.Deg2Rad));
            ApplyForce(Vector2.ClampMagnitude(direction, accelDelta * Time.deltaTime));
        }
        Debug.Log("Moving: " + moving);
    }

    void ApplyForce(Vector2 force)
    {
        acceleration += force;
    }
}
