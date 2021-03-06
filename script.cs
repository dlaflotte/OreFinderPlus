
/*Script Author: Onosendai
                    * Description: This script leverages the Ore Detector Raytrace mode allowing you to automatically record ore it "hits".
                    * Credits: Big thanks to @Reacher for the MOD allowing Ray Tracing of Ore.
                    *          https://steamcommunity.com/workshop/filedetails/?id=1967157772
             Version: 1.1
                   * Initial version to display and track ore deposits.
             Version: 1.2
                   * Added support for Better Stone v7.0.3f
             Version: 1.3
                   * 360 Ore Scanning and logging.  If you turn on fast scanning and leave the stepping at 18 the scan will take about 90 seconds.  If you scan EVERY degree of the sphere and wait until you have the proper charge for the distance selected then you will wait longer but it will be more accurate.
                   * Forward Plane Scanning (-5x5) times (-5x5) forward square.  Greater distance will make the square larger as these are degrees from the ore detector front projection
                   * New Status screen to see the angle (Azimuth and Elevation) of a current scan
                   * New mini status screen to show on small LCD's
                   * Added the disable and enable argument.  This will disable or enable the ore detector
                   * Added a scan program argument.  This will cause a onetime scan in whatever mode you have set and will then disable the ore detector
                   * Added save and restore of settings
                   * Added settings menu for making changes to common configuration options
             Version: 1.4
                   * Better Error handling for when the Ore Detector Raycast mod is not installed.  This will aid in giving direction to users when they subscript to the script.
             Version: 1.5
                   * Added function to allow for screens on cockpits.  No requirement for external LCD's.  Also when displaying the ministatus on a cockpit you can change the font size and it will remember that rather than forcing it to the 4.5/5.5 size.
             Version: 1.6
                   * Fixed the scan once functionality and added the scan once item to settings/save/load.
             Version: 1.7
                   * Fixed an issue with multiple cockpits on the same grid.
             Version: 1.8
                   * Big Thanks to Radar5k for adding the ability to use the Programming Block Screen as your OFP screen.  Now any PB with the tag [OFP] can be used to display content for OFP.
                   * Important Info:
                        * The new TAG that OFP is looking for is [OFP] not just OFP.  IF you have a screen that isnt working from an older build update the OFP_TAG variable below.  This was done to allow for users to name their Programming Blocks with the OFP tag without taking over the screen.
                        * We are now looking for the custom data in LCDs/Programming Blocks/or Cockpits.  If any have the OFP_Tag in the name OR have the OFP_SETTINGS_TAG in the custom data section we will load OFP.  This allows you to name your screens whatever you like and still have OFP work.
           */

//Tag for Ore Finder Plus to look for on the ore detector or LCD.Either tag the name or custom data
//   Example Name: LCD Test [OFP]
//                 LCD Port Side OFP
private static string OFP_TAG = "[OFP]"; // Changed default tag from OFP so we can name the PB "PB OFP" without outputting to the screen. (Radar5k).

private static string OFP_SETTINGS_TAG = "[OreFinderPlus]"; // Keep settings when recompiling. (Radar5k).

//ignoreList will allow you to exclude types of ORE.
//example: "Stone,Ice"
string ignoreList = "Stone";


//Deposit Range is the potential size of a deposit of ore.  The Below size is in meters.
//If you start getting the same ore deposits tagged with multiple GPS coordinates
//then make this number larger.
int depositRange = 200;

//*******Dont Touch Below here********
//************************************

//Scan Mode
// 0 = Scan Straight ahead of the ore detector
// 1 = Scan 360 degrees in all directions
// 2 = Scan only a defined forward plane
Dictionary<int, string> OFPScanMode = new Dictionary<int, string>
{
    {0,"Forward Scan" },
    {1,"360 Scan" },
    {2,"Forward Plane Scan" }
};

int scanMode = 1;


//Detection Distance in meters.  Default set to 1KM  If you're using the 360 scanner or even forward plane scanner you'll want to avoid making this much larger.
int detectionDistance = 1000;

//Quickscan will keep scanning every update10 and update100 even if the ore detector doesnt have enough "charge".  If you set fast charge OFF then the detector will
//wait until there is enough charge in the detector to hit the requested distance.
//The Downside to quickscan is that you may miss some ore at distance.  The plus side is it wont take 1million years to complete a 360 scan.
//Rule of thumb would be
//360 scan turn on fast scan
//Forward scan or Forward Plane scan use quickscan off as you'll make sure you hit your distance set.
Boolean quickScan = true;

//Only turn this on if you're using DNSpy to view variable data.  You will need to configure it to catch DivideByZero Exceptions.
private static bool DEBUGGING = false;
private static double VERSION_NUMBER = 1.8;
private static string PRODUCT_NAME = "Ore Finder Plus";

