using UnityEngine;
using Unity.Netcode;

public class RealisticDronePhysics : NetworkBehaviour
{
    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootForce = 20f;
    public float shootCooldown = 0.3f;
    private float lastShotTime = -10f;

    [Header("Motors")]
    public Transform motorFL, motorFR, motorBR, motorBL;
    public float maxMotorThrust = 15f;
    public float yawFactor = 0.5f;

    [Header("Flight")]
    public float tiltStrength = 0.5f;
    public float returnSpeed = 3f;

    private Rigidbody rb;
    private float throttle, roll, pitch, yaw;
    private int currentFlightMode = 0;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null || motorFL == null || motorFR == null || motorBR == null || motorBL == null)
        {
            Debug.LogError("Drone: Missing Rigidbody or motors!");
            return;
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    // Вызывается владельцем (локальным игроком)
    public void SetInput(float t, float r, float p, float y, int flightModeIndex)
    {
        if (!IsOwner) return;

        // Отправляем ввод на сервер
        SetInputServerRpc(t, r, p, y, flightModeIndex);
    }

    [ServerRpc(RequireOwnership = true)]
    void SetInputServerRpc(float t, float r, float p, float y, int flightModeIndex)
    {
        throttle = Mathf.Clamp01(t);
        roll = Mathf.Clamp(r, -1f, 1f);
        pitch = Mathf.Clamp(p, -1f, 1f);
        yaw = Mathf.Clamp(y, -1f, 1f);
        currentFlightMode = flightModeIndex;
    }

    public void Shoot()
    {
        if (!IsOwner) return;

        // Проверка кулдауна на клиенте — для плавности, но дублируется и на сервере
        if (Time.time - lastShotTime < shootCooldown) return;

        ShootServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    void ShootServerRpc()
    {
        if (Time.time - lastShotTime >= shootCooldown && projectilePrefab != null && firePoint != null)
        {
            lastShotTime = Time.time;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // Убедимся, что у снаряда есть NetworkObject
            NetworkObject netProj = projectile.GetComponent<NetworkObject>();
            if (netProj == null)
            {
                Debug.LogError("Projectile prefab must have a NetworkObject component!");
                Destroy(projectile);
                return;
            }

            netProj.Spawn(); // Спавним сетевой объект

            Rigidbody rbProj = projectile.GetComponent<Rigidbody>();
            if (rbProj != null)
            {
                rbProj.AddForce(firePoint.forward * shootForce, ForceMode.Impulse);
            }

            // Уничтожим снаряд через 5 секунд (на всех клиентах)
            DestroyProjectileServerRpc(netProj);
        }
    }

    [ServerRpc]
    void DestroyProjectileServerRpc(NetworkObjectReference projectileRef)
    {
        // Используем отложенное уничтожение
        if (projectileRef.TryGet(out NetworkObject netObj))
        {
            Destroy(netObj.gameObject, 5f);
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return; // ВСЯ физика — только на сервере

        if (DroneInputHandler.resetReq)
        {
            DroneInputHandler.resetReq = false;
            ResetPositionServerRpc();
            return;
        }

        if (currentFlightMode == 1) // Follow Mode
        {
            HandleFollowMode();
        }
        else // Acro / Horizon
        {
            HandleAcroHorizonMode();
        }

        ApplyGravityAndDrag();
    }

    [ServerRpc]
    void ResetPositionServerRpc()
    {
        if (!IsServer) return;

        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    private void HandleFollowMode()
    {
        // Целевые углы (в градусах)
        float targetRoll = roll * 30f;
        float targetPitch = pitch * 30f;

        // Текущие углы
        Vector3 currentEuler = transform.rotation.eulerAngles;
        currentEuler.x = NormalizeAngle(currentEuler.x);
        currentEuler.z = NormalizeAngle(currentEuler.z);

        // Плавный Lerp к целевым углам
        float newRoll = Mathf.LerpAngle(currentEuler.z, targetRoll, returnSpeed * Time.fixedDeltaTime);
        float newPitch = Mathf.LerpAngle(currentEuler.x, targetPitch, returnSpeed * Time.fixedDeltaTime);

        // Ограничение углов
        newRoll = Mathf.Clamp(newRoll, -45f, 45f);
        newPitch = Mathf.Clamp(newPitch, -45f, 45f);

        // Yaw — накопительный (управление курсом)
        float targetYaw = currentEuler.y + yaw * 30f * Time.fixedDeltaTime; // 30 град/с максимум

        // Формируем новую ориентацию
        Quaternion targetRotation = Quaternion.Euler(newPitch, targetYaw, newRoll);

        // Устанавливаем ориентацию через физику (через torque), а не напрямую!
        // Но для упрощения и стабильности в Follow-режиме можно использовать rotation override,
        // однако NetworkRigidbody ожидает, что вы управляете через силы.

        // Подход: использовать targetRotation для создания желаемой угловой скорости
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
        float angle;
        Vector3 axis;
        deltaRotation.ToAngleAxis(out angle, out axis);

        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;

        if (angle != 0 && !float.IsNaN(axis.x))
        {
            Vector3 desiredAngularVelocity = axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime);
            Vector3 torque = Vector3.Scale(rb.inertiaTensor, desiredAngularVelocity - rb.angularVelocity);
            rb.AddTorque(torque, ForceMode.Impulse);
        }

        // Тяга вверх
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
    }

    private void ApplyGravityAndDrag()
    {
        // Гравитация уже применяется Unity автоматически, если rb.useGravity = true
        // Но если вы хотите кастомную — оставьте. Иначе можно убрать.
        // rb.AddForce(Physics.gravity * rb.mass, ForceMode.Acceleration); // избыточно при useGravity=true

        // Аэродинамическое сопротивление
        rb.AddForce(-rb.linearVelocity * 0.5f, ForceMode.Force);
        rb.AddTorque(-rb.angularVelocity * 0.3f, ForceMode.Force);
    }

    // Вспомогательная функция для нормализации углов в [-180, 180]
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}