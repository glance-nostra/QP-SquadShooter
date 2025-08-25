using Fusion;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public struct PlayerInputData : INetworkInput
    {
        public float Horizontal;
        public float Vertical;

        public Vector2 ShootDirection;
        public bool ShootPressed;
    }
}