//used to scan 360 degrees around the ore detector
private const float minAngle = 0;
private const float maxAngle = 360;
private const float sphereElevationMinAngle = -90;
private const float sphereElevationMaxAngle = 90; //Optimization.  If we are scanning 360 in the azimuth there is no need to also scan 360 in the elevation as that will double scan the back 180 of the ship.

//Used to create a forward scan of a larger plane.  The below will scan -5,5 and -5,5 in a plane in the forward orientation of the ore detector
//If for example you wanted to do a semicircle infront of the ore detector you could
//set this to 90 for both azimuth and elevation.  that would give and arc of
// -90<->90 on the azimuth and -90<->90 on the elevation.  creating a 180 degree semicircle in front.
private const float forward_azimuth = 5;
private const float forward_elevation = 5;

//This is the degrees to move each scan during a 360 scan.
//Waiting for 1 degree in all directions is way to slow.
//At 18 steps it takes about 90 seconds to complete a 360 scan.
//You can speed this up by increasing the steps size but you may miss deposits.
float sphereScanStep = 18;

//This is the degrees to move each scan during a forward plane
//scan and the default 20 degree angle it takes about.
//With a 5x5 plane in front at a 1 step scan it is VERY accurate
//for that area but takes about 12 seconds to complet a scan.

float forwardScanStep = 1;
float azimuth = minAngle;
float elevation = minAngle;


//scanOnce will be used if you want to press a "scan now" button.  wait for the scan to finish and then
//I'll disable the ore detector.  probably good for scripting.  Can "scan" then once the detector is disabled move the ship 1k and scan again?
//by default we will be on continual scanning so this will be off.
bool scanOnce = false;

//this will hold the "revolutions" of a scan.  Perhaps this will give an indicator if the ship can move as the scan is done.
double scans_Completed = 0;

//All the known ore and locations we've found
List<MyDetectedEntityInfo> DiscoveredOre = new List<MyDetectedEntityInfo>();
readonly IMyOreDetector detector;
public IMyTextSurface surface; // Had to change from readonly in order to write to the screen. (Radar5k).

List<IMyTextPanel> lcdPanels = new List<IMyTextPanel>();
List<IMyCockpit> cockpits = new List<IMyCockpit>();
List<IMyProgrammableBlock> PBs = new List<IMyProgrammableBlock>(); // Hunting for PB's (Radar5k).

//Menu Handling Options
int currentScreen = 0;
/*Screens:
                        0=Menu
                        1=Deposits
                        2=GPS Coordinates
                        3=Compare All
                        4=Status Screen (Scans Done, Scan Mode, Ore Count, etc)
                        5=Clear Data
                        6=Settings
                        */
int maxScreen = 6;
int currentSelection = 1;
int maxSelection = 6; //Used for the main menu
int maxSettingsSelection = 6;  //Used for the settings menu
MyIni _ini = new MyIni();
private string test;
//Miniflash is for the mini status display.  When mini flash is set to false the status screen will flash "Scan Complete" for x ticks and then go back to showing status.
bool miniFlash = false;
int miniFlashTicks = 0;
int maxMiniFlashTicks = 100;
double flashedOn = 0;
bool setDistance = false;  //used to set the scan distance in the settings menu
bool disableOFP = false;
string sError = "";

/* ToDO:
                         * Make ignoreList easy to edit (perhaps in the menu)
                         * Perhaps add a bootup splash screen
                         * Add the ability to broadcast the ore to a channel listener so displays on a remote base can see the ore.  This would also allow for drones to zoom around scanning and have ore results at the base.
                         */


private bool IsKnown(MyDetectedEntityInfo foundOre)
{
    bool known = false;
    foreach (MyDetectedEntityInfo oreInstance in DiscoveredOre)
    {
        if (oreInstance.Name == foundOre.Name)
        {
            try
            {
                //We have the same ore type so now lets see if the are "close" to eachother.
                //If they are we will assume this is the same deposit of ore
                double gpsDistance = Vector3D.Distance((Vector3D)oreInstance.HitPosition, (Vector3D)foundOre.HitPosition);
                if (gpsDistance < depositRange)
                {
                    known = true;
                }
                else
                {
                    known = false;
                }
            }
            catch (Exception e)
            {
                Echo($"Error: {e.Message}");
                Me.Enabled = false;
            }
            if (DEBUGGING)
                throw (new DivideByZeroException());
        }
    }
    if (!known)
    {
        DiscoveredOre.Add(foundOre);
        Echo($"{foundOre.Name}, {foundOre.Relationship.ToString()}");
    }
    return known;
}
private void DisplayCompareAll(IMyTextSurface panel, bool staticScreen = false)
{
    string oreAll = "*** Known Ores Compared ***\n";
    foreach (MyDetectedEntityInfo oreTest in DiscoveredOre)
    {
        oreAll += $"**** {oreTest.Name}\n";
        foreach (MyDetectedEntityInfo oreInstance in DiscoveredOre)
        {
            double gpsDistance = Vector3D.Distance((Vector3D)oreTest.HitPosition, (Vector3D)oreInstance.HitPosition);
            string same = "Not Same Name\n";
            if (oreTest.Name == oreInstance.Name)
            {
                same = "Same Name\n";
            }
            oreAll += $"Ore1={oreTest.Name}, Ore2={oreInstance.Name}, Distance = {gpsDistance}\n{same}";

        }
    }
    if (!staticScreen)
        oreAll += "\n> Return To Menu";
    WriteToLCD(oreAll, panel);
}

