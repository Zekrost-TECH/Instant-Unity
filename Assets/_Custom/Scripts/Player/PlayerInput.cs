using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private JoystickController joystick;
    
    public Vector2 MoveInput { get; private set; }
    public event Action OnDashPressed;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void Start()
    {
        joystick = FindAnyObjectByType<JoystickController>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Dash.performed += HandleDash;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.Dash.performed -= HandleDash;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            MoveInput = Vector2.zero;
            return;
        }

        Vector2 systemInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector2 joystickInput = joystick != null ? joystick.InputDirection : Vector2.zero;

        // Priorizar joystick si está activo
        MoveInput = joystickInput.sqrMagnitude > 0.01f ? joystickInput : systemInput;
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
