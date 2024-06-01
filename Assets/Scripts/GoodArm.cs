using UnityEngine;
using UnityEditor;

public class GoodArm : MonoBehaviour
{
    public Lego lego;
    private InverseKinematics ik;

    private bool working = false;
    private float arrivedSince = 1e38f;

    void OnEnable()
    {
        ik = GetComponentInParent<InverseKinematics>();
    }

    public void Pickup()
    {
        working = true;
        ik.target.position = lego.end.position;
        ik.target.rotation = lego.end.rotation;
        arrivedSince = 1e38f;
    }

    void Attach()
    {
        lego.gameObject.transform.SetParent(ik.End().transform);
    }

    void Update()
    {
        if (!ik.Arrived)
        {
            arrivedSince = 1e38f;
        }
        else
        {
            arrivedSince = Mathf.Min(arrivedSince, Time.time);
        }

        if (working && (Time.time - arrivedSince) > 1f)
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


