using UnityEngine;
using UnityEditor;

public class ImpactParticles : MonoBehaviour {
  // Hit the button in editor to trigger
  private ParticleSystem system;

  public void Start() {
    system = GetComponent<ParticleSystem>();
    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

    var systems = GetComponentsInChildren<ParticleSystem>();
    foreach (var childSystem in systems) {
      var main = childSystem.main;
      main.useUnscaledTime = true;
      main.simulationSpeed = 0.5f;
    }
  }

  public void Impact() {
    system.Play(true);
    SingletonBehaviour<GameMainManager>.Instance.impact = transform;
  }
}

[CustomEditor(typeof(ImpactParticles))]
public class ImpactParticlesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector

        ImpactParticles script = (ImpactParticles)target;

        // Add a custom button in the inspector
        if (GUILayout.Button("Trigger Particle Systems"))
        {
            script.Impact();
        }
    }
}
