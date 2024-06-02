using UnityEngine;
using UnityEditor;

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
    private InverseKinematics ik;

    private bool working = false;
    private float arrivedSince = 1e38f;
    private Schedule schedule = null;

    void OnEnable()
    {
        ik = GetComponentInParent<InverseKinematics>();
    }

    public void Pickup()
    {
        working = true;
        schedule = new Schedule(ik.target.position, ik.target.rotation, Time.time, lego.end.position, lego.end.rotation, Time.time + 20f);
        arrivedSince = 1e38f;
    }

    void Attach()
    {
        lego.gameObject.transform.SetParent(ik.End().transform);
    }

    void Update()
    {
        if (schedule != null && !schedule.Done(Time.time))
        {
            (var p, var q) = schedule.Current(Time.time);
            Debug.Log(ik.PositionError());
            ik.target.position = p;
            ik.target.rotation = q;
        }

        if (!ik.Arrived)
        {
            arrivedSince = 1e38f;
        }
        else
        {
            arrivedSince = Mathf.Min(arrivedSince, Time.time);
        }

        if (working && schedule.Done(Time.time) && (Time.time - arrivedSince) > 1f)
        {
            ik.target.position += new Vector3(0, 1f, 0);
            working = false;
            Attach();
        }
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


