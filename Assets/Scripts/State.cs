using UnityEngine;
using System.Collections;

public abstract class State : MonoBehaviour {
    public MainDevice mainDevice
    {
        get {
            try {
                return GetComponentInParent<MainDevice>();
            } catch {
                return null;
            }
       }
    }
}
