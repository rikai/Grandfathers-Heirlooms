# Grandfather's Heirlooms

This mod is a continuation of [Grandfather's Gift Remade](https://www.nexusmods.com/stardewvalley/mods/4151) with direct permission from pepoluan.

This mod is directly inspired by [Grandfather's Gift 1.0 on NexusMods](https://www.nexusmods.com/stardewvalley/mods/985), but since that mod is not open source, pepoluan had to totally remake it from scratch.

Only the ideas remain; the code, the logic, even the words, everything came from pepoluan's diabolical mind (with great help from friends at [The Stardew Valley Forums](https://community.playstarbound.com/forums/mods.215/)).

* [**Grandfather's Gift Remade on NexusMods**](https://www.nexusmods.com/stardewvalley/mods/4151)
* [**Grandfather's Heirlooms on NexusMods**](https://www.nexusmods.com/stardewvalley/mods/8557)

## Description

The mod is meant for use on a fresh save.

It Gives you the Princess's Keepsake on the 2nd Day of Spring, in Year 1, in a gift box with the chest that the dagger was stored in, to help make early game combat a bit less stressful. The gift box opens once you have two free inventory slots.

## Requirements

* Stardew Valley 1.6 or newer
* SMAPI 4.1.0 or newer

## Configuration

Some `config.json` knobs to fine tune the experience:

| Knob | Purpose |
| --- | --- |
| `traceLogging` | Set to `false` if you think the TRACE logging is too chatty, making it difficult to troubleshoot other mods |
| `triggerDay` | The day when the mod will trigger. Limited to `2<=day<=28`. Season & year are locked to "spring" and 1, respectively |
| `weaponStats` | Sets the dagger's stats, in the order `minDamage/maxDamage/knockback/speed/precision/defense/type/mineBaseLevel/mineMinLevel/areaOfEffect/critChance/critMultiplier`. If the value is invalid, the mod uses the default stats and logs a warning. |

## Internationalization

The mod is available in English and Portuguese. Feel free to write a translation of the default.json in the i18n directory and submit a pull request.

Also happy to accept pull requests that flesh out the implementation.

## Compatibility

* No known conflicts.

## To Do / Future Plans?

* Yeah, I do have some plans to put more polish & finish to this mod.
* Custom sprite for the Princess's Keepsake
* And finally, if there's interest, additional gifts! (I'll need the When + Where + What)

## Discussion?

Ping me (@rikai#6969) on the [Stardew Valley Discord](https://discord.com/invite/stardewvalley) in one of the modding channels!

## Other Things

This mod is open source and available [on Github](https://github.com/rikai/Grandfathers-Heirlooms/)
