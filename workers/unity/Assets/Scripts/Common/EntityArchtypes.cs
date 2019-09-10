using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using MDG.Common.Components;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;

namespace MDG.Common
{
    public enum EntityArchtypesEnum
    {
        HUNTER,
        UNIT
    }
    //Will maintainDictionry of archtypes, honestly this could just be for all.
    /// <summary>
    /// Maintains dictionary of entity archtypes.
    /// And contains methods for creating entities.
    /// </summary>
    public class EntityArchtypes
    {
        //May initialize this via common installer, but either way moving that code along.
        readonly Dictionary<EntityArchtypesEnum, EntityArchetype> archTypes;

        public EntityArchtypes()
        {
            archTypes = new Dictionary<EntityArchtypesEnum, EntityArchetype>();
            /*
            archTypes[EntityArchtypesEnum.HUNTER] = World.Active.EntityManager.CreateArchetype(
                typeof(MouseInputComponent),
                typeof(CommandGiver)
            );

            archTypes[EntityArchtypesEnum.UNIT] = World.Active.EntityManager.CreateArchetype(
                typeof(UnitComponent),
                typeof(CommandListener));*/
        }
    
        //Convert these to EntityTemplates with SpatialOS as needed or combination of both.
        public static System.Type[] GetHunterArchtype()
        {
            /* return World.Active.EntityManager.CreateArchetype(
                 typeof(MouseInputComponent),
                 typeof(CommandGiver),
                 typeof(Metadata)
             );*/

            return new System.Type[1]
            {
                typeof(CommandGiver)
            };
        }

        //FOr arch time, will instead 
        public static System.Type[] GetUnitArchtype()
        {
            System.Type[] components = new System.Type[4]
            {
                typeof(UnitComponent),
                typeof(CommandListener),
                typeof(Clickable),
                typeof(CommandMetadata)
            };

            return components;
        }

        public static EntityArchetype GetResourceArchtype()
        {
            return World.Active.EntityManager.CreateArchetype(
                typeof(Clickable));
        }



    }
}