private void ClearLCD()
{
    foreach (IMyTextPanel panel in lcdPanels)
    {
        panel.WriteText("");
    }
}

private void RegisterLCDs()
{
    List<IMyTextPanel> allPanels = new List<IMyTextPanel>();
    List<IMyCockpit> allCockpits = new List<IMyCockpit>();
    List<IMyProgrammableBlock> allPBs = new List<IMyProgrammableBlock>();

    GridTerminalSystem.GetBlocksOfType(allCockpits);
    GridTerminalSystem.GetBlocksOfType(allPanels);
    GridTerminalSystem.GetBlocksOfType(allPBs);

    foreach (IMyCockpit iCockpit in allCockpits)
    {
        if ((iCockpit.CustomName.Contains(OFP_TAG)) | (iCockpit.CustomData.Contains(OFP_TAG)) | (iCockpit.CustomData.Contains(OFP_SETTINGS_TAG)))
        {
            MyIniParseResult result;
            _ini.TryParse(iCockpit.CustomData, out result);
            if ((!iCockpit.CustomData.Contains(OFP_SETTINGS_TAG)))
            {
                //No active ini data so we will set a default
                iCockpit.CustomData = OFP_SETTINGS_TAG;
                iCockpit.CustomData += "\n; Edit the below to change how this screen reacts.";
                iCockpit.CustomData += "\n; Options:";
                iCockpit.CustomData += "\n; default = Allow this screen to navigate all menus";
                iCockpit.CustomData += "\n; ore = Always show ore status";
                iCockpit.CustomData += "\n; coordinates = Always show coordinate screen";
                iCockpit.CustomData += "\n; status = shows the status of current (or single) scan";
                iCockpit.CustomData += "\n; ministatus = shows scan status on small screens";
                iCockpit.CustomData += "\n; settings = screen to set options";
                iCockpit.CustomData += "\n; :::::EXAMPLE::::";
                iCockpit.CustomData += "\n; OFP@0=default";
                iCockpit.CustomData += "\n; OFP@2=status";
                iCockpit.CustomData += "\nOFP@0 = default";
                iCockpit.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
            }
            //Need to capture the entire cockpit as it will have the custom data section for the INI
            cockpits.Add(iCockpit);
        }
    }


    foreach (IMyTextPanel panel in allPanels)
    {
        if ((panel.CustomName.Contains(OFP_TAG)) | (panel.CustomData.Contains(OFP_TAG)) | (panel.CustomData.Contains(OFP_SETTINGS_TAG)))
        {
            //Grab all panels that have the OFP_TAG in their name.

            //Check if the panel already has our INI data
            MyIniParseResult result;
            _ini.TryParse(panel.CustomData, out result);
            if ((!panel.CustomData.Contains(OFP_SETTINGS_TAG)))
            {
                //No active ini data so we will set a default
                panel.CustomData = OFP_SETTINGS_TAG;
                panel.CustomData += "\n; Edit the below to change how this screen reacts.";
                panel.CustomData += "\n; Options:";
                panel.CustomData += "\n; default = Allow this screen to navigate all menus";
                panel.CustomData += "\n; ore = Always show ore status";
                panel.CustomData += "\n; coordinates = Always show coordinate screen";
                panel.CustomData += "\n; status = shows the status of current (or single) scan";
                panel.CustomData += "\n; ministatus = shows scan status on small screens";
                panel.CustomData += "\n; settings = screen to set options";
                panel.CustomData += "\nScreen = default";
            }
            lcdPanels.Add(panel);
        }
    }
    // Hunting for Tag in Programmable Block Names and Custom Data (Copy/Paste/Edit of Cockpit loop). (Radar5k).
    foreach (IMyProgrammableBlock iPB in allPBs)
    {
        if ((iPB.CustomName.Contains(OFP_TAG)) | (iPB.CustomData.Contains(OFP_TAG)) | (iPB.CustomData.Contains(OFP_SETTINGS_TAG)))
        {
            MyIniParseResult result;
            _ini.TryParse(iPB.CustomData, out result);
            if ((!iPB.CustomData.Contains(OFP_SETTINGS_TAG)))
            {
                //No active ini data so we will set a default
                iPB.CustomData = OFP_SETTINGS_TAG;
                iPB.CustomData += "\n; Edit the below to change how this screen reacts.";
                iPB.CustomData += "\n; Options:";
                iPB.CustomData += "\n; default = Allow this screen to navigate all menus";
                iPB.CustomData += "\n; ore = Always show ore status";
                iPB.CustomData += "\n; coordinates = Always show coordinate screen";
                iPB.CustomData += "\n; status = shows the status of current (or single) scan";
                iPB.CustomData += "\n; ministatus = shows scan status on small screens";
                iPB.CustomData += "\n; settings = screen to set options";
                iPB.CustomData += "\n; :::::EXAMPLE::::";
                iPB.CustomData += "\n; OFP@0=default";
                iPB.CustomData += "\n; OFP@2=status";
                iPB.CustomData += "\nOFP@0 = default";
                iPB.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
            }
            PBs.Add(iPB);
        }
    }

    if (lcdPanels.Count == 0 && cockpits.Count == 0 && PBs.Count == 0)
    {
        throw new Exception($"Error: No LCD Panels, Cockpits, or Programmable Blocks found with the {OFP_TAG} tag in their name or Custom Data.");
    }
}

