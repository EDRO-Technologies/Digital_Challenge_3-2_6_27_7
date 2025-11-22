using UnityEngine;

public class RealisticDronePhysics : MonoBehaviour
{

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootForce = 20f;
    public float shootCooldown = 0.3f;

    private float lastShotTime = -10f;

    public void Shoot()
    {
        if (Time.time - lastShotTime < shootCooldown) return;
        if (projectilePrefab == null || firePoint == null) return;

        lastShotTime = Time.time;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * shootForce, ForceMode.Impulse);
        }

        Destroy(projectile, 5f);
    }


    public Transform motorFL, motorFR, motorBR, motorBL;
    public float maxMotorThrust = 15f;
    public float yawFactor = 0.5f;

    private Rigidbody rb;
    private float throttle, roll, pitch, yaw;
    private int currentFlightMode = 0;
    private float followStickValue = 0f;

    private float targetRoll = 0f;
    private float targetPitch = 0f;
    private float tiltStrength = 0.5f; // Сила наклона в режиме Follow
    private float returnSpeed = 3f;


    public void SetInput(float t, float r, float p, float y, int flightModeIndex)
    {
        throttle = Mathf.Clamp01(t);
        roll = Mathf.Clamp(r, -1f, 1f);
        pitch = Mathf.Clamp(p, -1f, 1f);
        yaw = Mathf.Clamp(y, -1f, 1f);
        currentFlightMode = flightModeIndex;

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

        if (currentFlightMode == 1) // Режим Follow
        {
            HandleFollowMode();
        }
        else // Режимы Acro и Horizon
        {
            HandleAcroHorizonMode();
        }

        ApplyGravityAndDrag();
    }

    private void HandleFollowMode()
    {
        // Используем roll и pitch от основного стика для определения угла наклона
        targetRoll = roll * 30f; // Максимальный угол крена (можно изменить)
        targetPitch = pitch * 30f; // Максимальный угол тангажа

        // Поворот по yaw остаётся как есть
        var currentEuler = transform.rotation.eulerAngles;
        float targetYaw = currentEuler.y + yaw * 2f;

        // Плавный переход к целевому углу
        float currentRoll = Mathf.LerpAngle(currentEuler.z, targetRoll, returnSpeed * Time.fixedDeltaTime);
        float currentPitch = Mathf.LerpAngle(currentEuler.x, targetPitch, returnSpeed * Time.fixedDeltaTime);

        // Ограничиваем углы, чтобы дрон не переворачивался
        currentRoll = ClampAngle(currentRoll, -45f, 45f);
        currentPitch = ClampAngle(currentPitch, -45f, 45f);

        transform.rotation = Quaternion.Euler(currentPitch, targetYaw, currentRoll);

        // Применяем тягу вверх
        float baseThrust = throttle * maxMotorThrust;
        rb.AddForce(transform.up * baseThrust, ForceMode.Force);
    }

    private void HandleAcroHorizonMode()
    {
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
    }

    private void ApplyGravityAndDrag()
    {
        rb.AddForce(Physics.gravity * rb.mass, ForceMode.Acceleration);
        rb.AddForce(-rb.linearVelocity * 0.5f, ForceMode.Force);
        rb.AddTorque(-rb.angularVelocity * 0.3f, ForceMode.Force);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

}
