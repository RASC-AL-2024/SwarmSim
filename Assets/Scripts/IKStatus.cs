using UnityEngine;
using System.Collections;

public class MovingAverage
{
    private float? current = null;
    private float factor;
    private int count = 0;

    public MovingAverage(float factor_)
    {
        factor = factor_;
    }

    public void update(float x)
    {
        current = current ?? x;
        current = factor * x + (1 - factor) * current;
        ++count;
    }

    public int Count() => count;

    public float? Average => current;

    public void clear()
    {
        current = null;
        count = 0;
    }
}

public interface IKStatusAbstract
{
    public abstract void Target(Transform target);
    public abstract bool Step();
}

public static class IKStatusFactory
{
    public static IKStatusAbstract getIKStatus(InverseKinematics ik, bool is_fast)
    {
        if (is_fast)
        {
            return new IKStatusFast();
        }
        else
        {
            return new IKStatus(ik);
        }
    }
}

public class IKStatusFast : IKStatusAbstract
{
    public void Target(Transform target)
    {
        // noop
    }

    public bool Step()
    {
        return true;
    }
}

public class IKStatus : IKStatusAbstract
{
    InverseKinematics ik;
    MovingAverage errorChange;
    float? oldError = null;

    public IKStatus(InverseKinematics ik)
    {
        this.ik = ik;
        errorChange = new MovingAverage(0.01f);
    }

    public void Target(Transform target)
    {
        ik.target = target;
        oldError = null;
        errorChange.clear();
    }

    // returns true if converged
    public bool Step()
    {
        var newError = ik.Error().magnitude;
        var delta = newError - (oldError ?? newError);
        errorChange.update(delta);
        oldError = newError;

        return newError <= 0.05 || (errorChange.Count() > 120 && errorChange.Average <= 0.0001);
    }
}

