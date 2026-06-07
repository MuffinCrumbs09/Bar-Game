using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Input
{
    /// <summary>
    /// Singleton class that reads player input and provides it to other scripts.
    /// </summary>
    public class InputReader : MonoBehaviour, Controls.IOnFootActions
    {
        public static InputReader Instance { get; private set; }
        private Controls _controls;

        #region Public
        public Vector2 MoveValue { get; private set; }
        public Vector2 LookValue { get; private set; }
        public bool IsSprinting { get; private set; }
        #endregion

        #region Actions
        public event Action JumpEvent;
        #endregion

        [Tooltip("Does the cursor start visible?"), SerializeField] private bool _initCursorVisibility = true;

        #region Unity Methods
        private void Awake()
        {
            // Set up the singleton instance
            if (Instance != null && Instance != this)
                Destroy(this);

            Instance = this;
        }

        private void Start()
        {
            // Initialize the controls and set the callbacks
            _controls = new Controls();
            _controls.OnFoot.SetCallbacks(this);
            _controls.OnFoot.Enable();

            // Set cursor visibility
            ToggleCursor(_initCursorVisibility);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Toggles the visibility and lock state of the cursor.
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleCursor(bool toggle)
        {
            Cursor.visible = toggle;

            if (toggle)
                Cursor.lockState = CursorLockMode.Confined;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }
        #endregion

        #region OnFoot Callbacks
        public void OnMovement(InputAction.CallbackContext context)
        {
            MoveValue = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookValue = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                JumpEvent?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            IsSprinting = context.performed;
        }
        #endregion
    }
}
