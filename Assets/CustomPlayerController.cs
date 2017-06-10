using UnityEngine;
using System.Collections;

/*
 * A custom character controller that uses UserInputs to handle movement. It uses "legs" to keep
 * it's "body" above the floor, letting the player walk up and down stairs or slopes smoothly. 
 */
public class CustomPlayerController : MonoBehaviour {

    /* The UserInputs object linked to this player */
    private UserInputs inputs;

    /* The expected position of the camera */
    public Transform restingCameraTransform;

    /* The current position of the camera. Smoothly changes to restingCameraTransform each frame */
    public Transform currentCameraTransform;
    
    /* The camera used for the player's view */
    public Camera playerCamera;

    /* The viewing angle of the player's camera */
    private float xRotation;
    private float yRotation;
    
    /* How fast currentCameraTransform morphs to restingCameraTransform each frame, in percentage. */
    [Range(1, 0)]
    public float morphPercentage;

    /* The direction and magnitude of player input */
    private Vector3 inputVector = Vector3.zero;
    
    /* Sliding determines how much of getAxis should be used over getAxisRaw. */
    [Range(1, 0)]
    public float sliding;

    /* How fast a player moves using player inputs */
    public float movementSpeed;
    public float runSpeedMultiplier;

    /* How fast a player accelerates towards their feet when falling. */
    public float gravity;

    /* How fast a player travels upward when they jump */
    public float jumpSpeed;
    
    /* The Y velocity of the player along with its max(positive) */
    public float currentYVelocity;
    public float maxYVelocity;

    /* Used to determine the state of the jump. If true, the next jump opportunity will cause the player to jump. */
    private bool jumpPrimed;
    /* The state of the jump key on the current and previous frame. true = pressed */
    private bool jumpKeyPrevious = false;
    private bool jumpKeyCurrent = false;

    /* The sizes of the player's capsule collider */
    public float playerBodyLength;
    public float playerBodyRadius;

    /* Percentage of player radius that is used to sepperate the legs from the player's center */
    [Range(1, 0)]
    public float legGap;

    /* How much distance will be between the player's collider and the floor */
    public float playerLegLength;
    private float currentLegLength;
    
    /* The length of the player's leg at this frame */
    private float expectedLegLength;

    /* How low a player can step down for them to snap to the ground */
    public float maxStepHeight;
    private float currentStepHeight;

    /* How many extra feet are used when handling ground checks */
    public int extraFeet;

    /* The position of the player's foot. The player body will always try to be playerLegLength above this point. */
    private Vector3 currentFootPosition;

    /* The length of each leg of the player */
    private float[] extraLegLenths;

    /* If the player is falling with gravity or standing with their legs */
    private bool falling = false;


    /* -------------- Built-in Unity Functions ---------------------------------------------------------- */
    
    void Start() {
        /*
         * Set the values of the player model to be equal to the values set in the script
         */

        /* Create the UserInputs object linked to this player */
        inputs = new UserInputs();

        /* Initilize the leg lengths */
        extraLegLenths = new float[extraFeet + 1];
        
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
        /*
         * Handle the inputs of the user and the movement of the player on this frame.
         */

        /* Update the inputs of the player */
        inputs.UpdateInputs();

        /* Use mouse input values to rotate the camera */
        RotateCamera();

        /* Update the player's jumping conditions */
        JumpingCondition();
        
        /* Change the player's leg lengths depending on their state */
        UpdateLegLengths();
        
        /* Get an input vector that is relative to the player's rotation and takes into account the player's speed */
        UpdateInputVector();
        
        /* Find the footPosition of the player and check if they are falling or standing */
        StepPlayer();
        
        /* Apply the movement to the player from taking steps, inputting directions and falling from gravity. */
        MovePlayer();

        /* Adjust the camera's position now that the player has moved */
        AdjustCameraPosition();
    }


    /* ----------------- Update Functions ------------------------------------------------------------- */

