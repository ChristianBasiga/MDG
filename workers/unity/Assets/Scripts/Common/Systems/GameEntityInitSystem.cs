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

namespace MDG.Common.Systems {

    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
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


        // This job is pointless as happens after creation.
        // so set up wise what I could do is have a method that queues up jobs to initialize 
        // other shit?
        public struct AddClientComponentsJob : IJobForEachWithEntity<NewlyAddedSpatialOSEntity, GameMetadata.Component, Position.Component, SpatialEntityId>
        {
            [ReadOnly]
            public NativeArray<Coordinates> initialUnitCoordinates;
            public NativeArray<int> initialUsed;

            [ReadOnly]
            public EntityCommandBuffer.Concurrent commandBuffer;

            [WriteOnly]
            public NativeArray<bool> spawnedHunter;
            public NativeArray<EntityEventPayload> initializedEntities;
            public NativeArray<int> numberOfInitializedEntities;
            public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, [ReadOnly] ref NewlyAddedSpatialOSEntity c0,
                [ReadOnly] ref GameMetadata.Component gameMetaData,
                ref Position.Component position, [ReadOnly] ref SpatialEntityId spatialEntityId)
            {

                initializedEntities[numberOfInitializedEntities[0]++] = new EntityEventPayload
                {
                    entityId = spatialEntityId.EntityId,
                    typeInitialized = gameMetaData.Type
                };
                switch (gameMetaData.Type)
                {
                    //Want to only 
                    //Pass it in to factory and add that alte ron.
                    //Scrapped, well partyly mainly just gong to be moved
                    // Which basically means that this is fine.
                    // the reason couldn't move is because scenes were different so not finding correct one
                    // It's like I just understood it. So this si perfect as is.
                    // but entity is same period.
                    case GameEntityTypes.Hunter:
                        commandBuffer.AddComponent(index, entity, new CommandGiver());
                        commandBuffer.AddComponent(index, entity, new MouseInputComponent());
                        spawnedHunter[0] = true;
                        break;
                    case GameEntityTypes.Unit:
                        //commandBuffer.AddComponent(index, entity, new CommandListener());
                        commandBuffer.AddComponent(index, entity, new Clickable());
                        //commandBuffer.AddComponent(index, entity, new CommandMetadata {CommandType =  CommandType.None});
                        //commandBuffer.AddComponent(index, entity, new InitialPosition());
                        if (initialUnitCoordinates.Length > 0 && initialUsed[0] < initialUnitCoordinates.Length)
                        {
                            position.Coords = initialUnitCoordinates[initialUsed[0]++];
                        }
                        break;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetOrCreateSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            initialUnitCoordinates = new Queue<Coordinates>();

            // Never thoguht this was good way, but for sure not good way now.
            // perhaps though init job is a function o call so that can set the data accordingly.
            // for position, since again same world same system, not differing instance.

            initialUnitCoordinates.Enqueue(new Coordinates { X = 1.0f, Z = 1.0f });
         //   initialUnitCoordinates.Enqueue(new Coordinates { X = 1.0f, Z = -2.0f });
          //  initialUnitCoordinates.Enqueue(new Coordinates { X = -2.0f, Z = 1.0f });
          //  initialUnitCoordinates.Enqueue(new Coordinates { X = 5.0f, Z = 5.0f });
        }


        protected override void OnUpdate()
        {

            /*

            NativeArray<EntityEventPayload> initializedEntities = new NativeArray<EntityEventPayload>(100, Allocator.TempJob);
            NativeArray<int> numberOfInitializedEntities = new NativeArray<int>(1, Allocator.TempJob);

            EntityCommandBuffer.Concurrent concurrent = PostUpdateCommands.ToConcurrent();
            NativeArray<bool> spawnedHunter = new NativeArray<bool>(1, Allocator.TempJob);

            NativeArray<Coordinates> initialCoords = new NativeArray<Coordinates>(initialUnitCoordinates.Count, Allocator.TempJob);
            initialCoords.CopyFrom(initialUnitCoordinates.ToArray());
            NativeArray<int> coordsUsed = new NativeArray<int>(1, Allocator.TempJob);
            coordsUsed[0] = 0;
            spawnedHunter[0] = false;

            AddClientComponentsJob addClientComponentsJob = new AddClientComponentsJob
            {
                initializedEntities = initializedEntities,
                numberOfInitializedEntities = numberOfInitializedEntities,
                commandBuffer = concurrent,
                spawnedHunter = spawnedHunter,
                initialUsed = coordsUsed,
                initialUnitCoordinates = initialCoords
            };

          //  jobHandle = addClientComponentsJob.Schedule(this);
          //  jobHandle.Complete();

            //Clear coords used so far.
            for (int i = 0; i < coordsUsed[0]; ++i)
            {
                initialUnitCoordinates.Dequeue();
            }

            for (int i = 0; i < numberOfInitializedEntities[0]; ++i)
            {
                //OnEntityInitialized?.Invoke(initializedEntities[i]);
            }

            numberOfInitializedEntities.Dispose();
            initializedEntities.Dispose();
            coordsUsed.Dispose();
            initialCoords.Dispose();

            bool doSpawnUnits = spawnedHunter[0];
            spawnedHunter.Dispose();
            //Then new Hunter has joined game and needs a UnitSpawner in client side.
            if (doSpawnUnits)
            {
                //Add Entity with component
                //Change UnitSpawner to be normal instead of adding in snapshot.
                WorkerSystem workerSystem = World.GetExistingSystem<WorkerSystem>();
                Entity entity = PostUpdateCommands.CreateEntity(EntityManager.CreateArchetype(
                    typeof(UnitSpawner.Component)
                ));
                PostUpdateCommands.SetComponent(entity, new UnitSpawner.Component {
                    AmountToSpawn = 1,
                });
            }
            */
        }
    }
}