# OreFinderPlus
## Description
 This is a Space Engineers in-game script for logging ore locations.  Currently the [Ore Detector](https://github.com/malware-dev/MDK-SE/wiki/Sandbox.ModAPI.Ingame.IMyOreDetector) allows a player to "scan" for ore deposits they can mine.
 The small ship block has a distance of 50m while the large ship block has a distance of 150m.  
 Unfortunatly these distances are too short to be useful and it is super easy to miss pockets of ore.  Ore Finder Plus [OFP] leverages the [Ore Detector Raycast](https://steamcommunity.com/sharedfiles/filedetails/?id=1967157772) mod to allow a Raycast collision with the ore from the detector.  In short the mode will allow the Ore Detector to fire a 
 small ray into a material (planet, asteroid, etc) and will report back the ore deposits it hits.  This in-game script creates a logging system around that raycast.

 With Ore Finder Plus you can:
 * Have a heads up display of ore deposits discovered
 * List all GPS coordinates for ore deposits
 * Import all GPS points
 * Increased distances (default 1KM detection range)

 The great thing about Ore Detector Raycast and Ore Finder Plus is that this doesnt "break" the immersion of the game.  This Mod and Script is not over powered.  You, as a player, still have to scan the planet or asteroid there is no "scan the world" button.

 ## Usage
 # Requirements for your vehicle:
 * Ore Detector somewhere on the grid.  Currently only the first one found is used.
 * Programming Block running this script
 * LCD Panel with the [OFP] designated tag.

 # Operation
 Once the above is met you will need to add a few commands to your hot bar.
 * up - Add the programming block from above to your hot bar with the argument of up.  This will navigate the menu items up
 * down - Add the programming block from above to your hot bar with the argument of down.  This will navigate the menu items down
 * apply - Add the programming block from above to your hot bar with the argument of apply.  This will select the current menu item
 * menu - Add the programming blcok from above to your hot bar with the argument of menu.  This is a quick key to go back to the main menu.
  # LCD's
  To configure an LCD to be used by OFP you need to add the OFP tag to its name
  example: MyText Panel [OFP]
  Once this is done a custom data section will be added to that screen allowing you to change its operation
  ```
  [OreFinderPlus]
    ; Edit the below to change how this screen reacts.
    ; Options:
    ; default = Allow this screen to navigate all menus
    ; ore = Always show ore status
    ; coordinates = Always show coordinate screen
    Screen = coordinates
```
    Setting the **Screen** option to Ore or Coordinates will force that screen to only display that.  You should have at least one LCD set to default.


 # Menu Options
 * Main Page: This is the main menu which allows navigation
 * Deposits Found: This is a summary screen showing ore found
 * Ore Coordinates: This screen shows all the ore found in GPS coordinates format.  **IMPORTANT** To get these coordinates into your GPS system you will press **K** and navigate to the LCD showing the coordinates.  Click **Edit Text**.  Once that windows opens up the GPS points are automatically added to your GPS system.  Navigate to the **GPS** tab and click **Show on HUD** if you want to see  the location.
 * Logging Data: This screen shows raw data being logged on the distances between ore deposits.  This isnt required but is good for troubleshooting.
 * Clear Data: This option will clear the found list of ore and the coordinates screen.

 ## Screen Shots
 * Menu Screen
 ![Menu](https://github.com/dlaflotte/OreFinderPlus/blob/master/images/menu.png?raw=true)
 
 * Deposits Found
 ![Deposits](https://github.com/dlaflotte/OreFinderPlus/blob/master/images/Deposits%20Found.PNG?raw=true)
 
 * Ore Coordinates
 ![Ore Coordinates](https://github.com/dlaflotte/OreFinderPlus/blob/master/images/Ore%20Coordinates.PNG?raw=true)

 * Logging Data
 ![Logging Data](https://github.com/dlaflotte/OreFinderPlus/blob/master/images/Logging%20Data.PNG?raw=true)

 * Clear Data
 ![Clear Data](https://github.com/dlaflotte/OreFinderPlus/blob/master/images/Clear%20Data.PNG?raw=true)

