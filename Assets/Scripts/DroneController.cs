using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class DroneController : MonoBehaviour
{
    public float maxThrust = 30f;
    public float torquePower = 15f;

    private Rigidbody rb;
    private float throttle, yaw, pitch, roll;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
    }

    // ��� �������� �� ����� ��� ���
    public void OnThrottle(CallbackContext ctx) => throttle = ctx.ReadValue<float>();
    public void OnYaw(CallbackContext ctx) => yaw = ctx.ReadValue<float>();
    public void OnPitch(CallbackContext ctx) => pitch = ctx.ReadValue<float>();
    public void OnRoll(CallbackContext ctx) => roll = ctx.ReadValue<float>();

    void FixedUpdate()
    {
        rb.AddForce(transform.up * throttle * maxThrust, ForceMode.Acceleration);

        Vector3 torque = new Vector3(
            roll * torquePower,     // X: Roll
            yaw * torquePower,      // Y: Yaw
            -pitch * torquePower    // Z: Pitch (�������� ��� "�����")
        );
        rb.AddTorque(torque, ForceMode.Acceleration);
    }
}