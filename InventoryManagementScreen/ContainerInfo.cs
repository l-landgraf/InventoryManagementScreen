using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {

        public class ContainerInfo {
            public IMyTerminalBlock block;
            public double loadFactor;

            public ContainerInfo(IMyTerminalBlock block) {
                this.block = block;
            }
        }
    }
}