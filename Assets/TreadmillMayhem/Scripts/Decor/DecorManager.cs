using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Shows the decorations the player owns. Children are named "Decor_&lt;id&gt;".</summary>
    public class DecorManager : MonoBehaviour
    {
        void Start() => Refresh();

        public void Refresh()
        {
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Decor_")) continue;
                string id = child.name.Substring("Decor_".Length);
                child.gameObject.SetActive(DecorCatalog.Visible(id));
            }
        }
    }
}
