using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;

namespace PlotHider
{
    [HarmonyPatch]
    internal static class GameSaveReadWritePatch
    {
        private const int VERSION = 1;

        public static void ReadV1Save(BinaryReader reader)
        {
            try
            {
                reader.ReadInt32();
                int count = reader.ReadInt32();

                EntryPoint.disabledLocations.Clear();
                for (int i = 0; i < count; i++)
                    EntryPoint.disabledLocations.Add(reader.ReadString());
            }
            catch (Exception ex)
            {
                Melon<EntryPoint>.Logger.Msg(ex);
            }
        }

        public static void WriteSave(BinaryWriter writer)
        {
            try
            {
                writer.Write(VERSION);
                writer.Write(EntryPoint.disabledLocations.Count);
                foreach (string s in EntryPoint.disabledLocations)
                    writer.Write(s);
            }
            catch (Exception ex)
            {
                Melon<EntryPoint>.Logger.Msg(ex);
            }
        }

        [HarmonyPatch(typeof(AutoSaveDirector), "LoadNewGame")]
        [HarmonyPrefix]
        public static void NewGamePrefix() => EntryPoint.disabledLocations.Clear();

        [HarmonyPatch(typeof(SavedGame), "Push", new[] { typeof(GameModel) })]
        [HarmonyPostfix]
        public static void ReadPostfix(SavedGame __instance)
        {
            string file = Path.Combine(GameContext.Instance.AutoSaveDirector.StorageProvider.Cast<FileStorageProvider>().savePath, 
                $"plothider_{__instance.gameState.GameName}_{(__instance.gameState.Summary.SaveNumber - 1) % 6}");
            if (!File.Exists(file))
            {
                EntryPoint.disabledLocations.Clear();
                return;
            }

            using (FileStream st = File.OpenRead(file))
            {
                BinaryReader reader = new BinaryReader(st);
                ReadV1Save(reader);
            }
        }

        [HarmonyPatch(typeof(SavedGame), "Pull", new[] { typeof(GameModel) })]
        [HarmonyPostfix]
        public static void WritePostfix(SavedGame __instance)
        {
            string file = Path.Combine(GameContext.Instance.AutoSaveDirector.StorageProvider.Cast<FileStorageProvider>().savePath, 
                $"plothider_{__instance.gameState.GameName}_{(__instance.gameState.Summary.SaveNumber - 1) % 6}".Replace(" ", string.Empty));

            using (FileStream st = File.Create(file))
            {
                BinaryWriter writer = new BinaryWriter(st);
                WriteSave(writer);
            }
        }
    }
}
