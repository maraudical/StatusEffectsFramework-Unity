# Change Log:

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