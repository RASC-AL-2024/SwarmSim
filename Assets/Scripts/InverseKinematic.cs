using UnityEngine;
using System.Collections.Generic;

public class InverseKinematics : MonoBehaviour {
  [SerializeField]
  public Transform target;

  [SerializeField]
  public Transform actual;

  [SerializeField]
  public Module[] chain;

  private List<ArticulationBody> bodies;
  
  void Start() {
    bodies = new List<ArticulationBody>();
    foreach (var module in chain) {
      bodies.Add(module.GetComponent<ArticulationBody>());

      // maybe should run in module init
      var drive = bodies[^1].xDrive;
      drive.driveType = ArticulationDriveType.Target;
      drive.target = 0;
      bodies[^1].xDrive = drive;
    }
  }

  string JacobianString(ArticulationJacobian jacobian) {
    string toPrint = "";
    for (int m = 0; m < jacobian.rows; ++m) {
      for (int n = 0; n < jacobian.columns; ++n) {
        toPrint += jacobian[m, n].ToString("0.0000");
        toPrint += ' ';
      }
      toPrint += '\n';
    }

    return toPrint;
  }

  void Update() {
    var jacobian = new ArticulationJacobian();
    bodies[^1].GetDenseJacobian(ref jacobian);
    // if base is immovable, jacobian is (3 * 6)x3
    // otherwise, jacobian is (4 * 6)x(3 + 6)
    var currentPosition = actual.position;

    var error = target.position - currentPosition;
    Debug.Log(error);

    // want (error - J(x) dX ) -> 0

    // Enumerate everything but root
    for (int i = 1; i < bodies.Count; ++i) {
      float gradient = 0f;
      for (int j = 0; j < 3; ++j) {
        // l2 loss (don't think this is actually convex lol)
        gradient += error[j] * jacobian[jacobian.rows - 6 + j, i - 1];
      }

      var drive = bodies[i].xDrive;
      drive.target += gradient;
      bodies[i].xDrive = drive;
    }
  }
}