    void RotateCamera() {
        /*
         * Use the user's mouse movement to rotate the player's camera
         */

        /* Ensure the X rotation does not overflow */
        xRotation -= inputs.mouseX;
        if(xRotation < 0) { xRotation += 360; }
        else if(xRotation > 360) { xRotation -= 360; }

        /* Prevent the Y rotation from rotating too high or low */
        yRotation += inputs.mouseY;
        yRotation = Mathf.Clamp(yRotation, -75, 75);

        /* Apply the rotation to the camera's currentCameraTransform*/
        currentCameraTransform.transform.localEulerAngles = new Vector3(-yRotation, -xRotation, 0);

        /* Update the camera's position with the new currentCameraTransform */
        playerCamera.transform.rotation = currentCameraTransform.transform.rotation;
    }
    
    void JumpingCondition() {
        /*
    	 * Check if the player has pressed the jump key. To be able to jump, the player must not be falling.
    	 * 
    	 * When the player jumps, their Y velocity will become positive.
    	 */
        jumpKeyPrevious = jumpKeyCurrent;
        jumpKeyCurrent = inputs.spaceBar;

        /* Pressing the jump key will prime the jump */
        if(jumpKeyCurrent == true && jumpKeyPrevious == false) {
            jumpPrimed = true;
        }
        /* Releasing the jump key will attempt to make the player jump */
        else if(jumpKeyCurrent == false && jumpKeyPrevious == true) {
            JumpAttempt();
        }
    }
    
    void UpdateLegLengths() {
        /*
         * Change the player's leg lengths depending on the state they are in.
         * If the player is standing, keep their leg length to it's expected amount.
         * If the player is falling, give them short legs.
         * If the player is falling, but is travelling against gravity, given them very short leg lengths.
         */

        if(falling == false) {
            currentLegLength = playerLegLength;
            currentStepHeight = maxStepHeight;
        }
        else if(currentYVelocity < 0) {
            currentLegLength = playerLegLength*0.5f;
            currentStepHeight = maxStepHeight*0.5f;
        }
        else {
            currentLegLength = playerLegLength*0.1f;
            currentStepHeight = maxStepHeight*0.1f;
        }
    }
    
    public void UpdateInputVector() {
        
        /* Get the player's horizontal viewing angle to get vectors relative to the player's view */
        Quaternion cameraYRotation = currentCameraTransform.transform.rotation;
        cameraYRotation.eulerAngles.Set(0, cameraYRotation.y, 0);
        
        /* Use two input types for each axis to allow more control on player movement */
        inputVector = new Vector3((1-sliding)*inputs.playerMovementXRaw + sliding*inputs.playerMovementX,
                0, (1-sliding)*inputs.playerMovementYRaw + sliding*inputs.playerMovementY);

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
        inputVector = cameraYRotation*inputVector;

    }
    
