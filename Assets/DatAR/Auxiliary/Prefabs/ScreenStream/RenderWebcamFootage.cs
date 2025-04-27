using System.Linq;
using UnityEngine;

public class RenderWebcamFootage : MonoBehaviour
{

    [SerializeField] private string webcamName = "OBS-Camera";

    void Start()
    {
        var devices = WebCamTexture.devices;
        
        // Use this code to find out what your webcam devices are called.
        // for( var i = 0 ; i < devices.Length ; i++ )
        //     Debug.Log(devices[i].name);
        
        var deviceExists = devices.ToList().Exists(webcam => webcam.name == webcamName);
        if (!deviceExists)
        {
            Debug.LogWarning("Provided webcam not found");
            gameObject.SetActive(false);
            return;
        }

        WebCamTexture webcamTexture = new WebCamTexture(devices.ToList()
            .Find(webcam => webcam.name == webcamName).name);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }
}
