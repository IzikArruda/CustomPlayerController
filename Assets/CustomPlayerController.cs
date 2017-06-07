using UnityEngine;
using System.Collections;

public class CustomPlayerController : MonoBehaviour {
    
    /* The expected position of the camera */
    public Transform restingCameraTransform;
    /* The current position of the camera */
    public Transform currentCameraTransform;

    /* The camera used for the player's view */
    public Camera playerCamera;

    /* The direction of player input */
    private Vector3 inputVector = Vector3.zero;

    /* Player stats that adjust how they control */
    [Range(1, 0)]
    public float sliding;
    public float movementSpeed;
    public float runSpeedMultiplier;
    public float gravity;
    public float bodyHeight;
    public float legHeight;

    /* How a high a step can be before the player will not lock onto the floor bellow */
    public float maxStepHeight;
    
    /* The position of the player's feet and their leg lenths */
    public Vector3 feetPosition;
    public float previousLegLength;
    public float currentLegLength;

    public bool falling = true;

    void Start() {
        /*
         * Set the values of the player model to be equal to the values set in the script
         */

        //Start by setting the player feet to vector3.zero
        feetPosition = Vector3.zero;

        //Adjust the player's body height to the given value
        GetComponent<CapsuleCollider>().height = bodyHeight;

        //Move the player's position so the space between the base of the player's body and their feet is feetHeight
        transform.position = feetPosition;
        transform.localPosition += new Vector3(0, bodyHeight/2f + legHeight, 0);

        //Set the current leg length value 
        currentLegLength = legHeight;
    }

    void Update() {
        newPlayerMovement();
    }

    void newPlayerMovement() {
        /*
         * A new playerMovement function.
         */

        /* Get the player's horizontal viewing angle to properly rotate the input vector */
        Transform cameraYAngle = restingCameraTransform.transform;
        cameraYAngle.localEulerAngles = new Vector3(0, cameraYAngle.localEulerAngles.y, 0);

        /* Use two input types for each axis to allow more control on player movement */
        inputVector = new Vector3((1-sliding)*Input.GetAxisRaw("Horizontal") + sliding*Input.GetAxis("Horizontal"),
                0, (1-sliding)*Input.GetAxisRaw("Vertical") + sliding*Input.GetAxis("Vertical"));

        /* Keep the movement's maginitude from going above 1 */
        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }

        /* Add the player speed to the movement vector */
        if(Input.GetKey(KeyCode.LeftShift)) {
            inputVector *= movementSpeed*runSpeedMultiplier;
        }
        else {
            inputVector *= movementSpeed;
        }

        /* Rotate the input direction to match the player's view */
        inputVector = cameraYAngle.rotation*inputVector;

        /* Get the "Up" vector from the player's current rotation */
        Vector3 upVector = cameraYAngle.rotation*Vector3.up;
        

        /* Apply all the movements that the player will undergo */
        //inputVector = new Vector3(0.03f, 0, 0); AUTO MOVE THE PLAYER

        /* Apply the movement to the player */
        MovePlayer(inputVector, upVector);
    }

    public void MovePlayer(Vector3 inputVector, Vector3 upDirection) {
        /*
         * Use the given inputVector to move the player in the proper direction and use the given
         * upDirection to determine if the player will need to stick to the floor or start falling.
         * 
         * To determine if the player has taken a step down or up, compare a rayTrace taken before this frame and 
         * a rayTrace taken at this frame. If their legLenths are different, then a step will be taken.
         * 
         * If the currentLegLength rayTrace does not connect to the floor, the player will undergo the effects
         * of gravity instead of taking a step. When under the effects of graivty, the previous step is ignored.
         */
        RaycastHit hitInfo = new RaycastHit();
        Vector3 gravityVector = Vector3.zero;
        Vector3 stepVector = Vector3.zero;

        /* Update the legLength values for this frame */
        previousLegLength = currentLegLength;
        hitInfo = RayTrace(transform.position - upDirection*bodyHeight/2f, -upDirection, legHeight+maxStepHeight);
        currentLegLength = hitInfo.distance;


        /* Check if the player is falling/feet do not touch an object */
        if(hitInfo.collider == null) { falling = true; }
        

        /* If the player was falling but now have their feet on the ground, put them into the proper standing position */
        if(hitInfo.collider != null && falling == true) {
            falling = false;

            /* Put the player's body a legHeight amount above the place where they landed */
            stepVector = transform.position - (hitInfo.point + upDirection*(bodyHeight/2f + legHeight));
            transform.position -= stepVector;

            currentLegLength = legHeight;
        }

        /* If the player is falling, apply a gravity vector */
        else if(falling == true) {
            gravityVector = -0.1f*upDirection;
        }

        /* If the player is on their feet, check if they have taken a step */
        else if(falling == false) {

            /* If a step will be taken, re-calculate the currentLegLength of the new position */
            stepVector = upDirection*(previousLegLength - currentLegLength);
            if(stepVector.magnitude != 0) {
                transform.position += stepVector;
                hitInfo = RayTrace(transform.position - upDirection*bodyHeight/2f, -upDirection, legHeight+maxStepHeight);
                currentLegLength = hitInfo.distance;
            }
        }


        /* Apply the movement of the players input */
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().MovePosition(transform.position + (gravityVector + inputVector)*Time.deltaTime*60);

        /* Move the player camera if a step was taken */
        currentCameraTransform.transform.position -= stepVector;
        

        //The player camera must always be where the currentCameraTransform is
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation = currentCameraTransform.rotation;

        
        /* Adjust the camera's position now that the player has moved */
        AdjustCamera();
        
        //WHAT NEEDS TO HAPPEN NEXDT:
        //A FUNCTION THAT SLIDES THE CURRENTCAMTRANS TOWARDS THE RESTYINGCAMTRANS.
        //THIS FUNCTION ALSO NEEDS TO MAKE SURE THE CURRENTCAMTRANS DOES NOT GO TOO FAR PAST RESTCAMTRANS
    }

    void AdjustCamera() {
        /*
         * Move the currentCameraTransform towards restingCameraTransform.
         */
        Vector3 positionDifference;
        float minimumPositionDifference = 0.01f;
        float maximumPositionDifference = bodyHeight/3f;
        float recoveryPercentage = 0.15f;

        /* Get the difference in positions of the cameraTransforms */
        positionDifference = restingCameraTransform.position - currentCameraTransform.position;


        /* If the difference in their position is small enough, snap the currentTransform to the restingTransform */
        if(positionDifference.magnitude < minimumPositionDifference) {
            currentCameraTransform.position = restingCameraTransform.position;
        }

        /* If the difference in their position is too large, clamp the currentTransform */
        else if(positionDifference.magnitude > maximumPositionDifference) {
            currentCameraTransform.position = restingCameraTransform.position - positionDifference.normalized*maximumPositionDifference;
            Debug.Log("THING");
        }

        /* Smoothly translate the currentTransform to restingTransform using a "recoveryPercentage" */
        else {
            currentCameraTransform.position += positionDifference*recoveryPercentage;
        }
    }





    public RaycastHit RayTrace(Vector3 position, Vector3 direction, float distance) {
        /*
         * Send a rayTrace from the given point in the given direction for the given distance. 
         * Return the rayCastHit info.
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet = new Ray(position, direction);

        if(Physics.Raycast(bodyToFeet, out hitInfo, distance)) {}

        return hitInfo;
    }
}
