using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerLook : MonoBehaviour
    {
        #region Public
        [Header("Camera Settings")]
        public float xSens = 30f;
        public float ySens = 30f;
        public Camera Cam;
        #endregion

        #region Unity Methods
        private void LateUpdate()
        {
            // Get look input from the InputReader singleton, and calculate rotation
            Vector2 lookInput = Input.InputReader.Instance.LookValue;
            float mouseX = lookInput.x * xSens * Time.deltaTime;
            float mouseY = lookInput.y * ySens * Time.deltaTime;

            // Rotate the player horizontally
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera vertically
            float currentXRotation = Cam.transform.localEulerAngles.x;
            currentXRotation = (currentXRotation > 180) ? currentXRotation - 360 : currentXRotation; // Convert to -180 to 180 range
            float desiredXRotation = Mathf.Clamp(currentXRotation - mouseY, -90f, 90f);
            Cam.transform.localEulerAngles = new Vector3(desiredXRotation, 0, Cam.transform.localEulerAngles.z);
        }
        #endregion
    }
}
