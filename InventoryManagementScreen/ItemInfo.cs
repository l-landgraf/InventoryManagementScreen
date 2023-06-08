namespace IngameScript {
    partial class Program {

        public partial class ManagedLCD {
            class ItemInfo {
                public string name;
                public string type;
                public string subtype;
                public float actualAmount;
                public float requestedAmount;

                public ItemInfo(string name, int actualAmount, int requestedAmount) {
                    this.name = name;
                    string[] split = name.Split('/');
                    this.type = split[0];
                    this.subtype = split[1];
                    this.actualAmount = actualAmount;
                    this.requestedAmount = requestedAmount;
                }
            }
        }
    }
}