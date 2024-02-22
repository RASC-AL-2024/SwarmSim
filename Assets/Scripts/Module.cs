using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Assertions;

[System.Serializable]
public class Connection {
    public Transform anchor;
    public Module other; // can be null
    public bool isRevolute;
    public float angle;
}

public class Module : MonoBehaviour {
  // We don't group these into a single module so we can edit them in the inspector
  [SerializeField]
  public Connection[] connections;

  private Connection GetChildConnection(Connection connection) {
    // Returns the child's connection that corresponds to the given one
    Assert.IsTrue(connection.other.transform.IsChildOf(this.transform));
    foreach (var childConnection in connection.other.connections)
      if (childConnection.other == this)
        return childConnection;

    // Shouldn't happen
    Assert.IsTrue(false);
    return null;
  }

  private (Vector3, Quaternion) compose((Vector3 pos, Quaternion rot) tA, (Vector3 pos, Quaternion rot) tB) {
    // run A then B
    var rotation = tA.rot * tB.rot;
    return (tB.pos + rotation * tA.pos, rotation);
  }

  private (Vector3, Quaternion) invert(Vector3 position, Quaternion rotation) {
    return (-position, Quaternion.Inverse(rotation));
  }

  private void UpdateConnection(Connection connection) {
    var childConnection = GetChildConnection(connection);

    // We want to align the objects so that the anchors line up (rotated 180 deg around y)
    // connection.anchor is relative to us
    // childConnection.anchor is relative to child
    // child is relative to us
    
    var p = childConnection.anchor.localPosition;
    // this angle calculation is right
    var r = Quaternion.Euler(0, 180, connection.angle + childConnection.angle) * childConnection.anchor.localRotation;
    (var position, var rotation) = compose((connection.anchor.localPosition, connection.anchor.localRotation), invert(p, r));

    // position might not be right but not sure
    connection.other.transform.localPosition = rotation * position;
    connection.other.transform.localRotation = rotation;

    var childBody = connection.other.GetComponent<ArticulationBody>();
    childBody.matchAnchors = false;
    childBody.jointType = ArticulationJointType.RevoluteJoint;
    childBody.anchorPosition = childConnection.anchor.localPosition;
    childBody.anchorRotation = childConnection.anchor.localRotation;
    childBody.parentAnchorPosition = connection.anchor.localPosition;
    childBody.parentAnchorRotation = Quaternion.Euler(0, 180, 0) * connection.anchor.localRotation;
  }

  public void UpdateConnections() {
    foreach (var connection in connections) {
      if (connection.other != null && connection.other.transform.IsChildOf(this.transform)) {
        UpdateConnection(connection);
      }
    }
  }
}

[CustomEditor(typeof(Module))]
public class ModuleEditor : Editor {
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    Module script = (Module)target;
    if (GUILayout.Button("Update Child Positions")) {
      script.UpdateConnections();
    }
  }
}

