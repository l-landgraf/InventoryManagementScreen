using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program {
        public static class Tools {
            public static string IniGetOrSet(MyIni ini, string section, string name, string def) {
                MyIniValue val = ini.Get(section, name);
                if (val.IsEmpty) {
                    ini.Set(section, name, def);
                    return def;
                }
                return val.ToString();
            }

            public static int IniGetOrSet(MyIni ini, string section, string name, int def) {
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

            public static string EscapeName(string name) {
                name = name.Replace("[", "[[");
                name = name.Replace("]", "]]");
                return name;
            }
        }
    }
}
