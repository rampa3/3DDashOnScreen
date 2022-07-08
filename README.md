# 3D Dash On Screen

A [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader) mod for [Neos VR](https://neos.com/) that replaces 2D overlay dash in Screen mode with the regular 3D one.<br>
Also F4 (now remappable trough mod config) is now the edit mode toggle button since it was removed when the freeform camera was added,
and spawning of facets and UI in userspace is fixed to not spawn into overlay.<br>Additionally, VR behaviour of camera controls UI and notifications are restored together with desktop tab control panel keybind (default key is N, configurable trough mod config). The mod can be disabled using mod config if needed.

## Installation
1. Install [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader).
1. Place [3DDashOnScreen.dll](https://github.com/rampa3/3DDashOnScreen/releases/latest/download/3DDashOnScreen.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If everything is working, your 2D dash will be replaced with the regular one, when using Screen mode.

## Credits
- [rampa3](https://github.com/rampa3) - original idea and research
- [eia485](https://github.com/eia485) - added cursor parking lot and helped fix errors
- [art0007i](https://github.com/art0007i) - wrote a couple of transpilers
