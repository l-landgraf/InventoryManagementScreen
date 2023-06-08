using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {

        public partial class ManagedLCD {
            class ContainerInfo {
                public IMyTerminalBlock block;
                public float freeSpace;

                public ContainerInfo(IMyTerminalBlock block, float freeSpace) {
                    this.block = block;
                    this.freeSpace = freeSpace;
                }
            }
        }
    }
}