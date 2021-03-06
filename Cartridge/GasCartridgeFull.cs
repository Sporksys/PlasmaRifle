using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlasmaRifle
{
    class GasCartridgeFull : Craftable
    {
        private TechData techData;
        private Atlas.Sprite itemSprite;
        
        public GasCartridgeFull() : base("GasCartridge", "Gas Cartridge", "A cylinder full of gas.")
        {
        
        }
        
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        
        public override string[] StepsToFabricatorTab => new string[] { "Resources", "AdvancedMaterials" };
        
        public override TechType RequiredForUnlock => TechType.StasisRifle;
        
        public override float CraftingTime => 2f;
        
        protected override TechData GetBlueprintRecipe()
        {
            if(this.techData == null)
            {
                this.techData = new TechData
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(Main.EmptyCartridgeTechType, 1)
                    }
                };
            }
            
            return this.techData;
        }
        
        protected override Atlas.Sprite GetItemSprite()
        {
            if(this.itemSprite == null)
            {
                this.itemSprite = SpriteManager.Get(TechType.ReactorRod);
            }
            
            return this.itemSprite;
        }
        
        public override GameObject GetGameObject()
        {
            GameObject prefab = Object.Instantiate(CraftData.GetPrefabForTechType(TechType.ReactorRod));
            
            GasCartridge gasCartridge = prefab.AddComponent<GasCartridge>();
            gasCartridge.charges = 3;
            
            Object.DestroyImmediate(prefab.GetComponent<Smell>());
            Object.DestroyImmediate(prefab.GetComponent<EcoTarget>());    
            
            WorldForces worldForces = prefab.GetComponent<WorldForces>();
            worldForces.underwaterGravity = 0.5f;
            worldForces.underwaterDrag = 2f;
            
            prefab.transform.root.localScale -= new Vector3(0.80f, 0.80f, 0.80f);
            
            return prefab;
        }
    }
}
