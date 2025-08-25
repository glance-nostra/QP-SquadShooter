using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    public bool fixedJoystick;
    protected override void Start()
    {
        base.Start();
        if (fixedJoystick)
        {
            background.gameObject.SetActive(false);
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (fixedJoystick)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (fixedJoystick) { 
            background.gameObject.SetActive(false);
        }
        base.OnPointerUp(eventData);
    }
}