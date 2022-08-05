using Life.Network;
using Life;
using Life.InventorySystem;
using System.Collections.Generic;
using PoliceUtils.Utils;

namespace PoliceUtils
{
    class BasePolice
    {

        internal PolicePlugin plugin;
        internal LifeServer server;

        public virtual void Init(PolicePlugin plugin, LifeServer server)
        {
        }

    }
}
