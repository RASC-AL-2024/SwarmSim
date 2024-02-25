using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float mouseSensitivity = 100f;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, transform.localEulerAngles.y + mouseX, 0f);

        float xMovement = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
        float zMovement = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime;

        transform.position += transform.right * xMovement + transform.forward * zMovement; ;
    }
}
