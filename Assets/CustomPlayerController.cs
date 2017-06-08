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
    public float playerBodyLength;
    public float playerLegLength;

    /* How a high a step can be before the player will not lock onto the floor bellow */
    public float maxStepHeight;



    /* How many extra feet are used when handling ground checks */
    public int extraFeet;
    /* The position of the player's foot and their leg lenths */
    public Vector3 currentFootPosition;
    public Vector3 previousFootPosition;
    public float[] currentLegLength;


    public bool falling = true;

    void Start() {
        /*
         * Set the values of the player model to be equal to the values set in the script
         */

        /* Initilize the leg lengths */
        currentLegLength = new float[extraFeet + 1];

        /* Put the starting foot position at the base of the player model */
        currentFootPosition = transform.TransformPoint(new Vector3(0, -GetComponent<CapsuleCollider>().height/2, 0));

        /* Adjust the player's height */
        GetComponent<CapsuleCollider>().height = playerBodyLength;
        
        /* Adjust the player model's position to reflect the player's leg length */
        transform.position = currentFootPosition;
        transform.localPosition += new Vector3(0, playerBodyLength/2f + playerLegLength, 0);
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
        Vector3 stepDifference = Vector3.zero;
        previousFootPosition = currentFootPosition;
        //How wide the player body is
        float bodyWidth = 0.3f;


        //Get a vector that points forward from the player
        Transform cameraYAngle = restingCameraTransform.transform;
        cameraYAngle.localEulerAngles = new Vector3(0, cameraYAngle.localEulerAngles.y, 0);
        Vector3 forwardVector = cameraYAngle.rotation*Vector3.forward;
        Vector3 tempForwardVector;

        /* Update the currentlegLength values for the legs that form a circle around the player */
        int standingLegCount = 0;
        for(int i = 0; i < currentLegLength.Length-1; i++) {

            /* Get the new temp forward vector  */
            tempForwardVector = Quaternion.AngleAxis(i*(360/(currentLegLength.Length-1)), upDirection)*forwardVector;

            /* Draw the point for reference */
            Debug.DrawLine(
                transform.position + tempForwardVector*bodyWidth - upDirection*playerBodyLength/2f,
                transform.position + tempForwardVector*bodyWidth - upDirection*playerBodyLength*2,
                Color.red);

            /* Update the legLength value for this leg */
            hitInfo = RayTrace(transform.position + tempForwardVector*bodyWidth - upDirection*playerBodyLength/2f, -upDirection, playerLegLength+maxStepHeight);
            if(hitInfo.collider != null) {
                standingLegCount++;
                currentLegLength[i] = hitInfo.distance;
            }
            /////NOTE: IF A LEG DOES NOT TOUCH ANYTHING, MAYBE IT SHOULD NOT BE COUNTED
            else {
                currentLegLength[i] = playerLegLength+maxStepHeight;
            }
        }
        /* The final legLength value will be directly in the center of the player */
        hitInfo = RayTrace(transform.position - upDirection*playerBodyLength/2f, -upDirection, playerLegLength+maxStepHeight);
        if(hitInfo.collider != null) {
            standingLegCount++;
        }
        /////NOTE: IF A LEG DOES NOT TOUCH ANYTHING, MAYBE IT SHOULD NOT BE COUNTED
        else {
            currentLegLength[currentLegLength.Length-1] = playerLegLength+maxStepHeight;
        }
        currentLegLength[currentLegLength.Length-1] = hitInfo.distance;


        /* If less than 3 legs hit an object, the player is falling */
        if(standingLegCount < 3) {
            falling = true;
        }else {
            falling = false;
        }


        /* If the player is falling, apply a gravity vector */
        if(falling == true) {
            gravityVector = -0.1f*upDirection;
        }

        /* If the player is standing, check if they have taken a step */
        else if(falling == false) {

            /* Calculate the current foot position of the player by finding the expectedLegLength */
            float expectedLegLength = 0;
            for(int i = 0; i < currentLegLength.Length; i++) {
                expectedLegLength += currentLegLength[i];
            }
            expectedLegLength /= currentLegLength.Length;
            currentFootPosition = transform.position - upDirection*(playerBodyLength/2f + expectedLegLength);
            
            /* Update the player to the new position */
            transform.position = currentFootPosition + upDirection*(playerBodyLength/2f + playerLegLength);


            /* Calculate the difference in leg length/camera movement in the y axis */
            //Debug.Log(playerLegLength - expectedLegLength);
            /* Move the player camera by however much the step was */
            currentCameraTransform.transform.position -= upDirection*(playerLegLength - expectedLegLength);
        }




        /* Apply the movement of the players input */
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().MovePosition(transform.position + (gravityVector + inputVector)*Time.deltaTime*60);
        
        //The player camera must always be where the currentCameraTransform is
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation = currentCameraTransform.rotation;
        
        /* Adjust the camera's position now that the player has moved */
        AdjustCamera();
    }

    void AdjustCamera() {
        /*
         * Move the currentCameraTransform towards restingCameraTransform.
         */
        Vector3 positionDifference;
        float minimumPositionDifference = 0.01f;
        float maximumPositionDifference = playerBodyLength/3f;
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
