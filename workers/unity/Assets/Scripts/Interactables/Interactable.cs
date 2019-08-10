using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Interfaces;
namespace MDG.Game
{

    public enum InteractableTypes
    {
        Enemy,
        Resourcee
    }
    public class Interactable : MonoBehaviour
    {
        public IVisible visibleSettings;
        public InteractableTypes type;
        //Will inject this through zenject.
        public virtual bool AmIVisible(States.State state);
    }
}