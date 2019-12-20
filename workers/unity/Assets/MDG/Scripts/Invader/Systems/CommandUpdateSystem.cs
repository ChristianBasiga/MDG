using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Mathematics;
using MdgSchema.Game.Resource;
using MDG.Invader.Components;
using MDG.Common.Systems;
using MDG.Common.Components;
using MDG.Logging;
using PositionSchema = MdgSchema.Common.Position;
using CommonJobs = MDG.Common.Jobs;
using CollisionSchema = MdgSchema.Common.Collision;
using SpawnSchema = MdgSchema.Common.Spawn;
using StructureSchema = MdgSchema.Common.Structure;

using PointSchema = MdgSchema.Common.Point;
using MDG.Common.Systems.Point;
using Improbable.Gdk.Subscriptions;
using MDG.Common.Systems.Position;
using MDG.Common.Datastructures;
using MDG.Common;
using MDG.Common.Systems.Spawn;
using MDG.DTO;

using MDG.ScriptableObjects.Weapons;
using MDG.Common.Systems.Structure;
using MdgSchema.Common.Util;
using StatSchema = MdgSchema.Common.Stats;
using Unity.Burst;
using MdgSchema.Common.Spawn;

// DEFINITELY CAN SEPERATE THIS INTO THREE DIFFERENT CLASSES.
// OR AT THE VERY LEAST MOVE THE RESPECTIVE JOBS IN OWN CLASSES.
namespace MDG.Invader.Systems
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
        struct CollectPayload
        {
            public EntityId resourceId;
            public EntityId requestingOccupant;
        }

        struct AttackPayload
        {
            public EntityId attackerId;
            public EntityId attackeeId;
            public Vector3f startingPosition;
            public Vector3f positionToAttack;
        }

        struct PendingAttack
        {
            public AttackPayload attackPayload;
            public JobHandle jobHandle;
            public NativeQueue<CommonJobs.ClientJobs.RaycastHit> InLineOfSight;
        }



        private EntityQuery collectorGroup;


        private JobHandle collectJobHandle;
        private NativeQueue<CollectPayload> pendingOccupy;
        private NativeQueue<CollectPayload> pendingCollects;
        private NativeQueue<CollectPayload> pendingReleases;

        Dictionary<long, BuildCommand> requestIdToBuildCommand;

        private NativeHashMap<EntityId, BuildCommand> buildCommands;

        private PositionSystem positionSystem;
        private CommandSystem commandSystem;
        private SpawnRequestSystem spawnRequestSystem;
        private ResourceRequestSystem resourceRequestSystem;
        private ComponentUpdateSystem componentUpdateSystem;
        private WorkerSystem workerSystem;
        private ComponentType[] authVelocityGroup;

        private EntityQuery combatStatsQuery;
        private EntityQuery commandListenerResetQuery;
        private EntityQuery interruptedGroup;
        private EntityQuery enemyQuery;
        private EntityQuery friendlyQuery;
        private EntityQuery attackQuery;
        private EntityQuery respawningEnemyQuery;

        ClientGameObjectCreator clientGameObjectCreator;
        // Change this to dictionary so that weapons of units more expandible.
        Weapon unitWeapon;


        /// <summary>
        /// Combine these move jobs into single job that switches on command listener.
        /// Also can avoid altering archtype and overhead of that, not sure how expensive that is though.
        /// but maintainign one archtype better than maintaining three that are mostly the same.
        /// Would switch twice, one twitch for determining how get velocity, but would mean
        /// job expect occupy payloads and attackee positions even if not needed. Like cmonn take advanage of data orientation my guy
        /// even if ignores rule of small
        /// </summary>

        struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityPosition.Component, PositionSchema.LinearVelocity.Component,
            CollisionSchema.BoxCollider.Component, CommandListener>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public void Execute(Entity entity, int jobIndex, ref MoveCommand moveCommand, [ReadOnly] ref EntityPosition.Component EntityPosition,
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                ref CommandListener commandListener)
            {

                Vector3f sameY = new Vector3f(moveCommand.destination.X, EntityPosition.Position.Y, moveCommand.destination.Z);
                Vector3f direction = HelperFunctions.Subtract(sameY,EntityPosition.Position);
                float distance = HelperFunctions.Magnitude(direction);
                if (!moveCommand.applied)
                {
                    linearVelocityComponent.Velocity = direction;
                    moveCommand.applied = true;
                }
                else if (distance <= HelperFunctions.Magnitude(boxCollider.Dimensions))
                {
                    linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                    commandListener.CommandType = CommandType.None;
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
            }
        }

        struct MoveToResourceJob : IJobForEachWithEntity<SpatialEntityId, CollectCommand, EntityPosition.Component, 
            PositionSchema.LinearVelocity.Component, CollisionSchema.BoxCollider.Component, CommandListener>
        {
            [WriteOnly]
            public NativeQueue<CollectPayload>.ParallelWriter occupyPayloads;

            public void Execute(Entity entity, int jobIndex, ref SpatialEntityId spatialEntityId, ref CollectCommand collectCommand, 
                ref EntityPosition.Component EntityPosition, ref PositionSchema.LinearVelocity.Component linearVelocityComponent,
                [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider, ref CommandListener commandListener)
            {
                Vector3f sameY = new Vector3f(collectCommand.destination.X, EntityPosition.Position.Y, collectCommand.destination.Z);
                Vector3f direction = HelperFunctions.Subtract(sameY, EntityPosition.Position);
                float distance = HelperFunctions.Magnitude(direction);

                // Just magnitude prob fine, if all uniform, they won't be uniform though.
                float minDistance = HelperFunctions.Magnitude(boxCollider.Dimensions);
                // When should I mark IsAtResource
                if (!collectCommand.IsCollecting && !collectCommand.IsAtResource)
                {
                    if (distance < minDistance)
                    {
                        collectCommand.IsAtResource = true;
                        linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                    }
                    else
                    {
                        linearVelocityComponent.Velocity = direction;
                    }
                }
                else
                {
                    collectCommand.IsCollecting = true;
                    // Otherwise we can begin collecting.
                    occupyPayloads.Enqueue(new CollectPayload { requestingOccupant = spatialEntityId.EntityId, resourceId = collectCommand.resourceId });
                }
            }
        }


        // Also need attack meta data for specifying attack range. Add later.
        #region Attack Command Jobs

        struct GetTargetPositionsJob : IJobForEach<SpatialEntityId, Enemy, EntityPosition.Component>
        {
            public NativeHashMap<EntityId, Vector3f>.ParallelWriter attackeePositions;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Enemy enemy, 
                [ReadOnly] ref EntityPosition.Component EntityPosition)
            {
                // Maybe add clickable and clicked.
                if (attackeePositions.TryAdd(spatialEntityId.EntityId, EntityPosition.Position))
                {
                }
            }
        }


        struct GetKilledEnemiesJob : IJobForEach<SpatialEntityId, Enemy, StatSchema.Stats.Component>
        {

            public NativeHashMap<EntityId, bool>.ParallelWriter deadEnemies;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Enemy c1, [ReadOnly] [ChangedFilter] ref StatSchema.Stats.Component c2)
            {
                Debug.Log($"looking at entity id {spatialEntityId.EntityId} with health {c2.Health}");
                if (c2.Health == 0)
                {
                    deadEnemies.TryAdd(spatialEntityId.EntityId, true);
                }
            }
        }

        // So what I want to do is actually act upon the AttackPayload stuff.
        // Only those entities do I want to check if line of sight, since that is when they are prepping to attack.
        // Let default reroute collision syttem handle it for most part.
        struct MoveToAttackTargetJob : IJobForEachWithEntity<SpatialEntityId, AttackCommand, PositionSchema.LinearVelocity.Component,
            EntityPosition.Component, CombatStats>
        {

            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            [ReadOnly]
            public NativeHashMap<EntityId, Vector3f> attackerToAttackeePosition;

            [ReadOnly]
            public NativeHashMap<EntityId, bool> killedEnemies;


            public NativeQueue<AttackPayload>.ParallelWriter attackPayloads;

            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId spatialEntityId, ref AttackCommand attackCommand, 
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent, [ReadOnly] ref EntityPosition.Component EntityPosition, 
                [ReadOnly] ref CombatStats combatStats)
            {
                Debug.Log("killed enemies length " + killedEnemies.Length);
                if (killedEnemies.TryGetValue(attackCommand.target, out _))
                {   
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(AttackCommand));
                }
                else if (attackerToAttackeePosition.TryGetValue(attackCommand.target, out Vector3f targetPosition))
                {
                    Vector3f sameY = new Vector3f(targetPosition.X, EntityPosition.Position.Y, targetPosition.Z);
                    Vector3f direction = HelperFunctions.Subtract(sameY, EntityPosition.Position);
                    float distance = HelperFunctions.Magnitude(direction);
                    if (distance <= combatStats.attackRange)
                    {
                        if (combatStats.attackCooldown == 0)
                        {
                            attackPayloads.Enqueue(new AttackPayload
                            {
                                attackerId = spatialEntityId.EntityId,
                                positionToAttack = targetPosition,
                                startingPosition = EntityPosition.Position,
                                attackeeId = attackCommand.target
                            });
                            attackCommand.attacking = true;
                        }
                        linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                    }
                    else
                    {
                        // If no longer in range, stop attacking, start following again.
                        attackCommand.attacking = false;
                        linearVelocityComponent.Velocity = direction;
                    }
                }
                else
                {
                    // If not in that dictionary, then has to be dead, remove attack command.
                    entityCommandBuffer.RemoveComponent<AttackCommand>(jobIndex, entity);
                    linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                }
            }
        }
        #endregion

        #region Build Command Jobs

        struct MoveToBuildLocationJob : IJobForEachWithEntity<SpatialEntityId, BuildCommand, PositionSchema.LinearVelocity.Component,
            EntityPosition.Component>
        {
            public NativeHashMap<EntityId, BuildCommand>.ParallelWriter entitiesBuilding;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId spatialEntityId, ref BuildCommand buildCommand, ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
            [ReadOnly] ref EntityPosition.Component EntityPositionComponent )
            {
                float distance = HelperFunctions.Distance(buildCommand.buildLocation, EntityPositionComponent.Position);

                if (buildCommand.isBuilding && buildCommand.structureId.IsValid())
                {
                    // If building add to queue for sending requests.
                    entitiesBuilding.TryAdd(spatialEntityId.EntityId, buildCommand);
                }
                else if (distance <= buildCommand.minDistanceToBuild)
                {
                    if (!buildCommand.structureId.IsValid() && !buildCommand.isBuilding)
                    {
                        buildCommand.isBuilding = true;
                        buildCommand.builderId = spatialEntityId.EntityId;
                        entitiesBuilding.TryAdd(spatialEntityId.EntityId, buildCommand);
                        linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                    }
                }
                else
                {
                    Vector3f normalizedDirection = HelperFunctions.Normalize(HelperFunctions.Subtract(buildCommand.buildLocation,EntityPositionComponent.Position));

                    linearVelocityComponent.Velocity = normalizedDirection;
                    buildCommand.isBuilding = false;
                }
            }
        }

        struct UpdateBuildCommandJob : IJobForEach<SpatialEntityId, BuildCommand>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, BuildCommand> pendingUpdates;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, ref BuildCommand buildCommand)
            {
                if (pendingUpdates.TryGetValue(spatialEntityId.EntityId, out BuildCommand newBuildCommand))
                {
                    buildCommand.structureId = newBuildCommand.structureId; 
                }
            }
        }
        #endregion

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            clientGameObjectCreator = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().ClientGameObjectCreator;
            unitWeapon = Resources.Load("ScriptableObjects/Weapons/UnitWorkerProjectile") as Weapon;
            combatStatsQuery = GetEntityQuery(
               ComponentType.ReadOnly<CombatMetadata>(),
               ComponentType.ReadWrite<CombatStats>()
               );
            attackQuery = GetEntityQuery(
                ComponentType.ReadWrite<AttackCommand>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<EntityPosition.Component>(),
                ComponentType.ReadOnly<CombatStats>(),
                ComponentType.Exclude<RerouteComponent>()
                );
            attackQuery.SetFilter(PositionSchema.LinearVelocity.ComponentAuthority.Authoritative);

            enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Enemy>(),
                ComponentType.ReadOnly<EntityPosition.Component>()
                );
            authVelocityGroup = new ComponentType[7]
            {
                ComponentType.ReadWrite<CommandListener>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>(),
                ComponentType.ReadOnly<EntityPosition.Component>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                null,
                null
            };

            respawningEnemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Enemy>(),
                ComponentType.ReadOnly<PendingRespawn.Component>()
                );

            pendingCollects = new NativeQueue<CollectPayload>(Allocator.Persistent);
            pendingOccupy = new NativeQueue<CollectPayload>(Allocator.Persistent);

            buildCommands = new NativeHashMap<EntityId, BuildCommand>(1000, Allocator.Persistent);
            requestIdToBuildCommand = new Dictionary<long, BuildCommand>();


            spawnRequestSystem = World.GetExistingSystem<SpawnRequestSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            positionSystem = World.GetExistingSystem<PositionSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            resourceRequestSystem = World.GetExistingSystem<ResourceRequestSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();

            interruptedGroup = GetEntityQuery(ComponentType.ReadOnly<CommandInterrupt>(), ComponentType.ReadOnly<SpatialEntityId>());
           // enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
            friendlyQuery = GetEntityQuery(ComponentType.ReadOnly<CommandListener>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (pendingCollects.IsCreated)
            {
                pendingCollects.Dispose();
                pendingOccupy.Dispose();
                buildCommands.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            RunCommandJobs();
            ProcessInterruptedCommands();
        }
        

        // Don't v
        private void RunCommandJobs()
        {
            float deltaTime = Time.deltaTime;
            #region Attack Command Jobs
            CommonJobs.ClientJobs.TickAttackCooldownJob tickAttackCooldownJob = new CommonJobs.ClientJobs.TickAttackCooldownJob
            {
                deltaTime = deltaTime
            };
            JobHandle tickAttackCoolDownHandle = tickAttackCooldownJob.Schedule(this);

            NativeHashMap<EntityId, Vector3f> attackeePositions = new NativeHashMap<EntityId, Vector3f>(enemyQuery.CalculateEntityCount() , Allocator.TempJob);
            NativeHashMap<EntityId, bool> killedEnemies = new NativeHashMap<EntityId, bool>(enemyQuery.CalculateEntityCount(), Allocator.TempJob);
            GetTargetPositionsJob getTargetPositionsJob = new GetTargetPositionsJob
            {
                attackeePositions = attackeePositions.AsParallelWriter()
            };

            JobHandle getTargetPositionsHandle = getTargetPositionsJob.Schedule(enemyQuery);

            GetKilledEnemiesJob getKilledEnemiesJob = new GetKilledEnemiesJob
            {
                deadEnemies = killedEnemies.AsParallelWriter()
            };

            JobHandle getKilledEnemiesHandle = getKilledEnemiesJob.Schedule(this);
            NativeQueue<AttackPayload> attackPayloads = new NativeQueue<AttackPayload>(Allocator.TempJob);
           
            MoveToAttackTargetJob moveToAttackTargetJob = new MoveToAttackTargetJob
            {
                attackerToAttackeePosition = attackeePositions,
                killedEnemies = killedEnemies,
                attackPayloads = attackPayloads.AsParallelWriter(),
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            JobHandle attackCommandDependancies = JobHandle.CombineDependencies(getKilledEnemiesHandle, getTargetPositionsHandle, tickAttackCoolDownHandle);
            JobHandle moveToAttackHandle = moveToAttackTargetJob.Schedule(attackQuery, attackCommandDependancies);
           // getKilledEnemiesHandle.Complete();
            //getTargetPositionsHandle.Complete()
            moveToAttackHandle.Complete();


            // Queue up to a buffer to do later. to avoid concurrency issues.
            var potentialTargetEntities = attackeePositions.GetKeyArray(Allocator.TempJob);
            foreach (var potentialTargetId in potentialTargetEntities)
            {
                workerSystem.TryGetEntity(potentialTargetId, out Entity targetEntity);
                if (EntityManager.HasComponent<SpawnSchema.RespawnMetadata.Component>(targetEntity) && EntityManager.GetComponentData<SpawnSchema.PendingRespawn.Component>(targetEntity).RespawnActive)
                {
                    attackeePositions.Remove(potentialTargetId);
                }
            }
            potentialTargetEntities.Dispose();
            attackeePositions.Dispose();
            killedEnemies.Dispose();


            #endregion
            MoveCommandJob moveCommandJob = new MoveCommandJob
            {
                // Need to be able to get post update commands in client world in job component system.
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<MoveCommand>();
            EntityQueryDesc entityQueryDesc = new EntityQueryDesc
            {
                All = authVelocityGroup
            };
            EntityQuery entityQuery = GetEntityQuery(entityQueryDesc);
            entityQuery.SetFilter(PositionSchema.LinearVelocity.ComponentAuthority.Authoritative);

            JobHandle moveToDestHandle = moveCommandJob.Schedule(entityQuery, moveToAttackHandle);

            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<CollectCommand>();
            authVelocityGroup[authVelocityGroup.Length - 2] = ComponentType.ReadOnly<SpatialEntityId>();

            entityQueryDesc = new EntityQueryDesc
            {
                All = authVelocityGroup
            };

            MoveToResourceJob moveToResourceJob = new MoveToResourceJob
            {
                occupyPayloads = pendingOccupy.AsParallelWriter()
            };
            entityQuery = GetEntityQuery(entityQueryDesc);

            moveToDestHandle.Complete();
            JobHandle moveToCollectHandle = moveToResourceJob.Schedule(entityQuery, moveToDestHandle);
            moveToCollectHandle.Complete();

            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<BuildCommand>();
            authVelocityGroup[authVelocityGroup.Length - 2] = ComponentType.ReadOnly<SpatialEntityId>();
            entityQuery = GetEntityQuery(entityQueryDesc);

            Queue<PendingAttack> pendingAttacks = ProcessAttackPayloads(attackPayloads);

            NativeHashMap<EntityId, BuildCommand> buildingUnits = new NativeHashMap<EntityId, BuildCommand>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            MoveToBuildLocationJob moveToBuildLocation = new MoveToBuildLocationJob{
                entitiesBuilding = buildingUnits.AsParallelWriter(),
            };

            JobHandle moveToBuildHandle = moveToBuildLocation.Schedule(entityQuery, moveToCollectHandle);

            ProcessPendingAttacks(pendingAttacks);

            JobHandle? updateBuildHandle = null;
            if (buildCommands.Length > 0)
            {
                UpdateBuildCommandJob updateBuildCommandJob = new UpdateBuildCommandJob
                {
                    pendingUpdates = buildCommands
                };

                updateBuildHandle = updateBuildCommandJob.Schedule(this, moveToBuildHandle);
            }
            RunCollectCommandRequests();
            moveToBuildHandle.Complete();
            ProcessBuildCommand(buildingUnits);
            buildingUnits.Dispose();
            ProcessBuildResponses();
            if (updateBuildHandle.HasValue)
            {
                updateBuildHandle.Value.Complete();
                buildCommands.Clear();
            }
        }

        // To go even further beyond in parraelizing maybe seperate this to request for each command too.
        private void RunCollectCommandRequests()
        {
                #region  Collect Command Request
                while (pendingOccupy.Count > 0)
                {
                    var occupyPayload = pendingOccupy.Dequeue();
                    resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                    {
                        ResourceRequestType = ResourceRequestType.OCCUPY,
                        OccupantId = occupyPayload.requestingOccupant,
                        ResourceId = occupyPayload.resourceId,
                        callback = HandleCollectCommandResponse,
                    });
                }
                while (pendingCollects.Count > 0)
                {
                    var collectPayload = pendingCollects.Dequeue();
                    resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                    {
                        ResourceRequestType = ResourceRequestType.COLLECT,
                        OccupantId = collectPayload.requestingOccupant,
                        ResourceId = collectPayload.resourceId,
                        callback = HandleCollectCommandResponse
                    });
                }
                #endregion
        }

        #region Response Callback Handlers
        private void HandleCollectCommandResponse(ResourceRequestSystem.ResourceRequestReponse receivedResponse)
        {
            // Simplest way is let them continue to it, then send occupy request and simply fail that way instead of interrupting mid way
            // otherwies would have to iterate past it.
            if (receivedResponse.OccupyResponse.HasValue)
            {
                var occupyResponse = receivedResponse.OccupyResponse.Value;
                if (occupyResponse.Occupied)
                {
                    // Need to re think this 
                    // Add to collecting
                    pendingCollects.Enqueue(new CollectPayload
                    {
                        requestingOccupant = occupyResponse.OccupantId,
                        resourceId = occupyResponse.ResourceId,
                    });
                }
                else if (occupyResponse.FullyOccupied)
                {
                    //Then remove component.
                    if (workerSystem.TryGetEntity(occupyResponse.OccupantId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;
                        PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                    }
                }
            }
            else if (receivedResponse.CollectResponse.HasValue)
            {
                var collectResponse = receivedResponse.CollectResponse.Value;

                if (collectResponse.TimesUntilDepleted == 0)
                {
                    if (collectResponse.DepleterId.HasValue)
                    {
                        // Irrelevant now since units no longer have inventory, but maybe down line
                        // increase collect skill to increase rate or whatever.
                        EntityId depleter = collectResponse.DepleterId.Value;

                        if (depleter.Equals(collectResponse.OccupantId))
                        {
                            // Do something.
                        }

                    }
                    if (workerSystem.TryGetEntity(collectResponse.OccupantId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;

                        CollectCommand collectCommand = entityManager.GetComponentData<CollectCommand>(entity);

                        List<QuadNode> quadNodes = positionSystem.querySpatialPartition(collectCommand.destination);

                        CollectCommand? closestNewResource = null;
                        float shortestDistance = float.PositiveInfinity;
                        foreach (QuadNode quadNode in quadNodes)
                        {
                            if (workerSystem.TryGetEntity(quadNode.entityId, out Entity potentialResourceEntity))
                            {
                                if (entityManager.HasComponent<Resource.Component>(potentialResourceEntity))
                                {
                                    EntityPosition.Component resourceTransform = entityManager.GetComponentData<EntityPosition.Component>(potentialResourceEntity);
                                    Vector3 pos = HelperFunctions.Vector3fToVector3(resourceTransform.Position);
                                    Vector3 dest = HelperFunctions.Vector3fToVector3(collectCommand.destination);
                                    float currDistance = Vector3.Distance(pos, dest);
                                    if ( currDistance < shortestDistance)
                                    {
                                        shortestDistance = currDistance;
                                        closestNewResource = new CollectCommand
                                        {
                                            resourceId = quadNode.entityId,
                                            destination = quadNode.position
                                        };
                                    }
                                }
                            }
                        }
                        if (closestNewResource.HasValue)    
                        {
                            entityManager.SetComponentData(entity, closestNewResource.Value);
                        }
                        else
                        {
                            PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                        }
                    }
                }
                else
                {
                    // Continue collecting.
                    pendingCollects.Enqueue(new CollectPayload
                    {
                        requestingOccupant = collectResponse.OccupantId,
                        resourceId = collectResponse.ResourceId,
                    });
                }

                // Then add points to Invader.
                if (receivedResponse.Success)
                {
                    // Get resource point value.
                    // Adds points while collecting resource, should I just add only when deplete?
                    // That's game design choice.
                    if (workerSystem.TryGetEntity(collectResponse.ResourceId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;
                        PointSchema.PointMetadata.Component pointMetadata = entityManager.GetComponentData<PointSchema.PointMetadata.Component>(entity);
                        // Replace this with getting from zenject store.
                        
                        PointRequestSystem pointRequestSystem = World.GetExistingSystem<PointRequestSystem>();

                        // Could I use call back here for something?
                        // point update already an event itself, just incase will add log.
                        pointRequestSystem.AddPointRequest(new MdgSchema.Common.Point.PointRequest
                        {
                            EntityUpdating = clientGameObjectCreator.PlayerLink.EntityId,
                            PointUpdate = pointMetadata.StartingPoints
                        }, (PointSchema.Point.UpdatePoints.ReceivedResponse pointResponsepointResponse) =>
                        {
                            Debug.Log("Callback for added points returned");
                        });
                    }
                }
            }
            else if (receivedResponse.ReleaseResponse.HasValue)
            {
                var releaseResponse = receivedResponse.ReleaseResponse.Value;
                if (receivedResponse.Success)
                {
                    if (workerSystem.TryGetEntity(releaseResponse.Occupant, out Entity entity))
                    {
                        PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                    }
                }
            }
        }
      
        #endregion
       

        // Main issue with this is that it's doing too much.
        // Tbh, for stuff like animations, it should be diff system also acting on CommandInterrupt.
        private void ProcessInterruptedCommands()
        {
            // Do job like it.
           Entities.With(interruptedGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId,  ref CommandInterrupt commandInterrupted) =>
           {
               switch (commandInterrupted.interrupting)
               {
                   case CommandType.Attack:
                      break;
                   case CommandType.Collect:
                       resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                       {
                           OccupantId = spatialEntityId.EntityId,
                           ResourceId = commandInterrupted.target.Value,
                           ResourceRequestType = ResourceRequestType.RELEASE
                       });
                       break;
                   case CommandType.Move:
                       break;
               }
           });
        }

        #region  Attack Command Functions
        private Queue<PendingAttack> ProcessAttackPayloads(NativeQueue<AttackPayload> attackPayloads)
        {
            Queue<PendingAttack> pendingAttacks = new Queue<PendingAttack>(attackQuery.CalculateEntityCount());
            // Later check range or melee
            EntityQuery lineOfSightQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                ComponentType.ReadOnly<EntityPosition.Component>(),
                ComponentType.Exclude<StructureSchema.Trap.Component>()
            );

            while (attackPayloads.Count > 0)
            {
                AttackPayload attackPayload = attackPayloads.Dequeue();
                int initialCapacity = enemyQuery.CalculateEntityCount() + attackQuery.CalculateEntityCount();
                NativeQueue<CommonJobs.ClientJobs.RaycastHit> entitiesOnLineOfSight = new NativeQueue<CommonJobs.ClientJobs.RaycastHit>(Allocator.TempJob);


                // Might as well just do physics ray cast at this point.
                CommonJobs.ClientJobs.Raycast raycastJob = new CommonJobs.ClientJobs.Raycast
                {
                    startPoint = HelperFunctions.Subtract(attackPayload.positionToAttack,attackPayload.startingPosition),
                    endPoint = attackPayload.positionToAttack,
                    hits = entitiesOnLineOfSight.AsParallelWriter(),
                    checking = attackPayload.attackerId
                };

                // Still good since parralel operation, BUT if I could stagger this completion it would be ideal.
                // What I could do is insert it into ANOTHER queue for pending attacks.
                // then in next loop complete
                pendingAttacks.Enqueue(new PendingAttack
                {
                    attackPayload = attackPayload,
                    jobHandle = raycastJob.Schedule(lineOfSightQuery),
                    InLineOfSight = entitiesOnLineOfSight,
                });
            }
            attackPayloads.Dispose();
            return pendingAttacks;
        }

        private void ProcessPendingAttacks(Queue<PendingAttack> pendingAttacks){

              while (pendingAttacks.Count > 0)
            {
                PendingAttack pendingAttack = pendingAttacks.Peek();
                AttackPayload attackPayload = pendingAttack.attackPayload;
                pendingAttack.jobHandle.Complete();

                workerSystem.TryGetEntity(attackPayload.attackerId, out Entity attackerEntity);
                workerSystem.TryGetEntity(attackPayload.attackeeId, out Entity targetEntity);

                float closestInLineOfSight = Mathf.Infinity;
                CommonJobs.ClientJobs.RaycastHit? closestEntity = null;

                while (pendingAttack.InLineOfSight.Count > 0)
                {
                    CommonJobs.ClientJobs.RaycastHit raycastHit = pendingAttack.InLineOfSight.Dequeue();
                    Debug.Log("Hit with raycast " + raycastHit.entityId);
                    float distanceFromHit = HelperFunctions.Distance(attackPayload.startingPosition, raycastHit.position);
                    if ( distanceFromHit < closestInLineOfSight)
                    {
                        closestInLineOfSight = distanceFromHit;
                        closestEntity = raycastHit;
                    }
                }

                // If in line of sight AND its first one in line of sight, ie: no blocking it then continue on with attack.
                // Confirmed in range and headed towards that position so line of sight must be filled, main question is what's in between.
                CombatMetadata combatMetadata = EntityManager.GetComponentData<CombatMetadata>(attackerEntity);
                CombatStats stats = EntityManager.GetComponentData<CombatStats>(attackerEntity);

                EntityManager.SetComponentData(attackerEntity, new CombatStats
                {
                    attackCooldown = combatMetadata.attackCooldown,
                    attackRange = stats.attackRange
                });


                // WOO them magic nums. Gotta update this.
                // will retrieve this from scriptable object instead of hardcoding the nums here.
                // Scriptable Object.
                ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(unitWeapon as Projectile);
                projectileConfig.startingPosition = attackPayload.startingPosition;
                // this part is fine
                Debug.Log($"{attackPayload.positionToAttack.X} {attackPayload.positionToAttack.Y} {attackPayload.positionToAttack.Z}");
                projectileConfig.linearVelocity = HelperFunctions.Subtract(attackPayload.positionToAttack, attackPayload.startingPosition);
                WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(unitWeapon);
                weaponMetadata.wielderId = clientGameObjectCreator.PlayerLink.EntityId.Id;
                byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);
                byte[] serializedWeaponMetadata = Converters.SerializeArguments(weaponMetadata);
                spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
                {
                    Position = attackPayload.startingPosition,
                    TypeToSpawn = MdgSchema.Common.GameEntityTypes.Weapon,
                    TypeId = 1,
                    Count = 1
                }, null, serializedWeaponMetadata, serializedWeapondata);

                /*

                if (closestEntity.HasValue && closestEntity.Value.entityId.Equals(attackPayload.attackeeId))
                {
                    Debug.Log("First in line of sight is target");

                    CombatMetadata combatMetadata = EntityManager.GetComponentData<CombatMetadata>(attackerEntity);
                    CombatStats stats = EntityManager.GetComponentData<CombatStats>(attackerEntity);

                    EntityManager.SetComponentData(attackerEntity, new CombatStats
                    {
                        attackCooldown = combatMetadata.attackCooldown,
                        attackRange = stats.attackRange
                    });

                    Vector3f sameYTarget = new Vector3f(attackPayload.positionToAttack.X, attackPayload.startingPosition.Y, attackPayload.positionToAttack.Z);

                    // WOO them magic nums. Gotta update this.
                    // will retrieve this from scriptable object instead of hardcoding the nums here.
                    // Scriptable Object.
                    ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(unitWeapon as Projectile);
                    projectileConfig.startingPosition = attackPayload.startingPosition;
                    projectileConfig.linearVelocity = HelperFunctions.Subtract(sameYTarget,attackPayload.startingPosition);

                    WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(unitWeapon);
                    byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);
                    byte[] serializedWeaponMetadata = Converters.SerializeArguments(weaponMetadata);
                    spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
                    {
                        Position = attackPayload.startingPosition,
                        TypeToSpawn = MdgSchema.Common.GameEntityTypes.Weapon,
                        TypeId = 1,
                        Count = 1
                    }, null, serializedWeaponMetadata, serializedWeapondata);

                }
                else
                {
                    Vector3f currentVelocity = HelperFunctions.Subtract(attackPayload.positionToAttack,attackPayload.startingPosition);

                    float angleOfTravel = Mathf.Atan2(currentVelocity.Z, currentVelocity.X);
                    float magnitude = HelperFunctions.Magnitude(currentVelocity);
                    // See if slightly left of vector or not.
                    // HelperFunctions.IsLeftOfVector(attackPayload.positionToAttack, )
                    //Meh just try to go 45 degrees.
                    angleOfTravel += 45 * Mathf.Deg2Rad;
                    angleOfTravel += Time.deltaTime;
                    Debug.Log("Not in light of sight. Adding reroute component here");
                    Vector3f newVelocity = HelperFunctions.Scale(new Vector3f(Mathf.Cos(angleOfTravel), attackPayload.startingPosition.Y, Mathf.Sin(angleOfTravel)),magnitude);
                    PostUpdateCommands.AddComponent(attackerEntity, new RerouteComponent
                    {
                        applied = false,
                        subDestination = newVelocity,
                        destination = currentVelocity
                    });
                }
                */
                pendingAttack.InLineOfSight.Dispose();
                pendingAttacks.Dequeue();
            }
        }

        #endregion

        // Realistically only first one sends request.
        // but also multiple concurrent build commands could be happening as well, keep that in mind.
        private void ProcessBuildCommand(NativeHashMap<EntityId, BuildCommand> buildingUnits)
        {
            
            var builderIds = buildingUnits.GetKeyArray(Allocator.TempJob);
            // Since sending build request
            for (int i = 0; i < builderIds.Length; ++i)
            {
                workerSystem.TryGetEntity(builderIds[i], out Entity entity);
                if (!EntityManager.HasComponent<BuildCommand>(entity))
                {
                    continue;
                }
                BuildCommand buildCommand = buildingUnits[builderIds[i]];

                if (!buildCommand.structureId.IsValid())
                {
                    StructureConfig structureConfig;

                    switch (buildCommand.structureType)
                    {
                        case StructureSchema.StructureType.Claiming:
                            structureConfig = new ClaimConfig
                            {
                                constructing = true,
                                health = 100,
                                ownerId = clientGameObjectCreator.PlayerLink.EntityId.Id,
                                structureType = StructureSchema.StructureType.Claiming,
                                territoryId = buildCommand.territoryId.Value.Id
                            };
                            break;
                        case StructureSchema.StructureType.Spawning:
                            structureConfig = new SpawnStructureConfig
                            {
                                structureType = buildCommand.structureType,
                                constructionTime = buildCommand.constructionTime,
                                constructing = true,
                                health = 10,
                                ownerId = clientGameObjectCreator.PlayerLink.EntityId.Id
                            };
                            break;
                        default:
                            throw new System.Exception("Invalid structure type for build command");
                    }

                    byte[] serialized = Converters.SerializeArguments(structureConfig);
                    Debug.Log("BUild location " + buildCommand.buildLocation);
                    spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
                    {
                        Position = buildCommand.buildLocation,
                        TypeToSpawn = GameEntityTypes.Structure
                    }, (EntityId structureId) =>
                    {
                        OnStructureBuilt(structureId, buildCommand);
                    }, serialized);
                }
                else
                {
                    if (buildCommands.ContainsKey(buildCommand.builderId))
                    {
                        continue;
                    }
                    // Otherwise send build requests.
                    // Maybe map   builder id to request id and that is the request cooldown.
                    // that way not sending every frame, but only after last build request processed.
                    Debug.Log("Sending build request for structure with id " + buildCommand.structureId);
                    long requestId = commandSystem.SendCommand(new StructureSchema.Structure.Build.Request
                    {
                        TargetEntityId = buildCommand.structureId,
                        Payload = new StructureSchema.BuildRequestPayload
                        {
                            BuilderId = buildCommand.builderId,
                            BuildRate = 1
                        }
                    });
                    // Later on check for response to this and revoke building command as needed.
                    requestIdToBuildCommand.Add(requestId,buildCommand);
                }
            }
            builderIds.Dispose();
        }

        private void OnStructureBuilt(EntityId structureId, BuildCommand buildCommand)
        {
            buildCommand.structureId = structureId;
            buildCommands.TryAdd(buildCommand.builderId, buildCommand);
        }

        private void ProcessBuildResponses()
        {
            var responses = commandSystem.GetResponses<StructureSchema.Structure.Build.ReceivedResponse>();

            for (int i = 0; i < responses.Count; ++i)
            {
                ref readonly var response = ref responses[i];
                BuildCommand buildCommand = requestIdToBuildCommand[response.RequestId];
                switch (response.StatusCode)
                {
                    case Improbable.Worker.CInterop.StatusCode.Success:
                        bool stopBuilding = response.ResponsePayload.Value.FinishedBuilding || response.ResponsePayload.Value.AlreadyBuilt;
                        buildCommand.isBuilding = !stopBuilding;
                        workerSystem.TryGetEntity(buildCommand.builderId, out Entity builderEntity);
                        PostUpdateCommands.RemoveComponent(builderEntity, typeof(BuildCommand));
                        break;
                }
            }
        }
    }
}