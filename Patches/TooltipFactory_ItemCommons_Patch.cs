using HarmonyLib;
using UnityEngine;
using System.Text;

namespace PlasmaRifle
{
    [HarmonyPatch(typeof(TooltipFactory), "ItemCommons")]
    class TooltipFactory_ItemCommons_Patch
    {
        private static bool Prefix(StringBuilder sb, TechType techType, GameObject obj)
        {
            if(techType == Main.techType)
            {
                string title = Language.main.Get(techType);
                sb.AppendFormat("<size=25><color=#ffffffff>{0}</color></size>", title);

                EnergyMixin energyMixin = obj.GetComponent<EnergyMixin>();
                if(energyMixin != null)
                {
                    GameObject battery = energyMixin.GetBattery();
                    IBattery iBattery = battery != null ? battery.GetComponent<IBattery>() : null;
                    if(iBattery != null)
                    {
                        sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0}</color></size>", iBattery.GetChargeValueText());
                    }
                    else
                    {
                        sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0}</color></size>", Language.main.Get("BatteryNotInserted"));
                    }
                }
                PlasmaRifle plasmaRifle = obj.GetComponent<PlasmaRifle>();
                if(plasmaRifle != null)
                {
                    sb.AppendFormat("\n<size=20><color={0}>{1}</color></size>", ConditionHandler.GetConditionColor(plasmaRifle.condition), plasmaRifle.GetTooltipString());
                }

                sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0}</color></size>", Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(techType)));

                return false;
            }

            return true;
        }
    }
}
