using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Improbable.Gdk.Core;
using MDG.Hunter.Monobehaviours;
using System.Linq;
using MDG.Common.Systems;
using MDG.Logging;
using MdgSchema.Common;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;

namespace MDG.Hunter.Systems
{
    /// <summary>
    ///  This will be flow, swtich on meta data of command listener.
    ///  Based on switch, it will key to the command monobehaviour.
    ///  the monobehaviour will hold tasks for Behaviour tree, each command is more of a single task rather than a tree.
    ///  But I suppose the process and acting accorindgly could become a behaviour tree.
    /// </summary>
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class CommandUpdateSystem : ComponentSystem
    {
        private bool assignedJobHandle = false;
        private ComponentUpdateSystem componentUpdateSystem;
        private WorkerSystem workerSystem;
        private Dictionary<EntityId, List<EntityId>> unitCollisionMappings;
        private EntityQuery enemyQuery;
        private EntityQuery friendlyQuery;
        public JobHandle CommandExecuteJobHandle { get; private set; }
        //Component Type
        //Perhaps should rename to meta data or payload.
        public delegate void CommandUpdateEventHandler(EntityId commandListener, System.Type type, CommandListener commandPayload);
        //Each unit should be abel to get GameObject attached to via SpatialEntityId
        public static event CommandUpdateEventHandler OnCommandExecute;
        public static event CommandUpdateEventHandler OnCommandTerminated;
        public struct CommandExecutionJob : IJobForEach<CommandListener, SpatialEntityId>
        {
            //re add changed filer later after move collision code to own system.
            public void Execute( ref CommandListener commandListener, [ReadOnly] ref SpatialEntityId spatialEntityId)
            {
               
                if (commandListener.CommandType == CommandType.None) return;

                // I could make this more scalable through reflection like I did with encounters in DF mod
                // create mapping of Command Type to type, reduce overhead of getting type.
                switch (commandListener.CommandType)
                {
                    case CommandType.Collect:
                        OnCommandExecute?.Invoke(spatialEntityId.EntityId, typeof(CollectBehaviour), commandListener);
                        break;
                    case CommandType.Move:
                        OnCommandExecute?.Invoke(spatialEntityId.EntityId, typeof(MoveBehaviour), commandListener);
                        break;
                    case CommandType.Attack:
                        OnCommandExecute?.Invoke(spatialEntityId.EntityId, typeof(AttackBehaviour), commandListener);
                        break;
                }
                commandListener.CommandType = CommandType.None;
            }
        }


        public struct CommandInterruptionJob : IJobForEach<EnemyComponent, SpatialEntityId>
        {
            [ReadOnly]
            Entity entityInterrupting;
            public void Execute(ref EnemyComponent c0, ref SpatialEntityId c1)
            {
                throw new System.NotImplementedException();
            }
        }

        //Gets subset of unit collisions of just enemies.
        public struct GetCollidedEnemies : IJobForEachWithEntity<SpatialEntityId, EnemyComponent>
        {
            [ReadOnly]
            public NativeArray<Entity> collidedAllUnits;
            public NativeArray<Entity> collidedEnemies;
            public void Execute(Entity entity, int index, ref SpatialEntityId c0, ref EnemyComponent c1)
            {
                //... Reading through collidedAllUnits should be fine, but might not be.
                // I could turn this to ComponentSystem and make my life easier, I could still do jobs in component system
            }
        }

        public struct HandleCollisionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> entities;
            [ReadOnly]
            public EntityCommandBuffer.Concurrent commandBuffer;
            public void Execute(int index)
            {
                Entity entity = entities[index];
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            unitCollisionMappings = new Dictionary<EntityId, List<EntityId>>();
            enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
            friendlyQuery = GetEntityQuery(ComponentType.ReadOnly<CommandListener>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
        }

        protected override void OnUpdate()
        {
            CommandExecutionJob commandExecutionJob = new CommandExecutionJob();
            CommandExecuteJobHandle = commandExecutionJob.Schedule(this);
            CommandExecuteJobHandle.Complete();
            //Receives events for collision.
            var eventPayloads = componentUpdateSystem.GetEventsReceived<EntityCollider.OnCollision.Event>();
            for (int i = 0; i < eventPayloads.Count; ++i)
            {
                ref readonly var eventPayload = ref eventPayloads[i];
                // We only care about Units. But others can get events received in other systems upon the same worker.
                // Insead of this check could include authortitive version of a component in query, but this works.
                if (eventPayload.Event.Payload.TypeOfEntity != GameEntityTypes.Unit ||  !PlayerLifecycleHelper.IsOwningWorker(eventPayload.EntityId, workerSystem.World))
                {
                    continue;
                }
                unitCollisionMappings[eventPayload.EntityId] = eventPayload.Event.Payload.CollidedWith;
            }

            if (unitCollisionMappings.Count > 0)
            {
                foreach (EntityId collidee in unitCollisionMappings.Keys)
                {
                    Debug.LogError($"Lookg at collisions of Entity {collidee.Id}");

                    Entity collideeEntity;
                    if (workerSystem.TryGetEntity(collidee, out collideeEntity))
                    {
                      
                        int numberOfEnemies = 0;
                        int numberOfFriendlies = 0;
                        EntityId nearestAlly = new EntityId(-1);
                        EntityId nearestEnemy = new EntityId(-1);

                        Vector3 unitPos = EntityManager.GetComponentData<Position.Component>(collideeEntity).Coords.ToUnityVector();
                        Vector3 nearestAllyPos = new Vector3(Mathf.Infinity, Mathf.Infinity);
                        Vector3 nearestEnemyPos = new Vector3(Mathf.Infinity, Mathf.Infinity);

                        float nearestEnemyDistance = Mathf.Infinity;
                        float nearestAllyDistance = Mathf.Infinity;
                        // Get all enemies and all friendlies 

                        //Similiar logic but more efficent and end result will differ later on.
                        Entities.With(enemyQuery).ForEach((ref SpatialEntityId id, ref Position.Component position) =>
                        {
                            if (unitCollisionMappings[collidee].Contains(id.EntityId))
                            {
                                numberOfEnemies += 1;
                                Debug.LogError("With enemy");

                                //Do based on distance later.
                                float distanceToEnemy = Vector3.Distance(unitPos, position.Coords.ToUnityVector());
                                if (distanceToEnemy < nearestEnemyDistance)
                                {
                                    nearestEnemy = id.EntityId;
                                    nearestEnemyDistance = distanceToEnemy;
                                    nearestEnemyPos = position.Coords.ToUnityVector();
                                }
                            }
                        });

                        Entities.With(friendlyQuery).ForEach((ref SpatialEntityId id, ref Position.Component position) =>
                        {
                            if (unitCollisionMappings[collidee].Contains(id.EntityId))
                            {
                               Debug.LogError("With friendly");
                                numberOfFriendlies += 1;
                                float distanceToAlly = Vector3.Distance(unitPos, position.Coords.ToUnityVector());
                                if (distanceToAlly < nearestEnemyDistance)
                                {
                                    nearestAlly = id.EntityId;
                                    nearestAllyDistance = distanceToAlly;
                                    nearestAllyPos = position.Coords.ToUnityVector();
                                }
                            }
                        });
                        // Update command accordingly. I could implmenet an attack but just logging cause lol.
                        // Then for targetid, workers will jsut get entity and get position from that. until command revoked.
                        // This sets it for next frame, which will cause auto destruction of previous command.

                        //Instead of sending as postupdate might actually have to update accordingl.
                        //Later try set component via EntityManager instead of Post, as post may end up being multiple frames down the road
                        //but I need it now for command update to work with [ChangedFilter]
                        if (numberOfFriendlies > numberOfEnemies && numberOfEnemies > 0)
                        { 
                            Debug.LogError("Collding with friends more han enemies");

                            Debug.LogError(nearestEnemyPos);
                            PostUpdateCommands.SetComponent(collideeEntity, new CommandListener { CommandType = CommandType.Attack, TargetId = nearestEnemy, TargetPosition = nearestEnemyPos });

                        }
                        else if (numberOfEnemies > numberOfFriendlies && numberOfFriendlies > 0)
                        {
                            Debug.LogError("COlliding with enemy  more than friends");
                            Debug.LogError(nearestAllyPos);
                            PostUpdateCommands.SetComponent(collideeEntity, new CommandListener { CommandType = CommandType.Move, TargetId = nearestAlly, TargetPosition = nearestAllyPos });

                        }
                        else if (numberOfEnemies > 0)
                        {
                            Debug.LogError(nearestEnemyPos);
                            PostUpdateCommands.SetComponent(collideeEntity, new CommandListener { CommandType = CommandType.Attack, TargetId = nearestEnemy, TargetPosition = nearestEnemyPos });
                        }
                    }
                }
            }
        }
    }
}