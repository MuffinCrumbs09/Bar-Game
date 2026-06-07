using UnityEngine;

namespace Player
{
    public class CameraHeadBob : MonoBehaviour
    {
        #region Headbob Settings
        [Header("System Toggle")]
        [SerializeField] private bool _enableHeadBob = true;

        [Header("Movement Bob (Walking)")]
        [SerializeField] private float _walkBobFrequency = 10f;
        [SerializeField] private float _walkBobHorizontalAmplitude = 0.03f;
        [SerializeField] private float _walkBobVerticalAmplitude = 0.05f;

        [Header("Movement Bob (Sprinting)")]
        [SerializeField] private float _sprintBobFrequency = 13f;
        [SerializeField] private float _sprintBobHorizontalAmplitude = 0.05f;
        [SerializeField] private float _sprintBobVerticalAmplitude = 0.08f;

        [Header("Idle Breathing")]
        [SerializeField] private float _idleBobFrequency = 1.5f;
        [SerializeField] private float _idleBobHorizontalAmplitude = 0.005f;
        [SerializeField] private float _idleBobVerticalAmplitude = 0.01f;

        [Header("Strafe Roll (Tilt)")]
        [Tooltip("Maximum roll angle (in degrees) when moving sideways.")]
        [SerializeField] private float _maxStrafeRoll = 1.5f;
        [Tooltip("Speed at which the camera tilts.")]
        [SerializeField] private float _strafeRollSpeed = 5f;

        [Header("Smoothing & Limits")]
        [Tooltip("How quickly the camera returns to its center/default position.")]
        [SerializeField] private float _returnSpeed = 8f;

        [Header("Debugging")]
        [Tooltip("Enable this to print status logs to the Unity Console.")]
        [SerializeField] private bool _showDebugLogs = false;
        #endregion

        private CharacterController _cc;
        private Transform _parentTransform;
        private Vector3 _defaultLocalPosition;
        private Vector3 _lastParentPosition;

        private float _movementCycle;
        private float _idleCycle;

        private float _currentRoll;
        private float _targetRoll;

        private void Start()
        {
            // Walk up the hierarchy to find the CharacterController that owns this camera.
            _cc = GetComponentInParent<CharacterController>();

            if (_cc == null)
            {
                Debug.LogWarning("[CameraHeadBob] CharacterController not found in parent hierarchy! " +
                                 "Please make sure this script is attached to a Camera that is a child of the Player GameObject.", this);

                // No CharacterController found, fallback to imediate parent
                _parentTransform = transform.parent;
            }
            else
            {
                _parentTransform = _cc.transform;
            }

            // Record where the camera sits in local space so every offset is relative to this origin.
            _defaultLocalPosition = transform.localPosition;

            if (_parentTransform != null)
            {
                _lastParentPosition = _parentTransform.position;
            }
        }

        private void Update()
        {
            if (_parentTransform == null) return;

            // Derive horizontal speed from frame-to-frame world displacement
            Vector3 currentParentPos = _parentTransform.position;
            Vector3 displacement = currentParentPos - _lastParentPosition;
            displacement.y = 0f; // Strip vertical movement so slopes don't inflate the speed reading.

            float speed = Time.deltaTime > 0 ? (displacement.magnitude / Time.deltaTime) : 0f;
            _lastParentPosition = currentParentPos;

            // If no CharacterController is present, assume the player is always grounded.
            bool isGrounded = _cc != null ? _cc.isGrounded : true;
            bool isMoving = speed > 0.1f;

            // Start each frame intending to return the camera to its default position and zero roll.
            Vector3 targetLocalPos = _defaultLocalPosition;
            _targetRoll = 0f;

            if (_showDebugLogs)
            {
                Debug.Log($"[CameraHeadBob] Speed: {speed:F2} | isMoving: {isMoving} | isGrounded: {isGrounded}");
            }

            if (!_enableHeadBob)
            {
                // Glide back to the resting position and rotation rather than snapping,
                // so toggling the system off mid-game doesn't cause a jarring jump.
                transform.localPosition = Vector3.Lerp(transform.localPosition, _defaultLocalPosition, _returnSpeed * Time.deltaTime);

                Vector3 currentRotation = transform.localEulerAngles;
                float resetRoll = Mathf.LerpAngle(currentRotation.z, 0, _returnSpeed * Time.deltaTime);
                transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, resetRoll);
                return;
            }

            if (isMoving && isGrounded)
            {
                // Blend bob parameters between walk and sprint based on how fast the player is moving.
                // speedFactor reaches 1 at the assumed sprint speed of 8 units/s.
                float speedFactor = Mathf.InverseLerp(0.1f, 8.0f, speed);

                float activeFrequency = Mathf.Lerp(_walkBobFrequency, _sprintBobFrequency, speedFactor);
                float activeHorizontalAmp = Mathf.Lerp(_walkBobHorizontalAmplitude, _sprintBobHorizontalAmplitude, speedFactor);
                float activeVerticalAmp = Mathf.Lerp(_walkBobVerticalAmplitude, _sprintBobVerticalAmplitude, speedFactor);

                // Scale cycle advancement by actual speed so the bob cadence matches
                // footsteps regardless of frame rate or variable movement speed.
                _movementCycle += activeFrequency * Time.deltaTime * (speed / 5f);

                // A Lissajous figure-8 curve: horizontal runs at half the frequency of vertical,
                // producing the natural oval head path seen when a person walks.
                float bobX = Mathf.Sin(_movementCycle * 0.5f) * activeHorizontalAmp;
                float bobY = Mathf.Sin(_movementCycle) * activeVerticalAmp;

                targetLocalPos = _defaultLocalPosition + new Vector3(bobX, bobY, 0f);

                // Convert world displacement into the player's local axes so we can isolate
                // purely sideways (X-axis) movement for the strafe roll calculation.
                Vector3 localDisplacement = _parentTransform.InverseTransformDirection(displacement);

                if (Time.deltaTime > 0)
                {
                    // Positive localSpeedX means moving right → roll left (negative Z), and vice versa,
                    // mimicking the way a body leans into a sidestep.
                    float localSpeedX = localDisplacement.x / Time.deltaTime;
                    _targetRoll = -localSpeedX * _maxStrafeRoll / 5f;
                    _targetRoll = Mathf.Clamp(_targetRoll, -_maxStrafeRoll, _maxStrafeRoll);
                }
            }
            else
            {
                // While stationary, simulate subtle breathing with a slow, low-amplitude sine wave.
                _idleCycle += _idleBobFrequency * Time.deltaTime;

                float breathingX = Mathf.Sin(_idleCycle * 0.5f) * _idleBobHorizontalAmplitude;
                float breathingY = Mathf.Sin(_idleCycle) * _idleBobVerticalAmplitude;

                targetLocalPos = _defaultLocalPosition + new Vector3(breathingX, breathingY, 0f);
            }

            // Smooth the camera toward the target position each frame to avoid hard transitions.
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, _returnSpeed * Time.deltaTime);

            // Ease the roll angle toward its target separately so tilt speed is independently tunable.
            _currentRoll = Mathf.Lerp(_currentRoll, _targetRoll, _strafeRollSpeed * Time.deltaTime);

            // Write the roll back onto the Z axis while preserving the existing X and Y rotations
            // set by whatever controls camera look direction.
            Vector3 currentEuler = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, _currentRoll);
        }
    }
}