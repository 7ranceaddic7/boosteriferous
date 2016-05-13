Boosteriferous
==============

Boosteriferous is a mod for the game Kerbal Space Program by Squad.
It allows you to configure the thrust profile of solid rocket boosters, giving
 more nuanced control than the 'Thrust Limiter' slider.  However, you still
 have to work out what you want ahead of time and set it up in the VAB/SPH -
 you can't throttle these solids in real time!
Note that Boosteriferous removes the 'Thrust Limiter' slider from engines
 which support thrust profiles.  You don't get to have a thrust profile and
 then scale it as well.

Boosteriferous is currently in alpha test.

Boosteriferous is developed by Edward Cree, who goes by the handle 'soundnfury'
 on IRC and 'ec429' elsewhere; his website is at http://jttlov.no-ip.org.

Boosteriferous is licensed under the GNU General Public License, version 2.


Thrust Profiles
---------------

Flat
~~~~
The Flat thrust profile is just the simple thrust limiter as present in stock;
the booster will run at the selected thrust level (Throttle Down Amount) for
its entire burn.

Step
~~~~
The Step profile has two parameters: Throttle Down Point and Throttle Down
Amount.  The booster will run at full thrust until it reaches the Throttle
Down Point, defined as a fraction of remaining propellant.  At this point, it
will reduce thrust to Throttle Down Amount.  Both values are percentages.

So for instance, if Point is 60 and Amount is 25, and the booster's original
thrust is 100kN, it will burn the first 40% of its fuel at full thrust (100kN)
then the remainder at one-quarter thrust, i.e. 25kN.

Requires tech General Rocketry to unlock.

One caveat to note: the Amount is based on the part's total fuel capacity; if
 you reduce the part's SolidFuel resource in the VAB/SPH, you will effectively
 chop off the beginning of your thrust profile.

Linear
~~~~~~
The booster will gradually throttle down from full thrust (at ignition) to the
specified Throttle Down Amount (at empty).  This means that the thrust-time
graph is actually an exponential decay.

Requires tech Fuel Systems to unlock.

StepLinear
~~~~~~~~~~
The booster will run at full thrust until it reaches the Throttle Down Point,
at which it will reduce thrust to Throttle Down Amount.  Over the remainder of
the burn it will gradually throttle back up to full thrust.

Requires tech Adv. Fuel Systems to unlock.


Interop with Info Mods
----------------------

Boosteriferous sets the ModuleEngines.maxThrust to the _average_ thrust of the
burn.  This means that info mods like KER should show correct burn times, but
the TWR may be misleading.
