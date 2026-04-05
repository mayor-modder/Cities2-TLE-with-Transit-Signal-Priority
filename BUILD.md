## Build Instructions

1. Install [Node.js 20](https://nodejs.org/), [.NET 8.0 SDK](https://dotnet.microsoft.com/download) and Modding Toolchain in-game

2. Clone the repository

```shell
git clone git@github.com:slyh/Cities2-TrafficLightsEnhancement.git
cd Cities2-TrafficLightsEnhancement
git submodule update --init --recursive
```

3. Build the plugin

```shell
dotnet restore
dotnet build --configuration Release
```

## Fork Workflow Guardrails

Use the repo scripts below instead of relying on ad-hoc `dotnet build` flags:

### Fast verification while working

This is compile-only and is useful for iteration, but it does not produce a playable installed mod.

```powershell
.\scripts\Verify-Mod.ps1
```

### Playable deploy before testing or closing out work

This runs the Cities II postprocessor and updates the local mod install in the game mods folder.

```powershell
.\scripts\Deploy-Mod.ps1
```

### Important

```powershell
dotnet build -p:DisablePostProcessors=true
```

That flag skips the mod postprocessor and deploy steps from the Cities II toolchain. It is a verification-only path, not a playable local install path.

Even on the verification path, the UI webpack step can still rebuild frontend assets. Do not treat updated `.mjs` or `.css` files as proof that the full mod was deployed.
