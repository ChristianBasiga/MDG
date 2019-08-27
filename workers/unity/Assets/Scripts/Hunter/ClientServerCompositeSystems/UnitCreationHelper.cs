using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
namespace MDG.Hunter.Systems.UnitCreation
{
    public class UnitCreationHelper
    {
        public static void AddClientSystems(World world)
        {
            world.GetOrCreateSystem<UnitCreationRequestSystem>();
        }

        public static void AddServerSystems(World world)
        {
            world.GetOrCreateSystem<UnitCreationSystem>();
        }
    }
}