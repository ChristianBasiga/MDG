using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine.AI;
namespace MDG.Invader.Commands
{
    //Perhaps Commands should be Components, can add and remove components from Entity at run time dude.
    //AKA perfect for these. I'm retarded.
   
    public enum CommandType
    {
        None,
        Move,
        Collect,
        Attack
    }

}