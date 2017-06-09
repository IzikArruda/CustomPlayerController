using UnityEngine;
using System.Collections;

public class CustomPlayerController : MonoBehaviour {
    
    /* The expected position of the camera */
    public Transform restingCameraTransform;

    /* The current position of the camera. Smoothly changes to restingCameraTransform each frame */
    public Transform currentCameraTransform;
    
    /* The camera used for the player's view */
    public Camera playerCamera;

    /* How fast currentCameraTransform morphs to restingCameraTransform each frame, in percentage. */
    [Range(1, 0)]
    public float morphPercentage;

    /* The direction of player input */
    private Vector3 inputVector = Vector3.zero;
    
    /* Sliding determines how much of getAxis should be used over getAxisRaw. */
    [Range(1, 0)]
    public float sliding;

    /* How fast a player moves using player inputs */
    public float movementSpeed;
    public float runSpeedMultiplier;

    /* How fast a player accelerates towards their feet when falling. */
    public float gravity;

    /* The sizes of the player's capsule collider */
    public float playerBodyLength;
    public float playerBodyRadius;

    /* Percentage of player radius that is used to sepperate the legs from the player's center */
    [Range(1, 0)]
    public float legGap;

    /* How much distance will be between the player's collider and the floor */
    public float playerLegLength;

    /* How low a player can step down for them to snap to the ground.
     * Note that they can only step up a maximum of playerLegLength. */
    public float maxStepHeight;

    /* How many extra feet are used when handling ground checks */
    public int extraFeet;

    /* The position of the player's foot. The player body will always try to be playerLegLength above this point. */
    [HideInInspector]
    public Vector3 currentFootPosition;

    /* The length of each leg of the player */
    private float[] currentLegLength;
    
    /* If the player is falling with gravity or standing with their legs */
    public bool falling = true;

    void Start() {
        /*
         * Set the values of the player model to be equal to the values set in the script
         */

        /* Initilize the leg lengths */
        currentLegLength = new float[extraFeet + 1];
        
        /* Put the starting foot position at the base of the player model */
        currentFootPosition = transform.TransformPoint(new Vector3(0, -GetComponent<CapsuleCollider>().height/2, 0));

        /* Adjust the player's height and width */
        GetComponent<CapsuleCollider>().height = playerBodyLength;
        GetComponent<CapsuleCollider>().radius = playerBodyRadius;

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

        /* Apply the movement to the player */
        MovePlayer(inputVector, cameraYAngle.rotation);
    }

    public void MovePlayer(Vector3 inputVector, Quaternion fixedPlayerView) {
        /*
         * Use the given inputVector to move the player in the proper direction and use the given
         * fixedPlayerViewingVector to find their forward and up vectors. 
         * 
         * To determine if the player has taken a step down or up, compare a rayTrace taken before this frame and 
         * a rayTrace taken at this frame. If their legLenths are different, then a step will be taken.
         * 
         * If the currentLegLength rayTrace does not connect to the floor, the player will undergo the effects
         * of gravity instead of taking a step. When under the effects of graivty, the previous step is ignored.
         */
        Vector3 gravityVector = Vector3.zero;
        Vector3 upDirection = fixedPlayerView*Vector3.up;
        Vector3 forwardVector = fixedPlayerView*Vector3.forward;
        Vector3 tempForwardVector;

        /* Update the currentlegLength values for the legs that form a circle around the player */
        LegCollisionTest(transform.position - upDirection*playerBodyLength/2f, -upDirection, playerLegLength+maxStepHeight, 0);
        for(int i = 1; i < currentLegLength.Length; i++) {
            tempForwardVector = Quaternion.AngleAxis(i*(360/(currentLegLength.Length-1)), upDirection)*forwardVector;
            LegCollisionTest(transform.position + tempForwardVector*legGap*playerBodyRadius - upDirection*playerBodyLength/2f, -upDirection, playerLegLength+maxStepHeight, i);
        }
        
        /* Get how many legs are touching an object */
        int standingCount = 0;
        for(int i = 0; i < currentLegLength.Length; i++) {
            if(currentLegLength[i] >= 0) {
                standingCount++;
            }
        }

        /* If enough legs are touching an object, the player is considered "standing" */
        int requiredCount = 1;
        if(standingCount >= requiredCount) {
            falling = false;
        }
        else {
            falling = true;
        }
        
        /* If the player is standing, check if they have taken a step */
        if(falling == false) {

            /* Calculate the current foot position of the player by finding the expectedLegLength */
            float expectedLegLength = 0;
            Debug.Log("---");
            for(int i = 0; i < currentLegLength.Length; i++) {
                if(currentLegLength[i] >= 0) {
                    expectedLegLength += currentLegLength[i];
                    Debug.Log(currentLegLength[i]);
                }
            }
            expectedLegLength /= standingCount;
            Debug.Log(expectedLegLength);
            currentFootPosition = transform.position - upDirection*(playerBodyLength/2f + expectedLegLength);
            
            /* Get how much distance was travelled from the step and apply it to the player and their camera */
            transform.position = currentFootPosition + upDirection*(playerBodyLength/2f + playerLegLength);
            currentCameraTransform.transform.position -= upDirection*(playerLegLength - expectedLegLength);
        }
        /* If the player is falling, apply a gravity vector */
        else if(falling == true) {
            gravityVector = -0.1f*upDirection;
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
    
    void LegCollisionTest(Vector3 position, Vector3 direction, float length, int index) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index.
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet = new Ray(position, direction);

        if(Physics.Raycast(bodyToFeet, out hitInfo, length)){
            currentLegLength[index] = hitInfo.distance;
            /* Draw the point for reference */
            Debug.DrawLine(
                position,
                position + direction*(playerLegLength+maxStepHeight),
                Color.red);
        }
        else {
            currentLegLength[index] = -1;
        }
    }

    void AdjustCamera() {
        /*
         * Move the currentCameraTransform towards restingCameraTransform.
         */
        Vector3 positionDifference;
        float minimumPositionDifference = 0.01f;
        float maximumPositionDifference = playerBodyLength/3f;

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
            currentCameraTransform.position += positionDifference*morphPercentage;
        }
    }
}
