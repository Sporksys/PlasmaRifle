using QModManager.API.ModLoading;
using HarmonyLib;
using System.Reflection;

namespace PlasmaRifle
{
    [QModCore]
    public static class Main
    {
        public static readonly string[] SlotNameArray = { "BatterySlot1", "BatterySlot2", "BatterySlot3", "BatterySlot4", "BatterySlot5" };

        public static TechType techType;

        internal static PlasmaRiflePrefab plasmaRifle { get; } = new PlasmaRiflePrefab();

        [QModPatch]
        public static void Patch()
        {
            plasmaRifle.Patch();
            Main.techType = plasmaRifle.TechType;

            Equipment.slotMapping.Add(Main.SlotNameArray[0], EquipmentType.BatteryCharger);
            Equipment.slotMapping.Add(Main.SlotNameArray[1], EquipmentType.BatteryCharger);
            Equipment.slotMapping.Add(Main.SlotNameArray[2], EquipmentType.BatteryCharger);
            Equipment.slotMapping.Add(Main.SlotNameArray[3], EquipmentType.BatteryCharger);
            Equipment.slotMapping.Add(Main.SlotNameArray[4], EquipmentType.BatteryCharger);

            KnownTech.onAdd += OnAdd;

            Harmony harmony = new Harmony("com.subnautica.plasmarifle.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void OnAdd(TechType techType, bool verbose)
        {
            if(Main.techType == techType)
            {
                Inventory.main.equipment.AddSlot(Main.SlotNameArray[0]);
                Inventory.main.equipment.AddSlot(Main.SlotNameArray[1]);
                Inventory.main.equipment.AddSlot(Main.SlotNameArray[2]);
                Inventory.main.equipment.AddSlot(Main.SlotNameArray[3]);
                Inventory.main.equipment.AddSlot(Main.SlotNameArray[4]);

                if(verbose)
                {
                    PDAEncyclopedia.Add("PlasmaRifle", true);
                }
            }
        }
    }
}
