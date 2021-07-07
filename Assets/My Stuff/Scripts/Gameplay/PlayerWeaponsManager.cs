using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponsManager : MonoBehaviour
{
    public enum WeaponSwitchState {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew
    }

    PlayerInputsHandler inputsHandler;
    PlayerCharacterController playerCharacterController;


    float defaultFov = 90;

    // weapon management
    WeaponSwitchState weaponSwitchState;

    // Start is called before the first frame update
    void Start()
    {
        weaponSwitchState = WeaponSwitchState.Down;

        inputsHandler = GetComponent<PlayerInputsHandler>();
        playerCharacterController = GetComponent<PlayerCharacterController>();

        SetFov(defaultFov);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetFov(float fov) {
        playerCharacterController.characterCamera.fieldOfView = fov;
    }
}
