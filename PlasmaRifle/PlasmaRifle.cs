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
        
        public void StartClean()
        {
            if(!this.isCleaning)
            {
                ErrorMessage.AddMessage(m_CleaningStart);
                InvokeRepeating(CleanMethod, ConditionHandler.CleanRate, ConditionHandler.CleanRate);
                this.isCleaning = true;
            }
        }
        
        public void DoClean()
        {
            this.currentCondition = Math.Min(this.maxCondition, (this.currentCondition + ConditionHandler.ConditionDegradeAmount));
            
            if(this.currentCondition == this.maxCondition)
            {
                ErrorMessage.AddMessage(m_CleaningEnd);
                CancelInvoke(CleanMethod);
                this.isCleaning = false;
            }
        }
        
        public void StartUnjam()
        {
            if(!this.isUnjamming)
            {
                Inventory.main.quickSlots.SetIgnoreHotkeyInput(true);
                this.isUnjamming = true;
                this.unjamSound.Play();
                this.unjamFx.Play(0);
                Invoke("FinishUnjam", 2f);
            }
        }
        
        private void FinishUnjam()
        {
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(false);
            this.cartridges.isJammed = false;
            this.isUnjamming = false;
            this.unjamSound.Stop();
            this.unjamFx.Stop(0);
        }
       
        private void Fire()
        {
            float currentTime = Time.realtimeSinceStartup;
            if(currentTime - this.lastFiredTime < FireRate)
            {
                return;
            }
            this.lastFiredTime = currentTime;
            
            GasCartridge currentCartridge = this.cartridges.GetCurrentCartridge();
            if(currentCartridge == null)
            {
                ErrorMessage.AddError(m_OutOfCharges);
                return;
            }
            if(this.cartridges.isJammed || this.isUnjamming)
            {
                ErrorMessage.AddError("Rifle is jammed!");
                return;
            }
            if(this.energyMixin.charge <= 5f)
            {   
                ErrorMessage.AddError(Language.main.Get("BatteryDepleted"));
                return;
            }
        
            if(this.isCleaning)
            {
                ErrorMessage.AddError(m_CleaningInterrupted);
                CancelInvoke(CleanMethod);
                isCleaning = false;
            }
            
            this.sphere.StopAndDestroy();
            this.fxControl.Play(1);
            FMODUWE.PlayOneShot(this.fireSound, this.transform.position, 1f);
            
            this.energyMixin.ConsumeEnergy(this.targetController.GetCurrentTaret() == null ? this.energyCost : (this.energyCost *2));
            this.sphere.ShootPlasma(this.muzzle.position, Player.main.camRoot.GetAimingTransform().rotation, this.projectileSpeed, this.damage, this.targetingController.GetCurrentTarget());
            
            //Player.main.GetComponent<Rigidbody>().AddForce(-MainCamera.camera.transform.forward * 0f, ForceMode.VelocityChange);
        
            currentCartridge.charges--;
            if(currentCartridge.charges <= 0)
            {
                this.cartridges.DestroyCurrentCartridge();
                
                if(ConditionHandler.IsJammed(this.currentCondition))
                {
                    this.cartridges.Jam();
                    ErrorMessage.AddError("Rifle is jammed!");
                }
                else
                {
                    FMODUEW.PlayOneShot(this.ejectSound, this.transform.position, 1f);
                    this.cartridges.EjectCartridge(this.transform);
                    this.currentCondition = Math.Max(0, this.currentCondition - ConditionHandler.ConditionDegradeAmount);
                    this.cartridges.CycleNewCartridge();
                }
            }
            
            this.UpdateBar();      
        }
        
        public void AppendTooltip(StringBuilder sb)
        {
            sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0} {1}</color></size>", "DAMAGE:", this.damage)
            sb.AppendFormat("\n<size=20><color={0}>{1} {2} / {3} {4}</color></size>", ConditionHandler.GetConditionColor(this.currentCondition), "CONDITION:", this.currentCondition, this.maxCondition, (this.isCleaning? "(Cleaning)" : ""));
            sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0} {1}</color></size>", "AMMO:", this.cartridges.GetTotalChargeCount());
        }
        
        public override string GetCustomUseText()
        {
            if(this.cartridges.isJammed && this.cartridges.count == 0)
            {
                return this.altUseTextJammed;
            }
            else if(this.isTargeting)
            {
                return this.altUseTextTargeting;
            }
            else
            {
                return this.altUseText;
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

        public class SaveData
        {
            public int condition;
            public bool isCleaning;
            public List<int> cartridgeCharges;

            public SaveData() { }

            public SaveData(int condition, bool isCleaning, Liste<int> cartridgeCharges)
            {
                this.condition = condition;
                this.isCleaning = isCleaning;
                this.cartridgeCharges = cartridgeCharges;
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            string fileName = GetFileName();
            SaveData saveData = new SaveData(this.condition, this.isCleaning, this.cartridges.GetChargesToSerialize());

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
                if(saveData.isCleaning)
                {
                    InvokeRepeating(CleanMethod, ConditionHandler.CleanRate, ConditionHandler.CleanRate);
                    this.isCleaning = true;
                }
                if(saveData.cartridgeCharges != null)
                {
                    this.InitCaridgeContainer();
                    this.cartridges.InitAfterDeserialize(saveData.cartridgeCharges);
                }
            }
        }

        private string GetFileName()
        {
            return Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "PlasmaRifle/data_" + this.gameObject.GetComponent<PrefabIdentifier>().Id + ".xml");
        }
    }
}
