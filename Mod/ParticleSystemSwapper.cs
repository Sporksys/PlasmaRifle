using UnityEngine;

namespace PlasmaRifle
{
    class ParticleSystemSwapper
    {
        private ParticleSystem particleSystem;
        private ParticleSystem.MinMaxGradient originalColor;
        private ParticleSystem.MinMaxGradient modColor;

        public ParticleSystemSwapper(ParticleSystem particleSystem, ParticleSystem.MinMaxGradient modColor)
        {
            this.particleSystem = particleSystem;
            if (this.particleSystem != null)
            {
                ParticleSystem.MainModule main = this.particleSystem.main;
                this.originalColor = main.startColor;
                this.modColor = modColor;
            }
        }

        public void SetOriginalColor()
        {
            ParticleSystem.MainModule main = this.particleSystem.main;
            main.startColor = this.originalColor;
        }

        public void SetModColor()
        {
            ParticleSystem.MainModule main = this.particleSystem.main;
            main.startColor = this.modColor;
        }
    }
}
