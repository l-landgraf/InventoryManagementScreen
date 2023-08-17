using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Screen {
            private Program program;
            public IMyTextSurface surface;
            public IMyTerminalBlock block;
            public string name;
            public int index;
            public string section;
            public Dictionary<IMyTerminalBlock, ContainerInfo> cargoContainers;
            private int totalStorageSpace;

            private SortedDictionary<string, ItemInfo> itemList;
            private List<string> filters;
            private int currentSelection = 0;
            private int[] stepSizes;
            private int currentStepSize = -1;
            private float screenWidth;
            private float amountSize;
            private float indexSize;
            private float selectionIndecatorSize;
            private int linesBelow;
            private int linesAbove;
            private int amountDigets;

            private string lastFont;
            private float lastFontSize;
            private float lastScreenWidth;
            private float lastCustomDataChanges;

            public Screen(Program program, IMyTextSurface surface, IMyTerminalBlock block, int index, string section) {
                this.program = program;
                this.surface = surface;
                this.block = block;
                this.index = index;
                this.section = section;
                itemList = new SortedDictionary<string, ItemInfo>();
                cargoContainers = new Dictionary<IMyTerminalBlock, ContainerInfo>();
            }

            public bool Reload(MyIni ini) {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                if (!LoadConfig(ini)) {
                    return false;
                }
                LoadCargoContainers();
                LoadCargoContainerCustomData(true);
                WriteAll();
                CalculatePadding();
                Count();
                Print();
                return true;
            }

            public void Recount() {
                if (block.Closed) {
                    return;
                }
                RemoveClosedContainers();
                Count();
                Print();
            }

            private void CalculatePadding() {
                if (surface.Font == lastFont && surface.FontSize == lastFontSize
                    && screenWidth == lastScreenWidth && program.customDataChanges == lastCustomDataChanges) {
                    return;
                }
                lastFont = surface.Font;
                lastFontSize = surface.FontSize;
                lastScreenWidth = screenWidth;
                lastCustomDataChanges = program.customDataChanges;

                string digets = "/";
                for (int i = 0; i < amountDigets; i++) {
                    digets += "0";
                }
                amountSize = MeasuerWidth(digets);
                indexSize = MeasuerWidth(Program.INDEX_EXAMPLE);
                selectionIndecatorSize = MeasuerWidth(Program.SELECTION_INDECATOR_END + "0");
            }

            private bool LoadConfig(MyIni ini) {
                if (!ini.ContainsSection(section)) {
                    return false;
                }
                string oldName = name;
                name = Tools.IniGetOrSet(ini, section, "name", "");
                filters = new List<string>(Tools.IniGetOrSet(ini, section, "filters", "Component").Split(','));
                linesAbove = Tools.IniGetOrSet(ini, section, "linesAbove", 2);
                linesBelow = Tools.IniGetOrSet(ini, section, "linesBelow", 2);
                screenWidth = Tools.IniGetOrSet(ini, section, "screenWidth", (int)surface.SurfaceSize.X);
                amountDigets = Tools.IniGetOrSet(ini, section, "amountDigets", 4);
                stepSizes = Array.ConvertAll(Tools.IniGetOrSet(ini, section, "stepSizes", "10,50,100,200").Split(','), int.Parse);
                currentStepSize = Tools.IniGetOrSet(ini, section, "defaultStepSize", 2);
                currentStepSize = MathHelper.Clamp(currentStepSize, 0, stepSizes.Length - 1);

                block.CustomData = ini.ToString();
                if (String.IsNullOrEmpty(name)) {
                    LogError("Name empty");
                    name = oldName;
                    return false;
                }
                if (!String.IsNullOrEmpty(oldName) && name != oldName) {
                    LogName("Name changed from " + oldName);
                    name = oldName;
                    return false;
                }

                surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                return true;
            }

            private void LoadCargoContainers() {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks,
                    b => { return b.CustomName.Contains("[" + name + "]") && b is IMyInventoryOwner; });

                foreach (var pair in cargoContainers) {
                    if (!pair.Key.CustomName.Contains("[" + name + "]")) {
                        RemoveCargoContainer(pair.Key);
                    }
                }

                foreach (var container in blocks) {
                    if (!container.CustomName.Contains(program.specialKeyword)) {
                        container.CustomName = container.CustomName + " " + program.specialKeyword;
                    }
                    if (container.CustomData.Length == 0) {
                        container.CustomData = Program.DEFAULT_CUSTOM_DATA;
                    }
                    AddContainer(container);
                }
            }

            private void AddContainer(IMyTerminalBlock container) {
                if (cargoContainers.ContainsKey(container)) {
                    return;
                }
                LogName("Added " + Tools.EscapeName(container.DisplayNameText));
                cargoContainers.Add(container, new ContainerInfo(container));
                RecalculateLoadFactors();
            }

            private void RemoveClosedContainers() {
                List<IMyTerminalBlock> remove = new List<IMyTerminalBlock>();
                foreach (var pair in cargoContainers) {
                    if (pair.Key.Closed) {
                        remove.Add(pair.Key);
                    }
                }
                foreach (var container in remove) {
                    RemoveCargoContainer(container);
                }
                RecalculateLoadFactors();
            }

            private void RemoveCargoContainer(IMyTerminalBlock container) {
                cargoContainers.Remove(container);
                LogName("Removed " + Tools.EscapeName(container.DisplayNameText));
            }


            private void RecalculateLoadFactors() {
                long sum = 0;
                foreach (var pair in cargoContainers) {
                    sum += pair.Key.GetInventory(0).MaxVolume.RawValue;
                }

                foreach (var pair in cargoContainers) {
                    pair.Value.loadFactor = (double)pair.Key.GetInventory(0).MaxVolume.RawValue / sum;
                }
            }

            private void LoadCargoContainerCustomData(bool onlyAddNew) {
                foreach (var pair in cargoContainers) {
                    foreach (var line in pair.Key.CustomData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                        string[] split = line.Split('=');
                        if (split.Length != 2) {
                            continue;
                        }
                        string itemName = split[0];
                        string[] itemSplit = itemName.Split('/');
                        if (!filters.Contains(itemSplit[0])) {
                            continue;
                        }
                        int itemAmount = int.Parse(split[1]);
                        if (itemList.ContainsKey(itemName)) {
                            if (!onlyAddNew) {
                                itemList[itemName].requestedAmount += itemAmount;
                            }
                        } else {
                            itemList.Add(itemName, new ItemInfo(itemName, 0, itemAmount));
                        }
                    }

                    currentSelection = LoopIndex(currentSelection, itemList.Count);
                }
            }

            private void Count() {
                string s = "";
                foreach (var entry in itemList) {
                    entry.Value.actualAmount = 0;
                }
                foreach (var pair in cargoContainers) {
                    for (int i = 0; i < pair.Key.InventoryCount; i++) {
                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        pair.Key.GetInventory(i).GetItems(items);
                        foreach (var item in items) {
                            string itemName = item.Type.TypeId.Split('_')[1] + "/" + item.Type.SubtypeId;
                            s += itemName + "\n";
                            if (itemList.ContainsKey(itemName)) {
                                itemList[itemName].actualAmount += item.Amount.ToIntSafe();
                            } else {
                                itemList.Add(itemName, new ItemInfo(itemName, 0, item.Amount.ToIntSafe()));
                            }
                        }
                    }
                }
            }

            private int SplitItems(int totalAmount, ContainerInfo info, int containerNr) {
                int value = (int)(totalAmount * info.loadFactor);
                if (totalAmount % cargoContainers.Count > containerNr) {
                    value = value + 1;
                }
                return value;
            }

            private void WriteAll() {
                int containerNr = 0;
                foreach (var pair in cargoContainers) {
                    IMyTerminalBlock container = pair.Key;
                    string[] lines = container.CustomData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lines.Length; i++) {
                        string[] split = lines[i].Split('=');
                        if (split.Length != 2) {
                            continue;
                        }
                        string itm = split[0];
                        if (!itemList.ContainsKey(itm)) {
                            continue;
                        }
                        lines[i] = itemList[itm].name + "=" + SplitItems((int)itemList[itm].requestedAmount, pair.Value, containerNr);
                        containerNr++;
                    }
                    string newData = string.Join("\n", lines);
                    container.CustomData = newData;
                }
            }

            private void Print() {
                string text = screenWidth + " " + Program.STEP_START + " ";
                for (int i = 0; i < stepSizes.Length; i++) {
                    if (i == currentStepSize) {
                        text += Program.STEP_SELECTION_INDECATOR_START + stepSizes[i] + Program.STEP_SELECTION_INDECATOR_END + " ";
                    } else {
                        text += stepSizes[i] + " ";
                    }

                }
                text += Program.STEP_END + "\n";

                if (itemList.Count > 0) {
                    for (int i = currentSelection - linesAbove; i < currentSelection + linesBelow + 1; i++) {
                        int index = LoopIndex(i, itemList.Count);
                        var entry = itemList.ElementAt(index);
                        string line;
                        string selectionIndecator;
                        line = GeneratePrintLine(index, entry.Value, i == currentSelection, out selectionIndecator);
                        string padding = Padding(line + entry.Value.actualAmount, screenWidth - selectionIndecatorSize - amountSize);
                        line += padding + entry.Value.actualAmount + "/" + entry.Value.requestedAmount;
                        line += Padding(line, screenWidth - selectionIndecatorSize);
                        line += selectionIndecator;
                        text += line + "\n";
                    }
                }
                foreach (var pair in cargoContainers) {
                    for (int i = 0; i < pair.Key.InventoryCount; i++) {
                        text += Math.Ceiling(((float)pair.Key.GetInventory(i).CurrentVolume.RawValue / (float)pair.Key.GetInventory(i).MaxVolume.RawValue) * 100) + "% ";
                    }
                }
                if (cargoContainers.Count() <= 0) {
                    text += "no cargo containers";
                }
                surface.WriteText(text);
            }

            private string GeneratePrintLine(int index, ItemInfo item, bool selected, out string selectionIndecator) {
                string line = SELECTION_INDECATOR_SPACE;
                selectionIndecator = "";
                if (selected) {
                    line = Program.SELECTION_INDECATOR_START;
                    selectionIndecator = Program.SELECTION_INDECATOR_END;
                }
                line += (index + "");
                line += Padding(line, indexSize);
                line += item.subtype;
                return line;
            }

            private int LoopIndex(int value, int max) {
                if (max == 0) {
                    return 0;
                }
                return (value + max) % max;
            }

            private float MeasuerWidth(String str) {
                return surface.MeasureStringInPixels(new StringBuilder(str), surface.Font, surface.FontSize).X;
            }

            private string Padding(string line, float targetWidth) {
                string padding = "";
                while (MeasuerWidth(line + padding) < targetWidth) {
                    padding += " ";
                }
                return padding;
            }

            public bool Rest() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                foreach (var entry in itemList) {
                    entry.Value.requestedAmount = 0;
                }
                WriteAll();
                Print();
                return true;
            }

            public bool ChangeStepSize() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                currentStepSize = LoopIndex(currentStepSize + 1, stepSizes.Length);
                Print();
                return true;
            }

            public bool IncreaseAmount() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                ItemInfo info = itemList.ElementAt(currentSelection).Value;
                info.requestedAmount += stepSizes[currentStepSize];
                WriteAll();
                Print();
                return true;
            }

            public bool DecraseAmount() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                ItemInfo info = itemList.ElementAt(currentSelection).Value;
                info.requestedAmount -= stepSizes[currentStepSize];
                if (info.requestedAmount < 0) {
                    info.requestedAmount = 0;
                }
                WriteAll();
                Print();
                return true;
            }

            public bool IncreaseSelection() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                currentSelection = LoopIndex(currentSelection + 1, itemList.Count);
                Print();
                return true;
            }

            public bool DecreaseSelection() {
                if (block.Closed) {
                    return false;
                }
                RemoveClosedContainers();
                currentSelection = LoopIndex(currentSelection - 1, itemList.Count);
                Print();
                return true;
            }



            private void LogName(string error) {
                Log(name + ":\n" + error);
            }
            private void Log(string error) {
                program.Log(error);
            }

            private void LogError(string error) {
                program.LogError(block, error);
            }
        }
    }
}