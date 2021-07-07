using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterController : MonoBehaviour
{

    PlayerInputsHandler inputHandler;
    CharacterController characterController;
    public Transform cameraTransform;
    public Camera characterCamera;
    float cameraVerticalAngle;
    float verticalAngle;
    float horizontalAngle;

    // control rotation speed precisely
    float RotationSpeed = 1;
    float RotationMultiplier = 1;

    // jump
    float jumpForce = 4f;
    float crouchJumpMultiplier = 0.5f;
    float lastTimeJumped = 0f;
    float jumpGroundingPreventionTime = 0.4f;
    float groundCheckDistance = 0.08f;
    public LayerMask groundCheckLayers;
    bool isGrounded;
    bool hasJumpedThisFrame;
    Vector3 groundNormal = Vector3.up;

    // movement
    float maxSpeed = 10f;
    Vector3 characterVelocity;
    Vector3 latestImpactSpeed;
    float movementSharpnessOnGround = 15;
    float maxSpeedCrouchedRatio = 0.5f;
    float SprintSpeedModifier = 2f;
    float airAcceleration = 5f;
    bool isCrouching = false;
    float targetCharacterHeight;


    void Start()
    {
        inputHandler = GetComponent<PlayerInputsHandler>();
        characterController = GetComponent<CharacterController>();
    }

    void Update() {

        hasJumpedThisFrame = false;

        GroundCheck();
        HandleCharacterMovement();
    }

    void GroundCheck() {
        // reset values before ground check
        isGrounded = false;
        groundNormal = Vector3.up;

        // check the player hasn't just jumped before checking if they are grounded
        if (Time.time >= lastTimeJumped + jumpGroundingPreventionTime) {
            
            Vector3 raycastOrigin = GetFeetPosition();
            Debug.DrawRay(raycastOrigin, Vector3.down * groundCheckDistance, Color.green, 3, false);
    
            if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore)) {
                // store the upward direction for the surface found
                groundNormal = hit.normal;
                // only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(groundNormal)) {
                    isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > characterController.skinWidth){
                        characterController.Move(Vector3.down * hit.distance);
                    }
                }
                else {
                    Debug.Log("hit something and it wasn't good, slope wise");
                }

            }
        }
    }

    // returns true if the slop angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal) {
        return Vector3.Angle(transform.up, normal) <= characterController.slopeLimit;
    }

    // gets the center point of the bottom hemisphere of the character controller capsule
    Vector3 GetCapsuleBottomHemisphere() {
        return transform.position + (Vector3.up * characterController.radius);
    }

    Vector3 GetFeetPosition() {
        return transform.position + (Vector3.down * characterController.height / 2);
    }

    // gets the center point of the top hemisphere of the character controller capsule
    Vector3 GetCapsuleTopHemisphere(float atHeight) {
        return transform.position + (Vector3.up * (atHeight - characterController.radius));
    }

    void HandleCharacterMovement() {
        {
        // horizontal character rotation
            // rotate the transform with the input speed around its local Y axis
            horizontalAngle += inputHandler.GetLookInputHorizontal() * RotationSpeed * RotationMultiplier;

        // veritcal character rotation
            // add vertical inputs to the camera's vertical angle
            verticalAngle += inputHandler.GetLookInputVertical() * RotationSpeed * RotationMultiplier;
            // limit the camera's vertical angle to min/max
            verticalAngle = Mathf.Clamp(verticalAngle, -65f, 70f);


            // apply the veritcal and horizontal angles to the local rotation
            transform.localEulerAngles = new Vector3(verticalAngle, horizontalAngle, 0f);
        }

        // character movement
        bool isSprinting = inputHandler.GetSprintInputHeld();
        {
            float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on the characters transform orientation
            Vector3 worldSpaceMoveInput = transform.TransformVector(inputHandler.GetMoveInput());

            // handle grounded movement
            if (isGrounded) {
                //calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity = worldSpaceMoveInput * maxSpeed * speedModifier;
                //reduce speed if crouching by crouch speed ratio
                if (isCrouching) {
                    targetVelocity *= maxSpeedCrouchedRatio;
                }
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal) * targetVelocity.magnitude;

                // smoothly interpolate between out current velocity and the target velicty based on acceleration speed
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

                //jumping
                if(inputHandler.GetJumpInputDown()) {
                    // start by canceling out the vertical component of out velocity
                    characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                    //then add the jumpSpeed value upwards
                    characterVelocity += Vector3.up * (isCrouching ? jumpForce * crouchJumpMultiplier : jumpForce);

                    // remember last time character jumped to prevent snapping to ground for a short time
                    lastTimeJumped = Time.time;
                    hasJumpedThisFrame = true;

                    // force grounded to false
                    isGrounded = false;
                    groundNormal = Vector3.up;

                }
            }

            //handle air movement
            else {
                //add air acceleration
                characterVelocity += worldSpaceMoveInput * airAcceleration * Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeed * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                //apply gravity to the velocity
                characterVelocity += Vector3.down * 9.81f * Time.deltaTime;
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(characterController.height);
        characterController.Move(characterVelocity * Time.deltaTime);
        //Debug.Log(characterVelocity);

        // detect obstruction to adjust velocity accordingly
        latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, 
        characterController.radius, characterVelocity.normalized, out RaycastHit hit, 
        characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore)) {
            // remember the last impact speed because the fall damage logic might need it
            latestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }
    
    // gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal) {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }
}
