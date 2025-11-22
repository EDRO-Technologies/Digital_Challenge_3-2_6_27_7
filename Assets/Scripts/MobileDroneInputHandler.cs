using UnityEngine;
using UnityEngine.UI;

public class MobileDroneInputHandler : MonoBehaviour
{
    public RealisticDronePhysics drone;
    public static bool resetReq = false;

    [Header("UI Controls")]
    public FloatingJoystick leftStick;      // Убедитесь, что тип правильный
    public Slider throttleSlider;   // ✅ Добавлено
    public Button resetButton;
    public Button shootButton;

    [Header("Gyro Settings")]
    public bool useGyroForYaw = true;
    private bool gyroEnabled = false;

    private float yawInput = 0f;

    void Start()
    {
        InitializeGyro();
        if (resetButton) resetButton.onClick.AddListener(() => resetReq = true);
        if (shootButton) shootButton.onClick.AddListener(() => OnShootButtonPressed());
    }

    void Update()
    {
        Debug.Log($"{SystemInfo.deviceType}");
        Debug.Log($"{Application.platform}");
        if (Application.platform != RuntimePlatform.Android &&
               Application.platform != RuntimePlatform.IPhonePlayer) return;
        if (drone == null) return;

        float roll = leftStick.Horizontal;   // -1..1
        float pitch = leftStick.Vertical;    // -1..1
        float throttle = throttleSlider ? throttleSlider.value : 0f; // ✅ Используется правильно

        if (useGyroForYaw && gyroEnabled)
        {
            yawInput = Input.gyro.rotationRateUnbiased.y * 2f;
        }
        else
        {
            yawInput = 0f;
        }

        int flightMode = 0; // Horizon
        drone.SetInput(throttle, roll, pitch, yawInput, flightMode);
    }

    private void InitializeGyro()
    {
        Input.gyro.enabled = true;
        gyroEnabled = true;
    }

    public void OnShootButtonPressed()
    {
        if (drone != null)
        {
            drone.Shoot();
        }
    }
}