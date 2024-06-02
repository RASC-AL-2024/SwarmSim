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

    IEnumerator PickupImpl()
    {
        robot.Target.transform.position = lego.start.transform.position;
        robot.Target.transform.rotation = lego.start.transform.rotation;
        yield return new WaitForSeconds(6f);
        robot.Target.transform.position = lego.end.transform.position;
        robot.Target.transform.rotation = lego.end.transform.rotation;
        yield return new WaitForSeconds(4f);
        Attach();
        robot.Target.transform.position += Vector3.up;
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


