
/*Script Author: Onosendai
         * Description: This script leverages the Ore Detector Raytrace mode allowing you to automatically record ore it "hits".
         * Credits: Big thanks to @Reacher for the MOD allowing Ray Tracing of Ore.
         *          https://steamcommunity.com/workshop/filedetails/?id=1967157772
        */

//Tag for Ore Finder Plus to look for on the ore detector or LCD.Either tag the name or custom data
//   Example Name: LCD Test [OFP]
//                 LCD Port Side OFP
private static string OFP_TAG = "OFP";

//ignoreList will allow you to exclude types of ORE.
//example: "Stone,Ice"
string ignoreList = "Stone";

//Detection Distance in meters.  Default set to 1KM
int detectionDistance = 1000;

//Deposit Range is the potential size of a deposit of ore.  The Below size is in meters.
//If you start getting the same ore deposits tagged with multiple GPS coordinates
//then make this number larger.
int depositRange = 200;



//*******Dont Touch Below here********
//************************************

private static double VERSION_NUMBER = 1.1;
private static string PRODUCT_NAME = "Ore Finder Plus";
//All the known ore and locations we've found
List<MyDetectedEntityInfo> DiscoveredOre = new List<MyDetectedEntityInfo>();
readonly IMyOreDetector detector;
readonly IMyTextSurface surface;

List<IMyTextPanel> lcdPanels = new List<IMyTextPanel>();

//Menu Handling Options
int currentScreen = 0;
/*Screens:
        0=Menu
        1=Status
        2=GPS Coordinates
        3=Compare All
        4=Clear Data
        */
int maxScreen = 4;
int currentSelection = 1;
int maxSelection = 4;
MyIni _ini = new MyIni();
string test;

/* ToDO:
         * Add Better Error Handling and reporting on errors
         * Make ignoreList easy to edit (perhaps in the menu)
         * Perhaps add a bootup splash screen
         * Add the ability to "start" and "stop" scanning (just disable the ore detector?
         * Add the ability to broadcast the ore to a channel listener so displays on a remote base can see the ore.  This would also allow for drones to zoom around scanning and have ore results at the base.
         * Echo data for found deposits to the PB and any errors.
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
        }
    }
    if (!known)
    {
        DiscoveredOre.Add(foundOre);
    }
    return known;
}
private void DisplayCompareAll(IMyTextPanel panel, bool staticScreen = false)
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
    GridTerminalSystem.GetBlocksOfType(allPanels);
    foreach (IMyTextPanel panel in allPanels)
    {
        if (panel.CustomName.Contains(OFP_TAG))
        {
            //Grab all panels that have the OFP_TAG in their name.

            //Check if the panel already has our INI data
            MyIniParseResult result;
            _ini.TryParse(panel.CustomData, out result);
            if (_ini.Get("OreFinderPlus", "Screen").ToString() == "")
            {
                //No active ini data so we will set a default
                panel.CustomData = "[OreFinderPlus]";
                panel.CustomData += "\n; Edit the below to change how this screen reacts.";
                panel.CustomData += "\n; Options:";
                panel.CustomData += "\n; default = Allow this screen to navigate all menus";
                panel.CustomData += "\n; ore = Always show ore status";
                panel.CustomData += "\n; coordinates = Always show coordinate screen";
                panel.CustomData += "\nScreen = default";
            }
            lcdPanels.Add(panel);
        }
    }
    if (lcdPanels.Count == 0)
    {
        throw new Exception($"Error: No LCD Panels found with the {OFP_TAG} tag name.");
    }
}

private void HandleMenu(string cmd)
{
    switch (cmd.ToLower())
    {
        case "screen":
            currentScreen = (currentScreen >= maxScreen) ? 0 : currentScreen + 1;
            break;
        case "apply":
            if (currentScreen != 0)
            {
                currentScreen = 0;
                currentSelection = 1;
            }
            else
            {
                currentScreen = currentSelection;
            }

            break;
        case "up":
            if (currentSelection > 1 && currentScreen == 0)
                currentSelection--;
            break;
        case "down":
            if (currentSelection < maxSelection && currentScreen == 0)
                currentSelection++;
            break;
        case "menu":
            currentScreen = 0;
            currentSelection = 1;
            break;
        case "clear":
            currentScreen = 3;
            currentSelection = 1;
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
            default:
                ShowScreen(0, panel, true);
                break;
        }


    }
}
private void DisplayMenu(IMyTextPanel panel, bool staticScreen = false)
{
    string menuSystem = "";
    menuSystem += $"***** {PRODUCT_NAME}: {VERSION_NUMBER} *****";
    menuSystem += (currentSelection == 1 || currentSelection == 0) ? "\n> Deposits Found" : "\n  Deposits Found";
    menuSystem += (currentSelection == 2) ? "\n> Ore Coordinates" : "\n  Ore Coordinates";
    menuSystem += (currentSelection == 3) ? "\n> Logging Data" : "\n  Logging Data";
    menuSystem += (currentSelection == 4) ? "\n> Clear Data" : "\n  Clear Data";
    WriteToLCD(menuSystem, panel);
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

private void DisplayDepositsFound(IMyTextPanel panel, bool staticScreen = false)
{
    string deposits = "***** Depoits *****";
    int iron = 0, stone = 0, nickel = 0, silicon = 0, cobalt = 0, magnesium = 0, silver = 0, gold = 0, uranium = 0, platinum = 0, ice = 0;

    foreach (MyDetectedEntityInfo oreInstance in DiscoveredOre)
    {

        switch (oreInstance.Name.ToLower())
        {
            case "iron":
                iron++;
                break;
            case "stone":
                stone++;
                break;
            case "nickel":
                nickel++;
                break;
            case "silicon":
                silicon++;
                break;
            case "cobalt":
                cobalt++;
                break;
            case "silver":
                silver++;
                break;
            case "gold":
                gold++;
                break;
            case "uranium":
                uranium++;
                break;
            case "platinum":
                platinum++;
                break;
            case "ice":
                ice++;
                break;
            default:
                break;
        }
    }
    deposits += $"\nIron={iron}";
    deposits += $"\nStone={stone}";
    deposits += $"\nNickel={nickel}";
    deposits += $"\nSilicon={silicon}";
    deposits += $"\nCobalt={cobalt}";
    deposits += $"\nMagnesium={magnesium}";
    deposits += $"\nSilver={silver}";
    deposits += $"\nGold={gold}";
    deposits += $"\nUranium={uranium}";
    deposits += $"\nPlatinum={platinum}";
    deposits += $"\nIce={ice}";
    if (!staticScreen)
        deposits += "\n> Return To Menu";
    WriteToLCD(deposits, panel);
}

private void ClearData(IMyTextPanel panel, bool staticScreen = false)
{
    //Clear all data from the recorded Ores
    string cleared = "***** DATA CLEARED *****";
    if (!staticScreen)
        cleared += "\n> Return To Menu";
    DiscoveredOre = new List<MyDetectedEntityInfo>();
    WriteToLCD(cleared, panel);
}
private void DisplayKnownOreGPS(IMyTextPanel panel, bool staticScreen = false)
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

private void ShowScreen(int selection, IMyTextPanel currentPanel, bool staticScreen = false)
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
            ClearData(currentPanel, staticScreen);
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
    detector.SetValue("RaycastTarget", t);
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
        if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
        {
            detector.SetValue("RaycastTarget", detector.GetPosition() + detector.WorldMatrix.Forward * ((updateSource & UpdateType.Update100) != 0 ? AvailableScanRange / 2 : detectionDistance));
            var result = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
            if (!result.IsEmpty())
            {
                IsKnown(result);
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