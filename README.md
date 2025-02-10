# Lethal Company Lives

>Are you tired of dying on that one moon full of cash registers? Are you alone? Even with a mod to keep your ship loot you still lost a day and a moon worth of cash registers.  
Well you need some "Company Lives"! The Company have finally advanced their 'Employee Reanimation Machine' (ERM) to reach the moons surfaces!  
However, the cost to activate the device at this distance is equal to how much it costs to ship an additional employee. (TLDR: More employees, less ERM)  

### Features:

* Adds lives that scale off of the number of players in the ship at the start of the day (Configurable default:4)
* The ability to stop the ship from leaving if you die but still have lives left. (Configurable default: disabled)
* Two types of lives; Team Lives are shared by all players, Player Lives are exclusive to each player. Player Lives are consumed before Team Lives are.
* Players can be revived by returning their body back to the ship, or by waiting. (Both are configurable and can be disabled individually defaults: Time: 30s, returned to ship: true)
* Makes goofing off with your friends safer, but more likely to lose somthing. (Revived player don't count towards the death penalty, but do if the ship leave before they respawn.)
* When a player revives a message will appear for that player if a Player Life is used, or to all players if a Team Life is used. (Not configurable... yet<sup>TM</sup>)

### Config:

* Party Size: The difference of this value to the number of players actually in the game. These are Team Lives. (default: 4)
* Global Lives: Additional lives that the team shares. (default: 0)
* Player Lives: The number lives that each player receives. Player Lives are consumed before Team Lives are. (default: 0)
* Revive on body collection: Revive a player early when their body is returned to the ship. (default: true)
* Respawn Timer (Seconds): The amount of time before respawning a player. Setting this to zero will disable this option. (default: 30s) ~15s is recommended for solo runs.
* Prevent Ship From Leaving: Stop the ship from leaving if there are lives left. (default: false) Recommended for solo runs.

Special thanks to `Ry` who made [LC ReviveDeadPlayers](https://thunderstore.io/c/lethal-company/p/Ry/LC_ReviveDeadPlayers/). This mod is basically a massive edit of to make functional on the latest patch and add additional features and polish.