using UnityEngine;
using UnityEngine.InputSystem;

public class DroneInputHandler : MonoBehaviour
{
    public RealisticDronePhysics drone;
    private DroneInput input;
    public static bool resetReq = false;


    void Start()
    {
        input = new DroneInput();
        input.Enable();
    }

    void Update()
    {
        if (drone == null) return;

        drone.SetInput(
            input.Player.Throttle.ReadValue<float>(),
            input.Player.Roll.ReadValue<float>(),
            input.Player.Pitch.ReadValue<float>(),
            input.Player.Yaw.ReadValue<float>()
        );

        if (input.Player.Reset.WasPressedThisFrame())
        {
            resetReq = true;
        }
    }
}
