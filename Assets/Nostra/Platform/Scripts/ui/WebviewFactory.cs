using System;
using UnityEngine;

namespace nostra.platform.webview
{
    public class WebviewFactory : MonoBehaviour, IWebviewFactory
    {
        [SerializeField] UniWebView webView;
        [SerializeField] GameObject blockerCanvas;

        Action<WebviewEventData> m_callback;
        WebViewType m_type;

        public Action OnWebviewOpened {get; set;} 

        private void Awake()
        {
            UniWebView.SetWebContentsDebuggingEnabled(true);
            blockerCanvas.SetActive(false);
            UniWebView.SetWebContentsDebuggingEnabled(true);
            webView.SetWindowUserResizeEnabled(true);
            webView.OnShouldClose += (view) =>
            {
                HideWebView();
                return false;
            };

            webView.OnMessageReceived += (view, message) =>
            {
                Debug.Log($"recieved: {message}");
                WebviewEventData webviewEventData = new WebviewEventData();
                webviewEventData.Event = WebviewEvent.OnMessageReceived;
                webviewEventData.Path = message.Path;
                webviewEventData.RawMessage = message.RawMessage;
                webviewEventData.Args = message.Args;
                m_callback?.Invoke(webviewEventData);
                OnWebviewOpened?.Invoke();
            };
        }
        public void OpenUrl(string url, Rect frameRect, WebViewType _type, Action<WebviewEventData> _callback)
        {
            m_type = _type;
            m_callback = _callback;
            webView.BackgroundColor = Color.clear;
            blockerCanvas.SetActive(true);
            webView.Frame = frameRect;
            webView.Load(url);
            webView.Show();
        }
        public void OpenUrl(string url, Rect frameRect, WebViewType _type, Action<WebviewEventData> _callback, CookieData[] cookie)
        {
            m_type = _type;
            m_callback = _callback;
            webView.BackgroundColor = Color.clear;
            blockerCanvas.SetActive(true);
            webView.Frame = frameRect;
            if(cookie != null)
            {
                var cookieString = "";
                for(int i=0; i < cookie.Length; i++)
                {
                    if(i != 0) cookieString += ",";
                    cookieString += $"{cookie[i].key}={cookie[i].value}";
                }
                UniWebView.SetCookie(url,cookieString, () => 
                {
                    Debug.Log($"cookie string: {cookieString}");
                });
            }
            webView.Load(url);
        }
        public void SetCookies(string _key, string _value, string url)
        {
            string cookieString = string.Empty;
            if (!string.IsNullOrEmpty(_key) && !string.IsNullOrEmpty(_value))
            {
                cookieString = $"{_key}={_value}";
            }
            UniWebView.SetCookie(url, cookieString, () =>
            {
                Debug.Log($"Cookie set: {cookieString} for URL: {url}");
            });
        }
        public void CallJS(string _jsmethod)
        {
            Debug.Log($"called : {_jsmethod}");
            webView.EvaluateJavaScript(_jsmethod);
        }

        public void ShowWebView()
        {
            webView.Show();
        }

        public async void HideWebView()
        {
            switch (m_type)
            {
                case WebViewType.COMMENTS:
                    CallJS("clearPostInfo");
                    break;
                case WebViewType.VAULT:
                    CallJS("clearVaultInfo");
                    break;
                case WebViewType.PROFILE:
                    CallJS("clearUserInfo");
                    break;
                case WebViewType.MULTIPLAYER_INVITE:
                    await UniWebView.ClearCookiesAsync();
                    break;
                default:
                    break;
            }
            m_type = WebViewType.NONE;
            webView.Hide();
            blockerCanvas.SetActive(false);
        }
    }
}