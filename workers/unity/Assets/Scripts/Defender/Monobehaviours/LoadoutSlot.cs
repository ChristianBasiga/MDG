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
        Text costText;


        public ScriptableStructures.Trap Trap { private set; get; }
        public ScriptableWeapons.Weapon Weapon { private set; get; }

        public enum SlotOptions
        {
            Weapon,
            Trap
        };


        public bool Selected { private set; get; }
        public SlotOptions SlotType { private set; get; }
        public void SetItem(ScriptableStructures.Trap trap)
        {
            this.Trap = trap;
            this.Weapon = null;
            itemImage.gameObject.SetActive(true);
            costText.gameObject.SetActive(true);
            costText.text = trap.Cost.ToString();
            itemImage.sprite = trap.Thumbnail;
            SlotType = SlotOptions.Trap;
        }

        // Overload for weapons.
        public void SetItem(ScriptableWeapons.Weapon weapon)
        {
            this.Weapon = weapon;
            this.Trap = null;
            costText.gameObject.SetActive(false);
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = weapon.ArtWork;
            SlotType = SlotOptions.Weapon;
        }

        public void RemoveItem()
        {
            itemImage.gameObject.SetActive(false);
        }

        public void Toggle(bool selected)
        {
            selectedIndicator.gameObject.SetActive(selected);
            Selected = true;
        }

        private void Awake()
        {
            itemImage = transform.Find("ItemImage").GetComponent<Image>();
            selectedIndicator = transform.Find("SelectedIndicator").GetComponent<Image>();
            costText = transform.Find("CostText").GetComponent<Text>();
            costText.gameObject.SetActive(false);
        }

        private void Start()
        {
            selectedIndicator.gameObject.SetActive(false);
        }
    }
}
