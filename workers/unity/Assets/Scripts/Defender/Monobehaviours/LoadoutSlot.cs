using UnityEngine;
using UnityEngine.UI;
using ScriptableStructures = MDG.ScriptableObjects.Structures;
using ScriptableWeapons = MDG.ScriptableObjects.Weapons;

namespace MDG.Defender.Monobehaviours
{
    public class LoadoutSlot : MonoBehaviour
    {
        // Fields set in editor.
        [SerializeField]
        Image selectedIndicator;
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
            selectedIndicator.gameObject.SetActive(selected);
        }

        private void Awake()
        {
            itemImage = transform.Find("ItemImage").GetComponent<Image>();
            selectedIndicator = transform.Find("SelectedIndicator").GetComponent<Image>();
        }
    }
}
