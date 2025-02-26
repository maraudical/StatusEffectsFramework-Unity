# Change Log:

## 3.1.0
- Feature: Added acronym and color fields to the optional section of Status Effects
- Feature: Added status references setup requests if dynamically adding new status effects in ECS
- Enhancement: Added more status effect database utilities
- Enhancement: More methods for ECS status variables
- Bug Fix: Fixed comparable name missing reference error
- Bug Fix: Fixed name processor not checking references properly
- Bug Fix: Fixed assembly definition naming
- Bug Fix: Samples scriptable object fixes
- Change: Updated some ECS syntax to a more recent version
- Change: Cleaned up baker dependencies

## 3.0.0
- Feature: Support for Entities and NetCode for Entities
- Enhancement: Updated samples to include Entities/Netcode for Entities/Netcode for GameObjects
- Bug Fix: Cleaned up assembly defenitions for unused dependencies
- Bug Fix: Cleaned up unused namespaces from scripts
- Bug Fix: Status effect data editor refresh issues
- Bug Fix: Name post processor not updating Ids in specific scenarios
- Bug Fix: Incorrect ordering of Status Effect durations for infinite and predicate timings
- Bug Fix: Max stacks not actually limiting stack count correctly
- Bug Fix: Non-stacking behavior logic with predicates
- Bug Fix: Conditional inherited timing when using events and predicates
- Change: Addressables are no longer needed, instead a database of all status effect datas is created in the Resources folder
- Change: Ids are now generated as Hash128 as opposed to strings
- Change: File reorganization for samples and main project
- Change: Renamed Entity to Player in samples to avoid Entities package confusion

## 2.0.1
- Enhancement: Added stack count to conditionals
- Enhancement: Added OnBaseValueChanged and OnSignProtectedChanged events to all Status Variables
- Enhancement: Cleaned up module sub asset management
- Bug Fix: Fixed conditional logic when adding new effects
- Bug Fix: Fixed removal logic to prioritize low values and then low durations
- Bug Fix: Fixed non-stacking data base value comparisons with negative values
- Change: Updated addressable flush timer for the NetworkStatusManager

## 2.0.0
- Feature: Added a NetworkStatusManager and NetworkStatusVariables that work with Netcode for GameObjects (1.8.0+)
- Enhancement: Added additional conditional options to check for group and comparable names
- Enhancement: Added an on value changed event to status variables
- Enhancement: Added IDs to various Name scriptable objects that fix addressables issues
- Enhancement: StatusVariables can now be nested
- Bug Fix: Module icon post processor now works in different file structures
- Bug Fix: Fixed critical issue preventing the status effect data inspector working with modules
- Bug Fix: Minor inspector fixes to multiple editors and drawers
- Bug Fix: Fixed StatusManager HighestDuration add logic
- Change: Updated naming conventions, this will break pre-existing code
- Change: Removed Info attribute
- Change: Removed global time override (it was redundant with Unity Event based Status Effects)
- Change: Modules now only enable one module, use StatusEffect.Stacks to scale it now

## 1.7.1
- Feature: StatusVariables now have an additional bool for int/float variables to clamp the value to the sign of the base value
- Enhancement: General cleanup and fixes of multiple property drawers
- Bug Fix: Fixed foldouts bugged in list for StatusVariables

## 1.7.0
- Feature: Awaitables support for versions 2023.1 or newer
- Enhancement: Modules can now be multi-edited
- Enhancement: New inspector icons
- Enhancement: New effect icons
- Enhancement: New VFX samples
- Enhancement: Can now change base value in inspector during runtime with multi-editing support
- Bug Fix: Namespace fixes
- Change: Made the StatusManager a component and removed the IStatus interface

Note: From now on instead of implementing the IStatus interface you will add the StatusManager component to a GameObject and then link StatusVariables with the SetManager() method. See the example for more detail on current implementation.

## 1.6.0
- Feature: Modules are now a more intuitive replacement for custom effects
- Enhancement: Updated/cleaned up many property drawers
- Enhancement: Added example status effects for the coin multiplier
- Enhancement: Updated samples with new modules and VFX
- Bug Fix: Trying to add 0 or negative stack count would throw errors
- Bug Fix: Fixed critical error where status variables would not update in builds

## 1.5.1
- Enhancement: Cleaned up some editor property drawers

## 1.5.0
- Enhancement: Updated the sample scene to have more info and examples
- Bug Fix: Multi-editing fixed for conditional and group drawers
- Bug Fix: Removed extra using statements and the one causing a build error
- Change: Removed the info box drawer

## 1.4.1
- Feature: Stackability full support
- Enhancement: Conditionals now can remove comparable names and groups
- Bug Fix: Various critical bug fixes

## 1.4.0
- Feature: Property drawer for Status Effects
- Feature: UniTask support to replace timer and custom effect coroutines if in project
- Feature: Can now create status name and comparable name scriptable objects
- Enhancement: Status Effect Data inspector overhaul
- Enhancement: Global time override can now be set and unset easily
- Enhancement: Updated the samples to have more options and show how an Effect UI can be set up
- Enhancement: Can now remove specific amounts of a stack
- Bug Fix: Custom property drawers now properly undo
- Bug Fix: Fixed timer and custom effect coroutines not stopping from specific cases
- Bug Fix: Fixed non-stacking behaviour not evaluating properly with negative values
- Change: Changed event based effects to use Unity Events instead of Actions
- Change: Returned the name and description as well as an icon option to Status Effect Data with localization support

## 1.3.1
- Feature: Added a global time override to tie duration to a specified action for all effects
- Enhancement: Add an inherited option to conditional timing
- Bug Fix: Fixed logic for non-stacking behaviour

## 1.3.0
- Feature: Conditional adding/removing when effects are applied with/without other effects
- Feature: Int based grouping system
- Enhancement: Slightly reorganized file structure
- Bug Fix: Now checks for effect value type when adding to statuses
- Bug Fix: Project Settings tab now properly updates

## 1.2.0
- Feature: Updated the samples
- Feature: Adding status effects has more options to set duration/removal conditions
- Enhancement: Updated inspector view of multiple property drawers
- Change: Removed localization support/description

## 1.1.0
- Feature: Added documentation
- Feature: Added change log
- Enhancement: Renamed many variables/methods
- Enhancement: Deleted old files
- Bug Fix: Fixed settings errors
- Bug Fix: Fixed status string indentation
- Bug Fix: Renamed Samples and Documentation to add the ending '~'

## 1.0.1
- Feature: Effects now control individual stackability
- Feature: Status effects now have a base value that can be inherited
- Feature: Added actions to status effects for stop/start event subscription
- Bug Fix: Adjusted the size of the status string bool hit box
- Bug Fix: Changed settings creation path to work on new projects

## 1.0.0
- Feature: Main status effect management
- Feature: Interface implementation
- Feature: Custom float/int/bool variables
- Feature: Custom effect method for increased control
- Feature: Custom editors/property drawers/project settings