private void HandleSettings()
{
    switch (currentSelection)
    {
        case (1):
            //Disable/Enable OFP
            disableOFP = !disableOFP;
            if (disableOFP)
            {
                azimuth = minAngle;
                elevation = minAngle;
            }
            break;
        case (2):
            scanMode++;
            scanMode %= OFPScanMode.Count;
            flashedOn = 0;
            scans_Completed = 0;
            break;
        case (3):
            scanOnce = !scanOnce;
            if (scanOnce)
                scans_Completed = 0;
            break;
        case (4):
            //Disable/Enable quickscanning
            quickScan = !quickScan;
            break;
        case (5):
            //Setting of Distance to scan
            setDistance = !setDistance;
            break;
        case (6):
            currentScreen = 0;
            currentSelection = 1;
            break;
        default:
            currentSelection = 1;
            currentScreen = 5;
            break;
    }
    RefreshScreens();
}
private void HandleMenu(string cmd)
{
    switch (cmd.ToLower())
    {
        case "screen":
            currentScreen = (currentScreen >= maxScreen) ? 0 : currentScreen + 1;
            break;
        case "apply":
            switch (currentScreen)
            {
                case (0):
                    currentScreen = currentSelection;
                    currentSelection = 1;
                    break;
                case (5):
                    //need to change some settings here and refresh.  Only need to change the screen if we are on the "return to menu" option
                    HandleSettings();
                    break;
                default:
                    currentScreen = 0;
                    currentSelection = 1;
                    break;
            }
            break;
        case "up":
            if (currentSelection > 1)
            {
                switch (currentScreen)
                {
                    case (0):
                        currentSelection--;
                        break;
                    case (5):
                        if (setDistance)
                        {
                            detectionDistance += 100;
                        }
                        else
                        {
                            currentSelection--;
                        }
                        break;
                }
            }
            break;
        case "down":
            switch (currentScreen)
            {
                case (0):
                    if (currentSelection < maxSelection)
                    {
                        currentSelection++;
                    }
                    break;
                case (5):
                    if (setDistance)
                    {
                        if (detectionDistance > 0)
                        {
                            detectionDistance -= 100;
                        }
                    }
                    else
                    {
                        if (currentSelection < maxSettingsSelection)
                        {
                            currentSelection++;
                        }
                    }
                    break;
            }
            break;
        case "menu":
            currentScreen = 0;
            currentSelection = 1;
            break;
        case "clear":
            currentScreen = 3;
            currentSelection = 1;
            break;
        case "enable":
            detector.Enabled = true;
            disableOFP = true;
            flashedOn = 0;
            scans_Completed = 0;
            break;
        case "disable":
            detector.Enabled = false;
            disableOFP = false;
            flashedOn = 0;
            scans_Completed = 0;
            break;
        case "scan":
            //scan once feature
            scanOnce = true;
            disableOFP = false;
            detector.Enabled = true;
            //Best scan once mode is probably 360 scan. However let us allow the end-user to use whatever mode they like.
            //This is set above by default to 360 but can be changed in the menu items.
            //scanMode = 1;
            flashedOn = 0;
            scans_Completed = 0;
            break;
        default:
            currentSelection = 1;
            currentScreen = 0;
            break;
    }
    RefreshScreens();
}

