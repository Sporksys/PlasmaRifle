using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEngine;

namespace PlasmaRifle
{
    public class CartridgeContainer : ItemsContainer
    {
        private GasCartridge currentCartridge;
        public bool isJammed = false;
        
        public CartridgeContainer(int width, int height, Transform transform, string label, FMODAsset errorSoundEffect) : base(width, height, transform, label, errorSoundEffect)
        {
            this.SetAllowedTechTypes(new TechType[] { Main.FullCartridgeTechType });
        }
        
        public bool Open()
        {
            PDA pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(this);
            return pda.Open(PDATab.Inventory, this.tr, null);
        }
        
        public void Jam()
        {
            this.isJammed = true;
            this.currentCartridge = null;
        }
        
        public void EjectCartridge(Transform ejectFrom)
        {
            GameObject emptyCartridge = CraftData.InstantiateFromPrefab(Main.EmptyCartridgeTechType);
            if(emptyCartridge != null)
            {
                emptyCartridge.transform.position = ejectFrom.position;
                emptyCartridge.GetComponent<Rigidbody>().AddForce(((ejectFrom.right + ejectFrom.forward) * 5f + (ejectFrom.up * 2f)), ForceMode.VelocityChange);
                emptyCartridge.GetComponent<GasCartridge>().StartDecay();
            }
        }
        
        public GasCartridge GetCurrentCartridge()
        {
            if(this.currentCartridge == null)
            {
                CycleNewCartridge();
            }
            return this.currentCartridge;
        }
        
        public void CycleNewCartridge()
        {
            IList<InventoryItem> itemList = this.GetItems(Main.FullCartridgeTechType);
            if(itemList != null)
            {
                foreach(InventoryItem item in itemList)
                {
                    this.currentCartridge = item.item.gameObject.GetComponent<GasCartridge>();
                }
            }
            else
            {
                this.currentCartridge = null;
            }
        }
        
        public void DestroyCurrentCartridge()
        {
            Object.DestroyImmediate(this.currentCartridge.gameObject);
            this.currentCartridge = null;
        }
        
        public int GetTotalChargeCount()
        {
            int totalChargeCount = 0;
            IList<InventoryItem> itemList = this.GetItems(Main.FullCartridgeTechType);
            if(itemList != null)
            {
                foreach(InventoryItem item in itemList)
                {
                    totalChargeCount += item.item.gameObject.GetComponent<GasCartridge>().charges;
                }
            }
            return totalChargeCount;
        }
        
        public float GetTotalChargePercent()
        {
            return (float)GetTotalChargeCount() / ((float)this.sizeX * (float)this.sizeY * 3.0f);
        }
        
        public List<int> GetChargesToSerialize()
        {
            List<int> cartridgeChargeList = new List<int>();
            IList<InventoryItem> itemList = this.GetItems(Main.FullCartridgeTechType);
            if(itemList != null)
            {
                //Why is this a for loop instead of a foreach?
                for(int i = 0; i < itemList.Count; i++)
                {
                    cartridgeChargeList.Add(itemList[i].item.gameObject.GetComponent<GasCartridge>().charges);
                }
            }
            
            return cartridgeChargeList;
        }
        
        public void InitAfterDeserialize(List<int> cartridgeCharges)
        {
            if(cartridgeCharges != null)
            {
                foreach(int cartridgeCharge in cartridgeCharges)
                {
                    GameObject cartridge = CraftData.InstantiateFromPrefab(Main.FullCartridgeTechType);
                    cartridge.SetActive(false);
                    cartridge.GetComponent<GasCartridge>().charges = cartridgeCharge;
                    this.AddItem(cartridge.GetComponent<Pickupable>());
                }
            }
        }
    }
}
