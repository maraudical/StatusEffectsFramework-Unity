<p align="center">
  <img src="https://drive.google.com/uc?id=1UYbBluu119xenikC8iY13CaT8lcp4J_G" height="200" alt="logo"><br>
  <a href="https://unity3d.com/en/get-unity/download/archive"><img src="https://img.shields.io/badge/unity-2021%20or%20later-green.svg" alt=""></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/blob/main/LICENSE.md"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/releases"><img src="https://img.shields.io/badge/version-1.3.1-blue" alt="version"></a>
  <a href="https://github.com/maraudical/StatusEffectsFramework/pulls"><img src="https://img.shields.io/github/issues-pr-raw/maraudical/StatusEffectsFramework" alt=""></a>
</p><br>

<p align="center">
  <strong>Status Effect Framework</strong> is a framework for implementing status effects into any game. It is easy to use, and fully customizable.
</p><br>

# Features
- Easy to implement framework for **any** game
- **Unlimited** customizability with each effect
- Float, int, and bool variables that **dynamically** update with effects
- Status effect grouping to easilly manage multiple effects
- Conditional adding/removing when effects are applied in tandem to other effects

# Supported versions
- (Tested on)Unity 2022.x
- (Tested on)Unity 2021.x

# Getting started with the Status Effect Framework

1. Add the **Status Effect Framework** through the package manager
1. Import the **Examples** (if wanted)
1. Change any settings, group names, and preset statuses in **ProjectSettings**
1. **Create** any status effects from the create menu
1. Implement the **IStatus** interface on any classes inheriting Monobehaviour
1. Implement any **StatusFloat/StatusInt/StatusBools** that will be affected
1. Use the extension methods on the **IStatus Monobehaviour** to add or remove effects

Check the [documentation](https://maraudical.gitbook.io/status-effect-framework/) for more information.

# Author
**Grady Milligan** - *Game Designer*

[LinkTree](https://linktr.ee/gradymilligan) - [GitHub](https://github.com/maraudical)

# License
[MIT](./LICENSE.md)