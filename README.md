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
- `targets`: Comma-separated build targets (android,ios,standaloneosx,standalonewindows64,webgl)
- `apk=true`: This will build the standalone testing apk
- `landscape=true`: Pass this if your game is landscape

Note: Parameters like `env` and `targets` are only needed for the `build` command, not for `verify`.

#### Examples

Validates the game's scripts and DLL against QuickPlay guidelines.
```
verify
```

Builds and uploads addressables for Android and mac editor platform to the staging environment and also build the testing apk.
```
build env=staging targets=android,standaloneosx apk=true
```

Builds and uploads addressables for Android and iOS platform to the production environment.
```
build env=production targets=android,ios
```

The recommended workflow is to first run `verify`, and once successful, proceed with the `build` command.