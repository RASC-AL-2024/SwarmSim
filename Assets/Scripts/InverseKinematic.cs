using UnityEngine;
using System.Collections.Generic;

public class InverseKinematics : MonoBehaviour
{
    // What transform we are trying to hit
    [SerializeField]
    public Transform target;

    // The chain of modules that we will use to hit
    // the transform. The first module should be the root,
    // the last module is what we use to try and hit the target.
    // MAKE SURE THAT THE CENTER OF MASS ON THE LAST ELEMENT IS RIGHT!
    // The jacobian gives linear position derivatives for the center of mass.
    [SerializeField]
    public Module[] chain;

    private List<ArticulationBody> bodies;

    void Start()
    {
        bodies = new List<ArticulationBody>();
        foreach (var module in chain)
        {
            bodies.Add(module.GetComponent<ArticulationBody>());

            // maybe should run in module init
            var drive = bodies[^1].xDrive;
            drive.driveType = ArticulationDriveType.Target;
            drive.target = 0;
            bodies[^1].xDrive = drive;
        }
    }

    string JacobianString(ArticulationJacobian jacobian)
    {
        string toPrint = "";
        for (int m = 0; m < jacobian.rows; ++m)
        {
            for (int n = 0; n < jacobian.columns; ++n)
            {
                toPrint += jacobian[m, n].ToString("0.0000");
                toPrint += ' ';
            }
            toPrint += '\n';
        }

        return toPrint;
    }

    public Vector3 PositionError()
    {
        // The center of mass of the last element in the chain.
        // e.g. the scooper.
        return target.position - bodies[^1].worldCenterOfMass;
    }

    float MinAbs(float a, float b)
    {
        return Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
    }

    float AngleError(float target, float current)
    {
        target = target % 360;
        current = current % 360;
        if (target > current)
        {
            return MinAbs(target - current, target - current - 360f);
        }
        return MinAbs(target - current, 360f - current + target);
    }

    public Vector3 RotationError()
    {
        // The center of mass of the last element in the chain.
        // e.g. the scooper.
        var y = target.eulerAngles;
        var yhat = bodies[^1].transform.eulerAngles;
        return new Vector3(
            AngleError(y.x, yhat.x),
            AngleError(y.y, yhat.y),
            AngleError(y.z, yhat.z));
    }

    void Update()
    {
        // https://nvidia-omniverse.github.io/PhysX/physx/5.3.1/docs/Articulations.html?highlight=cache%20indexing#cache-indexing

        // Jacobian includes linear and rotational components
        var jacobian = new ArticulationJacobian();
        bodies[^1].GetDenseJacobian(ref jacobian);
        Debug.Log(JacobianString(jacobian));

        var positionError = PositionError();
        var rotationError = RotationError();
        Debug.Log($"{positionError} {rotationError}");
        if (positionError.magnitude < 0.1 && rotationError.magnitude < 2)
        {
            return;
        }

        // Shitty L1 gradient descent using the jacobian.
        // I don't know if this is actually convex lol
        // Each body except the root has a revolute joint we 
        // can modify. Iterate them, get derivative of linear position
        // wrt to joint angle for all xyz, then optimize.
        for (int i = 1; i < bodies.Count; ++i)
        {
            float gradient = 0f;
            for (int j = 0; j < 3; ++j)
            {
                gradient += positionError[j] * jacobian[jacobian.rows - 6 + j, i - 1];
                gradient += 1e-2f * rotationError[j] * jacobian[jacobian.rows - 3 + j, i - 1];
            }

            var drive = bodies[i].xDrive;
            drive.target += gradient;
            bodies[i].xDrive = drive;
        }
    }
}
