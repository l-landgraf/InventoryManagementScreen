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
        public partial class ManagedLCD {
            private Program program;
            private IMyTextSurface surface;
            private IMyTerminalBlock block;
            private List<IMyTerminalBlock> cargoContainers;
            private string section;

            private SortedDictionary<string, ItemInfo> itemList;
            private string name;
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

            private string lastFont;
            private float lastFontSize;
            private float lastScreenWidth;
            private float lastCustomDataChanges;

            public ManagedLCD(Program program, IMyTextSurface surface, IMyTerminalBlock block, MyIni ini, string section) {
                this.program = program;
                this.surface = surface;
                this.block = block;
                this.section = section;
                itemList = new SortedDictionary<string, ItemInfo>();

                LoadConfig(ini);
                LoadCargoContainers();
                LoadCargoContainerCustomData(false);
                CalculatePadding();
                WriteAll();
                PrintItemList();
            }
            public void Reload(MyIni ini) {
                LoadConfig(ini);
                LoadCargoContainers();
                LoadCargoContainerCustomData(true);
                WriteAll();
                CalculatePadding();
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

                screenWidth = surface.SurfaceSize.X;
                string digets = "/";
                for (int i = 0; i < program.amountDigets; i++) {
                    digets += "0";
                }
                amountSize = MeasuerWidth(digets);
                indexSize = MeasuerWidth(Program.INDEX_EXAMPLE);
                selectionIndecatorSize = MeasuerWidth(Program.SELECTION_INDECATOR_END + "0");
            }

            private void LoadConfig(MyIni ini) {
                program.RemoveLCD(name);
                ini.TryParse(block.CustomData);
                string oldName = name;
                name = program.IniGetOrSet(ini, section, "name", "");


                filters = new List<string>(program.IniGetOrSet(ini, section, "filters", "Component").Split(','));
                linesAbove = program.IniGetOrSet(ini, section, "linesAbove", 2);
                linesBelow = program.IniGetOrSet(ini, section, "linesBelow", 2);

                stepSizes = Array.ConvertAll(program.IniGetOrSet(ini, section, "stepSizes", "10,50,100,200").Split(','), int.Parse);
                if (currentStepSize < 0) {
                    currentStepSize = program.IniGetOrSet(ini, section, "defaultStepSize", 2);
                }
                currentStepSize = MathHelper.Clamp(currentStepSize, 0, stepSizes.Length - 1);

                block.CustomData = ini.ToString();

                if (String.IsNullOrEmpty(name)) {
                    surface.WriteText("Missing name in Custom Data.");
                    throw new Exception("Missing name in Custom Data.");
                }
                if (!program.AddLCD(name, this)) {
                    surface.WriteText("name " + name + " already exists.\nchoose a diffrent one.");
                    throw new Exception("name " + name + " already exists. choose a diffrent one.");
                }
            }

            private void LoadCargoContainers() {
                cargoContainers = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargoContainers,
                    b => { return b.CustomName.Contains("[" + name + "]") && b is IMyInventoryOwner; });

                foreach (var container in cargoContainers) {
                    if (!container.CustomName.Contains(program.specialKeyword)) {
                        container.CustomName = container.CustomName + " " + program.specialKeyword;
                    }
                    if (container.CustomData.Length == 0) {
                        container.CustomData = Program.DEFAULT_CUSTOM_DATA;
                    }
                }
            }

            private void LoadCargoContainerCustomData(bool onlyAddNew) {
                foreach (var container in cargoContainers) {
                    if (!container.CustomName.Contains(program.specialKeyword)) {
                        container.CustomName = container.CustomName + " " + program.specialKeyword;
                    }
                    ReadItemData(container, onlyAddNew);
                }
            }

            private void ReadItemData(IMyTerminalBlock container, bool onlyAddNew) {
                foreach (var line in container.CustomData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
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

            public void Count() {
                string s = "";
                foreach (var entry in itemList) {
                    entry.Value.actualAmount = 0;
                }
                foreach (var curretContainer in cargoContainers) {
                    for (int i = 0; i < curretContainer.InventoryCount; i++) {
                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        curretContainer.GetInventory(i).GetItems(items);
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

            private int SplitItems(int totalAmount, int containerNr) {
                int value = totalAmount / cargoContainers.Count;
                if (totalAmount % cargoContainers.Count > containerNr) {
                    value = value + 1;
                }
                return value;
            }

            private void WriteAll() {
                for (int containerNr = 0; containerNr < cargoContainers.Count; containerNr++) {
                    IMyTerminalBlock container = cargoContainers[containerNr];
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
                        lines[i] = itemList[itm].name + "=" + SplitItems((int)itemList[itm].requestedAmount, containerNr);
                    }
                    string newData = string.Join("\n", lines);
                    container.CustomData = newData;
                }
            }

            private void WriteLine(string item) {
                for (int containerNr = 0; containerNr < cargoContainers.Count; containerNr++) {
                    IMyTerminalBlock container = cargoContainers[containerNr];
                    string[] lines = container.CustomData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int lineNr = 0; lineNr < lines.Length; lineNr++) {
                        string[] split = lines[lineNr].Split('=');
                        if (split.Length != 2) {
                            continue;
                        }
                        string currentItemName = split[0];
                        if (currentItemName != item) {
                            continue;
                        }
                        lines[lineNr] = item + "=" + SplitItems((int)itemList[item].requestedAmount, containerNr);
                    }
                    string newData = string.Join("\n", lines);
                    container.CustomData = newData;
                }
            }

            public void PrintItemList() {
                string text = " " + Program.STEP_START + " ";
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
                        line = GenerateLine(index, entry.Value, i == currentSelection, out selectionIndecator);
                        string padding = Padding(line + entry.Value.actualAmount, screenWidth - selectionIndecatorSize - amountSize);
                        line += padding + entry.Value.actualAmount + "/" + entry.Value.requestedAmount;
                        line += Padding(line, screenWidth - selectionIndecatorSize);
                        line += selectionIndecator;
                        text += line + "\n";
                    }
                }
                foreach (var container in cargoContainers) {
                    for (int i = 0; i < container.InventoryCount; i++) {
                        text += Math.Ceiling(((float)container.GetInventory(i).CurrentVolume.RawValue / (float)container.GetInventory(i).MaxVolume.RawValue) * 100) + "% ";
                    }
                }
                surface.WriteText(text);
            }

            private string GenerateLine(int index, ItemInfo item, bool selected, out string selectionIndecator) {
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

            public string Padding(string line, float targetWidth) {
                string padding = "";
                while (MeasuerWidth(line + padding) < targetWidth) {
                    padding += " ";
                }
                return padding;
            }

            public void Rest() {
                foreach (var entry in itemList) {
                    entry.Value.requestedAmount = 0;
                }
                WriteAll();
                PrintItemList();
            }

            public void ChangeStepSize() {
                currentStepSize = LoopIndex(currentStepSize + 1, stepSizes.Length);
                PrintItemList();
            }

            public void IncreaseAmount() {
                ItemInfo info = itemList.ElementAt(currentSelection).Value;
                info.requestedAmount += stepSizes[currentStepSize];
                WriteLine(itemList.ElementAt(currentSelection).Key);
                PrintItemList();
            }

            public void DecraseAmount() {
                ItemInfo info = itemList.ElementAt(currentSelection).Value;
                info.requestedAmount -= stepSizes[currentStepSize];
                if (info.requestedAmount < 0) {
                    info.requestedAmount = 0;
                }
                WriteLine(itemList.ElementAt(currentSelection).Key);
                PrintItemList();
            }

            public void IncreaseSelection() {
                currentSelection = LoopIndex(currentSelection + 1, itemList.Count);
                PrintItemList();
            }

            public void DecreaseSelection() {
                currentSelection = LoopIndex(currentSelection - 1, itemList.Count);
                PrintItemList();
            }
        }
    }
}