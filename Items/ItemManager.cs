using Ostranauts.Bit.Items.Categories;

namespace Ostranauts.Bit.Items
{
    public class ItemManager
    {
        private ItemCategoryManager _itemCategoryManager;

        /// <summary>
        /// Item category manager instance
        /// </summary>
        public ItemCategoryManager Categories
        {
            get { return _itemCategoryManager; }
        }

        public ItemManager() {
            // Initialize Categories
            _itemCategoryManager = new ItemCategoryManager();
        }
    }
}
