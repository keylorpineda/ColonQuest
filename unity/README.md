# Colón Quest — Unity client

Unity 2D project (WebGL + desktop). Owner: Jefferson.

Create the Unity project **inside this folder** so `unity/Assets`, `unity/Packages` and
`unity/ProjectSettings` are tracked and `unity/Library` stays ignored.

Planned layout:

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