private void RefreshScreens()
{
    foreach (IMyCockpit iCockPit in cockpits)
    {
        MyIniParseResult result;
        if (!_ini.TryParse(iCockPit.CustomData, out result))
            throw new Exception(result.ToString());
        for (int cPanel = 0; cPanel < iCockPit.SurfaceCount - 1; cPanel++)
        {
            string screenType = _ini.Get("OreFinderPlus", $"OFP@{cPanel}").ToString();
            if (screenType != "")
            {
                iCockPit.GetSurface(cPanel).ContentType = ContentType.TEXT_AND_IMAGE;
                switch (screenType)
                {
                    case "default":
                        ShowScreen(currentScreen, iCockPit.GetSurface(cPanel));
                        break;
                    case "menu":
                        ShowScreen(0, iCockPit.GetSurface(cPanel), true);
                        break;
                    case "ore":
                        ShowScreen(1, iCockPit.GetSurface(cPanel), true);
                        break;
                    case "coordinates":
                        ShowScreen(2, iCockPit.GetSurface(cPanel), true);
                        break;
                    case "status":
                        ShowScreen(4, iCockPit.GetSurface(cPanel), true);
                        break;
                    case "ministatus":
                        ShowScreen(7, iCockPit.GetSurface(cPanel), true, true);
                        break;
                    case "settings":
                        ShowScreen(5, iCockPit.GetSurface(cPanel), true);
                        break;
                    default:
                        ShowScreen(0, iCockPit.GetSurface(cPanel), true);
                        break;
                }
            }
        }
    }
    foreach (IMyTextPanel panel in lcdPanels)
    {
        MyIniParseResult result;
        if (!_ini.TryParse(panel.CustomData, out result))
            throw new Exception(result.ToString());
        string screenType = _ini.Get("OreFinderPlus", "Screen").ToString("default");
        switch (screenType)
        {
            case "default":
                ShowScreen(currentScreen, panel);
                break;
            case "menu":
                ShowScreen(0, panel, true);
                break;
            case "ore":
                ShowScreen(1, panel, true);
                break;
            case "coordinates":
                ShowScreen(2, panel, true);
                break;
            case "status":
                ShowScreen(4, panel, true);
                break;
            case "ministatus":
                ShowScreen(7, panel, true);
                break;
            case "settings":
                ShowScreen(5, panel, true);
                break;
            default:
                ShowScreen(0, panel, true);
                break;
        }


    }
    // Refresh PB Screen (Copy/Paste/Edit of Cockpit loop). (Radar5k).
    foreach (IMyProgrammableBlock iPB in PBs)
    {
        MyIniParseResult result;
        if (!_ini.TryParse(iPB.CustomData, out result))
            throw new Exception(result.ToString());
        for (int cPanel = 0; cPanel < iPB.SurfaceCount - 1; cPanel++)
        {
            string screenType = _ini.Get("OreFinderPlus", $"OFP@{cPanel}").ToString();
            if (screenType != "")
            {
                iPB.GetSurface(cPanel).ContentType = ContentType.TEXT_AND_IMAGE;
                switch (screenType)
                {
                    case "default":
                        ShowScreen(currentScreen, iPB.GetSurface(cPanel));
                        break;
                    case "menu":
                        ShowScreen(0, iPB.GetSurface(cPanel), true);
                        break;
                    case "ore":
                        ShowScreen(1, iPB.GetSurface(cPanel), true);
                        break;
                    case "coordinates":
                        ShowScreen(2, iPB.GetSurface(cPanel), true);
                        break;
                    case "status":
                        ShowScreen(4, iPB.GetSurface(cPanel), true);
                        break;
                    case "ministatus":
                        ShowScreen(7, iPB.GetSurface(cPanel), true, true);
                        break;
                    case "settings":
                        ShowScreen(5, iPB.GetSurface(cPanel), true);
                        break;
                    default:
                        ShowScreen(0, iPB.GetSurface(cPanel), true);
                        break;
                }
            }
        }
    }

}
private void DisplayMenu(IMyTextSurface panel, bool staticScreen = false)
{
    string menuSystem = "";
    menuSystem += $"***** {PRODUCT_NAME}: {VERSION_NUMBER} *****";
    menuSystem += (currentSelection == 1 || currentSelection == 0) ? "\n> Deposits Found" : "\n  Deposits Found";
    menuSystem += (currentSelection == 2) ? "\n> Ore Coordinates" : "\n  Ore Coordinates";
    menuSystem += (currentSelection == 3) ? "\n> Logging Data" : "\n  Logging Data";
    menuSystem += (currentSelection == 4) ? "\n> Status" : "\n  Status";
    menuSystem += (currentSelection == 5) ? "\n> Settings" : "\n  Settings";
    menuSystem += (currentSelection == 6) ? "\n> Clear Data" : "\n  Clear Data";
    WriteToLCD(menuSystem, panel);
}

