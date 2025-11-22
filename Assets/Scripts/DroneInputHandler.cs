using UnityEngine;
using UnityEngine.InputSystem;

public class DroneInputHandler : MonoBehaviour
{
    public RealisticDronePhysics drone;
    private DroneInput input;

    void Start()
    {
        input = new DroneInput();
        input.Enable();
    }

    void Update()
    {
        if (drone != null)
        {
            float t = input.Player.Throttle.ReadValue<float>();
            float r = input.Player.Roll.ReadValue<float>();
            float p = input.Player.Pitch.ReadValue<float>();
            float y = input.Player.Yaw.ReadValue<float>();
            drone.SetInput(t, r, p, y);
        }
    }
}
