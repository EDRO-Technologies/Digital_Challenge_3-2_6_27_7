using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class DroneInputHandler : NetworkBehaviour
{
    public RealisticDronePhysics drone;
    private DroneInput input;
    public static bool resetReq = false;

    void Start()
    {
        //if (!IsOwner) return;
        input = new DroneInput();
        input.Enable();
    }

    public enum FlightMode
    {
    Acro = -1,
    Horizon = 0,
    Angle = 1
    }


    void Update()
    {
        //if (!IsOwner) return;
        if (Application.platform == RuntimePlatform.Android &&
               Application.platform == RuntimePlatform.IPhonePlayer) return;
        if (drone == null) return;

        float switchValue = input.Player.FlightMode.ReadValue<float>();

        int modeIndex;
        if (switchValue < -0.5f) modeIndex = -1;     // ����
        else if (switchValue > 0.5f) modeIndex = 1;  // �����
        else modeIndex = 0;

        drone.SetInput(
            input.Player.Throttle.ReadValue<float>(),
            input.Player.Roll.ReadValue<float>(),
            input.Player.Pitch.ReadValue<float>(),
            input.Player.Yaw.ReadValue<float>(),
            modeIndex
        );

        if (input.Player.Reset.WasPressedThisFrame())
        {
            resetReq = true;
        }

        if (input.Player.Shoot.IsPressed())
        {
            drone.Shoot();
        }
    }
}
