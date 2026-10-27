# Colón Quest — Unity client

Unity 2D project (WebGL + desktop). Owner: Jefferson.

## Open the project

1. Install Unity Hub and the editor **6000.3.27f1** (the version pinned in
   `ProjectSettings/ProjectVersion.txt`) with the **WebGL Build Support** module.
2. In Unity Hub choose **Add project from disk** and select this `unity/` folder.
3. Commit the `.meta` file that Unity creates next to every new asset.

`unity/Library`, `Temp`, `Logs` and `UserSettings` stay ignored.

## Layout

```
Assets/
  Scripts/
    Finance/     # FinanceEngine: pure C#, no UnityEngine dependency
    Api/         # HTTP client and DTOs mirroring shared/schemas
    UI/          # SINPE, account statement, decision and negotiation panels
    World/       # Player movement, stations, map
  Plugins/WebGL/ # MediaRecorder .jslib for voice capture
  Tests/EditMode/
```
