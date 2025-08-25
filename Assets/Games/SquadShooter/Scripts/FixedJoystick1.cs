using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{

    public class FixedJoystick : Joystick
    {
        Vector2 startpos;
        public Player_Shooting entity;

        public override void OnPointerDown(PointerEventData eventData)
        {
            startpos = eventData.position;
            //  base.OnPointerDown(eventData);

        }
        public override void OnDrag(PointerEventData eventData)
        {
            Vector2 direction = eventData.position - startpos;

            if (direction.magnitude > 1)
            {
                base.OnDrag(eventData);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                entity.transform.rotation = Quaternion.Euler(0, -angle + 90, 0);
                //need to roate the player acording to joystick
            }


        }
        public override void OnPointerUp(PointerEventData eventData)
        {

            // entity.Shoot();
            //if (startpos != eventData.position)
            //{


            //    Debug.Log("Draged");
            //}
            //else
            //{

            //}
            base.OnPointerUp(eventData);
        }
    }
}