using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class DatARService : MonoBehaviour
{
    #region Singleton
    private static DatARService instance = null;
    public static DatARService Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<DatARService>();
                if(instance == null)
                {
                    GameObject go = new GameObject("DatARUtilSingleton");
                    go.AddComponent<DatARService>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
    #endregion

    [HideInInspector] public GameObject currentFocusTarget;

    private void Start()
    {
        // Disable to GazeProvider as it isn't relevant to DatAR and gets in the way of the outline generation.
        var gazeProvider = Camera.main.GetComponent<GazeProvider>();
        if(gazeProvider != null)
        {
            gazeProvider.enabled = false;
        }
    }

    void Update()
    {   // Only necessary if built using the Debug configuration, as Unity inserts a weird
        // developer console every update loop in this configuration. This is the only way to turn it off.
        Debug.developerConsoleVisible = false;
    }
}
