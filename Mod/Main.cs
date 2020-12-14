using QModManager.API.ModLoading;
using HarmonyLib;
using System.Reflection;

namespace PlasmaRifle
{
    [QModCore]
    public static class Main
    {
        public static TechType Mk1TechType;
        public static TechType Mk2TechType;
        public static TechType Mk3TechType;
        public static TechType FullCartridgeTechType;
        public static TechType EmptyCartridgeTechType;

        public static string Mk1ClassId;
        public static string Mk2ClassId;
        public static string Mk3ClassId;
        
        internal static PlasmaRiflePrefab_Mk1 plasmaRifleMk1 { get; } = new PlasmaRiflePrefab_Mk1();
        internal static PlasmaRiflePrefab_Mk2 plasmaRifleMk2 { get; } = new PlasmaRiflePrefab_Mk2();
        internal static PlasmaRiflePrefab_Mk3 plasmaRifleMk3 { get; } = new PlasmaRiflePrefab_Mk3();
        internal static GasCartridgeEmpty gasCartridgeEmpty { get; } = new GasCartridgeEmpty();
        internal static GasCartridgeFull gasCartridgeFull { get; } = new GasCartridgeFull();
        
        [QModPatch]
        public static void Patch()
        {
            plasmaRifleMk1.Patch();
            Main.Mk1TechType = plasmaRifleMk1.TechType;
            Main.Mk1ClassId = plasmaRifleMk1.ClassID;
            
            plasmaRifleMk2.Patch();
            Main.Mk2TechType = plasmaRifleMk2.TechType;
            Main.Mk2ClassId = plasmaRifleMk2.ClassID;
            
            plasmaRifleMk3.Patch();
            Main.Mk3TechType = plasmaRifleMk3.TechType;
            Main.Mk3ClassId = plasmaRifleMk3.ClassID;
            
            gasCartridgeEmpty.Patch();
            Main.EmptyCartridgeTechType = gasCartridgeEmpty.TechType;
            
            gasCartridgeFull.Patch();
            Main.FullCartridgeTechType = gasCartridgeFull.TechType;
           
            Harmony harmony = new Harmony("com.subnautica.plasmarifle.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        } 
    }
}
