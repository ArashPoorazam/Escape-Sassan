using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Refrences")]
    public PlayerMovementStates MoveStats;
    [SerializeField] private Collider2D _feetColl;
    [SerializeField] private Collider2D _bodyColl;
    private Rigidbody2D _rb; // rb = rigid body

    // movement variables
    private Vector2 _moveVlocity;
    private bool _isFacingRight;

    // collision check
    private RaycastHit2D _goundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _bumpedHead;

    // Jump vars
    public float VerticalVlocity { get; private set; } // the vertical velocity of the player
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;

    // Apex Vars
    private float _apexPoint;
    private float _timePastApexThreshhold;
    private bool _isPastApexThreshold;

    // Jump Buffer Vars
    private float _jumpBufferTimer;
    private bool _jumpRelaesedDuringBuffer;

    // Cayote Time Vars
    private float _coyoteTimeTimer;

    void Awake()
    {
        _isFacingRight = true;

        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();
    }

    void FixedUpdate()
    {
        CollisionCheck();
        Jump();

        if (_isGrounded)
        {
            Move(InputManager.Movement, MoveStats.GroundAcceleration, MoveStats.GroundDeceleration);
        }
        else
        {
            Move(InputManager.Movement, MoveStats.AirAcceleration, MoveStats.AirDeceleration);
        }
    }

    # region Movement

    private void Move(Vector2 moveInput, float acceleration, float deceleration)
    {
        // if we have a movement accelerate
        if (moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);

            Vector2 targetVlocity = Vector2.zero;
            targetVlocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxWalkSpeed;

            _moveVlocity = Vector2.Lerp(_moveVlocity, targetVlocity, acceleration * Time.deltaTime);
            _rb.velocity = new Vector2(_moveVlocity.x, _rb.velocity.y);
        }
        
        // if we have no movement decelerate
        if (moveInput == Vector2.zero)
        {
            _moveVlocity = Vector2.Lerp(_moveVlocity, Vector2.zero, deceleration * Time.deltaTime);
            _rb.velocity = new Vector2(_moveVlocity.x, _rb.velocity.y);
        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        // if we are moving right and not facing right, flip the player
        if (_isFacingRight && moveInput.x < 0f)
        {
            Turn(true);
        }
        // if we are moving left and facing right, flip the player
        else if (!_isFacingRight && moveInput.x > 0f)
        {
            Turn(false);
        }
        
    }
    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            _isFacingRight = false;
            transform.Rotate(0f, 180f, 0f);
        }
        else
        {
            _isFacingRight = true;
            transform.Rotate(0f, -180f, 0f);
        }
    }

    # endregion

    # region Jump

    private void JumpChecks()
    {
        // When we press the jump button
        if (InputManager.JumpWasPressed)
        {
            _jumpBufferTimer = MoveStats.JumpBufferTime;
            _jumpRelaesedDuringBuffer = false;
        }

        // When we release the jump button
        if (InputManager.JumpWasReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpRelaesedDuringBuffer = true;
            }
            
            if (_isJumping && VerticalVlocity > 0f)
            {
                if (_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = MoveStats.TimeForUpwardCancel;
                    VerticalVlocity = 0f;
                }
                else
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVlocity;
                }
            }
        }

        // Initate jump with jump buffer timer and cayote time
        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimeTimer > 0f))
        {
            InitiateJump(1);

            if (_jumpRelaesedDuringBuffer)
            {
                _isFalling = true;
                _fastFallReleaseSpeed = VerticalVlocity;
            }
        }

        // Double jump
        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < MoveStats.NumberofJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(1);
        }

        // Air jump after coyote time
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < MoveStats.NumberofJumpsAllowed -1)
        {
            _isFastFalling = false;
            InitiateJump(1);
        }

        // Landed
        if ((_isFalling || _isJumping) && _isGrounded && VerticalVlocity <= 0f)
        {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexThreshold = false;
            _numberOfJumpsUsed = 0;
            
            VerticalVlocity = Physics2D.gravity.y;
        }
    }

    private void InitiateJump(int numberOfJumps)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumps;
        VerticalVlocity = MoveStats.InitialJumpVelocity;
    }

    private void Jump()
    {
        // Apply gravity While jumping
        if (_isJumping)
        {
            // Check for head bumps
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            // Gravity on asending
            if (VerticalVlocity >= 0f)
            {
                // Apex controls
                _apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, VerticalVlocity);

                if (_apexPoint > MoveStats.ApexThreshhold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshhold = 0f;
                    }

                    if (_isPastApexThreshold)
                    {
                        _timePastApexThreshhold += Time.deltaTime;
                        if (_timePastApexThreshhold >= MoveStats.ApexHangTime)
                        {
                            VerticalVlocity = 0f;
                        }
                        else
                        {
                            VerticalVlocity = -0.01f;
                        }
                    }
                }

                // Gravity on assending but not past apex threshold
                else
                {
                    VerticalVlocity += MoveStats.Gravity * Time.deltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }

            // Gravity on descending
            else if (!_isFastFalling)
            {
                VerticalVlocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.deltaTime;
            }

            else if (VerticalVlocity < 0f)
            {
                if (!_isFalling)
                {
                    _isFalling = true;
                }
            }
        }

        // Jump cut
        if (_isFastFalling)
        {
            if (_fastFallTime >= MoveStats.TimeForUpwardCancel)
            {
                VerticalVlocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.deltaTime;
            }
            else if (_fastFallTime < MoveStats.TimeForUpwardCancel)
            {
                VerticalVlocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, _fastFallTime / MoveStats.TimeForUpwardCancel);
            }

            _fastFallTime += Time.deltaTime;
        }

        // Normal gravity while falling
        if (!_isGrounded && !_isJumping)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }

            VerticalVlocity += MoveStats.Gravity * Time.deltaTime;
        }

        // clamp fall speed
        VerticalVlocity = Mathf.Clamp(VerticalVlocity, -MoveStats.MaxFallSpeed, 50f);

        _rb.velocity = new Vector2(_rb.velocity.x, VerticalVlocity);
    }

    # endregion

    # region Collisions

    private void IsGrounded()
    {
        Vector2 boxcastOrigin = new Vector2(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
        Vector2 boxcastSize = new Vector2(_feetColl.bounds.size.x, MoveStats.GroundDetectionRayLength);

        _goundHit = Physics2D.BoxCast(boxcastOrigin, boxcastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);

        if (_goundHit.collider != null)
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }

        # region Debug visualization

        if (MoveStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxcastOrigin.x - boxcastSize.x / 2, boxcastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxcastOrigin.x + boxcastSize.x / 2, boxcastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxcastOrigin.x - boxcastSize.x / 2, boxcastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxcastSize.x, rayColor);
        }

        # endregion
    }

    private void HeadBumpCheck()
    {
        Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _bodyColl.bounds.max.y);
        Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x * MoveStats.HeadWidth, MoveStats.HeadDetectionRayLength);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);

        if (_headHit.collider != null)
        {
            _bumpedHead = true;
        }
        else
        {
            _bumpedHead = false;
        }

        # region Debug visualization

        if (MoveStats.DebugShowHeadBumpBox)
        {
            float headWidth = MoveStats.HeadWidth;

            Color rayColor;
            if (_bumpedHead)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y + MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x * headWidth, rayColor);
        }

        # endregion
    }

    private void CollisionCheck()
    {
        HeadBumpCheck();
        IsGrounded();
    }

    # endregion

    # region Timers

    private void CountTimers()
    {
        _jumpBufferTimer -= Time.deltaTime;

        if (!_isGrounded)
        {
            _coyoteTimeTimer -= Time.deltaTime;
        }
        else
        {
            _coyoteTimeTimer = MoveStats.JumpCoyoteTime;
        }
    }

    # endregion
}