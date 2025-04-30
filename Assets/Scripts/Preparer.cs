using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Preparer : MonoBehaviour
{
    private string folderName = "aso_screenshots";

    private void Awake() {
        folderName = $"aso_{Application.productName}";
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(TakeScreenshot());
        }
    }

    private IEnumerator TakeScreenshot()
    {
        yield return new WaitForEndOfFrame();

        string randomString = GetRandomString(8);
        string screenshotName = $"s_{randomString}.png";

        string projectRootPath = Directory.GetParent(Application.dataPath).FullName;
        string folderPath = Path.Combine(projectRootPath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, screenshotName);

        ScreenCapture.CaptureScreenshot($"{filePath}");
        Debug.Log($"Screenshot saved: {filePath}");
    }

    private string GetRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] stringChars = new char[length];
        System.Random random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }
}