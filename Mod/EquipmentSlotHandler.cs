using UnityEngine;

namespace PlasmaRifle
{
    class EquipmentSlotHandler
    {
        public static GameObject GetFirstChargedBattery(int slotIndex)
        {
            if(slotIndex >= 0 && slotIndex < Main.SlotNameArray.Length)
            {
                InventoryItem slottedBattery = Inventory.main.equipment.GetItemInSlot(Main.SlotNameArray[slotIndex]);
                if(slottedBattery != null)
                {
                    GameObject gameObject = slottedBattery.item.gameObject;
                    Battery battery = gameObject.GetComponent<Battery>();
                    if(battery.charge > 0.0f)
                    {
                        return gameObject;
                    }
                }
                return GetFirstChargedBattery(++slotIndex);
            }

            return null;
        }

    }
}
