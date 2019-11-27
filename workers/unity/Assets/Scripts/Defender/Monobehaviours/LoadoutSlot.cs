using UnityEngine;
using UnityEngine.UI;
using ScriptableStructures = MDG.ScriptableObjects.Structures;
using ScriptableWeapons = MDG.ScriptableObjects.Weapons;

namespace MDG.Defender.Monobehaviours
{
    public class LoadoutSlot : MonoBehaviour
    {

        Image selectedIndicator;
        Image defaultBackground;
        Image currentBackground;
        Image itemImage;


        public bool Selected { private set; get; }
       
        public void SetItem(ScriptableStructures.Trap trap)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = trap.Thumbnail;
        }

        // Overload for weapons.
        public void SetItem(ScriptableWeapons.Weapon weapon)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = weapon.ArtWork;
        }

        public void RemoveItem()
        {
            itemImage.gameObject.SetActive(false);
        }

        public void Toggle(bool selected)
        {
            Selected = selected;
            if (selected)
            {
                currentBackground.sprite = selectedIndicator.sprite;
            }
            else
            {
                currentBackground.sprite = currentBackground.sprite;
            }
        }
    }
}
