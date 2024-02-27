using UnityEngine;

public class ScoopControl : MonoBehaviour {
  // Keeps the scoop facing up
  [SerializeField]
  public Transform scoopDirection;

  private ArticulationBody body;

  void Start() {
    body = GetComponent<ArticulationBody>();
  }

  void Update() {
    float error = Vector3.SignedAngle(Vector3.up, scoopDirection.up, scoopDirection.forward);

    var drive = body.xDrive;
    drive.driveType = ArticulationDriveType.Target;
    drive.target += error * 0.05f;
    body.xDrive = drive;
  }
}
