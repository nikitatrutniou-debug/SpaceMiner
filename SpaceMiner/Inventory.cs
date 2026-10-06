using System;

namespace SpaceMiner
{
    public class Inventory
    {
        public int Dirt { get; private set; }
        public int Stone { get; private set; }
        public int Coal { get; private set; }
        public int Iron { get; private set; }
        public int Gold { get; private set; }
        public int Diamond { get; private set; }

        public int GetWeight()
        {
            return Dirt + Stone + Coal * 2 + Iron * 3 + Gold * 4 + Diamond * 6;
        }

        public void Add(int type)
        {
            switch (type)
            {
                case 1: Dirt++; break;
                case 2: Stone++; break;
                case 3: Coal++; break;
                case 4: Iron++; break;
                case 5: Diamond++; break;
            }
        }

        public string GetName(int slot)
        {
            switch (slot)
            {
                case 0: return "Земля";
                case 1: return "Камень";
                case 2: return "Уголь";
                case 3: return "Железо";
                case 4: return "Алмаз";
                default: return "";
            }
        }

        public int GetAmount(int slot)
        {
            switch (slot)
            {
                case 0: return Dirt;
                case 1: return Stone;
                case 2: return Coal;
                case 3: return Iron;
                case 4: return Diamond;
                default: return 0;
            }
        }
    }
}