private void DisplayMiniScanStatus(IMyTextSurface panel, bool staticScreen = false, bool isCockpit = false)
{
    string scanMiniStatus = "";
    if (disableOFP)
    {
        //Show a disabled screen
        if (!isCockpit)
            panel.FontSize = (float)5.5;
        panel.FontColor = Color.Red;
        panel.Alignment = TextAlignment.CENTER;
        scanMiniStatus = "Scan\nDisabled";
    }
    else
    {
        if (!miniFlash)
        {
            if (!isCockpit)
                panel.FontSize = (float)4.5;
            panel.FontColor = Color.White;
            panel.Alignment = TextAlignment.LEFT;
            scanMiniStatus += $"Scans:{scans_Completed}";
            scanMiniStatus += $"\nAzimuth:{azimuth}";
            scanMiniStatus += $"\nElevation:{elevation}";
        }
        else
        {
            if (scanOnce && scans_Completed >= 1)
            {
                miniFlashTicks = 0;
            }

            if (miniFlashTicks < maxMiniFlashTicks)
            {
                if (!isCockpit)
                    panel.FontSize = (float)5.5;
                panel.FontColor = Color.Green;
                panel.Alignment = TextAlignment.CENTER;
                scanMiniStatus = "Scan\nComplete";
                miniFlashTicks++;
            }
            else
            {
                if (!isCockpit)
                    panel.FontSize = (float)4.5;
                panel.FontColor = Color.White;
                miniFlash = false;
                miniFlashTicks = 0;
            }
        }
    }

    if (scanMode == 0)
    {
        //Change the status when the scanmode is direct line
        if (!isCockpit)
            panel.FontSize = (float)4.5;
        panel.FontColor = Color.White;
        panel.Alignment = TextAlignment.LEFT;
        scanMiniStatus = $"{OFPScanMode[0]}";
        scanMiniStatus += $"\nCharge:{detector.GetValue<double>("AvailableScanRange")}";
    }


    if (!staticScreen)
        scanMiniStatus += "\n> Return To Menu";

    WriteToLCD(scanMiniStatus, panel);
}

private void DisplayScanStatus(IMyTextSurface panel, bool staticScreen = false)
{
    string scanStatus = "";

    scanStatus += $"***** [OFP Scan Status] *****";
    scanStatus += $"\nOFP Enabled:{!disableOFP}";
    scanStatus += $"\nScan Once:{scanOnce}";
    scanStatus += $"\nMode:{OFPScanMode[scanMode]}";
    scanStatus += $"\nQuick Scan:{quickScan}";
    scanStatus += $"\nScans Completed:{scans_Completed}";
    scanStatus += $"\nOre Deposits Found: {DiscoveredOre.Count()}";
    scanStatus += $"\nOre Detector Enabled:{detector.Enabled}";
    scanStatus += $"\nAzimuth Angle:{azimuth}";
    scanStatus += $"\nElevation Angle:{elevation}";
    scanStatus += $"\nDistance Set:{detectionDistance}";
    scanStatus += $"\nAvailable Range:{detector.GetValue<double>("AvailableScanRange")}";
    scanStatus += $"\nIgnore List:{ignoreList}";
    if (!staticScreen)
        scanStatus += "\n> Return To Menu";

    WriteToLCD(scanStatus, panel);
}

private void DisplaySettings(IMyTextSurface panel, bool staticScreen = false)
{
    string menuSettings = "";

    menuSettings += $"***** [OFP Settings] *****";
    menuSettings += (currentSelection == 1) ? $"\n> OFP Enabled: {!disableOFP}" : $"\n  OFP Enabled: {!disableOFP}";
    menuSettings += (currentSelection == 2) ? $"\n> Scan Mode: {OFPScanMode[scanMode]}" : $"\n  Scan Mode: {OFPScanMode[scanMode]}";
    menuSettings += (currentSelection == 3) ? $"\n> Scan Once: {scanOnce}" : $"\n  Scan Once: {scanOnce}";
    menuSettings += (currentSelection == 4) ? $"\n> Quick Scanning: {quickScan}" : $"\n  Quick Scanning: {quickScan}";
    menuSettings += (currentSelection == 5) ? (setDistance) ? $"\n► Distance: {detectionDistance}" : $"\n> Distance: {detectionDistance}" : $"\n  Distance: {detectionDistance}";
    menuSettings += (currentSelection == 6) ? $"\n> Return To Menu" : $"\n  Return To Menu";

    WriteToLCD(menuSettings, panel);
}
private void WriteToLCD(string text_out, IMyTextSurface screen)
{

    if (screen != null)
    {
        screen.ContentType = ContentType.TEXT_AND_IMAGE;
        screen.WriteText(text_out);
    }
    else
    {
        Echo($"\nNo LCD found. Add {OFP_TAG} to the name of one or more LCD's.");
    }
}

private void DisplayDepositsFound(IMyTextSurface panel, bool staticScreen = false)
{
    string deposits = "***** Ore Depoits *****";
    //Switching over to a dictionary as the "hard coded" list only works with vanilla SE.  Want to mod this to work with any ore detected.
    Dictionary<string, int> oreForDisplay = new Dictionary<string, int>();

    foreach (MyDetectedEntityInfo oreInstance in DiscoveredOre)
    {
        string oreKey = oreInstance.Name;
        if (oreForDisplay.ContainsKey(oreKey))
        {
            int amount = oreForDisplay[oreKey];
            amount += 1;
            oreForDisplay[oreKey] = amount;
        }
        else
        {
            oreForDisplay.Add(oreKey, 1);
        }
    }
    //Echo($"Deposit Count: {oreForDisplay.Count()}");

    foreach (KeyValuePair<string, int> orePair in oreForDisplay)
    {
        deposits += $"\n{orePair.Key}={orePair.Value}";
    }
    if (!staticScreen)
        deposits += "\n> Return To Menu";
    WriteToLCD(deposits, panel);
}

