using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TriggerPassthroughComponent : MonoBehaviour
{
    public Subject<Collider> collideTrigger = new Subject<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        collideTrigger.OnNext(other);
    }
}
