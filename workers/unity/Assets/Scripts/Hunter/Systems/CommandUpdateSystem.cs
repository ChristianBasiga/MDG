using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;
using UnityEngine.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using MDG.Hunter.Monobehaviours;
using System.Linq;
using MDG.Common.Systems;
using MDG.Logging;
using MdgSchema.Common;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Mathematics;

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

        public struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityTransform.Component>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref MoveCommand moveCommand, [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);
                float distance = math.distance(moveCommand.destination, pos);
                const float minDistance = 1.0f;
                if (distance < minDistance)
                {
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
                else
                {
                    Vector3f destinationVector = new Vector3f(moveCommand.destination.x, moveCommand.destination.y, moveCommand.destination.z);
                    // Todo: apply units speed, from also including stat component in the filer.
                    entityTransform.Position = entityTransform.Position + (destinationVector - entityTransform.Position) * deltaTime;
                }
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
            MoveCommandJob moveCommandJob = new MoveCommandJob
            {
                deltaTime = Time.deltaTime,
                // Need to be able to get post update commands in client world in job component system.
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            moveCommandJob.Schedule(this).Complete();

            //Receives events for collision.
            /*
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
                
            }*/
        }
    }
}