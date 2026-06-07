using UnityEngine;
using Input;

namespace Player
{
    /// <summary>
    /// Handles player movement based on input from the InputReader.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private CharacterController _cc;
        private Vector3 _playerVelocity;
        private const float GRAVITY = -9.8f;
        private bool _isGrounded;
        private bool _isSubscribed;

        #region Speed Variables
        [Tooltip("Movement speed of the player"), SerializeField] private float _moveSpeed = 5f;
        [Tooltip("Sprint speed of the player"), SerializeField] private float _sprintSpeed = 8f;
        [Tooltip("Jump height of the player"), SerializeField] private float _jumpHeight = 2f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (!_isSubscribed)
            {
                InputReader.Instance.JumpEvent += HandleJump;
                _isSubscribed = true;
            }
        }

        private void Update()
        {
            CalculateMovement(InputReader.Instance.MoveValue, InputReader.Instance.IsSprinting);
            _isGrounded = _cc.isGrounded;
        }

        private void OnEnable()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.JumpEvent += HandleJump;
                _isSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (_isSubscribed && InputReader.Instance != null)
            {
                InputReader.Instance.JumpEvent -= HandleJump;
                _isSubscribed = false;
            }
        }
        #endregion

        private void CalculateMovement(Vector2 input, bool isSprinting)
        {
            // Calculate movement speed and direction
            Vector3 moveDir = Vector3.zero;
            float speed = isSprinting ? _sprintSpeed : _moveSpeed;
            moveDir.x = input.x;
            moveDir.z = input.y;

            _cc.Move(transform.TransformDirection(speed * Time.deltaTime * moveDir));

            // Apply gravity
            _playerVelocity.y += GRAVITY * Time.deltaTime;
            if (_isGrounded && _playerVelocity.y < 0)
                _playerVelocity.y = -2f;
            _cc.Move(_playerVelocity * Time.deltaTime);
        }

        private void HandleJump()
        {
            _playerVelocity.y += _jumpHeight;
        }
    }
}
