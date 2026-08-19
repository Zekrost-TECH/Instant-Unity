using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private JoystickController joystick;
    private FloatingJoystickController floatingJoystick;

    public Vector2 MoveInput { get; private set; }
    public event Action OnDashPressed;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void Start()
    {
        joystick = FindAnyObjectByType<JoystickController>();
        floatingJoystick = FindAnyObjectByType<FloatingJoystickController>();
    }

    private void OnEnable()
    {
        if (inputActions == null) inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        inputActions.Player.Dash.performed += HandleDash;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Dash.performed -= HandleDash;
        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        // InputSystem_Actions es IDisposable: sin Dispose se filtran los mapas nativos
        // cada vez que se recarga la escena.
        inputActions?.Dispose();
        inputActions = null;
    }

    private void Update()
    {
        if (inputActions == null) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            MoveInput = Vector2.zero;
            floatingJoystick?.ResetJoystick();
            return;
        }

        Vector2 systemInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Dead zone sobre RawValue (analógico) y normalización.
        Vector2 raw = floatingJoystick != null ? floatingJoystick.RawValue : Vector2.zero;
        Vector2 touchInput = raw.sqrMagnitude > 0.01f ? raw.normalized : Vector2.zero;

        if (touchInput.sqrMagnitude <= 0.01f && joystick != null)
            touchInput = joystick.InputDirection;

        MoveInput = touchInput.sqrMagnitude > 0.01f ? touchInput : systemInput;
    }

    private void HandleDash(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;
            
        OnDashPressed?.Invoke();
    }

    public void TriggerDash()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        OnDashPressed?.Invoke();
    }
}
