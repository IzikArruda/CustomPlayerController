using UnityEngine;
using System.Collections;

/*
 * Contains values for each input the user can use.
 */
public class UserInputs {

    public float playerMovementX;
    public float playerMovementY;
    public float playerMovementXRaw;
    public float playerMovementYRaw;
    public bool leftMouseButton;
    public bool rightMouseButton;
    public float mouseX;
    public float mouseY;
    public bool spaceBar;

    public void UpdateInputs() {
        /*
         * Update the input values of the player for this frame
         */

        playerMovementX = Input.GetAxis("Horizontal");
        playerMovementY = Input.GetAxis("Vertical");
        playerMovementXRaw = Input.GetAxisRaw("Horizontal");
        playerMovementYRaw = Input.GetAxisRaw("Vertical");
        leftMouseButton = Input.GetMouseButtonDown(0);
        rightMouseButton = Input.GetMouseButtonDown(1);
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        spaceBar = Input.GetKey("space");
    }
}
