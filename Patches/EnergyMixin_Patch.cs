using HarmonyLib;

namespace PlasmaRifle
{
    [HarmonyPatch(typeof(EnergyMixin), "OnAddItem")]
    internal class EnergyMixin_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(EnergyMixin __instance, InventoryItem item)
        {
            if(TechType.Lubricant == item.item.GetTechType())
            {
                PlasmaRifle plasmaRifle = __instance.gameObject.GetComponent<PlasmaRifle>();
                if(plasmaRifle != null)
                {
                    plasmaRifle.StartClean();
                }
            }
        }
    }
}
