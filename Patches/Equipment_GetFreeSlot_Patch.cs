using HarmonyLib;
using UnityEngine;

namespace PlasmaRifle
{
    [HarmonyPatch(typeof(Inventory), "Pickup")]
    class Equipment_GetFreeSlot_Patch
    {
        private static bool Prefix(ItemsContainer ____container, Equipment ____equipment, ref bool __result, Pickupable pickupable, bool noMessage = false)
        {
            if(!____container.HasRoomFor(pickupable))
            {
                __result = false;
            }
            Vector3 position = pickupable.gameObject.transform.position;
            TechType techType = pickupable.GetTechType();
            pickupable = pickupable.Pickup();
            InventoryItem inventoryItem = new InventoryItem(pickupable);

            if(TechType.Battery == techType || TechType.PrecursorIonBattery == techType)
            {
                ____container.UnsafeAdd(inventoryItem);
            }
            else if(!((IItemsContainer)____equipment).AddItem(inventoryItem))
            {
                ____container.UnsafeAdd(inventoryItem);
            }
            if(!noMessage)
            {
                UniqueIdentifier component = pickupable.GetComponent<UniqueIdentifier>();
                if(component != null)
                {
                    NotificationManager.main.Add(NotificationManager.Group.Inventory, component.Id, 4f);
                }
            }
            KnownTech.Analyze(pickupable.GetTechType());
            if(Utils.GetSubRoot() != null)
            {
                pickupable.destroyOnDeath = false;
            }
            if (!noMessage)
            {
                uGUI_IconNotifier.main.Play(techType, uGUI_IconNotifier.AnimationType.From);
            }
            SkyEnvironmentChanged.Send(pickupable.gameObject, Player.main.GetSkyEnvironment());
            __result = true;
            return false;
        }
    }
}
