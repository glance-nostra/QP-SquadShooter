using UnityEngine;

namespace nostra.platform.share
{
    public class NostraShare : MonoBehaviour, INostraShare
    {
        public void Share(string _title, string _url, Texture2D _texture)
        {
            NativeShare share = new NativeShare();
            share.SetTitle(_title);
            share.AddFile(_texture);
            share.SetUrl(_url);
            share.Share();
        }
    }
}