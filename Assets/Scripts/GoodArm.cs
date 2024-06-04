using UnityEngine;
using UnityEditor;
using System.Collections;

public record Schedule(Vector3 startPos, Quaternion startRot, float startTime, Vector3 endPos, Quaternion endRot, float endTime)
{
    public (Vector3, Quaternion) Current(float time)
    {
        var t = Mathf.Clamp((time - startTime) / (endTime - startTime), 0, 1);
        return (Vector3.Lerp(startPos, endPos, t), Quaternion.Slerp(startRot, endRot, t));
    }

    public bool Done(float time)
    {
        return time > endTime;
    }
}

public class GoodArm : MonoBehaviour
{
    public Lego lego;
    public Lego effector;
    private RevoluteRobot robot;

    private bool working = false;
    private float arrivedSince = 1e38f;
    private Schedule schedule = null;

    void OnEnable()
    {
        robot = GetComponentInParent<RevoluteRobot>();
    }

    public void Pickup()
    {
        StartCoroutine(PickupImpl());
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

    // target.l2w = start.l2w
    // start.l2w = center.l2w @ s2.l2p
    // target.l2w = start.l2w @ s2.l2p^{-1}
    // just need to subtract local pos from target
    //
    // set target = goal -> center goes to goal
    // set target = goal . T -> center goals to goal . T
    // s2c = -5, target = T, goal = T + 5 implies center => T + 5, s2c = T
    //

    IEnumerator PickupImpl()
    {
        (var pos, var rot) = compose(
            invert(effector.start.transform.localPosition, effector.start.transform.localRotation),
            (lego.start.transform.position, lego.start.transform.rotation));
        robot.Target.transform.position = pos;
        robot.Target.transform.rotation = rot;
        yield return new WaitForSeconds(8f);
        robot.Target.transform.position += 0.37f * lego.start.transform.TransformDirection(Vector3.up);
        // robot.Target.transform.position = lego.end.transform.position;
        // robot.Target.transform.rotation = lego.end.transform.rotation;
        // yield return new WaitForSeconds(4f);
        // Attach();
        // robot.Target.transform.position += Vector3.up;
    }

    void Attach()
    {
        lego.gameObject.transform.SetParent(robot.Joints[^1].transform);
    }

    void Update()
    {
        // if (schedule != null && !schedule.Done(Time.time))
        // {
        //     (var p, var q) = schedule.Current(Time.time);
        //     Debug.Log(ik.PositionError());
        //     ik.target.position = p;
        //     ik.target.rotation = q;
        // }

        // if (!ik.Arrived)
        // {
        //     arrivedSince = 1e38f;
        // }
        // else
        // {
        //     arrivedSince = Mathf.Min(arrivedSince, Time.time);
        // }

        // if (working && schedule.Done(Time.time) && (Time.time - arrivedSince) > 1f)
        // {
        //     ik.target.position += new Vector3(0, 1f, 0);
        //     working = false;
        //     Attach();
        // }
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
            arm.Pickup();
        }
    }
}


