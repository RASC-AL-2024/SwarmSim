using UnityEngine;
using UnityEditor;
using System.Collections;

[System.Serializable]
public record Connections
{
    public Lego lego;
    public Transform attachFemale;
    public Transform attachMale;
    public Transform otherFemale;
}

public class GoodArm : MonoBehaviour
{
    public Lego effector;
    private RevoluteRobot robot;

    public Lego act;
    public Lego zoink;

    [SerializeField]
    public Connections[] connections;

    private bool working = false;
    private float arrivedSince = 1e38f;
    private Lego current = null;

    void OnEnable()
    {
        robot = GetComponentInParent<RevoluteRobot>();
    }

    public void WrappedPickup(Lego lego, Transform female)
    {
        StartCoroutine(Pickup(lego, female));
    }

    private (Vector3, Quaternion) compose((Vector3 pos, Quaternion rot) tA, (Vector3 pos, Quaternion rot) tB)
    {
        // perform A, then perform B
        return (tB.pos + tB.rot * tA.pos, tB.rot * tA.rot);
    }

    private (Vector3, Quaternion) invert(Vector3 position, Quaternion rotation)
    {
        var inv = Quaternion.Inverse(rotation);
        return (inv * -position, inv);
    }

    private (Vector3, Quaternion) invert((Vector3 position, Quaternion rotation) x)
    {
        return invert(x.position, x.rotation);
    }

    public IEnumerator Pickup(Lego lego, Transform female)
    {
        current = lego;
        (var pos, var rot) = compose(
            invert(effector.males[0].transform.localPosition, effector.males[0].transform.localRotation),
            (female.transform.position, female.transform.rotation));
        robot.Target.transform.position = pos;
        robot.Target.transform.rotation = rot;
        yield return new WaitForSeconds(4f);

        float end = Time.time + 2f;
        var old = robot.Target.transform.position;
        while (Time.time < end)
        {
            float t = 1 - (end - Time.time) / 2;
            robot.Target.transform.position = old + t * 0.37f * female.transform.TransformDirection(Vector3.up);
            yield return null;
        }

        // Attach
        lego.gameObject.transform.SetParent(robot.Joints[^1].transform);
    }

    public IEnumerator Place(Transform targetFemale, Transform currentMale)
    {
        (var relPos, var relRot) = compose(
            (currentMale.transform.localPosition, currentMale.transform.localRotation),
            (current.transform.localPosition, current.transform.localRotation));
        (var pos, var rot) = compose(
            invert(relPos, relRot),
            (targetFemale.transform.position, targetFemale.transform.rotation));
        robot.Target.transform.position = pos;
        robot.Target.transform.rotation = rot;
        Debug.Log(robot.Target.transform.eulerAngles);
        yield return new WaitForSeconds(4f);

        float end = Time.time + 2f;
        var old = robot.Target.transform.position;
        while (Time.time < end)
        {
            float t = 1 - (end - Time.time) / 2;
            robot.Target.transform.position = old + t * 0.37f * targetFemale.transform.TransformDirection(Vector3.up);
            yield return null;
        }

        current.gameObject.transform.SetParent(targetFemale.parent.transform);
        current = null;
    }

    IEnumerator Go()
    {
        foreach (var connection in connections)
        {
            yield return Pickup(connection.lego, connection.attachFemale);
            yield return Place(connection.otherFemale, connection.attachMale);
        }
        robot.Target.transform.localPosition = new Vector3(0, 2.5f, 0);
        robot.Target.transform.localRotation = Quaternion.identity;
    }

    public void GoWrapper()
    {
        StartCoroutine(Go());
    }
}

[CustomEditor(typeof(GoodArm))]
public class GoodArmEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GoodArm arm = (GoodArm)target;
        if (GUILayout.Button("Sync"))
        {
            arm.GoWrapper();
        }
    }
}


