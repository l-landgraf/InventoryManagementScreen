using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program : MyGridProgram {

        /*
          Inventory Management Screen

          Commands:
          
          increases requested amount
          <screenName> inc
          
          decreases requested amount
          <screenName> dec
          
          moves the cursor up
          <screenName> up
          
          moves the cursor down
          <screenName> down
          
          sets all requested amoutn to 0
          <screenName> reset
          
          changes to the nex stepsize
          <screenName> change
          
          
          Custom Data Default Settings:
          name       NO DEFAULT
          tag        NO DEFAULT
          filter=""
          linesAbove=2
          linesBelow=2
          stepSizes=1,10,50,100,500
          defaultStepSize=2
          screenWidth=800
         */

        public string nameTag;
        public string customDataTag;
        public string specialKeyword;
        public int amountDigets;



        private string prevCustomData;
        private int customDataChanges = 0;
        private Dictionary<string, ManagedLCD> lcds = new Dictionary<string, ManagedLCD>();
        private List<ManagedLCD> reloadQueue = new List<ManagedLCD>();
        private List<string> errors = new List<string>();
        private string prevPrint;
        private MyIni ini;


        public const string INDEX_EXAMPLE = "00000";
        public const string STEP_START = "[";
        public const string STEP_END = "]";
        public const string STEP_SELECTION_INDECATOR_START = ">";
        public const string STEP_SELECTION_INDECATOR_END = "<";
        public const string SELECTION_INDECATOR_START = " >> ";
        public const string SELECTION_INDECATOR_END = " << ";
        public const string SELECTION_INDECATOR_SPACE = "    ";
        public const string DEFAULT_CUSTOM_DATA = "Special Container modes:\r\nPositive number: stores wanted amount, removes excess (e.g.: 100)\r\nNegative number: doesn't store items, only removes excess (e.g.: -100)\r\nKeyword 'all': stores all items of that subtype (like a type container)\r\nComponent/BulletproofGlass=0\r\nComponent/Canvas=0\r\nComponent/Computer=0\r\nComponent/Construction=0\r\nComponent/Detector=0\r\nComponent/Display=0\r\nComponent/Explosives=0\r\nComponent/Girder=0\r\nComponent/GravityGenerator=0\r\nComponent/InteriorPlate=0\r\nComponent/LargeTube=0\r\nComponent/Medical=0\r\nComponent/MetalGrid=0\r\nComponent/Motor=0\r\nComponent/PowerCell=0\r\nComponent/RadioCommunication=0\r\nComponent/Reactor=0\r\nComponent/SmallTube=0\r\nComponent/SolarCell=0\r\nComponent/SteelPlate=0\r\nComponent/Superconductor=0\r\nComponent/Thrust=0\r\nOre/Cobalt=0\r\nOre/Gold=0\r\nOre/Ice=0\r\nOre/Iron=0\r\nOre/Magnesium=0\r\nOre/Nickel=0\r\nOre/Platinum=0\r\nOre/Scrap=0\r\nOre/Silver=0\r\nOre/Stone=0\r\nOre/Uranium=0\r\nIngot/Cobalt=0\r\nIngot/Gold=0\r\nIngot/Iron=0\r\nIngot/Magnesium=0\r\nIngot/Nickel=0\r\nIngot/Platinum=0\r\nIngot/Silicon=0\r\nIngot/Silver=0\r\nIngot/Stone=0\r\nIngot/Uranium=0\r\nAmmoMagazine/AutocannonClip=0\r\nAmmoMagazine/AutomaticRifleGun_Mag_20rd=0\r\nAmmoMagazine/LargeCalibreAmmo=0\r\nAmmoMagazine/LargeRailgunAmmo=0\r\nAmmoMagazine/MediumCalibreAmmo=0\r\nAmmoMagazine/Missile200mm=0\r\nAmmoMagazine/NATO_25x184mm=0\r\nAmmoMagazine/NATO_5p56x45mm=0\r\nAmmoMagazine/PaintGunMag=0\r\nAmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd=0\r\nOxygenContainerObject/OxygenBottle=0\r\nGasContainerObject/HydrogenBottle=0\r\nPhysicalGunObject/AngleGrinder2Item=0\r\nPhysicalGunObject/AngleGrinder3Item=0\r\nPhysicalGunObject/AngleGrinder4Item=0\r\nPhysicalGunObject/AngleGrinderItem=0\r\nPhysicalGunObject/AutomaticRifleItem=0\r\nPhysicalGunObject/BasicHandHeldLauncherItem=0\r\nPhysicalGunObject/HandDrill2Item=0\r\nPhysicalGunObject/HandDrill3Item=0\r\nPhysicalGunObject/HandDrill4Item=0\r\nPhysicalGunObject/HandDrillItem=0\r\nPhysicalGunObject/PhysicalPaintGun=0\r\nPhysicalGunObject/UltimateAutomaticRifleItem=0\r\nPhysicalGunObject/Welder2Item=0\r\nPhysicalGunObject/Welder3Item=0\r\nPhysicalGunObject/Welder4Item=0\r\nPhysicalGunObject/WelderItem=0\r\nPhysicalObject/SpaceCredit=0\r\nConsumableItem/DrillInhibitorBlocker=0\r\nConsumableItem/JetpackInhibitorBlocker=0\r\nConsumableItem/Medkit=0\r\nDatapad/Datapad=0";
        public Program() {
            ini = new MyIni();
            LoadCustomData();
            LoadLcds();
            LoadCustomData();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void LoadCustomData() {
            if (Me.CustomData == prevCustomData) {
                return;
            }
            prevCustomData = Me.CustomData;
            customDataChanges++;
            ini.TryParse(Me.CustomData);
            nameTag = IniGetOrSet(ini, "IMS", "nameTag", "[IMS]");
            customDataTag = IniGetOrSet(ini, "IMS", "customDataTag", "IMS");
            specialKeyword = IniGetOrSet(ini, "IMS", "specialKeyword", "Special");
            amountDigets = IniGetOrSet(ini, "IMS", "amountDigets", 4);
            Me.CustomData = ini.ToString();
        }

        public void Main(string argument, UpdateType updateSource) {
            Print();

            if (updateSource.HasFlag(UpdateType.Update100)) {
                Update();
            } else {
                Commands(argument);
            }
        }

        private void Update() {
            if (reloadQueue.Count > 0) {
                ManagedLCD reloadLcd = reloadQueue[0];
                reloadQueue.Remove(reloadLcd);
                reloadQueue.Add(reloadLcd);
                reloadLcd.Reload(ini);
            }

            LoadCustomData();
            foreach (var lcd in lcds) {
                lcd.Value.Count();
                lcd.Value.PrintItemList();
            }
        }

        private void Print() {
            string print = "";
            print += "Managed LCDs: " + lcds.Count + "\n";
            foreach (var lcd in lcds) {
                print += (lcd.Key) + "\n";
            }

            if (errors.Count > 0) {
                print += ("Errors: \n");
            }

            foreach (string str in errors) {
                print += (str) + "\n";
            }

            if (prevPrint != print) {
                Echo(print);
                prevPrint = print;
            }
        }

        public void Commands(string argument) {
            string[] split = argument.Split(' ');
            if (split.Length != 2) {
                LogError("Unknown command\n\"" + argument + "\"");
                return;
            }

            string lcdName = split[0];
            if (!lcds.ContainsKey(lcdName)) {
                LogError("Unknown lcd " + lcdName + "in \n\"" + argument + "\"");
                return;
            }
            ManagedLCD lcd = lcds[lcdName];

            if (split[1].Equals("inc", StringComparison.OrdinalIgnoreCase)) {
                lcd.IncreaseAmount();
            } else if (split[1].Equals("dec", StringComparison.OrdinalIgnoreCase)) {
                lcd.DecraseAmount();
            } else if (split[1].Equals("up", StringComparison.OrdinalIgnoreCase)) {
                lcd.DecreaseSelection();
            } else if (split[1].Equals("down", StringComparison.OrdinalIgnoreCase)) {
                lcd.IncreaseSelection();
            } else if (split[1].Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                lcd.Rest();
            } else if (split[1].Equals("change", StringComparison.OrdinalIgnoreCase)) {
                lcd.ChangeStepSize();
            } else {
                LogError("Unknown command\n\"" + argument + "\"");
            }
        }

        public void LoadLcds() {
            List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(lcds, b => b.CustomName.Contains(nameTag) && b is IMyTextSurfaceProvider && Me.CubeGrid.IsSameConstructAs(b.CubeGrid));
            foreach (var term in lcds) {
                IMyTextSurfaceProvider provider = (IMyTextSurfaceProvider)term;
                bool foundSection = false;
                if (!ini.TryParse(term.CustomData)) {
                    LogError("Unable to Parse Customdata from " + term.CustomName);
                    continue;
                }
                for (int i = 0; i < provider.SurfaceCount; i++) {
                    string section = customDataTag + " " + i;
                    if (ini.ContainsSection(section)) {
                        CreateNewLcd(provider.GetSurface(i), term, ini, section);
                        foundSection = true;
                    }
                }
                if (!foundSection) {
                    LogError("No Sections found in " + term.CustomName);
                }
            }

            lcds.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(lcds, b => b.CustomName.Contains(nameTag) && b is IMyTextSurface && Me.CubeGrid.IsSameConstructAs(b.CubeGrid));
            foreach (var term in lcds) {
                IMyTextSurface surface = (IMyTextSurface)term;
                if (!ini.TryParse(term.CustomData)) {
                    LogError("Unable to Parse Customdata from " + term.CustomName);
                    continue;
                }
                if (ini.ContainsSection(customDataTag)) {
                    CreateNewLcd(surface, term, ini, customDataTag);
                }
            }
        }

        public void CreateNewLcd(IMyTextSurface surface, IMyTerminalBlock block, MyIni ini, string section) {
            try {
                new ManagedLCD(this, surface, block, ini, section);
            } catch (Exception e) {
                LogError(block.CustomName + ":\n" + e.Message);
            }
        }

        public void LogError(string message) {
            errors.Insert(0, message);
        }

        public bool AddLCD(string name, ManagedLCD lcd) {
            if (lcds.ContainsKey(name)) {
                return false;
            } else {
                lcds.Add(name, lcd);
                reloadQueue.Add(lcd);
                return true;
            }
        }
        public void RemoveLCD(string name) {
            if (name == null) {
                return;
            }
            reloadQueue.Remove(lcds[name]);
            lcds.Remove(name);
        }

        public string IniGetOrSet(MyIni ini, string section, string name, string def) {
            MyIniValue val = ini.Get(section, name);
            if (val.IsEmpty) {
                ini.Set(section, name, def);
                return def;
            }
            return val.ToString();
        }
        public int IniGetOrSet(MyIni ini, string section, string name, int def) {
            MyIniValue val = ini.Get(section, name);
            if (val.IsEmpty) {
                ini.Set(section, name, def);
                return def;
            }
            int ret;
            if (val.TryGetInt32(out ret)) {
                return ret;
            } else {
                ini.Set(section, name, def);
                return def;
            }
        }
    }
}