using UnityEngine; 

public class FPSCamera : MonoBehaviour {
  // Mouse to turn, WASDQE for movement, space to pause/unpause

  [SerializeField]
  public float mouseSensitivity;

  [SerializeField]
  public float movementSensitivity;

  Vector2 rotation = Vector2.zero;

  private bool stopped = false;

  void Update() {
    if (Input.GetKeyDown(KeyCode.Space))
      stopped = !stopped;
    if (stopped)
      return;

    rotation.x += mouseSensitivity * Input.GetAxis("Mouse X"); 
    rotation.y += mouseSensitivity * Input.GetAxis("Mouse Y"); 
    transform.localRotation = Quaternion.AngleAxis(rotation.x, Vector3.up) * Quaternion.AngleAxis(rotation.y, Vector3.left);

    transform.Translate(Vector3.forward * Input.GetAxis("Vertical") * movementSensitivity);
    transform.Translate(Vector3.right * Input.GetAxis("Horizontal") * movementSensitivity);

    float upAxis = 0f;
    if (Input.GetKey(KeyCode.Q)) {
      upAxis += 1f;
    } else if (Input.GetKey(KeyCode.E)) {
      upAxis -= 1f;
    }
    transform.Translate(Vector3.up * upAxis * movementSensitivity);
  }
};
