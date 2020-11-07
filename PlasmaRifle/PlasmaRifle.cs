using SMLHelper.V2.Utility;
using System;
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

        private static readonly float BaseEnergyCost = 5f;
        private static readonly float IonEnergyCost = 25f;
        private static readonly float ChargeToPlasmasRate = 20f;
        private static readonly float DamagePerPlasma = 4f;

        private static readonly string CleanMethod = "DoClean";

        [AssertNotNull]
        public GameObject effectSpherePrefab;
        [AssertNotNull]
        public FMOD_StudioEventEmitter chargeBegin;
        [AssertNotNull]
        public FMOD_StudioEventEmitter chargeLoop;
        [AssertNotNull]
        public FMODAsset fireSound;
        [AssertNotNull]
        public VFXController fxControl;

        private enum PlasmaRifleState
        {
            Empty,
            Chambered,
            Charged
        }

        public Animator animator;
        public Transform muzzle;
        public Renderer bar;
        private Transform tr;
        private float chargeAmount;
        private bool isCharging;
        private PlasmaSphere sphere;

        private float energyCost = BaseEnergyCost;
        private float chargeDuration = 3f;
        private string quickReloadText;
        private string dischargeText;
        private PlasmaRifleState currentState;
        private bool isCleaning = false;
        public int condition = ConditionHandler.MaxCondition;

        private ParticleSystemSwapper[] swapperArray = new ParticleSystemSwapper[5];

        private float chargeNormalized => this.chargeAmount / this.energyCost;

        public void SetValuesFromOriginal(StasisRifle sRifle)
        {
            if (sRifle != null)
            {
                this.animator = sRifle.animator;
                this.muzzle = sRifle.muzzle;
                this.chargeBegin = sRifle.chargeBegin;
                this.chargeLoop = sRifle.chargeLoop;
                this.fireSound = sRifle.fireSound;
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

            this.tr = this.GetComponent<Transform>();

            string binding = GameInput.GetBindingName(GameInput.Button.AltTool, GameInput.BindingSet.Primary);
            this.quickReloadText = "Quick Reload (<color=#ADF8FFFF>" + binding + "</color>)";
            this.dischargeText = "Discharge (<color=#ADF8FFFF>" + binding + "</color>)";

            this.energyMixin.compatibleBatteries.Add(TechType.Lubricant);

            if (this.fxControl != null)
            {
                ParticleSystem particleSystem = this.fxControl.emitters[1].fx.GetComponent<ParticleSystem>();
                swapperArray[0] = new ParticleSystemSwapper(particleSystem, Color.magenta);
                swapperArray[1] = new ParticleSystemSwapper(particleSystem.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[2] = new ParticleSystemSwapper(particleSystem.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[3] = new ParticleSystemSwapper(particleSystem.transform.GetChild(2).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                swapperArray[4] = new ParticleSystemSwapper(particleSystem.transform.GetChild(3).gameObject.GetComponent<ParticleSystem>(), Color.magenta);

                if (this.sphere == null && this.effectSpherePrefab != null)
                {
                    this.sphere = Object.Instantiate<GameObject>(this.effectSpherePrefab, this.tr.position, Quaternion.identity).GetComponent<PlasmaSphere>();
                }

                this.UpdateBar();
            }
        }

        public override void OnDraw(Player p)
        {
            base.OnDraw(p);

            this.sphere.SetParticleColors();
            foreach (ParticleSystemSwapper swapper in swapperArray) {
                if (swapper != null)
                {
                    swapper.SetModColor();
                }
            }
        }

        public override void OnHolster()
        {
            base.OnHolster();

            this.sphere.ResetParticleColors();
            foreach (ParticleSystemSwapper swapper in swapperArray)
            {
                if (swapper != null)
                {
                    swapper.SetOriginalColor();
                }
            }
        }

        public override bool OnRightHandDown()
        {
            if (this.isCleaning)
            {
                ErrorMessage.AddError("Plasma Rifle cleaning interrupted");
                CancelInvoke(CleanMethod);
                isCleaning = false;
            }

            if (!this.isCharging && this.chargeAmount == this.energyCost)
            {
                this.Fire();
            }
            else
            {
                this.BeginCharge();
                this.Charge();
            }
            return this.isCharging;
        }

        public override bool OnRightHandHeld()
        {
            this.Charge();
            return base.OnRightHandHeld();
        }

        public override bool OnRightHandUp()
        {
            this.AbandonCharge();
            return base.OnRightHandUp();
        }

        public override bool OnAltUp()
        {
            if (this.currentState == PlasmaRifleState.Empty)
            {
                if (!this.QuickReload())
                {
                    ErrorMessage.AddError("No batteries to chamber");
                }
            }
            else if (this.currentState == PlasmaRifleState.Charged)
            {
                this.chargeAmount = 0.0f;
                this.UpdateBar();
            }

            return base.OnAltUp();
        }

        private void LastUpdate()
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

            float maxPlasma = this.chargeAmount * ChargeToPlasmasRate;
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
            if(this.animator == null || this.animator.isActiveAndEnabled)
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
