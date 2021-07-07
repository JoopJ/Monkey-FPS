using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputsHandler : MonoBehaviour
{

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool CanProcessInput() {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    public float GetLookInputHorizontal(){
        return Input.GetAxis("Mouse X") * gameConstants.mouseSensitivity;
    }
    public float GetLookInputVertical(){
        return Input.GetAxis("Mouse Y") * gameConstants.mouseSensitivity;
    }

    public bool GetSprintInputHeld() {
        return Input.GetAxis("Sprint") != 0;
    }

    public bool GetJumpInputDown() {
        if (CanProcessInput()) {
            return Input.GetKey("space");
        }

        return false;
    }

    public Vector3 GetMoveInput() {
        if (CanProcessInput()) {
            Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            // constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max movement speed
            move = Vector3.ClampMagnitude(move, 1);

            return move;
        }

        return Vector3.zero;
    }

}
