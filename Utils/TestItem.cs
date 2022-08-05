using Life.CharacterSystem;
using Life.InventorySystem;
using UnityEngine;

namespace PoliceUtils.Utils
{
    [CreateAssetMenu(fileName = "New Test Item", menuName  = "Life/Test")]
    internal class TestItem : Item
    {
        public TestItem()
        {
            this.itemName = "Test item";
            this.slug = "Test";
            Debug.Log("Test item there");
        }
    }
}
