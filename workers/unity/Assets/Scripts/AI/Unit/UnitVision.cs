using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Interfaces;
using MDG.Common;
using System.Linq;
namespace MDG.Units
{
    public class UnitVision : MonoBehaviour
    {
        /*

        UnitState state;
        Dictionary<string, Interactable> seen;

        //Unity State component can get hooks to this.
        public delegate void VisionChangeHandler(Interactable see);
        public event VisionChangeHandler OnGetVision;
        public event VisionChangeHandler OnLoseVision;
        void Start()
        {
            seen = new Dictionary<string, Interactable>();
        }
        // Invoked from Behaviour tree.
        public List<Interactable> GetVision(InteractableTypes query = InteractableTypes.None)
        {
            if (query == InteractableTypes.None)
            {
                return seen.Values.ToList();
            }
            return seen.Values.Where((Interactable i) => i.interactableType == query) as List<Interactable>;
        }

        public bool HasVision(InteractableTypes query = InteractableTypes.None)
        {
            if (query == InteractableTypes.None)
            {
                return seen.Count > 0;
            }
            return seen.Values.Any((Interactable interactable) => interactable.interactableType == query);
        }

        private void OnTriggerEnter(Collider other)
        {
            Interactable interactable = other.GetComponent<Interactable>();
            if (interactable && state.CanSee(interactable))
            {
                OnGetVision?.Invoke(interactable);
                seen[other.name] = interactable; 
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (seen.ContainsKey(other.name)) {
                OnLoseVision?.Invoke(seen[other.name]);
                seen.Remove(other.name);
            }
        }*/
    }
}