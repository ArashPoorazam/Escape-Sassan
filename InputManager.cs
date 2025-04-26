using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Player Input Component
    public static PlayerInput PlayerInput;

    // Input Values
    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool AttackWasPressed;
    public static bool AttackIsHeld;
    public static bool AttackWasReleased;

    // Input Actions
    private InputAction _MovementActions;
    private InputAction _JumpActions;
    private InputAction _AttackActions;

    private void Awake()
    {
        // Initialize Player Input Component
        PlayerInput = GetComponent<PlayerInput>();

        // Initialize Input Actions
        _MovementActions = PlayerInput.actions["Move"];
        _JumpActions = PlayerInput.actions["Jump"];
        _AttackActions = PlayerInput.actions["Attack"];
    }

    private void Update()
    {
        // Movement is a Vector2, so we can directly assign it
        Movement = _MovementActions.ReadValue<Vector2>();

        // Jump and Attack are boolean values, so we can use the appropriate methods to check their state
        JumpWasPressed = _JumpActions.WasPressedThisFrame();
        JumpIsHeld = _JumpActions.IsPressed();
        JumpWasReleased = _JumpActions.WasReleasedThisFrame();

        // Attack is a boolean value, so we can use the appropriate methods to check its state
        AttackWasPressed = _AttackActions.WasPressedThisFrame();
        AttackIsHeld = _AttackActions.IsPressed();
        AttackWasReleased = _AttackActions.WasReleasedThisFrame();
    }

}
