<p align="center">
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://drive.google.com/uc?id=1SdkuY-5eJyhta0zzGLv-xDRkmS_7YcaB">
  <img alt="logo" src="https://drive.google.com/uc?id=1eKRQM8cIOLvdS8ENIT7HhlV-Tnpcz211">
</picture>
</p><br>
<p align="center">
  <a href="https://unity3d.com/en/get-unity/download/archive"><img src="https://img.shields.io/badge/unity-2021%20or%20later-green.svg" alt=""></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/blob/main/LICENSE.md"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/releases"><img src="https://img.shields.io/badge/version-4.0.0-blue" alt="version"></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/pulls"><img src="https://img.shields.io/github/issues-pr-raw/maraudical/StatusEffectsFramework" alt=""></a>
</p>
<p align="center">
  <strong>Status Effect Framework</strong> is a framework for implementing status effects into any game. It is easy to use, and fully customizable.
</p><br>

<p align="center">
  <img src="https://drive.google.com/uc?id=1bUVXb2KGp71c3v7f1Tmcv3sfW2ksJ5Oi" height="400" alt="sample_1">
  <img src="https://drive.google.com/uc?id=1VFKSCil3bBtSU-83rKXC2W9HEfqWbM2g" height="400" alt="sample_2">
</p><br>

# Features
- Easy to implement framework for **any** game
- **Unlimited** customizability with each effect
- Float, int, and bool variables that **dynamically** update with effects
- Status effect grouping to easilly manage multiple effects
- Conditional adding/removing when effects are applied in tandem to other effects
- Create modules to add additional functionality as required
- Fully compatible with many common packages (see below)

# Supported versions
- Unity 6 or newer

# Other support
- Unity - Localization
- Unity - Awaitable
- Unity - Netcode for GameObjects (1.8.0+)
- Unity - Entities
- Unity - Netcode for Entities
- Cysharp - UniTask

# Getting started with the **Status Effect Framework**

1. Add the **Status Effect Framework** through the package manager
> Note: [Install via git](https://maraudical.gitbook.io/status-effect-framework/setup) with this URL: `https://github.com/maraudical/StatusEffectsFramework-Unity.git`
2. Import the **Examples** (highly recommended)
3. Change any settings, group names, and preset statuses in **ProjectSettings**
4. **Create** any status effects from the create menu
5. Add the **StatusManager** component onto any object
6. Implement any **StatusFloat/StatusInt/StatusBools** into scripts that will be affected
7. Link the **StatusVariables** to the **StatusManager** and simply add or remove effects

Check the [documentation](https://maraudical.gitbook.io/status-effect-framework/) for more information.

# Author
**Grady Milligan** - *Game Designer*

[LinkTree](https://linktr.ee/gradymilligan) - [GitHub](https://github.com/maraudical)

# License
[MIT](./LICENSE.md)