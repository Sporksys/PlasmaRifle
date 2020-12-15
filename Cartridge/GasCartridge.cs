using System.Text;
using UnityEngine;

namespace PlasmaRifle
{
    public class GasCartridge : MonoBehaviour
    {
        private static readonly float DecayTimeInSeconds = 60f;

        public int charges = 0;

        public void AppendTooltip(StringBuilder stringBuilder)
        {
            if(stringBuilder != null)
            {
                stringBuilder.AppendFormat("\n<size=20><color=#DDDEDEFF>Charges: {0}</color></size>", this.charges);
            }
        }

        public void StartDecay()
        {
            Invoke("Decay", DecayTimeInSeconds);
        }

        private void Decay()
        {
            DestroyImmediate(this.gameObject);
        }
    }
}
