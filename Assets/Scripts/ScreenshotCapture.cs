using UnityEngine;
using System.IO;
using static UnityEngine.ScreenCapture;

public class ScreenshotCapture : MonoBehaviour
{
    public KeyCode captureKey = KeyCode.Space;
    public StereoScreenCaptureMode eyeMode = StereoScreenCaptureMode.BothEyes;

    void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"Screenshot_{eyeMode}_{timestamp}.png";

#if UNITY_ANDROID && !UNITY_EDITOR
            string folderPath = Path.Combine(Application.persistentDataPath, "Screenshots");
#else
            string folderPath = Path.Combine(Application.dataPath, "Screenshots");
#endif

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fullPath = Path.Combine(folderPath, filename);
            ScreenCapture.CaptureScreenshot(fullPath, eyeMode);
            Debug.Log($"Screenshot saved to: {fullPath}");
        }
    }
}

