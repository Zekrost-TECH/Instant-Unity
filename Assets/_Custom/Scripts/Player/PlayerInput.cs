using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Tools;

public class PlayerInput : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private JoystickController joystick;
    private MMTouchJoystick feelJoystick;

    public Vector2 MoveInput { get; private set; }
    public event Action OnDashPressed;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void Start()
    {
        joystick = FindAnyObjectByType<JoystickController>();
        feelJoystick = FindAnyObjectByType<MMTouchJoystick>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Dash.performed += HandleDash;
    }

    private void OnDisable()
    {
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
            return;
        }

        Vector2 systemInput = inputActions.Player.Move.ReadValue<Vector2>();

        // El joystick táctil de Feel manda si se está usando. NormalizedValue basta:
        // PlayerMovement normaliza MoveInput, así que la magnitud analógica da igual.
        Vector2 touchInput = feelJoystick != null ? feelJoystick.NormalizedValue : Vector2.zero;

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
