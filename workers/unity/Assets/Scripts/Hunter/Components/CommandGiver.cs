using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Hunter.Commands;
using Improbable.Gdk.Core;
namespace MDG.Hunter.Components
{
    //Maybe command giver component is more so the mouse stuff as well.
    public struct CommandGiver : IComponentData
    {
        //Should be EntityId instead, not hard typed to only be interactable Selected, not best but best way to reference is thorugh id.
        public EntityId SelectedListener;
        public bool HasSelected;
    }
}