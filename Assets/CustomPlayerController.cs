using UnityEngine;
using System.Collections;

public class CustomPlayerController : MonoBehaviour {
    
    /* The position of the camera used for the player view */
    public Transform restingCameraTransform;

    /* The direction of player input */
    private Vector3 inputVector = Vector3.zero;

    /* Player stats that adjust how they control */
    [Range(1, 0)]
    public float sliding;
    public float movementSpeed;
    public float runSpeedMultiplier;
    public float gravity;
    public float bodyHeight;
    public float feetHeight;
    //How a high a step can be before the player will not lock onto the floor bellow
    public float maxStepHeight;

    /* The position of the player's feet */
    public Vector3 feetPosition;

    public bool falling = false;

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
        transform.localPosition += new Vector3(0, bodyHeight/2f + feetHeight, 0);
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
        //inputVector = new Vector3(0.03f, 0, 0);

        /* Apply the movement to the player */
        MovePlayer(inputVector, upVector);


        /* Push the player down if a raycast from their center doesnt hit the floor */
        /*float playerHeight = 2;
        bool isGrounded;
        float snapDistance = 1f;
        RaycastHit hitInfo = new RaycastHit();
        if(Physics.Raycast(new Ray(transform.position, Vector3.down), out hitInfo, snapDistance)) {
            Debug.Log("HIT THE THING");
            isGrounded = true;
            transform.position = hitInfo.point;
            transform.position = new Vector3(hitInfo.point.x, hitInfo.point.y + playerHeight / 2, hitInfo.point.z);
        }
        else {
            isGrounded = false;
        }*/
    }

    public void MovePlayer(Vector3 inputVector, Vector3 upDirection) {
        /*
         * Use the given inputVector to move the player in the proper direction and use the given
         * upDirection to determine if the player will need to stick to the floor or start falling.
         * 
         * To determine if the player has taken a step down/up, rayTrace from the base of the player's body
         * down to their feet. No collision means the player is currently falling, but doesnt mean we apply gravity yet.
         * 
         * After moving the player using the given inputVector, recalculate their footing position. If there is no
         * collision, then the player is currently falling. Here are the other outcomes:
         * footing is equal before and after moving: The player has remained on flat ground.
         * footing is more after moving: The player walked down something, snap the footing and move their body down.
         * footing is less after moving: The player walked up something, snap the footing and move their body up.
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet;
        float currentFeetLength = feetHeight;
        float nextFeetLength = feetHeight + maxStepHeight;
        bool applyGravity = true;

        /* Get the current state of the player's footing */
        //note: the initial footing check should not exceed feetHeight, because we do not want the 
        //player to be walking around with legs larger than feetHeight. This does not seem like a  proper fix however
        bodyToFeet = new Ray(transform.position - upDirection*bodyHeight/2f, -upDirection);
        if(Physics.Raycast(bodyToFeet, out hitInfo, currentFeetLength)) {
            currentFeetLength = hitInfo.distance;
        }

        /* Apply the input movement to the player */
        transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        //transform.position += (inputVector)*Time.deltaTime*60;
        GetComponent<Rigidbody>().MovePosition(transform.position + (inputVector)*Time.deltaTime*60);


        /* Recalculate the footing of the player on the next step with a higher feetLength to catch walking down stairs */
        bodyToFeet = new Ray(transform.position - upDirection*bodyHeight/2f, -upDirection);
        if(Physics.Raycast(bodyToFeet, out hitInfo, nextFeetLength)) {
            nextFeetLength = hitInfo.distance;
            applyGravity = false;
        }

        /* Determine what happened on this frame to the player */
        if(nextFeetLength < currentFeetLength) {
            Debug.Log("player stepped up");
        }
        else if(nextFeetLength > currentFeetLength) {
            Debug.Log("player stepped down");
        }

        
        /* Make the player fall if gravity comes into play */
        if(applyGravity == true) {
            falling = true;
        }
        if(falling == true) {
            Debug.Log("FALLING");
        }



        /* If there is a discrepincy in the footing and the player is not falling, make them step */
        float steppingDifference = currentFeetLength - nextFeetLength;
        if(steppingDifference != 0 && applyGravity == false) {
            Debug.Log(steppingDifference);
            transform.localPosition += new Vector3(0, steppingDifference, 0);
        }
    }

}
