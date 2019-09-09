using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Jobs;
using MdgSchema.Player;
using MDG.Common.Components;
using MDG.Hunter.Components;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using Unity.Collections;
using MDG.Hunter.Commands;
using MDG.Hunter.Systems.UnitCreation;
using MdgSchema.Spawners;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Rendering;

namespace MDG.Common.Systems {

    [AlwaysUpdateSystem]
    public class GameEntityInitSystem : ComponentSystem
    {

        public struct EntityEventPayload
        {
            public GameEntityTypes typeInitialized;
            public EntityId entityId;
        }

        public delegate void OnEntityInitHandler(EntityEventPayload type);
        public event OnEntityInitHandler OnEntityInitialized;
        JobHandle jobHandle;
        CommandSystem commandSystem;
        WorkerSystem workerSystem;
        Queue<Coordinates> initialUnitCoordinates;


        // This might end up doing too much but essentially, it sets the mesh renderer as needed.

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetOrCreateSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }


        protected override void OnUpdate()
        {
            /*
            Entities.WithAll(ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(), ComponentType.ReadOnly<CommandGiver>()).ForEach(
                (ref NewlyAddedSpatialOSEntity c0) =>
                {
                    // So when spawn new hunter and spawn 3 own units, need to spawn all units spawned by previous hunters.
                    // holding static copy, honestly prob easiest way to do this.
                    // all entities to be rendered need to be stored.

                    // What could happen is every frame, go through all of these entities
                    // check if there exists in activ
                   
                    Entities.WithAll(ComponentType.ReadOnly<Position.Component>(), ComponentType.ReadOnly<Clickable>()).ForEach((ref Position.Component pos) =>
                    {
                        
                        var ent = World.Active.EntityManager.CreateEntity(typeof(RenderMesh), typeof(Unity.Transforms.LocalToWorld), typeof(Unity.Transforms.Scale),
                                   typeof(Unity.Transforms.Translation), typeof(Unity.Rendering.RenderBounds), typeof(Improbable.
                                   ));
                        World.Active.EntityManager.SetSharedComponentData(ent, new RenderMesh { mesh = CustomGameObjectCreator.mesh, material = CustomGameObjectCreator.material });
                        World.Active.EntityManager.SetComponentData(ent, new Improbable.Position.Component { Coords = pos.Coords });
                        World.Active.EntityManager.SetComponentData(ent, new Unity.Transforms.Scale { Value = 50.0f });
                    });
                });*/
        }
    }
}