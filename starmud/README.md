StarMUD is an online multiplayer game (more specifically, a MUD) that has been active since 1992.  It has hundreds (if not thousands) of unique users, although only a handful still play today.

Since the original owners and developers have long since abandoned the codebase many years ago, I have slowly rewritten or modernised almost every aspect of the game itself over the years.

While it is very against the game's rules for non-developers and non-administrators to view StarMUD source code, I have opted to make an exception in this case.

The code is written in LPC, which is a language somewhat similar in syntax to C and C++, but purposed specifically for MUDs.

Contents ::

  darkmatter_d.c
    A daemon (service) that handles a player resource, using database techniques retain specific player data and consistency.
    
  multiattack_monster.c
    An inherit for a special type of monster, designed to lower memory, CPU and bandwidth usage by mapping then reducing many special attacks into one discrete attack.
  
  tunnel_e1_upper4.c
    A room which contains some set theory used as a randomised logic puzzle. This is more player-facing.
    
  pslam.c
    An offensive command, able to hit multiple targets by filtering the room for matching targets using a function pointer (may be translated as 'lambda function' with modern languages).
  
  igglewicz.c
    A vendor interfacing with the dark matter daemon above.  This is more player-facing. 
