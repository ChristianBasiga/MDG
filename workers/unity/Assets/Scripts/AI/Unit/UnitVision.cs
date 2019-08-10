using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Interfaces;
using MDG.Game;
namespace MDG.Units
{
    public class UnitVision : MonoBehaviour
    {
        UnitState state;
        public delegate void QueryMatchedHandler(Interactable matched);
        public event QueryMatchedHandler OnQueryMatch;
        //IVisible not what I want to store.
        Dictionary<string, Interactable> seen;
        void Start()
        {
            seen = new Dictionary<string, Interactable>();
        }

        //Change sight to enum, fine for now.
        public void GetSight(List<InteractableTypes> query = null)
        {
            foreach (KeyValuePair<string, Interactable> collider in seen)
            {
                if (query == null || query.Contains(collider.Value.type))
                {
                    OnQueryMatch?.Invoke(collider.Value);
                }
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            Interactable interactable = other.GetComponent<Interactable>();

            if (interactable.AmIVisible(state))
            {
                // Next to entity ids, name of gameobjects is unique.
                // only need uniqueness with respect to client instance so game object name is fine.
                seen[other.name] = interactable; 
            }
        }
        private void OnTriggerExit(Collider other)
        {
            seen.Remove(other.name);
        }
    }
}