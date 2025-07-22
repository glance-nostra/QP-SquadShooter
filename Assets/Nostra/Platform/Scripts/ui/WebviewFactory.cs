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

        private void Awake()
        {
            blockerCanvas.SetActive(false);
            webView.OnShouldClose += (view) =>
            {
                HideWebView();
                return false;
            };
            webView.OnMessageReceived += (view, message) =>
            {
                WebviewEventData webviewEventData = new WebviewEventData();
                webviewEventData.Event = WebviewEvent.OnMessageReceived;
                webviewEventData.Path = message.Path;
                webviewEventData.RawMessage = message.RawMessage;
                webviewEventData.Args = message.Args;
                m_callback?.Invoke(webviewEventData);
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
        public void CallJS(string _jsmethod)
        {
            webView.EvaluateJavaScript(_jsmethod);
        }
        public void HideWebView()
        {
            switch(m_type)
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
                default:
                    break;
            }
            m_type = WebViewType.NONE;
            webView.Hide();
            blockerCanvas.SetActive(false);
        }
    }
}