    public void StepPlayer() {
        /*
         * Use the given inputVector to move the player in the proper direction and use the given
         * fixedPlayerView as the player's rotation to keep Vector.Up/Foward relative to the player.
         * 
         * To determine if the player has taken a step down or up, compare a rayTrace taken before this frame and 
         * a rayTrace taken at this frame. If their legLenths are different, then a step will be taken.
         * 
         * If the currentLegLength rayTrace does not connect to the floor, the player will undergo the effects
         * of gravity instead of taking a step. When under the effects of graivty, the previous step is ignored.
         */
        Vector3 gravityVector = Vector3.zero;
        Vector3 upDirection = transform.rotation*Vector3.up;
        Vector3 forwardVector = transform.rotation*Vector3.forward;
        Vector3 tempForwardVector;

        /* Update the currentlegLength values for the legs that form a circle around the player */
        LegCollisionTest(transform.position - upDirection*playerBodyLength/2.5f, -upDirection, currentLegLength+currentStepHeight, 0);
        for(int i = 1; i < extraLegLenths.Length; i++) {
            tempForwardVector = Quaternion.AngleAxis(i*(360/(extraLegLenths.Length-1)), upDirection)*forwardVector;
            LegCollisionTest(transform.position + tempForwardVector*legGap*playerBodyRadius - upDirection*playerBodyLength/2.5f, -upDirection, currentLegLength+currentStepHeight, i);
        }

        /* Get how many legs are touching an object */
        int standingCount = 0;
        for(int i = 0; i < extraLegLenths.Length; i++) {
            if(extraLegLenths[i] >= 0) {
                standingCount++;
            }
        }

        /* If enough legs are touching an object, the player is considered "standing" */
        int requiredCount = 1;
        if(standingCount >= requiredCount) {
            falling = false;
            currentYVelocity = 0;
        }
        else {
            /* Attempt to consume a jump if the player was standing but will now start falling */
            JumpAttempt();
            falling = true;
        }

        /* If the player is standing, check if they have taken a step */
        if(falling == false) {

            /* Calculate the current foot position of the player by finding the expectedLegLength */
            expectedLegLength = 0;
            for(int i = 0; i < extraLegLenths.Length; i++) {
                if(extraLegLenths[i] >= 0) {
                    expectedLegLength += extraLegLenths[i];
                }
            }
            expectedLegLength /= standingCount;
            currentFootPosition = transform.position - upDirection*(playerBodyLength/2f + expectedLegLength);
        }
    }
    
    public void MovePlayer() {
        /*
         * Move the player relative to what has occured this frame so far, such as any steps taken
         * or if the player should be falling. 
         * 
         * Step movements are done by setting the  player's position 
         * relative to their foot position and saved legLength for this frame.
         * 
         * Gravity is determined by tracking the upward/downward velocity of the player and whether they are falling.
         */
        Vector3 upDirection = transform.rotation*Vector3.up;
        Vector3 gravityVector = Vector3.zero;

        /* If the player is standing, position their body relative to their foot position and the length of their legs.
         * Any distance travelled up or down from a step will not be applied to the player's camera. */
        if(falling == false) {
            transform.position = currentFootPosition + upDirection*(playerBodyLength/2f + currentLegLength);
            currentCameraTransform.transform.position -= upDirection*(currentLegLength - expectedLegLength);
        }

        /* If the player is falling, apply gravity to their yVelocity. Reset yVelocity if they are standing. */
        if(falling == true) {
            currentYVelocity -= gravity*Time.deltaTime*60;
            /* Prevent the player from falling faster than terminal velocity */
            if(currentYVelocity < -maxYVelocity) {
                currentYVelocity = -maxYVelocity;
            }
            gravityVector = currentYVelocity*upDirection;
        }
        else {
            currentYVelocity = 0;
        }

        /* Apply the movement of the players input */
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().MovePosition(transform.position + gravityVector + (inputVector)*Time.deltaTime*60);
    }

    void AdjustCameraPosition() {
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
        }

        /* Smoothly translate the currentTransform to restingTransform using a "recoveryPercentage" */
        else {
            currentCameraTransform.position += positionDifference*morphPercentage;
        }

        /* Place the player camera using currentCameraTransform */
        playerCamera.transform.position = currentCameraTransform.position;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    void LegCollisionTest(Vector3 position, Vector3 direction, float length, int index) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index.
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet = new Ray(position, direction);

        if(Physics.Raycast(bodyToFeet, out hitInfo, length)){
            extraLegLenths[index] = hitInfo.distance;
            ///* Draw the point for reference */
            //Debug.DrawLine(
            //    position,
            //    position + direction*(currentLegLength+currentStepHeight),
            //    Color.red);
        }
        else {
            extraLegLenths[index] = -1;
        }
    }
    
    void JumpAttempt(){
    	/*
    	 * Try to make the player jump. A jump must be primed (jumpPrimed == true) for the player to jump.
    	 */
    	
    	if(jumpPrimed == true && falling == false){
    		jumpPrimed = false;
            falling = true;
            currentYVelocity = jumpSpeed;
        }
    }
}
