using nostra.character;
using nostra.core.games;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class GameC : GamesController
    {
        [SerializeField] private GameController gameManager;
        [SerializeField] private GameObject gameCanvas;

        private NostraCharacter[] Characters = null;

        protected override void OnCardStateChanged(CardState _cardState)
        {
            Debug.Log("Current Card State : " + _cardState);
            switch (_cardState)
            {
                case CardState.LOADED:
                    gameCanvas.SetActive(false);
               //     gameManager.OnLoaded(gameCanvas);
                    break;
                case CardState.FOCUSED:
                    gameCanvas.SetActive(false);
                //    gameManager.OnFocussed(gameCanvas);
                    break;
                case CardState.START:
                 //   gameManager.OnStart(gameCanvas);
                    break;
                case CardState.PAUSE:
                //    gameManager.OnPause(gameCanvas);
                    break;
                case CardState.RESTART:
               //     gameManager.OnRestart(gameCanvas);
                    break;
                case CardState.REDIRECT:
                 //   gameManager.OnRedirect(gameCanvas);
                    break;
                case CardState.NEXT_LEVEL:
                  //  gameManager.OnNextLevel(gameCanvas);
                    break;
                case CardState.GAMEOVER_WATCH:
                    break;
                case CardState.HIDDEN:
                //    GameObjectPool.ClearPoolAndDestroy();
                    break;
            }
        }

        //protected override void OnReplayStart()
        //{
        //    Debug.Log("In OnReply");
        //    gameManager.ReplayButton();
        //    PlayerCharacterCustomise(Characters[0]);
        //}
    }

}