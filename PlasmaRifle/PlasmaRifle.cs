using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlasmaRifle
{
    [RequireComponent(typeof(EnergyMixin))]
    public class PlasmaRifle : PlayerTool, IProtoEventListener
    {

        public override string animToolName => TechType.StasisRifle.AsString(true);

        private static readonly string CleanMethod = "DoClean";
        private static readonly string TargetingMethod = "DoTargeting";
        private static readonly float FireRate = 1f;
        
        public static readonly string m_CleaningStart = "Librication applied, cleaning rifle";
        public static readonly string m_CleaningiEnd = "Plasma Rifle cleaning complete";
        public static readonly string m_CleaningInterrupted = "Plasma Rifle cleaning interrupted";
        public static readonly string m_OutOfCharges = "Out of charges";
        public static readonly string t_OpenCartridges = "Cartridges(<color=#ADF8FFFF>{0}</color>)";
        public static readonly string t_Unjam = "Clear Jam (<color=#ADF8FFFF>{0}</color>)";
        public static readonly string t_nexTarget = "Next Target (<color=#ADF8FFFF>{0}</color>)";

        private static readonly string CleanMethod = "DoClean";

        [AssertNotNull]
        public GameObject effectSpherePrefab;
        [AssertNotNull]
        public FMOD_StudioEventEmitter chargeBegin;
        [AssertNotNull]
        public FMODAsset fireSound;
        [AssertNotNull]
        public VFXController fxControl;
        
        private CartridgeContaineir cartridges;

        public Animator animator;
        public Transform muzzle;
        public Renderer bar;
        private PlasmaSphere sphere;

        private float energyCost = 5f;
        private string altUseText;
        private string altUseTextJammed;
        private string altUseTextTargeting;
        private bool isCleaning = false;
        private bool isUnjamming = false;
        private bool isTargeting = false;
        public int maxCondition;
        public int currentCondition;
        public float projectileSpeed;
        public float damage;
        public int ammoWidth;
        public int ammoHeight;
        public bool canTarget;
        private float lastFiredTime;
        
        public FMODAsset ejectSound;
        public FMOD_CustomLoopingEmitter unjamSound;
        public VFXController unjamFx;
        public FMOD_CustomLoopingEmitter targetingSound;
        public FMOD_CustomEmitter sonarSound;
        
        private float rotateSpeed = 0.0f
        private int rpmIndex;
        
        private readonly TargetingController targetController = new TargetingController();
        private readonly ParticleSystemSwapper[] swapperArray = new ParticleSystemSwapper[5];
       
        public void SetInitValues(int maxCondition, int projectileSpeed, int damage, int ammoWidth, int ammoHeight, bool canTarget)
        {
            this.maxCondition = maxCondition;
            this.currentCondition = maxCondition;
            this.projectileSpeed = projectileSpeed;
            this.damage = damage;
            this.ammoWidth = ammoWidth;
            this.ammoHeight = ammoHeight;
            this.canTarget = canTarget;
        }

        public void SetValuesFromOriginal(StasisRifle sRifle)
        {
            if (sRifle != null)
            {
                this.animator = sRifle.animator;
                this.muzzle = sRifle.muzzle;
                this.chargeBegin = sRifle.chargeBegin;
                this.bar = sRifle.bar;
                this.mainCollider = sRifle.mainCollider;
                this.drawSound = sRifle.drawSound;
                this.ikAimRightArm = sRifle.ikAimRightArm;
                this.ikAimLeftArm = sRifle.ikAimLeftArm;
                this.useLeftAimTargetOnPlayer = sRifle.useLeftAimTargetOnPlayer;
                this.leftHandIKTarget = sRifle.leftHandIKTarget;
                this.drawTime = sRifle.drawTime;
                this.holsterTime = sRifle.holsterTime;
                this.dropTime = sRifle.dropTime;
                this.pickupable = sRifle.pickupable;
                this.hasFirstUseAnimation = sRifle.hasFirstUseAnimation;
                this.hasBashAnimation = sRifle.hasBashAnimation;
                this.firstUseSound = sRifle.firstUseSound;

                this.fxControl = sRifle.fxControl;

                if (this.bar != null)
                {
                    this.bar.materials[0].SetColor(ShaderPropertyID._Color, new Color(0.20f, 0.20f, 0.20f, 1f));
                }
            }
        }

        public override void Awake()
        {
            base.Awake();
            
            string altUseBinding = GameInput.GetBindingName(GameInput.Button.AltTool, GameInput.BindingSet.Primary);
            
            this.altUseText = String.Format(t_OpenCartridges, altUseBinding);
            this.altUseTextJammed = String.Format(t_Unjam, altUseBinding);
            this.altUseTextTargeting = String.Format(t_nextTarget, altUseBinding);
            this.energyMixin.compatibleBatteries.Add(TechType.Lubricant);
            
            this.InitCartridgeContainer();
            
            this.fireSound = CraftData.GetPrefabForTechType(TechType.RepulsionCannon).GetComponent<RepulsionCannon>().shootSound;
            this.ejectSound = CraftData.GetPrefabForTechType(TechType.PropulsionCannon).GetComponent<PropulsionCannon>().shootSound;
            this.sonarSound = CraftData.GetPrefabForTechType(TechType.Seamoth).GetComponent<Seamoth>().sonarSound;
            
            EngineRpmSFXManager engineSFXManager = CraftData.GetPrefabForTechType(TechType.Seaglide).GetComponent<Seaglide>().engineRPMManager;
            if(engineSFXManager != null)
            {
                this.targetingSound = engineSFXManager.engineRpmSFX;
                this.rpmIndex = this.targetingSound.GetParameterIndex("rpm");
            }
            
            FireExtinguisher prefab = CraftData.GetPrefabForTechType(TechType.FireExtinguisher).GetComponent<FireExtinguisher>();
            this.unjamSound = prefab.soundEmitter;

            if (this.fxControl != null)
            {
                ParticleSystem particleSystem = this.fxControl.emitters[1].fx.GetComponent<ParticleSystem>();
                swapperArray[0] = new ParticleSystemSwapper(particleSystem, Color.magenta);
                swapperArray[1] = new ParticleSystemSwapper(particleSystem.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[2] = new ParticleSystemSwapper(particleSystem.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[3] = new ParticleSystemSwapper(particleSystem.transform.GetChild(2).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[4] = new ParticleSystemSwapper(particleSystem.transform.GetChild(3).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
            }
            
            if (this.sphere == null && this.effectSpherePrefab != null)
            {
                this.sphere = Object.Instantiate<GameObject>(this.effectSpherePrefab, this.tr.position, Quaternion.identity).GetComponent<PlasmaSphere>();
                if(this.canTarget)
                {
                    this.sphere.EnemyKilledEvent += OnEnemyKilled;
                }
            }

            this.UpdateBar();
        }
        
        public void OnEnemyKilled()
        {
            this.targetControlled.CheckForDeadTargets();
        }

        private void OnAddItem(InventoryItem item) => UpdateBar();
        
        private void OnRemoveItem(InventoryItem item) => UpdateBar();
        
        private void InitCartridgeContainer()
        {
            if(this.cartridges == null)
            {
                this.cartridges = new CartridgeContainer(this.ammoWidth, this.ammoHeight, this.transform, "Cartridges", null);
                this.cartridges.onAddItem += OnAddItem;
                this.cartridges.onRemoveItem += OnRemoveItem;
            }
        }
        
        private void UpdateBar()
        {
            this.bar.materials[1].SetFloat(ShaderPropertyID._Amount, this.cartridges.GetTotalChargePercent());
        }

        public override void OnDraw(Player p)
        {
            base.OnDraw(p);
            SafeAnimator.SetBool(Utils.GetLocalPlayerComp().armsController.gameObject.GetComponent<Animator>(), "charged_stasisrifle", false);

            /*this.sphere.SetParticleColors();
            foreach (ParticleSystemSwapper swapper in swapperArray) {
                if (swapper != null)
                {
                    swapper.SetModColor();
                }
            }*/
        }
        
        public override void OnHolster()
        {
            base.OnHolster();
            this.StopTargeting();
            
            /*this.sphere.ResetParticleColors();
            foreach (ParticleSystemSwapper swapper in swapperArray)
            {
                if (swapper != null)
                {
                    swapper.SetOriginalColor();
                }
            }*/
        }

        public void Update()
        {
            if(this.isTargeting)
            {
                this.rotateSpeed += 0.005f;
                if(this.rotateSpeed >= 0.85f)
                {
                    this.rotateSpeed = 0.85f;
                }
                this.targetingSound.SetParameterValue(this.rpmIndex, this.rotateSpeed);
                this.targetingSound.Play();
            }
            else
            {
                this.StopTargeting();
            }
        }
        
        private void StopTargeting()
        {
            this.isTargeting = false;
            this.rotateSpeed = 0.0f;
            this.targetingSound.Stop();
        }
        
        public override bool OnLeftHandUp()
        {
            if(this.canTarget)
            {                
                if(!this.isTargeting)
                {
                    if(this.energyMixin.charge <= 5f)
                    {
                        ErrorMessage.AddError(Language.main.Get("BatteryDepleted"));
                        return base.OnLeftHandUp();
                    }
                    
                    this.isTargeting = true;
                    this.Animate(true);
                    
                    this.sonarSound.Stop();
                    this.sonarSound.transform.position = this.transform.position;
                    
                    this.sonarSound.Play();
                    SNCameraRoot.main.SonarPing();
                    Invoke(TargetingMethod), 2f);
                    
                    this.energyMixin.ConsumeEnergy(this.energyCost);
                }
                else
                {
                    this.isTargeting = false;
                    this.Animate(false);
                    this.targetingSOund.SetParameterValue(this.rpmIndex, 0.0f);
                    this.targetingController.ReleaseTargets();
                }
            }
            
            return base.OnLeftHandUp();
        }
        
        private void DoTargeting()
        {
            this.targetController.AcquireTargets();
        }

        public override bool OnRightHandDown()
        {
            this.Fire();
            return this.isCharging;
        }

        public override bool OnAltUp()
        {
            if(this.cartridges.isJammed && this.cartridges.count == 0)
            {
                this.StartUnjam();
            }
            else if(this.isTargeting)
            {
                this.targetController.CycleNextTarget();
            }
            else
            {
                this.cartridges.Open();
            }
            
            return base.OnAltUp();
        }

        private void LateUpdate()
        {
            if (this.energyMixin.HasItem())
            {
                this.currentState = PlasmaRifleState.Chambered;

                if (this.energyMixin.charge == 0)
                {
                    Pickupable pickupable = this.energyMixin.storageRoot.transform.GetChild(0).gameObject.GetComponent<Pickupable>();

                    if (TechType.Lubricant == pickupable.GetTechType())
                    {
                        Object.DestroyImmediate(pickupable);
                        ErrorMessage.AddMessage("Lubrication applied, cleaning rifle");

                        InvokeRepeating(CleanMethod, ConditionHandler.CleanRate, ConditionHandler.CleanRate);
                        isCleaning = true;
                    }
                }
            }
            else if (this.chargeAmount > 0.0f)
            {
                this.currentState = PlasmaRifleState.Charged;
            }
            else
            {
                this.currentState = PlasmaRifleState.Empty;
            }
        }

        public void DoClean()
        {
            this.condition = Math.Min(ConditionHandler.MaxCondition, (this.condition + ConditionHandler.ConditionDegradeAmount));

            if (this.condition == ConditionHandler.MaxCondition)
            {
                ErrorMessage.AddMessage("Plasma Rifle cleaning complete");
                CancelInvoke(CleanMethod);
                this.isCleaning = false;
            }
        }

        private bool QuickReload()
        {
            GameObject gameObject = EquipmentSlotHandler.GetFirstChargedBattery(0);

            if (gameObject != null)
            {
                Pickupable pickupable = gameObject.GetComponent<Pickupable>();
                Battery battery = gameObject.GetComponent<Battery>();

                if (battery.charge > 0.0f)
                {
                    this.energyMixin.SetBattery(pickupable.GetTechType(), battery.charge);

                    Inventory.main.equipment.RemoveItem(pickupable);
                    return true;
                }
            }

            return true;
        }

        private void BeginCharge()
        {
            if (this.isCharging)
            {
                return;
            }
            else if (!Player.main.IsSwimming())
            {
                ErrorMessage.AddError("It is only safe to use the Plasma Rifle while submerged");
                return;
            }
            else if (this.energyMixin.charge <= 0.0f)
            {
                ErrorMessage.AddError(Language.main.Get("BatteryDepleted"));
            }
            else
            {
                if (this.isCleaning)
                {
                    ErrorMessage.AddError("Plasma Rifle cleaning interrupted");
                    CancelInvoke(CleanMethod);
                    isCleaning = false;
                }
            
                TechType batteryType = energyMixin.GetBattery().GetComponent<TechTag>().type;
                if (TechType.Battery == batteryType)
                {
                    this.energyCost = BaseEnergyCost;
                }
                else if (TechType.PrecursorIonBattery == batteryType)
                {
                    this.energyCost = IonEnergyCost;
                }

                this.isCharging = true;
                this.fxControl.Play(0);
                this.chargeBegin.StartEvent();
                this.chargeLoop.StartEvent();
                this.Animate(true);
            }
        }

        private void AbandonCharge()
        {
            if (this.isCharging)
            {
                this.EndCharge();
                this.chargeAmount = 0.0f;
                this.UpdateBar();
            }
        }

        private void EndCharge()
        {
            if (!this.isCharging)
            {
                return;
            }
            else
            {
                SafeAnimator.SetBool(Utils.GetLocalPlayerComp().armsController.gameObject.GetComponent<Animator>(), "charged_stasisrifle", false);
                this.isCharging = false;
                this.fxControl.StopAndDestroy(0, 0.0f);
                if (this.chargeBegin.GetIsStartingOrPlaying())
                {
                    this.chargeBegin.Stop(false);
                }
                if (this.chargeLoop.GetIsStartingOrPlaying())
                {
                    this.chargeLoop.Stop(false);
                }
                this.Animate(false);
            }
        }

        private void Charge()
        {
            if (!this.isCharging)
            {
                return;
            }
            else
            {
                float amount = this.energyCost * Time.deltaTime / this.chargeDuration;
                float charge = this.energyMixin.charge;
                if (amount >= charge)
                {
                    amount = charge;
                }
                else if (this.chargeAmount + amount >= this.energyCost)
                {
                    amount = this.energyCost - this.chargeAmount;
                }
                this.energyMixin.ConsumeEnergy(amount);
                this.chargeAmount += amount;
                this.UpdateBar();

                if (this.chargeAmount == this.energyCost)
                {
                    this.EndCharge();
                }
            }
        }

        private void Fire()
        {
            if (this.chargeAmount <= 0.0f || this.energyMixin.charge == 0.0f)
            {
                return;
            }

            if (!Player.main.IsSwimming())
            {
                ErrorMessage.AddError("It is only safe to use the Plasma Rifle while submerged");
                return;
            }

            this.fxControl.Play(1);
            FMODUWE.PlayOneShot(this.fireSound, this.tr.position, 1f);

            float maxPlasma = this.chargeAmount * ChargeToPlasmaRate;
            float deliverablePlasma = Math.Min(this.energyMixin.charge, maxPlasma);
            float plasmaDamage = deliverablePlasma * DamagePerPlasma;

            this.sphere.ShootPlasma(this.muzzle.position, Player.main.camRoot.GetAimingTransform().rotation, plasmaDamage);
            this.energyMixin.ConsumeEnergy(5000f);
            this.chargeAmount = 0.0f;
            this.UpdateBar();

            Player.main.GetComponent<Rigidbody>().AddForce(-MainCamera.camera.transform.forward * 15f, ForceMode.VelocityChange);

            EjectBattery();
        }

        private void EjectBattery()
        {
            Pickupable battery = this.energyMixin.GetBattery().GetComponent<Pickupable>();

            if (ConditionHandler.IsDestroyed(this.condition))
            {
                Object.DestroyImmediate(battery);
                ErrorMessage.AddError("Rifle jammed - battery was destroyed");
            }
            else
            {
                this.condition = Math.Max(0, (this.condition - ConditionHandler.ConditionDegradeAmount));

                battery.Drop();
                battery.GetComponent<Rigidbody>().AddForce(MainCamera.camera.transform.right * 5f, ForceMode.VelocityChange);

            }
        }

        private void UpdateBar()
        {
            if (this.bar == null)
            {
                return;
            }
            else
            {
                this.bar.materials[1].SetFloat(ShaderPropertyID._Amount, this.chargeNormalized);
            }
        }

        private void Animate(bool state)
        {
            if(this.animator == null || !this.animator.isActiveAndEnabled)
            {
                return;
            }

            SafeAnimator.SetBool(this.animator, "using_tool", state);
        }

        public string GetTooltipString()
        {
            return "CONDITION:" + this.condition + "%" + (this.isCleaning ? " (Cleaning)" : "");
        }

        public override bool GetUsedToolThisFrame() => this.isCharging;

        public override string GetCustomUseText()
        {
            switch(this.currentState)
            {
                case PlasmaRifleState.Empty: return this.quickReloadText;
                case PlasmaRifleState.Charged: return this.dischargeText;
                default: return null;
            }
        }

        public class SaveData
        {
            public int condition;
            public bool isCharged;
            public bool isCleaning;

            public SaveData() { }

            public SaveData(int condition, bool isCharged, bool isCleaning)
            {
                this.condition = condition;
                this.isCharged = isCharged;
                this.isCleaning = isCleaning;
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            string fileName = GetFileName();
            SaveData saveData = new SaveData(this.condition, (this.chargeAmount >= BaseEnergyCost), this.isCleaning);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SaveData));
            using(StringWriter stringWriter = new StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, saveData);

                FileInfo fileInfo = new FileInfo(fileName);
                fileInfo.Directory.Create();
                File.WriteAllText(fileName, stringWriter.ToString());
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            string fileText = File.ReadAllText(GetFileName());

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SaveData));
            using(StringReader stringReader = new StringReader(fileText))
            {
                SaveData saveData = (SaveData)xmlSerializer.Deserialize(stringReader);
                this.condition = saveData.condition;
                if(saveData.isCharged)
                {
                    this.chargeAmount = this.energyCost;
                }
                if(saveData.isCleaning)
                {
                    InvokeRepeating(CleanMethod, ConditionHandler.CleanRate, ConditionHandler.CleanRate);
                    this.isCleaning = true;
                }
            }
        }

        private string GetFileName()
        {
            return Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "PlasmaRifle/data_" + this.gameObject.GetComponent<PrefabIdentifier>().Id + ".xml");
        }
    }
}
