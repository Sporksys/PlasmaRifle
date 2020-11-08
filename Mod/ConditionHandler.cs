using System;

namespace PlasmaRifle
{
    class ConditionHandler
    {
        public static readonly int MaxCondition = 100;
        public static readonly int ConditionDegradeAmount = 5;
        public static readonly float CleanRate = 20f;

        private static Random rand = new Random();

        private static readonly ConditionTier[] conditionTiers = new ConditionTier[]
        {
            new ConditionTier(100, 76, 0, "#BFFF00FF"),
            new ConditionTier(75, 51, 5, "#FFFF00FF"),
            new ConditionTier(50, 26, 10, "#FFBF00FF"),
            new ConditionTier(25, 1, 25, "#FF8000FF"),
            new ConditionTier(0, 0, 50, "#FF4000FF")
        };

        public class ConditionTier
        {
            public int high;
            public int low;
            public int destroyChance;
            public string colorHexCode;

            public ConditionTier(int high, int low, int destroyChance, string colorHexCode)
            {
                this.high = high;
                this.low = low;
                this.destroyChance = destroyChance;
                this.colorHexCode = colorHexCode;
            }
        }

        public static bool IsDestroyed(int condition)
        {
            foreach(ConditionTier tier in conditionTiers)
            {
                if(tier.high >= condition && tier.low <= condition)
                {
                    return rand.Next(1, 100) <= tier.destroyChance;
                }
            }

            return false;
        }

        public static string GetConditionColor(int condition)
        {
            foreach(ConditionTier tier in conditionTiers)
            {
                if(tier.high >= condition && tier.low <= condition)
                {
                    return tier.colorHexCode;
                }
            }

            return "#DDDEDEFF";
        }
    }
}
