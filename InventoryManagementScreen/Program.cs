using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program : MyGridProgram {
        private string[] icons = new string[] { "Ooo", "oOo", "ooO" };
        public const string NAME_TAG_DEFAULT = "[IMS]";
        public const string CUSTOM_DATA_TAG_DEFAULT = "IMS";
        public const string SPECIAL_KEYWORD_DEFAULT = "Special";
        public const string STEP_START = "[";
        public const string STEP_END = "]";
        public const string STEP_SELECTION_INDECATOR_START = ">";
        public const string STEP_SELECTION_INDECATOR_END = "<";
        public const string SELECTION_INDECATOR_START = " >> ";
        public const string SELECTION_INDECATOR_END = " << ";
        public const string SELECTION_INDECATOR_SPACE = "    ";
        public const string DEFAULT_CUSTOM_DATA = "Special Container modes:\r\nPositive number: stores wanted amount, removes excess (e.g.: 100)\r\nNegative number: doesn't store items, only removes excess (e.g.: -100)\r\nKeyword 'all': stores all items of that subtype (like a type container)\r\nComponent/BulletproofGlass=0\r\nComponent/Canvas=0\r\nComponent/Computer=0\r\nComponent/Construction=0\r\nComponent/Detector=0\r\nComponent/Display=0\r\nComponent/Explosives=0\r\nComponent/Girder=0\r\nComponent/GravityGenerator=0\r\nComponent/InteriorPlate=0\r\nComponent/LargeTube=0\r\nComponent/Medical=0\r\nComponent/MetalGrid=0\r\nComponent/Motor=0\r\nComponent/PowerCell=0\r\nComponent/RadioCommunication=0\r\nComponent/Reactor=0\r\nComponent/SmallTube=0\r\nComponent/SolarCell=0\r\nComponent/SteelPlate=0\r\nComponent/Superconductor=0\r\nComponent/Thrust=0\r\nOre/Cobalt=0\r\nOre/Gold=0\r\nOre/Ice=0\r\nOre/Iron=0\r\nOre/Magnesium=0\r\nOre/Nickel=0\r\nOre/Platinum=0\r\nOre/Scrap=0\r\nOre/Silver=0\r\nOre/Stone=0\r\nOre/Uranium=0\r\nIngot/Cobalt=0\r\nIngot/Gold=0\r\nIngot/Iron=0\r\nIngot/Magnesium=0\r\nIngot/Nickel=0\r\nIngot/Platinum=0\r\nIngot/Silicon=0\r\nIngot/Silver=0\r\nIngot/Stone=0\r\nIngot/Uranium=0\r\nAmmoMagazine/AutocannonClip=0\r\nAmmoMagazine/AutomaticRifleGun_Mag_20rd=0\r\nAmmoMagazine/LargeCalibreAmmo=0\r\nAmmoMagazine/LargeRailgunAmmo=0\r\nAmmoMagazine/MediumCalibreAmmo=0\r\nAmmoMagazine/Missile200mm=0\r\nAmmoMagazine/NATO_25x184mm=0\r\nAmmoMagazine/NATO_5p56x45mm=0\r\nAmmoMagazine/PaintGunMag=0\r\nAmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd=0\r\nOxygenContainerObject/OxygenBottle=0\r\nGasContainerObject/HydrogenBottle=0\r\nPhysicalGunObject/AngleGrinder2Item=0\r\nPhysicalGunObject/AngleGrinder3Item=0\r\nPhysicalGunObject/AngleGrinder4Item=0\r\nPhysicalGunObject/AngleGrinderItem=0\r\nPhysicalGunObject/AutomaticRifleItem=0\r\nPhysicalGunObject/BasicHandHeldLauncherItem=0\r\nPhysicalGunObject/HandDrill2Item=0\r\nPhysicalGunObject/HandDrill3Item=0\r\nPhysicalGunObject/HandDrill4Item=0\r\nPhysicalGunObject/HandDrillItem=0\r\nPhysicalGunObject/PhysicalPaintGun=0\r\nPhysicalGunObject/UltimateAutomaticRifleItem=0\r\nPhysicalGunObject/Welder2Item=0\r\nPhysicalGunObject/Welder3Item=0\r\nPhysicalGunObject/Welder4Item=0\r\nPhysicalGunObject/WelderItem=0\r\nPhysicalObject/SpaceCredit=0\r\nConsumableItem/DrillInhibitorBlocker=0\r\nConsumableItem/JetpackInhibitorBlocker=0\r\nConsumableItem/Medkit=0\r\nDatapad/Datapad=0";
        public const string INDEX_EXAMPLE = "00000";

        public string nameTag;
        public string customDataTag;
        public string specialKeyword;



        private string prevCustomData;
        private int customDataChanges = 0;
        private Dictionary<string, Screen> screenNames = new Dictionary<string, Screen>();
        private Dictionary<IMyTerminalBlock, Dictionary<int, Screen>> screenBlocks = new Dictionary<IMyTerminalBlock, Dictionary<int, Screen>>();
        private List<IMyTerminalBlock> reloadQueue = new List<IMyTerminalBlock>();
        private int currentReload = 0;
        private string logs = "";
        private Dictionary<IMyTerminalBlock, string> errors = new Dictionary<IMyTerminalBlock, string>();
        private string prevPrint;
        private MyIni ini;

        private int iconIndex;
        private bool ticktock;
        private int phase;

        public Program() {
            ini = new MyIni();
            LoadCustomData();
            UpdateAllBlocks();
            CalculateUpdateQueue();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            phase = 0;
        }
        public void Main(string argument, UpdateType updateSource) {
            Print();
            if (updateSource.HasFlag(UpdateType.Update10)) {
                if (phase >= 9) {
                    Log("Update");
                    Update();
                    phase = 0;
                } else {
                    Log("recount");
                    Recount();
                }
                phase++;
            } else {
                Commands(argument);
            }
        }

        private void Recount() {
            foreach (var screen in screenNames.Values) {
                screen.Recount();
            }
        }

        private void Update() {
            LoadCustomData();
            if (ticktock) {
                UpdateCurrentBlock();
            } else {
                UpdateAllScreens();
            }
            ticktock = !ticktock;
        }

        private void LoadCustomData() {
            if (Me.CustomData == prevCustomData) {
                return;
            }
            prevCustomData = Me.CustomData;
            customDataChanges++;
            ini.TryParse(Me.CustomData);
            nameTag = Tools.IniGetOrSet(ini, "IMS", "nameTag", "[IMS]");
            customDataTag = Tools.IniGetOrSet(ini, "IMS", "customDataTag", "IMS");
            specialKeyword = Tools.IniGetOrSet(ini, "IMS", "specialKeyword", "Special");
            Me.CustomData = ini.ToString();
        }

        private void Print() {
            string print = "";
            //print += icons[iconIndex] + "\n";
            iconIndex++;
            iconIndex = iconIndex % icons.Length;

            print += "---( Managed LCDs: " + screenNames.Count + " )---\n";
            foreach (var pair in screenNames) {
                print += (pair.Key) + ":\n";
                int nr = 0;
                foreach (var pair1 in pair.Value.cargoContainers) {
                    print += +nr + ": " + Tools.EscapeName(pair1.Key.DisplayNameText) + "\n";
                    print += pair1.Value.loadFactor + "\n\n";
                    nr++;
                }
                print += "\n";
            }

            //if (currentReload >= reloadQueue.Count) {
            //    print += currentReload + " / " + reloadQueue.Count + " Next Update:\n" + Tools.EscapeName(Me.DisplayNameText) + "\n";
            //} else {
            //    print += currentReload + " / " + reloadQueue.Count + " Next Update:\n" + Tools.EscapeName(reloadQueue[currentReload].DisplayNameText) + "\n";
            //}

            if (errors.Count > 0) {
                print += "---( Errors )---" + "\n";
                foreach (var pair in errors) {
                    print += Tools.EscapeName(pair.Key.DisplayNameText) + ":\n";
                    print += pair.Value + "\n";
                }
                print += "\n";
            }

            if (logs != "") {
                print += ("--( Logs )--\n" + logs);
            }

            if (prevPrint != print) {
                Echo(print);
                prevPrint = print;
            }
        }


        private void Commands(string argument) {
            string[] split = argument.Split(' ');
            if (split.Length != 2) {
                Log("Unknown command\n\"" + argument + "\"");
                return;
            }

            string lcdName = split[0];
            if (!screenNames.ContainsKey(lcdName)) {
                Log("Unknown lcd \"" + lcdName + "\" in \"" + argument + "\"");
                return;
            }
            Screen screen = screenNames[lcdName];

            if (split[1].Equals("inc", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.IncreaseAmount()) {
                    RemoveScreen(screen);
                }
            } else if (split[1].Equals("dec", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.DecraseAmount()) {
                    RemoveScreen(screen);
                }
            } else if (split[1].Equals("up", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.DecreaseSelection()) {
                    RemoveScreen(screen);
                }
            } else if (split[1].Equals("down", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.IncreaseSelection()) {
                    RemoveScreen(screen);
                }
            } else if (split[1].Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.Rest()) {
                    RemoveScreen(screen);
                }
            } else if (split[1].Equals("change", StringComparison.OrdinalIgnoreCase)) {
                if (!screen.ChangeStepSize()) {
                    RemoveScreen(screen);
                }
            } else {
                Log("Unknown command\n\"" + argument + "\"");
            }
        }

        private void UpdateAllScreens() {
            List<Screen> toRemove = new List<Screen>();
            foreach (var screen in screenNames.Values) {
                ini.TryParse(screen.block.CustomData);
                string prevName = screen.name; ;
                if (!screen.Reload(ini)) {
                    toRemove.Add(screen);
                }
            }

            foreach (var screen in toRemove) {
                RemoveScreen(screen);
                UpdateBlock(screen.block);
            }
        }

        private void CalculateUpdateQueue() {
            reloadQueue.Clear();
            //GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(reloadQueue, (block) => block.CubeGrid.IsSameConstructAs(Me.CubeGrid) && block.CustomName.Contains(nameTag));
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(reloadQueue, (block) => block.CubeGrid.IsSameConstructAs(Me.CubeGrid) && block.CustomName.Contains(nameTag));
        }

        private void UpdateAllBlocks() {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks, (block) => block.CubeGrid.IsSameConstructAs(Me.CubeGrid) && block.CustomName.Contains(nameTag));
            //GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(blocks, (block) => block.CubeGrid.IsSameConstructAs(Me.CubeGrid) && block.CustomName.Contains(nameTag));
            foreach (var block in blocks) {
                UpdateBlock(block);
            }
        }

        private void UpdateCurrentBlock() {
            if (currentReload >= reloadQueue.Count) {
                currentReload = 0;
                CalculateUpdateQueue();
                LoadCustomData();
            } else {
                IMyTerminalBlock block = reloadQueue[currentReload];
                UpdateBlock(block);
                currentReload++;
            }
        }

        private void UpdateBlock(IMyTerminalBlock block) {
            ini.TryParse(block.CustomData);
            bool successfull = false;
            errors.Remove(block);
            if (block is IMyTextSurfaceProvider) {
                IMyTextSurfaceProvider provider = (IMyTextSurfaceProvider)block;
                for (int i = 0; i < provider.SurfaceCount; i++) {
                    if (screenBlocks.ContainsKey(block) && screenBlocks[block].ContainsKey(i)) {
                        successfull = true;
                        continue;
                    }
                    if (CreateScreen(provider.GetSurface(i), block, i, customDataTag + " " + i)) {
                        successfull = true;
                    }
                }
            } else if (block is IMyTextSurface) {
                IMyTextSurface surface = (IMyTextSurface)block;
                if (CreateScreen(surface, block, 0, customDataTag)) {
                    successfull = true;
                }
            }
            if (!successfull) {
                LogError(block, "Error in CustomData (is a \"" + customDataTag + " #\" tag missing?)");
            }
        }

        private bool CreateScreen(IMyTextSurface surface, IMyTerminalBlock term, int index, string section) {
            Screen newScreen = new Screen(this, surface, term, index, section);
            if (newScreen.Reload(ini)) {
                if (!InsertScreen(newScreen)) {
                    LogError(term, "Screen with name " + newScreen.name + " already exists.");
                }
                return true;
            }
            return false;
        }

        private void RemoveScreen(Screen screen) {
            Log("Removed " + screen.name);
            screenNames.Remove(screen.name);
            if (screenBlocks.ContainsKey(screen.block)) {
                Dictionary<int, Screen> dic = screenBlocks[screen.block];
                dic.Remove(screen.index);
                if (dic.Count <= 0) {
                    screenBlocks.Remove(screen.block);
                }
            }
        }

        private bool InsertScreen(Screen screen) {
            if (screenNames.ContainsKey(screen.name)) {
                return false;
            }
            Log("Added " + screen.name);
            screenNames.Add(screen.name, screen);
            if (!screenBlocks.ContainsKey(screen.block)) {
                screenBlocks[screen.block] = new Dictionary<int, Screen>();
            }
            screenBlocks[screen.block].Add(screen.index, screen);
            return true;
        }

        public void Log(string message) {
            logs = message + "\n\n" + logs;
        }

        public void LogError(IMyTerminalBlock block, string error) {
            if (errors.ContainsKey(block)) {
                errors[block] = errors[block] + "\n\n" + error;
            } else {
                errors.Add(block, error);
            }
        }

    }
}