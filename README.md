# QuickPlay Game Repo

This README provides information on how to set up, build and deploy the game using the available pipeline.

## Table of Contents

- [Repository Structure](#repository-structure)
- [Prerequisites](#prerequisites)
- [GitHub Actions Workflows](#github-actions-workflows)
  - [Game Processing Workflow](#game-processing-workflow)
  - [Examples](#examples)
- [Addressables](#addressables)



## Repository Structure

- `/Assets/Games/<GameName>` - Put your game-specific code and assets in this location. Make sure the game folder name follows Pascal casing without any space.
- `game_config_template.json` - Configuration file for the game.

## Prerequisites

Before pushing the code for final review, ensure you have set up the following:

### 1. Assembly Definition File (<GameName>.asmdef)

An Assembly Definition file is required to properly isolate and compile game-specific code. Create this using:

1. Open the Unity Editor
2. Navigate to **Nostra -> Tools -> Assembly Definition Builder** 
3. Fill in the required fields for your game
4. Add any dependencies specific to your game in this file

The asmdef file is crucial for proper DLL extraction in the QuickPlay pipeline.

### 2. Game Configuration File (game_config_template.json)

This file must be properly configured before submitting code or running any pipeline scripts. Ensure all paths and settings are correct for your game, as this configuration is essential for the pipeline.


## GitHub Actions Workflows

### Game Processing Workflow

The repository includes a GitHub Actions workflow that automates the process of building, testing, and deploying game DLLs and addressable assets.

#### Key Features

- **DLL Extraction**: Extracts game DLL code from the Unity project
- **DLL Verification**: Validates DLL against rules and best practices
- **Addressable Assets Building**: Builds addressable assets for different platforms
- **Deployment**: Uploads assets to CDN
- **Commenting**: Provides feedback as PR comments with verification results

#### Workflow Triggers

- Triggered manually via PR comments
- Two distinct commands: `verify` and `build`


**Command Purposes:**

- `verify`: Validates the game scripts and DLL to ensure they follow QuickPlay guidelines. This step is required before building.
- `build`: Builds the addressable assets and uploads both the addressables and DLL. This should only be run after a successful verification.

**Parameters for `build` command:**

- `env`: Environment to target (staging, production)
- `targets`: Comma-separated build targets (android, ios, webgl, standaloneosx)

Note: Parameters like `env` and `targets` are only needed for the `build` command, not for `verify`.

#### Examples

Validates the game's scripts and DLL against QuickPlay guidelines.
```
verify
```

Builds and uploads addressables for Android platform to the staging environment.
```
build env=staging targets=android
```

Builds and uploads addressables for Android and iOS platform to the production environment.
```
build env=production targets=android,ios
```

The recommended workflow is to first run `verify`, and once successful, proceed with the `build` command.


# Building Quick-Play Games

### Quick-Play Submission Guidelines

A Quick-Play game should be submitted as an **addressable asset bundle** along with game code compiled into **DLL**, the addressable asset bundle must contain the **game** prefab.

The game prefab would act as the entry point for the game on the Nostra Quick-Play platform, it should contain the `GamesController` component which is responsible for establishing an interface between the game and the QP Platform.`GamesController` is defined in the `nostra-qp-sdk` which is already included in this platform repository.

#### 1. Quick-Play Game Constraints

There are some constraints that needs to followed while building a Quick-Play game, which are as follows:

- The game must be a **single scene** game, which means the game should not have multiple scenes.
- The game must use only the set of **Tags** and **Layers** defined in the platform.
- Do not include any new third party packages or plugins into the project, use the ones already included in the platform, if any new packages are required contact the platform Dev team.
- Do not use Unity PlayerPrefs, instead use PlayerPrefs system provided by the nostra-qp-sdk
   ```csharp
      public class MyGameController : GamesController
      {
            void SavePlayerDatat()
            {
               GetPlayerPrefsManager().SetInt("PlayerScore", 100);
            }
      }
   ```
- Do not change Time.timeScale as that can cause the platform to freeze or behave unexpectedly.
- Game must use Unity's new input system, and should not use the legacy input system.
- Each game class must have a name space that follows the Format: `nostra.studioName.gameName`
- Game code should not use functions like `FindObjectByName` as it can give unexpected results in the quick-play environment, with multiple games loaded at the same time.
- Game should use *Universal Render Pipeline* (URP) for rendering.

#### 2. Quick-Play Game Feature Expectations

- Game should have an **auto-play** (preview) mode where the game plays itself without user input, to be used for the game card preview.
- Game should enter play mode when the user clicks play button on play card without any intermediate screen.
- There should be a **watch** (Record/Replay) using *Chrono Stream* recording framework.

#### 3. General Coding Guidelines
- Avoid frequent instantiation of game objects, use object pooling where possible.
- Do not use `DontDestroyOnLoad` as it can cause issues with the platform's scene management.

### Nostra Quick-Play SDK Integration

#### 1. Interfacing with Quick-Play Platform, Via `GamesController`

The "game" prefab must have a component attached to it that need to derive from `GamesController`, which is the entry point for the game on the Nostra Quick-Play platform.

You must override the `OnCardStateChanged` method in your GamesController class to handle the game state changes. The `CardState` enum defines the various states that a game can be in, such as `CardState.PLAY`, `CardState.PAUSE`, `CardState.STOP`, etc.

```csharp
    protected override void OnCardStateChanged(CardState _cardState)
        {
            switch (_cardState)
            {
                case CardState.LOADED:
                    gameManager.OnLoaded();
                    break;
                case CardState.FOCUSED:
                    gameCanvas.SetActive(false);
                    gameManager.OnFocussed();
                    break;
                case CardState.START:
                    gameCanvas.SetActive(true);
                    gameManager.OnStart();
                    gameRecorder.StartRecording();
                    break;
                case CardState.PAUSE:
                    gameCanvas.SetActive(false);
                    gameManager.OnPause();
                    break;
                case CardState.RESTART:
                    gameCanvas.SetActive(true);
                    gameManager.onRestart();
                    break;
                case CardState.REDIRECT:
                    gameCanvas.SetActive(true);
                    gameManager.OnStart();
                    break;
                case CardState.GAMEOVER_WATCH:
                    break;
            }
        }
```

#### 2. Using "Nostra Characters" in Quick-Play Games 

`GamseController` let's you request any amount of **Nostra Character** from platform to be used in your game
```csharp
public void GetCharacters()
{
      // Request 5 Nostra Characters from the platform
      NostraCharacter[] characters = GetGameCharacters(5);

      //Apply player customisation to character
      PlayerCharacterCustomise(charactes[0]); //Important: must apply player customisation to player character
      NostraCharacter player = characters[0];

      //Apply random character to bots/NPCs
      for (int i = 1; i < characters.Length; i++)
      {
         NostraCharacter bot = characters[i];
         PlayerRandomCustomisation(bot);
      }
}
```

funtions like `GetGameCharacters`,`PlayerCharacterCustomise` and `PlayerRandomCustomisation` are defined in the `GamesController` base class in nostra-sdk, which you can use to request characters and apply customisation.

Once you have received the characters from the platform, you can replace the default `RuntimeAnimationController` used by the nostra character with your own, given that your animation controller is compatible with the nostra character's character rig.

```csharp
[SerializeField] private RuntimeAnimatorController playerAnimationController; // Your custom animator controller
[SerializeField] private RuntimeAnimatorController botAnimationController; // Your custom animator controller for bots

public void GetCharacters()
{
      // Request 5 Nostra Characters from the platform
      NostraCharacter[] characters = GetGameCharacters(5);

      //Apply player customisation to character
      PlayerCharacterCustomise(charactes[0]); //Important: must apply player customisation to player character
      NostraCharacter player = characters[0];

      player.SetAnimatorController(playerAnimationController); // Set your custom animator controller

      //Apply random character to bots/NPCs
      for (int i = 1; i < characters.Length; i++)
      {
         NostraCharacter bot = characters[i];
         PlayerRandomCustomisation(bot);
         bot.SetAnimatorController(botAnimationController); // Set your custom animator controller for bots
      }
}
```

#### 3. Using PlayerPrefs in Quick-Play Games

Quick-Play games should use the `PlayerPrefsManager` provided by the `GamesController` to store and retrieve player preferences. This ensures that player data is correctly synced with the platform.

```csharp
public class MyGameController : GamesController
{
    void SavePlayerData()
    {
        // Save player score
        GetPlayerPrefsManager().SetInt("PlayerScore", 100);
        
        // Save player name
        GetPlayerPrefsManager().SetString("PlayerName", "Player1");
    }

    void LoadPlayerData()
    {
        // Load player score
        int score = GetPlayerPrefsManager().GetInt("PlayerScore", 0);
        
        // Load player name
        string name = GetPlayerPrefsManager().GetString("PlayerName", "Guest");
    }
}
```

> Do not use Unity's `PlayerPrefs` directly, as it may not work correctly in the Quick-Play environment. Always use the `GetPlayerPrefsManager()` method provided by the `GamesController` to access player preferences.

#### 4. Invoking Game Over Leaderboard 

when your game ends, you must invoke the game over screen thought the platform. The platform would show the users a leader board with all the participants listed. Following code snippet explains how to do that

```csharp
public class MyGameController : GamesController {
    public void OnGameOver(){
        GameOverLeaderboard leaderboard = new GameOverLeaderboard();
                    foreach (PlayerData player in playersList)
                    {
                        GameOverRank rank = new GameOverRank();
                        rank.playerName = player.PlayerName;
                        rank.playerScore = player.platformIndex;
                        rank.isPlayer = player.isPlayer;
                        leaderboard.lb.Add(rank);
                    }
        GameOverScreen(leaderboard, hasNextLevel);
    }
}
```
<p align="center">
   <img src="./docs/GameOver.jpeg" alt="Invite Option" height="400"/>
</p>

### Testing Game in Quick-Play Environment

Once you have your game prefab ready with the `GamesController` component, you can test your game in the Quick-Play environment by following these steps:

1. **Make your game prefab an addressable asset**: Ensure your game prefab is marked as an addressable asset in Unity. This allows the Quick-Play platform to load your game dynamically.

<p align="center">
   <img src="./docs/adressableGamePrefab.png" alt="Invite Option" height="200"/>
</p>

2. **Open QuickPlay Scene**: Open the `QuickPlayController` scene in the Unity editor.
3. **Add an entry in TestPosts in QuickPlayController**: Add a new entry with the game name and addressable path

<p align="center">
   <img src="./docs/TestPost.png" alt="Test Post" height="400"/>
</p>

4. **Run the scene**: Press play in the Unity editor to start the Quick-Play platform. Your game should appear as first post in the Quick-Play feed.

<p align="center">
   <img src="./docs/GamePost.png" alt="Quick Play Feed" height="400"/>
</p>

### Submitting Games In DLL and Addressable Asset Bundle, Combination

We understand that developer might not feel comfortable with directly sharing there game source code, hence we have created tools to compile your game code into a DLL and package your game prefab as an addressable asset bundle. This allows you to submit your game without sharing the source code directly. In order to ready your game for submission, follow these steps:

1. Put all you game scripts and prefabs in any one directory in your Assets folder
2. Create an assembly definition at the root of this directory, the preferred way is using Nostra Assembly Definition builder provided in the platform but you can create it manually as well. (ignore this step if you have one already)

    Open the Nostra Assembly Definition builder from the top menu: `Nostra > Assembly Definition Builder`

<p align="center">
   <img src="./docs/assemDef.png" alt="Quick Play Feed" height="200"/>
</p>

3. Once you have the assmebly definition window open add all the required details and click **"Create Assembly Definition"**, add any missing references in the Assembly definition file if required, wait for everything to compile and add missing refernces if any

4. Now click **"Extract DLL from Assembly Definition"**, that will replace the assembly definition with a .dll file

5. **Remapping to Dll:** now your game code is compiled into dll and ready to be shared! but the issue is that your prefabs still refer to the original **.cs** files so they have to be remapped to refer the newly generated **.dll** file before they can be build into adressables. To automate this process you can use the Remapping tool provided in platform got to *Nostra > Remap Prefabs*. In the assmebly name fild add your dll name and int the target directory field select the directory that contains all your prefabs and click **Remap Assets** button

<p align="center">
   <img src="./docs/Remapper.png" alt="Invite Option" height="200"/>
</p>

Now verify that your prefab components, they should be replaced by the ones defined in dll assembly.

> Warning!
> Make sure you back your project before doing the remap step because this final step is not reversible.

# Multiplayer Support

### Multiplayer Flow

The platform manages matchmaking and player data sharing for all quick-play games.To enable the invite option and multiplayer features for your game, it must be marked as multiplayer on the platform. Once marked, you will see an invite option on your game card.

<p align="center">
   <img src="./docs/inviteOption.png" alt="Invite Option" height="400"/>
</p>

Users can invite friends to play together using the invite option, or use quick play to match with a random user.

<p align="center">
   <img src="./docs/inviteList.png" alt="Invite Option in Game Card" height="400"/>
</p>

When a user sends invites to friends or selects quick play, the platform creates a room and sends the invites.  
The platform also handles spawning the *Nostra Character* for each player, syncing user data, and managing character customization.

## Handling Multiplayer Game Start

For multiplayer games, after the user completes the invite flow, the `CardState` will change to `CardState.START_MULTIPLAYER`.  
Multiplayer games must handle this state change in their `GamesController` class:

```csharp
public override void OnCardStateChanged(CardState state)
{
    base.OnCardStateChanged(state);
    if (state == CardState.START_MULTIPLAYER)
    {
        StartMultiplayerGame();
    }
}
```

### Spawning Multiplayer Nostra Character

Games can access the spawned *Nostra Character* for the local player using the `LoadPlayer` method defined in the `GamesController` base class in nostra-sdk.  
The spawned character will have Fusion components like `NetworkObject`, `NetworkTransform`, and `NetworkMechAnimator` attached, so you can use it as a standard Fusion object.

### Defining Multiplayer Game Config

Individual games can define their multiplayer game configuration by overriding the `GameContext` field in the `GameController` class:

```csharp
public override MultiplayerGameContext GameContext => new MultiplayerGameContext() 
{
      sessionProperties = new Dictionary<string, SessionProperty>(), // Session properties defined cretiong multiplayer game session
      matchMaker = new MyCustomMatchMaker(), // Custom matchmaker implementation
      playerAnimatorController = animtor, // Animator controller for player character
};
```

- **sessionProperties**: Define any session properties that you want to set when creating a multiplayer game session.
- **matchMaker**: Implement a custom matchmaker by deriving from `IMatchMaker` interface to handle matchmaking logic.
- **playerAnimatorController**: Set the animator controller for the player character. This is used to sync animations across the network.

### Registering Custom Network Runner Callbacks for Fusion Network Runner

If you need to register your own `INetworkRunnerCallbacks` for the Fusion Network runner (when creating the `NetworkRunner`), you can do so by overriding the `GetRequiredCallbacks` method in the `GameController` class:

```csharp
public override List<INetworkRunnerCallbacks> GetRequiredCallbacks()
{
    base.RegisterNetworkCallbackHandlers(runner);
    return new List<INetworkRunnerCallbacks>()
    {
        callbackHandler1,
        callbackHandler2,
    };
}
```

*Platform repo for quickplay*
