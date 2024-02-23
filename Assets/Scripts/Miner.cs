using UnityEngine;
using System.Collections.Generic;

public class Miner : MonoBehaviour {
  // Where to scoop froom
  [SerializeField]
  public Transform minePosition;

  private List<Transform> waitingRovers;
  private IKStatus status;

  private Transform? activeLoading;

  void Start() {
    waitingRovers = new List<Transform>();
    status = new IKStatus(GetComponent<InverseKinematics>());
  }

  public void RegisterRover(Transform container) {
    Debug.LogFormat("Registered: {0}", container);
    waitingRovers.Add(container);
  }

  public void UnregisterRover(Transform container) {
    Debug.LogFormat("Unregistered: {0}", container);
    waitingRovers.Remove(container);
  }

  void Update() {
    bool converged = status.Step();

    // The rover left
    if (activeLoading != null && !waitingRovers.Contains(activeLoading)) {
      status.Target(minePosition);
      activeLoading = null;
      return;
    }

    // We have a new load
    if (converged && activeLoading == null && waitingRovers.Count > 0) {
      // Round robin
      activeLoading = waitingRovers[0]; 
      waitingRovers.RemoveAt(0);
      waitingRovers.Add(activeLoading);
      status.Target(activeLoading);
      return;
    }

    // We dropped off a load
    if (converged && activeLoading != null) {
      activeLoading = null;
      status.Target(minePosition);
      return;
    }
  }
}
