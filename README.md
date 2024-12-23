

//The rules of podracing are as follows:
/*A, Basic Rules:
1. For a qualifying run you are required to end the run with the most Qualifying Takeoffs performed at any of the Takeoff Locations.
2. Only one Qualifying Takeoff can be performed per a Takeoff Location.
3. The order in which these takeoffs are performed doesn't matter.
4. Score is tallied by the amount of takeoffs performed as priority, and time of the run as secondary. (L03, T15:00 > L02, T04:00)
5. The rules of Podracing are vague, but are to be taken literally. It is half the fun to figure out the Takeoff itself, and find loop holes to get the most optimal route.
6. Resume Expedition only.

B, Start and End:
1. The run starts the instant you interact with the cockpit of your ship while wearing a spacesuit.
2. The run ends one second after you become grounded* while wearing a spacesuit. Leaving this state cancels this rule until next time.
3. The run ends one second after waking up in another loop. (The animation time is added to the run)

C, Takeoff Location is defined either as:
1. A specified radius around a markable/lock-on location**. (Hollow's Lantern is a Lock-On, Village is a markable ship-log)
2. Any*** markable main ship-log location. (Village - main, Zero-G Cave - child)
3. If the criteria for two or more takeoffs are met at once, they all count as their own takeoffs. (2 in 1)

D, Qualifying Takeoff is defined either as:
1. Any ship takeoff, so far your takeoff makes a charging sound. (This includes the first takeoff)
2. Any shipless**** takeoff, so far you takeoff least 0.8 seconds after you become grounded*.
3. Any Death.

E, Disqualifying actions during a run:
1. Lock-On
2. Mark-On HUD
3. Exit to Title Screen
4. Any game modifications, except the Podracing Tracker mod

F, Permitted use of tools during a run:
1: External notes
2: External timers
3: Podracing Tracker mod

*Grounded is defined as being in gravity. The value of gravity on your hud is visible and is higher than zero. (Gravity 0.1x-inf)
**By 'location' it is meant literally a 'ship-log location'. Some Takeoff Locations are then either markable ship-logs, or objects that can be Locked-On in-game.
***If ship-log location isn't specified in criteria, any markable location can be counted as a Takeoff Location, so far all other criteria are met. (Any 0-50m)
****You can count any takeoff as shipless, so far your ship is more than 100m away. (Ship 100-inf m)*/

//The following is a list of all the Takeoff Locations in the game, and their specific criteria for a takeoff.
/*Ordered by the difficulty of a ship takeoff.
 - Basic description of the takeoff location (Specific tracking-ready parameters)

Timber Hearth:
 - Anywhere in the Village. (Village 0-50m)
 - Nomai Mines, the inside of the largest geyser. (Timber Hearth 215-220m & Nomai Mines 110-130m)

Quantum Moon:
 - Anywhere on the Quantum Moon. (Quantum Moon 0-300m)

Giant's Deep:
 - Anywhere on the Giant's Deep's orbit. (Giant's Deep 750-2000m & Any 0-70m)

White Hole:
 - Anywhere around the White Hole. (White Hole Station 0-1000m & Any 0-50m)

Ash Twins:
 - Inside the sand pillar anywhere on Ember Twin's equator. (Ember Twin 150-200m & Ash Twin 370-375m & Any 0-80)
 - Inside the sand pillar on Ash Twin's equator. (Ash Twin 135-150m & Ember Twin 365-370m)

Hollow's Lantern:
 - Anywhere on the Hollow's Lantern. (Hollow's Lantern 0-300m)

Brittle Hollow:
 - Above the Black Hole Forge. (Brittle Hollow 250-300m & Black Hole Forge 180-200m)

The Interloper:
 - Next to the Shuttle. (Frozen Nomai Shuttle 0-50m)
 - On the North Pole. (Comet 0-100m & Sun 0-3000m)

Dark Bramble:
 - Next to Feldspar's Camp (Feldspar's Camp 0-50m)
 - On the The Vessel (The Vessel 0-300m)
 - Next to Frozen Jellyfish (Frozen Jellyfish 0-50m)

Sun Station:
 - On the Sun Station. (Sun Station 0-2500m)

The Stranger:
 - Anywhere on The Stranger. (The Stranger 0-500m & Any 0-100m)

The Attlerock:
 - On top of the Lunar Lookout. (The Attlerock 80-85m & Lunar Lookout 0-10m)
