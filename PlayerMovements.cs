using UnityEngine;

public class PlayerMovements : MonoBehaviour
{
    [Header("Refrences")]
    public PlayerStates Stats;
    [SerializeField] private Collider2D _feetCollider;
    [SerializeField] private Collider2D _bodycollider;
    private Rigidbody2D _rb; // rb = rigid body
    [SerializeField] private Animator animator;

    // Health
    private float _currentHealth;

    // movement variables
    private Vector2 _moveVlocity;
    private bool _isFacingRight;

    // collision check
    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _bumpedHead;

    // Jump vars
    public float VerticalVlocity { get; private set; } 
    public static object Instance { get; internal set; }

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
        _currentHealth = Stats.MaxHealth;

        _rb = GetComponent<Rigidbody2D>();
        
        _isFacingRight = true;
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
            Move(InputManager.Movement, Stats.GroundAcceleration, Stats.GroundDeceleration);
        }
        else
        {
            Move(InputManager.Movement, Stats.AirAcceleration, Stats.AirDeceleration);
        }
    }

    # region Movement

    private void Move(Vector2 moveInput, float acceleration, float deceleration)
    {
        // if we have a movement accelerate
        if (moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);

            Vector2 targetVlocity = new Vector2(moveInput.x, 0f) * Stats.MaxWalkSpeed;

            _moveVlocity = Vector2.Lerp(_moveVlocity, targetVlocity, acceleration * Time.deltaTime);
            _rb.velocity = new Vector2(_moveVlocity.x, _rb.velocity.y);
        }
        
        // if we have no movement decelerate
        else if (moveInput == Vector2.zero)
        {
            _moveVlocity = Vector2.Lerp(_moveVlocity, Vector2.zero, deceleration * Time.deltaTime);
            _rb.velocity = new Vector2(_moveVlocity.x, _rb.velocity.y);
        }

        SetWalkingSpeed(_rb.velocity.x);
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
            _jumpBufferTimer = Stats.JumpBufferTime;
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
                    _fastFallTime = Stats.TimeForUpwardCancel;
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
        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < Stats.NumberofJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(1);
        }

        // Air jump after coyote time
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < Stats.NumberofJumpsAllowed -1)
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

            SetIsJumping(false);
            SetVrticalVlocity(VerticalVlocity);
        }
    }

    private void InitiateJump(int numberOfJumps)
    {
        if (!_isJumping)
        {
            _isJumping = true;

            SetIsJumping(true);
        }

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumps;
        VerticalVlocity = Stats.InitialJumpVelocity;
        
        SetVrticalVlocity(VerticalVlocity);
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
                _apexPoint = Mathf.InverseLerp(Stats.InitialJumpVelocity, 0f, VerticalVlocity);

                if (_apexPoint > Stats.ApexThreshhold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshhold = 0f;
                    }

                    if (_isPastApexThreshold)
                    {
                        _timePastApexThreshhold += Time.deltaTime;
                        if (_timePastApexThreshhold >= Stats.ApexHangTime)
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
                    VerticalVlocity += Stats.Gravity * Time.deltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }

            // Gravity on descending
            else if (!_isFastFalling)
            {
                VerticalVlocity += Stats.Gravity * Stats.GravityOnReleaseMultiplier * Time.deltaTime;
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
            if (_fastFallTime >= Stats.TimeForUpwardCancel)
            {
                VerticalVlocity += Stats.Gravity * Stats.GravityOnReleaseMultiplier * Time.deltaTime;
            }
            else if (_fastFallTime < Stats.TimeForUpwardCancel)
            {
                VerticalVlocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, _fastFallTime / Stats.TimeForUpwardCancel);
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

            VerticalVlocity += Stats.Gravity * Time.deltaTime;
        }

        // clamp fall speed
        VerticalVlocity = Mathf.Clamp(VerticalVlocity, -Stats.MaxFallSpeed, 50f);
        _rb.velocity = new Vector2(_rb.velocity.x, VerticalVlocity);

        SetVrticalVlocity(VerticalVlocity);
    }

    # endregion

    # region Collisions

    private void IsGroundedCheck()
    {
        Vector2 boxcastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        Vector2 boxcastSize = new Vector2(_feetCollider.bounds.size.x, Stats.GroundDetectionRayLength);

        _groundHit = Physics2D.BoxCast(boxcastOrigin, boxcastSize, 0f, Vector2.down, Stats.GroundDetectionRayLength, Stats.GroundLayer);

        if (_groundHit.collider != null)
        {
            _isGrounded = true;
            SetIsGrounded(true);
        }
        else
        {
            _isGrounded = false;
            SetIsGrounded(false);
        }
    }

    private void HeadBumpCheck()
    {
        Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _bodycollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x * Stats.HeadWidth, Stats.HeadDetectionRayLength);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, Stats.GroundDetectionRayLength, Stats.GroundLayer);

        if (_headHit.collider != null)
        {
            _bumpedHead = true;
        }
        else
        {
            _bumpedHead = false;
        }
    }

    private void CollisionCheck()
    {
        HeadBumpCheck();
        IsGroundedCheck();
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
            _coyoteTimeTimer = Stats.JumpCoyoteTime;
        }
    }

    # endregion

    # region Health

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        Debug.Log("Player Health: " + _currentHealth);

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        _currentHealth = 0f;
        _rb.velocity = Vector2.zero;
        _rb.isKinematic = true;

        Destroy(gameObject);
    }

    # endregion

    # region Animator

    // Animator set vertical velocity
    private void SetVrticalVlocity(float value)
    {
        animator.SetFloat("VerticalVlocity", value);

        // Debug.Log("VerticalVlocity: " + value);
    }
    private void SetWalkingSpeed(float value)
    {
        animator.SetFloat("WalkingSpeed", Mathf.Abs(value));

        // Debug.Log("WalkingSpeed: " + value);
    }
    private void SetIsJumping(bool value)
    {
        animator.SetBool("IsJumping", value);
    }
    private void SetIsGrounded(bool value)
    {
        animator.SetBool("IsGrounded", value);
    }

    #endregion
}