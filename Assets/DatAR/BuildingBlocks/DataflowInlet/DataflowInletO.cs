using System;
using Cysharp.Threading.Tasks;
using DatAR.DataModels.Passables;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

public class DataflowInletO : MonoBehaviour
{
    private IDisposable _dataSubscription;
    public readonly BehaviorSubject<Passable> dataO;
    [SerializeField] private DataflowOutletL inputGameObject;


    [SerializeField] private MeshRenderer inlet;
    private Color _originalColor;

    [SerializeField] private LineRenderer dataLinkPrefab;
    private LineRenderer _dataLink;

    private TriggerPassthroughComponent _linkTrigger;

    public DataflowInletO()
    {
        // Instantiate with an empty _data object
        dataO = new BehaviorSubject<Passable>(
            new Passable());
    }
    
    void Start()
    {
        if (inlet)
        {
            _originalColor = inlet.material.color;
            
            // Currently the inlet is necessarily the trigger object.
            StartTriggerListenerOn(inlet.gameObject);
        }

        StartDataListener();
    }

    private void StartDataListener()
    {
        if (inputGameObject)
        {
            _dataSubscription?.Dispose();

            // Pass on any events from the linked game object
            _dataSubscription = inputGameObject.GetComponent<DataflowOutletL>()
                .dataL
                .Sample(TimeSpan.FromMilliseconds(200)) // Don't take in more updates once per time unit
                .Subscribe(incomingData =>
                {
                    PulseIndicator();
                    dataO.OnNext(incomingData);
                    DrawLink();
                });
        } 
    }

    private void StartTriggerListenerOn(GameObject linkTrigger)
    {
        _linkTrigger = linkTrigger.AddComponent<TriggerPassthroughComponent>();
        
        _linkTrigger.collideTrigger.Subscribe((other) =>
        {
            var dataSender = other.GetComponent<MyControllerComponent>()?
                .myController?.GetComponent<DataflowOutletL>();
            
            if (!dataSender)
            {
                return;
            }

            SetInputGameObject(dataSender);
        });
    }

    private async void PulseIndicator()
    {
        if (inlet)
        {
            inlet.material.color = Color.green;
            await UniTask.Delay(TimeSpan.FromSeconds(.5));
            inlet.material.color = _originalColor;
        }
    }

    public void SetInputGameObject(DataflowOutletL incomingGameObject)
    {
        if (incomingGameObject == inputGameObject)
        {
            return;
        }
        
        inputGameObject = incomingGameObject;
        
        StartDataListener();
    }

    private void OnDestroy()
    {
        _dataSubscription?.Dispose();
    }

    private void DrawLink()
    {
        if (_dataLink)
        {
            Destroy(_dataLink.gameObject);
        }
        _dataLink = Instantiate(dataLinkPrefab);
        UpdateLinkPosition();
    }

    private void LateUpdate()
    {
        UpdateLinkPosition();
    }

    private void UpdateLinkPosition()
    {
        if (!inputGameObject && _dataLink)
        {
            Destroy(_dataLink.gameObject);
            return;
        }

        if (_dataLink)
        {
            _dataLink.SetPosition(0, inputGameObject.outlet.transform.position);
           
            _dataLink.SetPosition(1, inlet.transform.position);
            

        }
    }
}