private void ClearData(IMyTextSurface panel, bool staticScreen = false)
{
    //Clear all data from the recorded Ores
    string cleared = "***** DATA CLEARED *****";
    if (!staticScreen)
        cleared += "\n> Return To Menu";
    DiscoveredOre = new List<MyDetectedEntityInfo>();
    scans_Completed = 0;
    WriteToLCD(cleared, panel);
}
private void DisplayKnownOreGPS(IMyTextSurface panel, bool staticScreen = false)
{
    string locationGPS = "*** ORE GPS ***";
    foreach (MyDetectedEntityInfo oreInstance in DiscoveredOre)
    {
        Vector3D location = (Vector3D)oreInstance.HitPosition;
        locationGPS += $"\nGPS:{OFP_TAG}-{oreInstance.Name}:{location.X}:{location.Y}:{location.Z}:";
    }
    locationGPS += $"\n{DiscoveredOre.Count()}";
    if (!staticScreen)
        locationGPS += "\n> Return To Menu";
    WriteToLCD(locationGPS, panel);

}

private void ShowScreen(int selection, IMyTextSurface currentPanel, bool staticScreen = false, bool isCockpit = false)
{
    switch (selection)
    {
        case 0:
            DisplayMenu(currentPanel, staticScreen);
            break;
        case 1:
            DisplayDepositsFound(currentPanel, staticScreen);
            break;
        case 2:
            DisplayKnownOreGPS(currentPanel, staticScreen);
            break;
        case 3:
            DisplayCompareAll(currentPanel, staticScreen);
            break;
        case 4:
            DisplayScanStatus(currentPanel, staticScreen);
            break;
        case 5:
            DisplaySettings(currentPanel, staticScreen);
            break;
        case 6:
            ClearData(currentPanel, staticScreen);
            break;
        case 7:
            DisplayMiniScanStatus(currentPanel, staticScreen, isCockpit);
            break;
        default:
            DisplayMenu(currentPanel, staticScreen);
            break;
    }
}
public Program()
{
    var detectors = new List<IMyOreDetector>();
    GridTerminalSystem.GetBlocksOfType(detectors);
    if (detectors.Count == 0)
    {
        throw new Exception("Error: No ore detector found");
    }

    //Load from saved settings
    string[] storedData = Storage.Split(';');
    if (storedData.Count() == 5)
    {
        try
        {
            //Lets try to parse out the saved values and set them again
            bool tmpBool = true;
            int tmpInt = 0;

            //Disable OFP
            if (bool.TryParse(storedData[0], out tmpBool))
                disableOFP = tmpBool;
            else
                sError += "\nError Parsing Saved disableOFP value";

            //Scan Mode
            if (int.TryParse(storedData[1], out tmpInt))
                scanMode = tmpInt;
            else
                sError += "\nError Parsing Saved scanMode value";

            //Scan Once
            if (bool.TryParse(storedData[2], out tmpBool))
                scanOnce = tmpBool;
            else
                sError += "\nError Parsing Saved Scan Once value";

            //Quick Scanning
            if (bool.TryParse(storedData[3], out tmpBool))
                quickScan = tmpBool;
            else
                sError += "\nError Parsing Saved quickScan value";

            //Distance
            if (int.TryParse(storedData[4], out tmpInt))
                detectionDistance = tmpInt;
            else
                sError += "\nError Parsing Saved detectionDistance value";

        }
        catch (Exception e)
        {
            sError = e.Message + sError;
            throw (new Exception(sError));
        }

    }
    detector = detectors.First();
    surface = Me.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = 3;
    Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10 | UpdateFrequency.Update1;

    //Thinking about adding a broadcast to allow bases to record ore too
    //IGC.RegisterBroadcastListener(OFP_TAG);
    RegisterLCDs();
    SetupDetector();
    ClearLCD();
    RefreshScreens();
}
private void SetupDetector()
{
    //This function was taken from the sample from @Reacher
    // https://steamcommunity.com/sharedfiles/filedetails/?id=1967149949
    var t = new Vector3D(-0.75, 0, 0.5);
    try
    {
        detector.SetValue("RaycastTarget", t);
    }
    catch
    {
        //Seems the Mod is missing if we cant set the proper value on the ore detector
        Echo("Error:\n No OreDetectorRaycast Mod\n*Please make sure that the OreDetectorRaycast mod is installed and enabled*\nhttps://steamcommunity.com/workshop/filedetails/?id=1967157772");
        Me.Enabled = false;
    }
    if (detector.GetValue<Vector3D>("RaycastTarget") != t)
        throw new Exception("RaycastTarget inequal");
    detector.SetValue("RaycastTarget", detector.GetPosition() + detector.WorldMatrix.Forward);
    var res = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
    if (res.TimeStamp == 0)
        throw new Exception("RaycastResult invalid");
    detector.SetValue("OreBlacklist", ignoreList);
    if (detector.GetValue<string>("OreBlacklist") != ignoreList)
        throw new Exception("OreIgnorelist inequal");
    try
    {
        detector.SetValue("ScanEpoch", 0L);
    }
    catch
    {
        return;
    }
    throw new Exception("shouldnt be able to write ScanEpoch");
}

