using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class SoundManage : MonoBehaviour
    {
        [Space(10)]
        [Header("All Sound manager")]
        [SerializeField] private List<AudioSource> WholePlayer; // Player all audiosource
        [SerializeField] private List<BotSounds> AllBots; // All bots all audio source
        [SerializeField] private Sprite musicon, musicoff;
        [SerializeField] private AudioSource backgroundmusic;
        public List<AudioSource> bullets = new List<AudioSource>();
        // Change the sound volume
        public void LeaveRoom()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void SoundOnOff(float volume)
        {
            backgroundmusic.volume = volume;
            for (int playerAudios = 0; playerAudios < WholePlayer.Count; playerAudios++)
            {

                WholePlayer[playerAudios].volume = volume;


            }

            for (int botAudios = 0; botAudios < AllBots.Count; botAudios++)
            {
                for (int botAudio1 = 0; botAudio1 < AllBots[botAudios].BotAudiosource.Count; botAudio1++)
                {
                    AllBots[botAudios].BotAudiosource[botAudio1].volume = volume;
                }
            }

            foreach (AudioSource bullets_audio in bullets)
            {
                bullets_audio.volume = volume;
            }
        }
        public void SoundMute()
        {

        }
        // Play or Stop sound
        public void SoundPlayStop(int status)
        {
            if (status == 1)
            {
                backgroundmusic.Play();

                for (int playerAudios = 0; playerAudios < WholePlayer.Count; playerAudios++)
                {
                    WholePlayer[playerAudios].Play();
                }

                for (int botAudios = 0; botAudios < AllBots.Count; botAudios++)
                {
                    for (int botAudio1 = 0; botAudio1 < AllBots[botAudios].BotAudiosource.Count; botAudio1++)
                    {
                        AllBots[botAudios].BotAudiosource[botAudio1].Play();
                    }
                }



            }
            else if (status == 0)
            {
                backgroundmusic.Stop();
                for (int playerAudios = 0; playerAudios < WholePlayer.Count; playerAudios++)
                {
                    WholePlayer[playerAudios].Stop();
                }

                for (int botAudios = 0; botAudios < AllBots.Count; botAudios++)
                {
                    for (int botAudio1 = 0; botAudio1 < AllBots[botAudios].BotAudiosource.Count; botAudio1++)
                    {
                        AllBots[botAudios].BotAudiosource[botAudio1].Stop();
                    }
                }
            }
        }

    }
    [System.Serializable]
    public struct BotSounds
    {
        public List<AudioSource> BotAudiosource; // Bot audio source list

    }
}

