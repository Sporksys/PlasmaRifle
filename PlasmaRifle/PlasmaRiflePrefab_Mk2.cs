using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlasmaRifle
{
    class PlasmaRiflePrefab_Mk2 : Equipable
    {
        private Atlas.Sprite itemSprite;
        private TechData techData;
        private PDAEncyclopedia.EntryData entryData;

        public PlasmaRiflePrefab_Mk2() : base("PlasmaRifleMk2", "Plasma Rifle Mk2", "Uses stasis technology to deliver a playload of plasma.")
        {

        }

        public override EquipmentType EquipmentType => EquipmentType.Hand;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Tools;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Tools" };
        public override float CraftingTime => 5f;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override string DiscoverMessage => "NotificationBlueprintUnlocked";
        public override TechType RequiredForUnlock => TechType.StasisRifle;

        protected override TechData GetBlueprintRecipe()
        {
            if (this.techData == null)
            {
                this.techData = new TechData
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(Main.Mk1TechType, 1)
                    }
                };
            }

            return this.techData;
        }
        
        public override PDAEncyclopedia.EntryData EncyclopediaEntryData => GetEntryData();
        
        private PDAEncyclopedia.EntryData GetEntryData()
        {
            if(this.entryData == null)
            {
                entryData = new PDAEncyclopedia.EntryData();
                entryData.key = this.ClassID;
                entryData.nodes = new string[] {"Tech", "Equipment" };
                
                Texture2D texture = GetItemSprite().texture;
                entryData.popup = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            }
            
            return entryData;
        }

        protected override Atlas.Sprite GetItemSprite()
        {
            if(this.itemSprite == null)
            {
                this.itemSprite = ImageUtils.LoadSpriteFromFile("./QMods/PlasmaRifle/Assets/plasma_rifle_icon.png");
            }

            return this.itemSprite;
        }

        public override GameObject GetGameObject()
        {
            GameObject prefab = Object.Instantiate(CraftData.GetPrefabForTechType(TechType.StasisRifle));

            PlasmaRifle pRifle = prefab.AddComponent<PlasmaRifle>();
            pRifle.SetInitValues(125, 130, 750, 4, 2, false);
            
            StasisRifle sRifle = prefab.GetComponent<StasisRifle>();
            pRifle.SetValuesFromOriginal(sRifle);
            pRifle.effectSpherePrefab = Object.Instantiate(sRifle.effectSpherePrefab);

            PlasmaSphere pSphere = pRifle.effectSpherePrefab.AddComponent<PlasmaSphere>();

            StasisSphere sSphere = pRifle.effectSpherePrefab.GetComponent<StasisSphere>();
            pSphere.SetValuesFromOriginal(sSphere);

            Object.DestroyImmediate(sSphere);
            Object.DestroyImmediate(sRifle);

            return prefab;
        }
    }
}
