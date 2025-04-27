using System;
using Cysharp.Threading.Tasks;
using DatAR.DataModels.Passables;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

public class DataflowInletOO : MonoBehaviour
{
    private IDisposable _dataSubscription;
    public readonly BehaviorSubject<Passable> dataOO;
    [SerializeField] private DataflowOutletR inputGameObjectS;

    [SerializeField] private MeshRenderer inlet;
    private Color _originalColor;

    [SerializeField] private LineRenderer dataLinkPrefab;
    private LineRenderer _dataLink;

    private TriggerPassthroughComponent _linkTrigger;

    public DataflowInletOO()
    {
        // Instantiate with an empty _data object
        dataOO = new BehaviorSubject<Passable>(
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
       if (inputGameObjectS)
        {
            _dataSubscription?.Dispose();

            // Pass on any events from the linked game object
            _dataSubscription = inputGameObjectS.GetComponent<DataflowOutletR>()
                .dataR
                .Sample(TimeSpan.FromMilliseconds(200)) // Don't take in more updates once per time unit
                .Subscribe(incomingDataS =>
                {
                    PulseIndicator();
                    dataOO.OnNext(incomingDataS);
                    DrawLink();
                });
        }
    }

    private void StartTriggerListenerOn(GameObject linkTrigger)
    {
        _linkTrigger = linkTrigger.AddComponent<TriggerPassthroughComponent>();
        
        _linkTrigger.collideTrigger.Subscribe((other) =>
        {
            
            var dataSenderS = other.GetComponent<MyControllerComponent>()?
                .myController?.GetComponent<DataflowOutletR>();
            if (!dataSenderS)
            {
                return;
            }

            SetInputGameObject(dataSenderS);
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

    public void SetInputGameObject(DataflowOutletR incomingGameObjectS)
    {
        if (incomingGameObjectS == inputGameObjectS)
        {
            return;
        }
        
       
        inputGameObjectS = incomingGameObjectS;
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
        if (!inputGameObjectS && _dataLink)
        {
            Destroy(_dataLink.gameObject);
            return;
        }

        if (_dataLink)
        {
            
            _dataLink.SetPosition(0, inputGameObjectS.outlet.transform.position);
        
            _dataLink.SetPosition(1, inlet.transform.position);

        }
    }
}
