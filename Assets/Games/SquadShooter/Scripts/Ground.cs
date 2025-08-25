using System.Collections.Generic;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Ground : MonoBehaviour
    {
        [Space(10)]
        [Header("All bot spawn point and move point")]
        public List<Transform> allSpawnPoint; // Positions of bot for each ground
        public int botCount; // Total bot needed in the ground
        public List<Transform> botRandomMove;

        [Space(10)]
        [Header("Player spawn point")]
        public Transform playerSpawnPos; // Position fot player for each ground

        [Space(10)]
        [Header("Geound active object")]
        public List<GameObject> ActiveObject; // Object list which should be active in the start of the match




        //Start


    }
}