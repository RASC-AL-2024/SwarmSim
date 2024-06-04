using UnityEngine;
using UnityEditor;
using System.Collections;

public class GoodArm : MonoBehaviour
{
    public Lego effector;
    private RevoluteRobot robot;

    public Lego act;
    public Lego zoink;

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
        return (tB.pos + tB.rot * tA.pos, tA.rot * tB.rot);
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
        yield return new WaitForSeconds(8f);
        robot.Target.transform.position += 0.37f * female.transform.TransformDirection(Vector3.up);
        yield return new WaitForSeconds(4f);

        // Attach
        lego.gameObject.transform.SetParent(robot.Joints[^1].transform);
    }

    public IEnumerator Place(Lego target, Transform targetFemale, Transform currentMale)
    {
        // (var pos, var rot) = compose(
        //     invert(effector.males[0].transform.localPosition, effector.males[0].transform.localRotation),
        //     (targetFemale.transform.position, targetFemale.transform.rotation));
        (var relPos, var relRot) = compose(
            (currentMale.transform.localPosition, currentMale.transform.localRotation),
            (current.transform.localPosition, current.transform.localRotation));

        (var pos, var rot) = compose(
            invert(relPos, relRot),
            (targetFemale.transform.position, targetFemale.transform.rotation));
        robot.Target.transform.position = pos;
        robot.Target.transform.rotation = rot;
        yield return new WaitForSeconds(8f);

        current.gameObject.transform.SetParent(target.transform);
        current = null;
    }

    IEnumerator Go()
    {
        yield return Pickup(act, act.females[0]);
        yield return Place(zoink, zoink.females[0], act.males[0]);
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


