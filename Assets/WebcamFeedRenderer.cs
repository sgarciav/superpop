using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Renders live webcam feed on a UI RawImage or material.
/// Optional: Can be used to display background in the TestMediaPipeViaUDP scene.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class WebcamFeedRenderer : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private RawImage rawImage;
    private bool isInitialized = false;

    [SerializeField] private int desiredWidth = 1280;
    [SerializeField] private int desiredHeight = 720;
    [SerializeField] private int targetFPS = 30;

    private void Start()
    {
        rawImage = GetComponent<RawImage>();
        InitializeWebcam();
    }

    private void InitializeWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("[WebcamFeedRenderer] No webcam devices found!");
            return;
        }

        // Use first available camera
        webcamTexture = new WebCamTexture(devices[0].name, desiredWidth, desiredHeight, targetFPS);
        webcamTexture.Play();
        
        rawImage.texture = webcamTexture;
        isInitialized = true;
        
        Debug.Log($"[WebcamFeedRenderer] Initialized with {devices[0].name} at {desiredWidth}x{desiredHeight}@{targetFPS}fps");
    }

    private void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
        }
    }

    public bool IsInitialized => isInitialized;
    public WebCamTexture GetWebcamTexture() => webcamTexture;
}
