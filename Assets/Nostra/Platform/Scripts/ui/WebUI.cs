using System;
using System.Collections;
using Newtonsoft.Json;
using nostra.webUI;
using UnityEngine;
using UnityEngine.Networking;

namespace nostra.platform.webview
{
    public class WebUI : MonoBehaviour, IWebUI
    {
        const string MODAL_PATH = "Modal/index.html";

        [SerializeField] UniWebView webView;


        string EscapeForJS(string s) =>
            s.Replace("\\", "\\\\")
             .Replace("'", "\\'")
             .Replace("\"", "\\\"")
             .Replace("\n", "")
             .Replace("\r", "");

        public void ShowModal(Modal modal)
        {
            ShowModal(modal.title, modal.message, modal.actions);
        }


        private void ShowModal(string title, string message, ModalAction[] actions)
        {
            title = EscapeForJS(title);
            message = EscapeForJS(message);

            var actionsWV = new ModalActionWV[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                actionsWV[i] = new ModalActionWV
                {
                    id = i,
                    label = actions[i].label,
                    type = actions[i].type
                };
            }

        #if UNITY_ANDROID && !UNITY_EDITOR
            // Android: use UnityWebRequest to read from StreamingAssets (inside APK)
            string assetPath = System.IO.Path.Combine(Application.streamingAssetsPath, MODAL_PATH);
            StartCoroutine(LoadAndShowModal(assetPath, title, message, actions, actionsWV));
        #else
            // PC/Editor: direct file access
            var assetPath = System.IO.Path.Combine(Application.streamingAssetsPath, MODAL_PATH);
            var htmlContent = System.IO.File.ReadAllText(assetPath);
            LoadWebViewContent(htmlContent, title, message, actions, actionsWV);
        #endif
        }

        private IEnumerator LoadAndShowModal(string path, string title, string message, ModalAction[] actions, ModalActionWV[] actionsWV)
        {
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string htmlContent = www.downloadHandler.text;
                LoadWebViewContent(htmlContent, title, message, actions, actionsWV);
            }
            else
            {
                Debug.LogError("Failed to load HTML: " + www.error);
            }
        }

        private void LoadWebViewContent(string htmlContent, string title, string message, ModalAction[] actions, ModalActionWV[] actionsWV)
        {
            webView.LoadHTMLString(htmlContent, null);
            webView.Show(fade: true, duration: 0.2f);
            webView.OnMessageReceived += (view, msg) =>
            {
                Debug.Log("JS message received: " + msg.Path);

                if (msg.Path == "READY")
                {
                    string js = $"window.openUnityModal('{title}', '{message}', {JsonConvert.SerializeObject(actionsWV)})";
                    webView.EvaluateJavaScript(js);
                }

                if (msg.Path == "ACTION_CLICKED")
                {
                    if (msg.Args.TryGetValue("id", out var idStr) && int.TryParse(idStr, out int id))
                    {
                        if (id >= 0 && id < actions.Length)
                        {
                            actions[id].onClick?.Invoke();
                        }
                        else
                        {
                            Debug.LogWarning($"Invalid action ID: {id}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No valid action ID found in message args.");
                    }
                    webView.Hide(fade: true, duration: 0.2f);
                }
            };
        }

    }

    [Serializable]
    public class ModalActionWV
    {
        public int id;
        public string label;
        public string type;
    }
}
