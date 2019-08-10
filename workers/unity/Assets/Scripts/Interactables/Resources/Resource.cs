using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Units;
using MDG.Interfaces;
namespace MDG.Game.Resources
{
    public class Resource : MonoBehaviour, IVisible
    {

        public bool isVisible(UnitModel unit)
        {
            //Each resource will mention if visible to this Unit
            return true;
        }

        
    }
}