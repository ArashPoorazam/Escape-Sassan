using UnityEngine;
using UnityEngine.SocialPlatforms;

[CreateAssetMenu(menuName = "Player Stats")]
public class PlayerStates : ScriptableObject
{
    [Header("Health")]
    [Range(1f, 100f)] public float MaxHealth = 100;
    
    [Header("Move")]
    [Range(1f, 100f)] public float MaxWalkSpeed = 7f;
    [Range(0.25f, 50f)] public float GroundAcceleration = 5f;
    [Range(0.25f, 50f)] public float GroundDeceleration = 20f;
    [Range(0.25f, 50f)] public float AirAcceleration = 5f;
    [Range(0.25f, 50f)] public float AirDeceleration = 5f;

    [Header("Grounded/Collisions check")]
    public LayerMask GroundLayer;
    public float GroundDetectionRayLength = 0.02f;
    public float HeadDetectionRayLength = 0.02f;
    [Range(0f, 1f)] public float HeadWidth = 0.75f;

    [Header("Jump")]
    public float JumpHeight = 3.2f;
    [Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f; // to adjust jump height based on gravity
    public float TimeTillReachJumpApex = 0.35f;
    [Range(1f, 5f)] public float GravityOnReleaseMultiplier = 1.5f;
    public float MaxFallSpeed = 16f;
    [Range(1, 5)] public int NumberofJumpsAllowed = 1;

    [Header("Jump Cuts")]
    [Range(0.02f, 0.3f)] public float TimeForUpwardCancel = 0.027f;

    [Header("Jump Apax")]
    [Range(0.5f, 1f)] public float ApexThreshhold = 0.97f;
    [Range(0.01f, 1f)] public float ApexHangTime = 0.075f;

    [Header("Jump Buffer")]
    [Range(0f, 1f)] public float JumpBufferTime = 0.125f;

    [Header("Jump Cayot Time")]
    [Range(0f, 1f)] public float JumpCoyoteTime = 0.05f;

    // Gravity calculations
    public float Gravity {get; private set;}
    public float InitialJumpVelocity {get; private set;}
    public float AdjustedJumpHieght {get; private set;}

    public void OnValidate()
    {
        CalculateValues();
    }
    public void OnEnable()
    {
        CalculateValues();
    }
    private void CalculateValues()
    {
        AdjustedJumpHieght = JumpHeight * JumpHeightCompensationFactor;
        Gravity = -(2 * AdjustedJumpHieght) / Mathf.Pow(TimeTillReachJumpApex, 2);
        InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillReachJumpApex;
    }
}
