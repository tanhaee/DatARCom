using System;
using Cysharp.Threading.Tasks;
using DatAR.DataModels.Passables;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

public class DataflowOutletR : MonoBehaviour
{
    public readonly BehaviorSubject<Passable> dataR;

    public MeshRenderer outlet;
    private Color _originalColor;

    public DataflowOutletR()
    {
        // Instantiate with an empty _data object
        dataR = new BehaviorSubject<Passable>(
            new Passable());
    }

    void Start()
    {
        if (outlet)
        {
            _originalColor = outlet.material.color;
            var tag = outlet.gameObject.AddComponent<MyControllerComponent>();
            tag.myController = gameObject;
        }
    }

    public void Send(Passable newData)
    {
        PulseIndicator();
        dataR.OnNext(newData);
    }

    private async void PulseIndicator()
    {
        if (outlet)
        {
            outlet.material.color = Color.cyan;
            await UniTask.Delay(TimeSpan.FromSeconds(.5));
            outlet.material.color = _originalColor;
        }
    }
}
