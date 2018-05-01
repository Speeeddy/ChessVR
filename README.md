This repository is a collection for code and implementation "Scenes" used in the Chess VR Project.

The software used is Unity for the Game Development and Visual Studio for debugging/editing. This repository is not complete in the sense that only the user-written code is uploaded. Other dependencies such as:
1. Google VR (GVR Library) (Version used: v1.140.0 https://github.com/googlevr/gvr-android-sdk/releases)
2. Simple Firebase Unity (Open-source Firebase wrapper in C# https://github.com/dkrprasetya/simple-firebase-unity)
3. GamePlan VFX Chess Set (https://assetstore.unity.com/packages/3d/2d-3d-chess-pack-93915)
4. Chess Board Textures (https://assetstore.unity.com/packages/3d/chess-board-textures-68969)
5. Cope! Free Skybox Pack (https://assetstore.unity.com/packages/2d/textures-materials/sky/cope-free-skybox-pack-22252)
are not uploaded here and can be downloaded separately.

NewGame.cs script is part of the GameObject Canvas of OpenScene scene.
CreateDB.cs script is part of GameObject Canvas inside CreateScene scene.
BoardManager.cs script is part of the GameObject Plane (1) inside GameSceneBlack and GameSceneWhite scenes.

All chess objects including planes and pieces are attached to the Plane (1) GameObject in the GameSceneBlack and GameSceneWhite using public variables of the BoardManager.cs script.

Made by: Nipun Sood and Amreen Shaikh, BITS Goa, India as project for the course "CSF314 Software Development for Portable Devices".
