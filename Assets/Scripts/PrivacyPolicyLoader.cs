using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PrivacyPolicyLoader : MonoBehaviour
{
    public string privacyLink;
    private UniWebView _privacyPolicyScreen;

    public void LoadURL()
    {
        if (_privacyPolicyScreen == null)
        {
            GameObject webViewGameObject = new GameObject("PrivacyPolicyScreen");
            _privacyPolicyScreen = webViewGameObject.AddComponent<UniWebView>();
            _privacyPolicyScreen.Frame = Screen.safeArea;
            _privacyPolicyScreen.OnPageFinished += OnPageFinished;
            _privacyPolicyScreen.OnShouldClose += OnShouldClose;
            _privacyPolicyScreen.EmbeddedToolbar.Show();
            _privacyPolicyScreen.SetAllowBackForwardNavigationGestures(true);
        }

        _privacyPolicyScreen.Load(privacyLink);
        _privacyPolicyScreen.Show();
    }

    GameObject _bg;

    private void OnPageFinished(UniWebView webView, int statusCode, string url)
    {
        GameObject bg = new GameObject("BG");
        bg.AddComponent<Image>();
        bg.GetComponent<Image>().color = Color.black;
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(2000, 5000);
        bg.transform.SetParent(GameObject.Find("UI").transform, false);
        _bg = bg;
        StartCoroutine(CloseScreen());
    }

    private bool OnShouldClose(UniWebView webView)
    {
        if(webView != null) 
            Destroy(webView.gameObject);
        webView = null;
        if(_bg != null)
            Destroy(_bg);
        StopAllCoroutines();
        return true;
    }

    public void CloseWebView()
    {
        if (_privacyPolicyScreen != null)
        {
            _privacyPolicyScreen.Hide();
            Destroy(_privacyPolicyScreen.gameObject);
            _privacyPolicyScreen = null;
        }
        if(_bg != null)
            Destroy(_bg);
        StopAllCoroutines();
    }

    IEnumerator CloseScreen(){
        Vector3 firstTouchPos, secondTouchPos;
        while(true){
            yield return new WaitWhile(() => !Input.GetMouseButtonDown(0));
            firstTouchPos = Input.mousePosition;
            Debug.Log(firstTouchPos);
            yield return new WaitWhile(() => !Input.GetMouseButtonUp(0));
            secondTouchPos = Input.mousePosition;
            Debug.Log(secondTouchPos);
            if((secondTouchPos - firstTouchPos).x > 200){
                CloseWebView();
            }
        }
    }
}