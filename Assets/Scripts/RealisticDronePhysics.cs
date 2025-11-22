using UnityEngine;

public class RealisticDronePhysics : MonoBehaviour
{
    public Transform motorFL, motorFR, motorBR, motorBL;
    public float maxMotorThrust = 15f;
    public float yawFactor = 0.5f;

    private Rigidbody rb;
    private float throttle, roll, pitch, yaw;

    public void SetInput(float t, float r, float p, float y)
    {
        throttle = Mathf.Clamp01(t);
        roll = Mathf.Clamp(r, -1f, 1f);
        pitch = Mathf.Clamp(p, -1f, 1f);
        yaw = Mathf.Clamp(y, -1f, 1f);
    }

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null || motorFL == null || motorFR == null || motorBR == null || motorBL == null)
        {
            Debug.LogError("Drone: Missing Rigidbody or motors!");
        }

        startPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (DroneInputHandler.resetReq)
        {
            DroneInputHandler.resetReq = false;

            transform.rotation = Quaternion.identity;
            rb.angularVelocity = Vector3.zero;
            transform.position = startPosition;

            return;
            
        }

        float baseThrust = throttle * maxMotorThrust;
        float rollDiff = roll * maxMotorThrust * 0.5f;
        float pitchDiff = pitch * maxMotorThrust * 0.5f;
        float yawDiff = yaw * maxMotorThrust * yawFactor;

        float T_FL = Mathf.Clamp(baseThrust - pitchDiff - rollDiff + yawDiff, 0, maxMotorThrust);
        float T_FR = Mathf.Clamp(baseThrust - pitchDiff + rollDiff - yawDiff, 0, maxMotorThrust);
        float T_BR = Mathf.Clamp(baseThrust + pitchDiff + rollDiff + yawDiff, 0, maxMotorThrust);
        float T_BL = Mathf.Clamp(baseThrust + pitchDiff - rollDiff - yawDiff, 0, maxMotorThrust);

        rb.AddForceAtPosition(transform.up * T_FL, motorFL.position, ForceMode.Force);
        rb.AddForceAtPosition(transform.up * T_FR, motorFR.position, ForceMode.Force);
        rb.AddForceAtPosition(transform.up * T_BR, motorBR.position, ForceMode.Force);
        rb.AddForceAtPosition(transform.up * T_BL, motorBL.position, ForceMode.Force);

        float yawTorque = yaw * 5f;
        rb.AddTorque(Vector3.up * yawTorque, ForceMode.Force);


        rb.AddForce(Physics.gravity * rb.mass, ForceMode.Acceleration);
        rb.AddForce(-rb.linearVelocity * 0.5f, ForceMode.Force);
        rb.AddTorque(-rb.angularVelocity * 0.3f, ForceMode.Force);
    }

}
