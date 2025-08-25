using System.Collections.Generic;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Grass : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] allgrass;
        [SerializeField] private Color InsideGrass, outsidegrass;
        public List<Entity> entered_player = new List<Entity>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //if(entered_player.Count > 1)
            //{
            //    bool playeravilabe = false;
            //    foreach (var item in entered_player)
            //    {
            //        if (item.is_player) 
            //        {
            //            playeravilabe = true;
            //            break;
            //        }

            //    }
            //    if (playeravilabe)
            //    {
            //        foreach (var item in entered_player)
            //        {
            //            item.BodyVisibility(true);
            //            item.insideGrass = false;
            //        }
            //    }
            //}
        }

        private void OnTriggerEnter(Collider other)
        {

            Entity entity = other.GetComponent<Entity>();
            if (entity)
            {
                entered_player.Add(entity);
                GetingintheGrass(entity);
                entity.EnteredGrass = this;
                //entity.insideGrass = true;
                //entity.BodyVisibility(InsideGrass);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity)
            {

                entered_player.Remove(entity);
                OutsideGrass(entity);
                //entity.BodyVisibility(outsidegrass);
                if (entity.EnteredGrass == this)
                {
                    entity.EnteredGrass = null;
                }
            }
        }
        public void GetingintheGrass(Entity enity)
        {
            for (int i = 0; i < allgrass.Length; i++)
            {
                allgrass[i].material.color = InsideGrass;
            }

        }

        public void OutsideGrass(Entity enity)
        {

            for (int i = 0; i < allgrass.Length; i++)
            {
                allgrass[i].material.color = outsidegrass;
            }

        }
    }
}