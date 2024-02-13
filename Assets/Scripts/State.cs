using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

[Serializable]
public class State
{
    [Serializable]
    public class SerializableVector2
    {
        public float x; public float y;
        public SerializableVector2(Vector2 vec)
        {
            x = vec.x;
            y = vec.y;
        }
    }

    [Serializable]
    public class SerializableQuaternion
    {
        public float x; public float y; public float z; public float w;
        public SerializableQuaternion(Quaternion q)
        {
            x = q.x; 
            y = q.y;
            z = q.z;
            w = q.w;
        }
    }

    [JsonIgnore]
    public Vector2 pos;

    [JsonIgnore]
    public Quaternion rot;

    public SerializableVector2 json_pos;
    public SerializableQuaternion json_rot;

    public State()
    {
        pos = Vector2.zero;
    }

    public State(Vector2 pos_, Quaternion rot_)
    {
        pos = pos_;
        rot = rot_;
    }

    public void serializeObject()
    {
        json_pos = new SerializableVector2(pos);
        json_rot = new SerializableQuaternion(rot);
    }
}
