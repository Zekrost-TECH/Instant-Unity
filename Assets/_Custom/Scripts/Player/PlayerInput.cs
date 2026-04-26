using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    
    public Vector2 MoveInput { get; private set; }
    public event Action OnDashPressed;

    private void Awake()
    {
        // Asumimos que se generó la clase C# llamada InputSystem_Actions desde el editor
        inputActions = new InputSystem_Actions();
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

        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
    }

    private void HandleDash(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;
            
        OnDashPressed?.Invoke();
    }
}