public void Save()
{
    //We should save any custom settings the user has set in the menus
    Storage = string.Join(";",
        disableOFP.ToString(),
        scanMode,
        scanOnce.ToString(),
        quickScan.ToString(),
        detectionDistance);
}
public void Main(string argument, UpdateType updateSource)
{
    try
    {
        var AvailableScanRange = detector.GetValue<double>("AvailableScanRange");
        MyCommandLine _commandLine = new MyCommandLine();
        if (_commandLine.TryParse(argument))
        {
            test = _commandLine.Argument(0);
            HandleMenu(_commandLine.Argument(0));
        }

        Vector3D target = new Vector3D();

        if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
        {
            if ((!quickScan && AvailableScanRange < detectionDistance) || disableOFP)
            {
                //We want to exit out and make sure we only scan if the distance is enough.
                //We also want to exit out if we have OFP Disabled

                return;
            }

            if (scanOnce)
                if (scans_Completed >= 1)
                {
                    //Once we've scanned once we will turn off the detector and stop scanning.
                    miniFlash = true;
                    //Not sure I need to disable the Detector as that doesnt disable the script.  But if I return before the "ray" is cast then we may save on performance
                    //detector.Enabled = false;
                    return;
                }
            Vector3D targetDirection;

            switch (scanMode)
            {
                case (0):
                default:
                    target = detector.GetPosition() + detector.WorldMatrix.Forward * ((updateSource & UpdateType.Update100) != 0 ? AvailableScanRange / 2 : detectionDistance);
                    break;
                case (1):

                    //Need to do a full elevation rotation before moving the azimuth
                    if (azimuth <= maxAngle)
                    {
                        if (elevation <= sphereElevationMaxAngle)
                        {

                            elevation += sphereScanStep;
                        }
                        else
                        {
                            elevation = sphereElevationMinAngle;
                            azimuth += sphereScanStep;
                        }
                    }
                    else
                    {
                        azimuth = minAngle;
                        elevation = sphereElevationMinAngle;
                        scans_Completed++;
                    }
                    Vector3D.CreateFromAzimuthAndElevation(azimuth, elevation, out targetDirection);
                    targetDirection = Vector3D.TransformNormal(targetDirection, detector.WorldMatrix);
                    target = detector.GetPosition() + detector.WorldMatrix.Forward + targetDirection * detectionDistance;
                    break;
                case (2):
                    //We are creating a projected plane detectionDistance forward from the ore detector of size -forward_azimuth to forward_azimuth and -forward_elevation to forward_elevation
                    //Need to do a full elevation rotation before moving the azimuth
                    if (azimuth < forward_azimuth)
                    {
                        if (elevation < forward_elevation)
                        {

                            elevation += forwardScanStep;
                        }
                        else
                        {
                            elevation = -forward_elevation;
                            azimuth += forwardScanStep;
                        }
                    }
                    else
                    {
                        azimuth = -forward_azimuth;
                        elevation = -forward_elevation;
                        scans_Completed += .5;
                    }
                    Vector3D.CreateFromAzimuthAndElevation(azimuth, elevation, out targetDirection);
                    targetDirection = Vector3D.TransformNormal(targetDirection, detector.WorldMatrix);
                    target = detector.GetPosition() + detector.WorldMatrix.Forward + targetDirection * detectionDistance;
                    break;
            }

            if (detector.Enabled)
            {
                if (scans_Completed % 1 == 0 && scans_Completed > 0 && scans_Completed > flashedOn)
                {
                    miniFlash = true;
                    flashedOn = scans_Completed;
                }
                //Echo($"Azimuth {azimuth}\nElevation {elevation}\nScans Completed {scans_Completed}");
                detector.SetValue("RaycastTarget", target);

                if (DEBUGGING)
                    throw (new DivideByZeroException());

                var result = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
                if (!result.IsEmpty())
                {
                    IsKnown(result);
                }
            }
        }
        RefreshScreens();

    }
    catch (Exception e)
    {
        Echo($"Error {e.StackTrace.ToString()}");
        Me.Enabled = false;
    }
}