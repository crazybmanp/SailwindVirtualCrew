using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace SailwindVirtualCrew
{
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.zorkinian.virtualcrew";
        public const string PLUGIN_NAME = "VirtualCrew";
        public const string PLUGIN_VERSION = "0.1.5";

        //--settings--
        internal static ConfigEntry<bool> exampleSetting;
        internal static ConfigEntry<KeyboardShortcut> ToggleCrewWindow;
        internal static ConfigEntry<bool> ExtraWorkingStaminaDrain;

        // PID slider ranges
        internal static ConfigEntry<float> PidMaxP;
        internal static ConfigEntry<float> PidMaxI;
        internal static ConfigEntry<float> PidMaxD;

        private float tickTimer = 0f;
        private const float tickInterval = 1f;

        private ConfigEntry<KeyboardShortcut> BuildShipMap;
        private ConfigEntry<KeyboardShortcut> ScanItems;

        public static Plugin Instance { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_ID);

            exampleSetting = Config.Bind("Section", "Key", true, new ConfigDescription("Information about the config setting"));

            PidMaxP = Config.Bind("Autopilot", "PidMaxP", 0.25f, "Maximum value for the P (proportional) slider.");
            PidMaxI = Config.Bind("Autopilot", "PidMaxI", 0.25f, "Maximum value for the I (integral) slider.");
            PidMaxD = Config.Bind("Autopilot", "PidMaxD", 0.25f, "Maximum value for the D (derivative) slider.");
            ExtraWorkingStaminaDrain = Config.Bind(
                "Crew",
                "ExtraWorkingStaminaDrain",
                false,
                "When enabled, crew assigned to active tasks lose stamina twice as fast. When disabled, working and idle crew use the same baseline stamina drain.");

            ToggleCrewWindow = Config.Bind("CrewHotkeys", "ToggleCrewWindow", new KeyboardShortcut(KeyCode.B));
            BuildShipMap = Config.Bind("CrewHotkeys", "BuildShipMap", new KeyboardShortcut(KeyCode.V));

            ScanItems = Config.Bind("CrewHotkeys", "ScanItems", new KeyboardShortcut(KeyCode.P));

            gameObject.AddComponent<CrewSoundPlayer>();
            gameObject.AddComponent<DeveloperWindow>();
            gameObject.AddComponent<VirtualCrewDebugWindow>();
            gameObject.AddComponent<CrewWindow>();
            gameObject.AddComponent<SailGroupsWindow>();
            gameObject.AddComponent<SailGroupMembersWindow>();
            gameObject.AddComponent<WorkRequestsWindow>();
            gameObject.AddComponent<NavigatorWindow>();
            gameObject.AddComponent<MaintenanceWindow>();
            gameObject.AddComponent<PilotingWindow>();
            gameObject.AddComponent<CrewRosterWindow>();
            gameObject.AddComponent<LookoutWindow>();
            gameObject.AddComponent<WorkstationCustomizerWindow>();
            gameObject.AddComponent<FavoriteActionsWindow>();
        }

        private void Start()
        {
            ModIntegrations.Initialize(_harmony);
            CrewApiProbe.Run();
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                VirtualCrewManager.Instance.Tick();
            }

            VirtualCrewManager.Instance.TrimTick();
            CrewDebugObjects.Tick();
            CrewNavigationCoordinator.Instance.Tick();

            if (BuildShipMap.Value.IsDown())
            {
            Console.WriteLine("====================");
            Console.WriteLine("Building ship map!");
            Console.WriteLine("====================");
            // Ok, now to learn about iteration. Grab each mast, get all sails on mast, give them a name, spray output.
            string vesselKey = GameState.currentBoat.name.Replace("(Clone)", "").Trim();
                VirtualCrewManager.Instance.SetCurrentVessel(vesselKey);
                VirtualCrewManager.Instance.Reset();

                GPButtonRopeWinch[] winches = GameState.currentBoat.GetComponentsInChildren<GPButtonRopeWinch>();
                // Build a dictionary of all winches by rope controller
                var winchDictionary = new Dictionary<RopeController, GPButtonRopeWinch>();
                foreach (GPButtonRopeWinch winch in winches)
                {
                    if (winch.rope == null) continue;
                    winchDictionary.Add(winch.rope, winch);
                }


                Mast[] mastList = GameState.currentBoat.GetComponentsInChildren<Mast>();

                var mastNameDictionary = new Dictionary<string, Mast>();
                var processedSails = new HashSet<Sail>();

                Console.WriteLine("Found the following masts:");
                var squareSheetControllersByMastFamily = new Dictionary<string, RopeController[]>();

                foreach (Mast mast in mastList)
                {
                    Console.WriteLine("-" + mast.name);
                    if (!mastNameDictionary.ContainsKey(mast.name))
                    {
                        mastNameDictionary.Add(mast.name, mast);
                    }
                }
                Console.WriteLine("-------------------------------------------");

                foreach (Mast mast in mastList)
                {
                    Console.WriteLine("Processing mast:" + mast.name);
                    Sail[] sails = mast.GetComponentsInChildren<Sail>(); //get all the sails attached to said mast

                    Console.WriteLine("Mast has the following sails:");
                    foreach (Sail sail in sails)
                    {
                        Console.WriteLine("-" + sail.name);
                    }

                    // There are some cases where a mast is just an "Extension" of another mast. We need
                    // to treat it as though it's part of the main mast.
                    string possibleExtensionName = mast.name + "_extension"; 
                    if (mastNameDictionary.ContainsKey(possibleExtensionName))
                    {
                        Console.WriteLine("Mast also has an extension. Adding those sails to be processed.");
                        var extensionSails = mastNameDictionary[possibleExtensionName].GetComponentsInChildren<Sail>();
                        foreach (Sail sail in extensionSails)
                        {
                            Console.WriteLine("-" + sail.name);
                        }

                        sails = sails.AddRangeToArray(extensionSails);
                    }

                    List<Sail> deferredSquares = new List<Sail>();
                    RopeController portRopeControllerForSquares = null;
                    RopeController starboardRopeControllerForSquares = null;

                    foreach (Sail sail in sails) {
                        if (processedSails.Contains(sail))
                        {
                            Console.WriteLine("Skipping already processed sail:" + sail.name);
                            continue;
                        }

                        processedSails.Add(sail);
                        Console.WriteLine(string.Format("Processing the combination of Mast: {0}, Sail: {1}", mast.name, sail.name));

                        GPButtonRopeWinch halyardCandidate = null;
                        GPButtonRopeWinch sheetCandidate = null;
                        GPButtonRopeWinch portSheetCandidate = null;
                        GPButtonRopeWinch starboardSheetCandidate = null;

                        SailConnections connections = sail.GetComponent<SailConnections>();

                        // Add sail to map
                        if ((sail.name.Contains("lateen") || sail.name.Contains("gaff")))
                        {
                            Console.WriteLine("Attempting to add lateen or gaff");
                            halyardCandidate = winchDictionary[connections.reefController];
                            sheetCandidate = winchDictionary[connections.angleControllerMid];

                            SimpleSail lateen = new SimpleSail(sail, halyardCandidate, sheetCandidate, mast.name);
                            VirtualCrewManager.Instance.addSail(lateen);
                            Console.WriteLine("Successfully added sail to map:" + sail.name);
                            Console.WriteLine("---");
                        }
                        else if (sail.squareSail || sail.name.Contains("square"))
                        {
                            Console.WriteLine("Attempting to add square sail");

                            if (!winchDictionary.Keys.Contains(connections.angleControllerLeft))
                            {
                                // This is one of those squares that is ganged to another square. We need to find the rope controller for that square.
                                Console.WriteLine("This is a deferred square sail");
                                deferredSquares.Add(sail);
                            }
                            else {
                                Console.WriteLine("This is a primary square sail");
                                halyardCandidate = winchDictionary[connections.reefController];
                                Console.WriteLine("Got halyard winch:" + halyardCandidate.name);
                                portRopeControllerForSquares = connections.angleControllerLeft;
                                Console.WriteLine("Got port rope controller:" + portRopeControllerForSquares.name);
                                starboardRopeControllerForSquares = connections.angleControllerRight;
                                Console.WriteLine("Got starboard rope controller:" + starboardRopeControllerForSquares.name);
                                squareSheetControllersByMastFamily[GetMastFamilyName(mast.name)] = new[] { portRopeControllerForSquares, starboardRopeControllerForSquares };

                                portSheetCandidate = winchDictionary[portRopeControllerForSquares];
                                Console.WriteLine("Got port sheet winch:" + portSheetCandidate.name);
                                starboardSheetCandidate = winchDictionary[starboardRopeControllerForSquares];
                                Console.WriteLine("Got starboard sheet winch:" + starboardSheetCandidate.name);

                                DualSheetSail dual = new DualSheetSail(sail, halyardCandidate, portSheetCandidate, starboardSheetCandidate, DualSheetSail.DualSheetSailSubtype.Square, mast.name);
                                VirtualCrewManager.Instance.addSquareSail(dual);
                                Console.WriteLine("Successfully added sail to map:" + sail.name);
                                Console.WriteLine("---");
                            }
                        }
                        else if (sail.name.Contains("jib") || sail.name.Contains("genoa")) {
                            Console.WriteLine("Attempting to add jib/genoa sail");
                            halyardCandidate = winchDictionary[connections.reefController];
                            portSheetCandidate = winchDictionary[connections.angleControllerLeft];
                            starboardSheetCandidate = winchDictionary[connections.angleControllerRight];

                            DualSheetSail dual = new DualSheetSail(sail, halyardCandidate, portSheetCandidate, starboardSheetCandidate, DualSheetSail.DualSheetSailSubtype.Jib, mast.name);
                            VirtualCrewManager.Instance.addDualSheetSail(dual);
                            Console.WriteLine("Successfully added sail to map:" + sail.name);
                        }                            
                        else {
                            Console.WriteLine("Could not assemble sail and winches for sail:" + sail.name);
                        }

                    }

                    Console.WriteLine("All sails on this mast have been first-pass-processed.");
                    if (deferredSquares.Count > 0)
                    {
                        Console.WriteLine("There are " + deferredSquares.Count + " deferred squares that need to be second-pass-processed.");
                    }

                    foreach (Sail deferredSquareSail in deferredSquares)
                    {
                        Console.WriteLine("Attempting to add deferred square sail:" + deferredSquareSail.name);
                        SailConnections connections = deferredSquareSail.GetComponent<SailConnections>();
                        
                        if (portRopeControllerForSquares == null || starboardRopeControllerForSquares == null)
                        {
                            RopeController[] familyControllers;
                            if (squareSheetControllersByMastFamily.TryGetValue(GetMastFamilyName(mast.name), out familyControllers))
                            {
                                portRopeControllerForSquares = familyControllers[0];
                                starboardRopeControllerForSquares = familyControllers[1];
                                Console.WriteLine("Using primary square sheet controllers from mast family:" + GetMastFamilyName(mast.name));
                            }
                            else
                            {
                                Console.WriteLine("WARNING: We never found a primary square for this mast family. Square cannot be added");
                                continue;
                            }
                        }
                        var portSheetCandidate = winchDictionary[portRopeControllerForSquares];
                        var starboardSheetCandidate = winchDictionary[starboardRopeControllerForSquares];

                        var halyardCandidate = winchDictionary[connections.reefController];
                        DualSheetSail dual = new DualSheetSail(deferredSquareSail, halyardCandidate, portSheetCandidate, starboardSheetCandidate, DualSheetSail.DualSheetSailSubtype.Square, mast.name);
                        VirtualCrewManager.Instance.addSquareSail(dual);
                        Console.WriteLine("Successfully added deferred Square sail to map:" + deferredSquareSail.name);
                    }

                    Console.WriteLine("-------------------------------------------");
                }

                // Anchor winches
                foreach (GPButtonRopeWinch winch in winches)
                {
                    if (winch.rope is RopeControllerAnchor)
                        VirtualCrewManager.Instance.AnchorWinches.Add(winch);
                }
                Console.WriteLine($"Found {VirtualCrewManager.Instance.AnchorWinches.Count} anchor winch(es).");
                CrewNavigationCoordinator.Instance.RebuildWorkstations();
            }

            if (ScanItems.Value.IsDown())
            {
                CrewNavigationCoordinator.Instance.ForceRingLookoutBell();
            }
        }

        private static T GetPrivateField<T>(object obj, string fieldName)
            => Traverse.Create(obj).Field(fieldName).GetValue<T>();

        private static string GetMastFamilyName(string mastName)
        {
            return mastName
                .Replace("_extension", "")
                .Replace(" mid", "")
                .Replace(" top", "")
                .Trim();
        }

        private Sail findAttachedSailForWinchIfExists(GPButtonRopeWinch winch)
        {
            //Console.WriteLine("Checking winch " + winch.name);
            // Check for single-sheeted sails
            if (winch.rope is RopeControllerSailAngle)
            {
                //Console.WriteLine("Winch has associated rope type of " + winch.rope.name);
                return GetPrivateField<Sail>((RopeControllerSailAngle)winch.rope, "sail");
            }
            if (winch.rope is RopeControllerSailReef)
            {
                //Console.WriteLine("Winch has associated rope type of " + winch.rope.name);
                return GetPrivateField<Sail>((RopeControllerSailReef)winch.rope, "sail");
            }

            // Check for double-sheeted sails
            if (winch.rope is RopeControllerSailAngleJib)
            {
                //Console.WriteLine("Winch has associated rope type of " + winch.rope.name);
                return GetPrivateField<Sail>(((RopeControllerSailAngleJib)winch.rope).jibAngleMaster, "sail");
            }
            if(winch.rope is RopeControllerSailAngleSquare)
            {
                //Console.WriteLine("Winch has associated rope type of " + winch.rope.name);
                return GetPrivateField<Sail>(((RopeControllerSailAngleSquare)winch.rope).squareAngleMaster, "sail");
            }

            //Console.WriteLine("Could not determine associated sail");
            return null;
        }
    }
}
