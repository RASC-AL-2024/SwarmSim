using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Assertions;

// Usage:
// Make a prefab for each module. Modules are connected by "Connections"
// In the prefab, define some child anchor objects that are
// positioned at points on the module where other modules should be able to attach.
// +x should always face away from the surface (i.e. towards other objects)
// It is also assumed that modules have articulation body components.
// To build a larger structure in the editor, define a root module
// and then progressively add children. You will need to specify in the editor
// the "other" field for each of the connections you add for both the parents and the children.
// If you hit the "Update child positions" button stuff should snap in place.
// In the future this can be done dynamically.

[System.Serializable]
public class Connection
{
    public Transform anchor;
    public Module other; // can be null
}

public class Module : MonoBehaviour
{
    // We don't group these into a single module so we can edit them in the inspector
    [SerializeField]
    public Connection[] connections;

    private Connection GetChildConnection(Connection connection)
    {
        // Returns the child's connection that corresponds to the given one
        Assert.IsTrue(connection.other.transform.IsChildOf(this.transform));
        foreach (var childConnection in connection.other.connections)
            if (childConnection.other == this)
                return childConnection;

        // Shouldn't happen
        Assert.IsTrue(false);
        return null;
    }

    void Start()
    {
        UpdateConnections();
    }

    private (Vector3, Quaternion) compose((Vector3 pos, Quaternion rot) tA, (Vector3 pos, Quaternion rot) tB)
    {
        // (B b)(A a) = (BA Ba + b
        // (0 1)(0 1)   (0 1)

        return (tB.pos + (tA.rot * tB.rot) * tA.pos, tA.rot * tB.rot);
    }

    private (Vector3, Quaternion) invert(Vector3 position, Quaternion rotation)
    {
        return (-position, Quaternion.Inverse(rotation));
    }

    private void UpdateConnection(Connection connection)
    {
        var childConnection = GetChildConnection(connection);

        // We want to align the objects so that the anchors line up (rotated 180 deg around y)
        // connection.anchor is relative to us
        // childConnection.anchor is relative to child
        // child is relative to us
        // Some of these transforms are probably broken
        // con.al2p = cc.al2p @ rot180y
        // cc.al2p = c.c2p @ cc.al2c
        // c.c2p = cc.al2p @ (cc.al2c)^-1
        // c.c2p = con.al2p @ (rot180y)^{-1} @ (cc.al2c)^-1
        //

        var p = childConnection.anchor.localPosition;
        // this angle calculation is right
        var r = childConnection.anchor.localRotation * Quaternion.Euler(0, 180, 0);
        (var position, var rotation) = compose((connection.anchor.localPosition, connection.anchor.localRotation), invert(p, r));

        var childObject = connection.other.gameObject;

        // (var position, var rotation) = compose(
        //   compose(
        //     invert(childConnection.anchor.localPosition, childConnection.anchor.localRotation),
        //     invert(new Vector3(), Quaternion.Euler(0, 180, 0))
        //   ),
        //   (connection.anchor.localPosition, connection.anchor.localRotation)
        // );

        // position might not be right but not sure
        connection.other.transform.localPosition = rotation * position;
        connection.other.transform.localRotation = rotation;

        var childBody = connection.other.GetComponent<ArticulationBody>();
        childBody.matchAnchors = true; // breaks otherwise :( (unity bug??)
        childBody.jointType = ArticulationJointType.RevoluteJoint;
        childBody.anchorPosition = childConnection.anchor.localPosition;
        childBody.anchorRotation = childConnection.anchor.localRotation;
        childBody.parentAnchorPosition = connection.anchor.localPosition;
        childBody.parentAnchorRotation = Quaternion.Euler(0, 180, 0) * connection.anchor.localRotation;
    }

    public void UpdateConnections()
    {
        foreach (var connection in connections)
        {
            if (connection.other != null && connection.other.transform.IsChildOf(this.transform))
            {
                UpdateConnection(connection);
            }
        }
    }
}

[CustomEditor(typeof(Module))]
public class ModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Module script = (Module)target;
        if (GUILayout.Button("Update Child Positions"))
        {
            script.UpdateConnections();
        }
    }
}

