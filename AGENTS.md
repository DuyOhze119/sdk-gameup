# AGENTS.md

## Cursor Cloud specific instructions

This repository is a **Unity 2022.3.62f2** project (the "GameUp SDK" plus the
"Backpack Tower Defense" host game). It is a mobile (Android/iOS) Unity app:
there is **no backend, web server, database, cache, or queue**, and there are
**no automated test suites** (no `[Test]`/PlayMode/EditMode assemblies in
`Assets/`). All third-party SDKs (LevelPlay, Firebase, AdMob, AppsFlyer,
GameAnalytics, AppMetrica, MaxSdk, EDM4U) are vendored in-tree under `Assets/`,
so opening the project does not require fetching them. UPM packages are pinned in
`Packages/manifest.json` / `Packages/packages-lock.json` and are restored by the
Editor on first open (two of them are git-URL deps, so the first open needs
network access to GitHub).

### Unity Editor location (pre-installed in the VM snapshot)

- The matching Editor is installed at
  `/opt/unity/2022.3.62f2/Editor/Unity` (Linux, includes the built-in
  `LinuxStandaloneSupport` playback engine — enough to build/run a Linux
  Standalone player). No Android/iOS build modules are installed.
- It is NOT on `PATH`. Invoke it by full path, e.g.
  `/opt/unity/2022.3.62f2/Editor/Unity -version`.
- Do not reinstall it in the update script — it is large (~4 GB extracted) and
  lives in the snapshot.

### License activation is REQUIRED before anything will run (the main gotcha)

The Editor refuses to do anything (compile, build, run, headless tests) until a
license is activated. A fresh VM has **no license**, so batchmode runs fail with:

```
No valid Unity Editor license found. Please activate your license.
```

Activate using ONE of the following (credentials must be provided as secrets):

1. Account sign-in (Personal or Pro). Requires `UNITY_EMAIL` + `UNITY_PASSWORD`
   (and `UNITY_SERIAL` for Pro/Plus):
   ```
   /opt/unity/2022.3.62f2/Editor/Unity -batchmode -nographics -quit \
     -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" \
     [-serial "$UNITY_SERIAL"] -logFile /tmp/unity_activate.log
   ```
   (Accounts with 2FA cannot sign in headlessly — use the manual `.ulf` path.)

2. Manual license file. Generate an `.alf` with
   `Unity -batchmode -nographics -quit -createManualActivationFile`, upload it at
   <https://license.unity3d.com/manual> to get a `.ulf`, then:
   ```
   /opt/unity/2022.3.62f2/Editor/Unity -batchmode -nographics -quit \
     -manualLicenseFile /path/to/Unity_lic.ulf -logFile /tmp/unity_activate.log
   ```

Activation persists in `~/.local/share/unity3d/Unity/` and the system licensing
store; re-activate if a fresh VM does not have it.

### Running / building (after activation)

There is no `npm run dev` equivalent. Typical headless actions, run from the repo
root (`/workspace`), each needing the license:

- Compile-check / let the Editor import & resolve packages:
  ```
  /opt/unity/2022.3.62f2/Editor/Unity -batchmode -nographics -quit \
    -projectPath /workspace -logFile /tmp/unity_open.log
  ```
- Build a Linux Standalone player (best "run the app" option in this VM): write a
  small editor build method (e.g. `BuildScript.PerformLinuxBuild` calling
  `BuildPipeline.BuildPlayer` with `Assets/Scenes/SampleScene.unity`) and invoke
  it via `-executeMethod`. The built player can run under the VM display
  (`DISPLAY=:1`).

Interactive Editor / Play mode work uses the virtual display at `DISPLAY=:1`.

### Notes / gotchas

- First Editor open is slow (asset import for the whole project) and writes a
  `Library/` folder (git-ignored); subsequent opens are faster.
- Building an actual Android APK is NOT possible here without installing the
  Android module + Android SDK/NDK/JDK; demonstrate runtime via a Linux
  Standalone build instead.
- `Assets/google-services.json` (Firebase, project `backpack-tower-defense`) is
  committed; real ads/analytics need live third-party keys configured via
  `GameUp SDK → Setup` (see `README.md`, which is in Vietnamese).
