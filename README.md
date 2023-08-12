# DelayedAttackAction

**This mod is obsolete.** A better system has been added to the game for managing when rounds are fired during an attack action.

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) that adds a delay to when rounds are fired in an attack action.

It is compatible with game patch **1.0.4**. All library mods are fragile and susceptible to breakage whenever a new version is released.

The default behavior for an attack action is to fire the first round at the start of the action. There isn't a built-in way to simulate an attack having to charge up first or acquire a target lock. With this mod, you can configure specific subsystems to wait before firing the first round. There isn't any UI that shows the delay in the attack action right now but that may be possible to add in the future.

You will need to build the project in this repo and install it as a library mod and you will also have to add ConfigEdit/ConfigOverride files (see below). A single mod can have both libraries and ConfigEdit/ConfigOverride files.

Included in this repo is an example ConfigOverride for the `wpn_main_sniper_03` subsystem which is a single round per attack subsystem. The ConfigOverride exaggerates the attack duration so the effect is easier to see and sets the first round to be delayed until 85% of the way into the attack action.

The ConfigOverride works by adding attributes to the `custom` section of a subsystem. There is a custom string attribute and a custom float attribute, both named `action_start_time`. The custom string can be one of three values:

- `middle`
- `end`
- `percentage`

If the custom string is `percentage`, then the custom float attribute should be set to a decimal value between 0 and 1. For example, 85% would be written as `0.85` and any value over 1 is considered an error that will revert the subsystem back to stock behavior.
