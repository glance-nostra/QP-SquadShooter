using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class SafeArea : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect safearea;
        Vector2 minancher, maxancher;
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            safearea = Screen.safeArea;

            minancher = safearea.position;
            maxancher = minancher + safearea.size;
            minancher.x /= Screen.width;
            minancher.y /= Screen.height;
            maxancher.x /= Screen.width;
            maxancher.y /= Screen.height;

            rectTransform.anchorMin = minancher;
            rectTransform.anchorMax = maxancher;
        }
    }
}