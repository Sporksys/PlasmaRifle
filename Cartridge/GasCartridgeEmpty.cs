using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlasmaRifle
{
    class GasCartridgeEmpty : Craftable
    {
        private TechData techData;
        private Atlas.Sprite itemSprite;
        
        public GasCartridgeEmpty() : base("EmptyGasCartridge", "Empty Gas Cartridge", "A cylinder meant to hold gas.")
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
                        new Ingredient(TechType.Titanium, 1),
                        new Ingredient(TechType.Glass, 1)
                    }
                };
            }
            
            return this.techData;
        }
        
        protected override Atlas.Sprite GetItemSprite()
        {
            if(this.itemSprite == null)
            {
                this.itemSprite = SpriteManager.Get(TechType.DepletedReactorRod);
            }
            
            return this.itemSprite;
        }
        
        public override GameObject GetGameObject()
        {
            GameObject prefab = Object.Instantiate(CraftData.GetPrefabForTechType(TechType.ReactorRod));
            
            prefab.AddComponent<GasCartridge>();
            
            Object.DestroyImmediate(prefab.GetComponent<Smell>());
            Object.DestroyImmediate(prefab.GetComponent<EcoTarget>());
            
            MeshRenderer renderer = prefab.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>();
            renderer.material.DisableKeyword("MARMO_EMISSION");
            
            WorldForces worldForces = prefab.GetComponent<WorldForces>();
            worldForces.underwaterGravity = 0.5f;
            worldForces.underwaterDrag = 2f;
            
            prefab.transform.root.localScale -= new Vector3(0.80f, 0.80f, 0.80f);
            
            return prefab;
        }
    }
}
