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

    public Vector3 Error()
    {
        // The center of mass of the last element in the chain.
        // e.g. the scooper.
        return target.position - bodies[^1].worldCenterOfMass;
    }

    void Update()
    {
        // https://nvidia-omniverse.github.io/PhysX/physx/5.3.1/docs/Articulations.html?highlight=cache%20indexing#cache-indexing

        // Jacobian includes linear and rotational components
        var jacobian = new ArticulationJacobian();
        bodies[^1].GetDenseJacobian(ref jacobian);

        var error = Error();
        if (error.magnitude < 0.1)
        {
            return;
        }

        // Shitty L2 gradient descent using the jacobian.
        // I don't know if this is actually convex lol
        // Each body except the root has a revolute joint we 
        // can modify. Iterate them, get derivative of linear position
        // wrt to joint angle for all xyz, then optimize.
        //
        // TODO: Maybe make this one shot calculation then lerp to animate.
        for (int i = 1; i < bodies.Count; ++i)
        {
            float gradient = 0f;
            for (int j = 0; j < 3; ++j)
            {
                gradient += Time.deltaTime * error[j] * jacobian[jacobian.rows - 6 + j, i - 1] * 60;
            }

            var drive = bodies[i].xDrive;
            drive.target += gradient;
            bodies[i].xDrive = drive;
        }
    }
}
