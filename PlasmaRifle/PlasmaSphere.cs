using UnityEngine;
using Object = UnityEngine.Object;

namespace PlasmaRifle
{
    class PlasmaSphere : MonoBehaviour
    {
        private readonly float ShellRadius = 0.5f;
        private readonly float Speed = 100f;
        private readonly float Energy = 1f;
        private readonly float Consumption = 0.35f;
        private readonly float VisibilityDistance = 0.5f;

        private LayerMask layerMask;

        [AssertNotNull]
        public VFXController fxControl;

        private float tempSpeed;
        private float tempEnergy;
        private bool active;
        private bool visible;
        private float damage;
        private float path;

        public float currentSpeed => this.tempSpeed * this.tempEnergy;

        private ParticleSystemSwapper[] swapperArray = new ParticleSystemSwapper[11];

        public void SetValuesFromOriginal(StasisSphere sSphere)
        {
            if(sSphere != null)
            {
                this.fxControl = sSphere.fxControl;
                this.layerMask = sSphere.hitLayers;

                Object.DestroyImmediate(this.gameObject.GetComponent<MeshRenderer>());

                if(this.fxControl != null)
                {
                    ParticleSystem particleSystem = this.fxControl.emitters[0].fx.GetComponent<ParticleSystem>();
                    swapperArray[0] = new ParticleSystemSwapper(particleSystem, Color.magenta);
                    swapperArray[1] = new ParticleSystemSwapper(particleSystem.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[2] = new ParticleSystemSwapper(particleSystem.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>(), Color.white);
                    swapperArray[3] = new ParticleSystemSwapper(particleSystem.transform.GetChild(2).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[4] = new ParticleSystemSwapper(particleSystem.transform.GetChild(3).gameObject.GetComponent<ParticleSystem>(), Color.white);

                    particleSystem = this.fxControl.emitters[1].fx.GetComponent<ParticleSystem>();
                    swapperArray[5] = new ParticleSystemSwapper(particleSystem, Color.magenta);
                    swapperArray[6] = new ParticleSystemSwapper(particleSystem.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[7] = new ParticleSystemSwapper(particleSystem.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[8] = new ParticleSystemSwapper(particleSystem.transform.GetChild(2).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[9] = new ParticleSystemSwapper(particleSystem.transform.GetChild(3).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                    swapperArray[10] = new ParticleSystemSwapper(particleSystem.transform.GetChild(4).gameObject.GetComponent<ParticleSystem>(), Color.magenta);
                }
            }
        }

        public void SetParticleColors()
        {
            foreach (ParticleSystemSwapper swapper in swapperArray)
            {
                if (swapper != null)
                {
                    swapper.SetModColor();
                }
            }
        }

        public void ResetParticleColors()
        {
            foreach(ParticleSystemSwapper swapper in swapperArray)
            {
                if(swapper != null)
                {
                    swapper.SetOriginalColor();
                }
            }
        }

        public void ShootPlasma(Vector3 position, Quaternion rotation, float damage)
        {
            this.damage = damage;
            this.transform.position = position;
            this.transform.rotation = rotation;
            this.active = true;
            this.tempEnergy = Energy;
            this.visible = false;
            this.tempSpeed = Speed;
            this.path = 0.0f;
            this.gameObject.SetActive(true);
        }

        protected void OnMadeVisible() => this.fxControl.Play(0);

        protected void OnHit(RaycastHit hitInfo)
        {
            if(this.visible)
            {
                this.fxControl.StopAndDestroy(0, 1f);
            }

            this.fxControl.Play(1);

            Collider[] colliderArray = Physics.OverlapSphere(this.transform.position, 5f, 5);
            
            foreach(Collider collider in colliderArray)
            {
                LiveMixin liveMixin = collider.gameObject.GetComponent<LiveMixin>();
                if(liveMixin != null && liveMixin.IsAlive())
                {
                    if(liveMixin.IsAlive())
                    {
                        liveMixin.TakeDamage(this.damage, default, DamageType.Undefined);
                    }
                }
            }
        }

        protected void OnEnergyDepelted()
        {
            this.fxControl.StopAndDestroy(0, 1f);
        }

        protected virtual void Update()
        {
            if(!this.active)
            {
                return;
            }
            this.tempEnergy -= this.Consumption * Time.deltaTime;
            if(this.tempEnergy <= 0.0f)
            {
                this.tempEnergy = 0.0f;
            }
            float maxDistance = this.currentSpeed * Time.deltaTime;
            this.path += maxDistance;
            if(!this.visible && this.path >= this.VisibilityDistance)
            {
                this.visible = true;
                this.OnMadeVisible();
            }
            if(Physics.SphereCast(this.transform.position, this.ShellRadius, this.transform.forward, out RaycastHit hitInfo, maxDistance, this.layerMask.value))
            {
                maxDistance = hitInfo.distance;
                if (!this.visible)
                {
                    this.visible = true;
                    this.OnMadeVisible();
                }
                this.OnHit(hitInfo);
                this.Deactivate();
            }
            this.transform.position += this.transform.forward * maxDistance;
            if(this.tempEnergy > 0.0f)
            {
                return;
            }
            this.OnEnergyDepelted();
            this.Deactivate();
        }

        public virtual void Deactivate()
        {
            if(!this.active)
            {
                return;
            }
            this.active = false;
            this.tempEnergy = Energy;
            this.visible = false;
            this.tempSpeed = 0.0f;
            this.path = 0.0f;
        }

    }
}
