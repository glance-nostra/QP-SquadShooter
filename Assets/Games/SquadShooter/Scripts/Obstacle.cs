using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Obstacle : MonoBehaviour
    {
        [Space(10)]
        [Header("Obstacle type")]
        [SerializeField] private Type ThisObsType; // Find that which type of the object is

        [Space(10)]
        [Header("Hit managing")]
        [SerializeField] private int TotalHit; // How many bullet need to destroy the object
        private int HittedCount; // How many hits happend

        [Space(10)]
        [Header("Game Manager")]
        public GameController GameManager; // Gamemanager access

        [Space(10)]
        [Header("Catcus Obstacle")]
        public bool isCatcus;
        public int DamageAmount = 1;

        // Called on activation of object
        private void OnEnable()
        {
            HittedCount = 0; // Making count 0
        }

        // Called on object collide
        private void OnCollisionEnter(Collision collision)
        {
            // Things happend when it's exit gate
            if (ThisObsType == Type.ExitGate)
            {
                GameManager.RestartGame();
            }

            // Thingd happend when it's breakble object
            if (ThisObsType == Type.Breakable)
            {
                if (collision.gameObject.TryGetComponent<Bullet>(out Bullet bullet))
                {
                    Hitted();
                }
            }

            //if(ThisObsType == Type.Unbreakable)
            //{
            //    if(isCatcus)
            //    {
            //        if(collision.gameObject.TryGetComponent<Player_Manager>(out Player_Manager player))
            //        {

            //            player.ReduceHeath(DamageAmount);
            //        }
            //    }
            //}
        }

        // Called when bullet hitted
        void Hitted()
        {
            HittedCount++;
            if (HittedCount == TotalHit)
            {
                this.gameObject.SetActive(false);
            }
        }

        // Types of obstacle
        enum Type
        {
            Breakable,
            Unbreakable,
            ExitGate
        }

    }
}