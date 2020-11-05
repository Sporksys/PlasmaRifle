using HarmonyLib;
using UnityEngine;

namespace PlasmaRifle.Patches
{
    [HarmonyPatch(typeof(uGUI_Equipment), "Awake")]
    class uGUI_Equipment_Awake_Patch
    {
        private static void Prefix(uGUI_Equipment __instance)
        {
            foreach(uGUI_EquipmentSlot equipmentSlot in __instance.GetComponentsInChildren<uGUI_EquipmentSlot>(true))
            {
                if(equipmentSlot.slot == "Chip1")
                {
                    uGUI_EquipmentSlot batterySlot1 = UnityEngine.Object.Instantiate<uGUI_EquipmentSlot>(equipmentSlot, equipmentSlot.transform.parent);
                    batterySlot1.slot = Main.SlotNameArray[0];
                    batterySlot1.transform.localPosition = new Vector3(-220f, -300f, 0.0f);

                    uGUI_EquipmentSlot batterySlot2 = UnityEngine.Object.Instantiate<uGUI_EquipmentSlot>(equipmentSlot, equipmentSlot.transform.parent);
                    batterySlot2.slot = Main.SlotNameArray[1];
                    batterySlot2.transform.localPosition = new Vector3(-110f, -300f, 0.0f);

                    uGUI_EquipmentSlot batterySlot3 = UnityEngine.Object.Instantiate<uGUI_EquipmentSlot>(equipmentSlot, equipmentSlot.transform.parent);
                    batterySlot3.slot = Main.SlotNameArray[2];
                    batterySlot3.transform.localPosition = new Vector3(0f, -300f, 0.0f);

                    uGUI_EquipmentSlot batterySlot4 = UnityEngine.Object.Instantiate<uGUI_EquipmentSlot>(equipmentSlot, equipmentSlot.transform.parent);
                    batterySlot4.slot = Main.SlotNameArray[3];
                    batterySlot4.transform.localPosition = new Vector3(110f, -300f, 0.0f);

                    uGUI_EquipmentSlot batterySlot5 = UnityEngine.Object.Instantiate<uGUI_EquipmentSlot>(equipmentSlot, equipmentSlot.transform.parent);
                    batterySlot5.slot = Main.SlotNameArray[4];
                    batterySlot5.transform.localPosition = new Vector3(220f, -300f, 0.0f);

                    break;
                }
            }
        }
    }
}
