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


        struct BuildPayload
        {
            public EntityId builderId;
            public EntityId structureId;
        }

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


        private NativeQueue<BuildPayload> buildRequestsToSend;

        private PositionSystem positionSystem;
        private CommandSystem commandSystem;
        private SpawnRequestSystem spawnRequestSystem;
        private ResourceRequestSystem resourceRequestSystem;
        private ComponentUpdateSystem componentUpdateSystem;
        private WorkerSystem workerSystem;
        private Dictionary<EntityId, List<EntityId>> unitCollisionMappings;
        private ComponentType[] authVelocityGroup;

        private EntityQuery combatStatsQuery;
        private EntityQuery commandListenerResetQuery;
        private EntityQuery interruptedGroup;
        private EntityQuery enemyQuery;
        private EntityQuery friendlyQuery;
        private EntityQuery attackQuery;
        public JobHandle CommandExecuteJobHandle { get; private set; }

        // Change this to dictionary so that weapons of units more expandible.
        Weapon unitWeapon;


        struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityTransform.Component, PositionSchema.LinearVelocity.Component,
            CollisionSchema.BoxCollider.Component, CommandListener>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref MoveCommand moveCommand, [ReadOnly] ref EntityTransform.Component entityTransform,
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                ref CommandListener commandListener)
            {

                Vector3f sameY = new Vector3f(moveCommand.destination.X, entityTransform.Position.Y, moveCommand.destination.Z);
                Vector3f direction = sameY - entityTransform.Position;
                float distance = HelperFunctions.Distance(sameY, entityTransform.Position);

                if (!moveCommand.applied)
                {
                    linearVelocityComponent.Velocity = direction;
                    moveCommand.applied = true;
                }
                else if (distance <= boxCollider.Dimensions.ToUnityVector().magnitude)
                {
                    Debug.Log("Finished moving");
                    linearVelocityComponent.Velocity = Vector3f.Zero;
                    commandListener.CommandType = CommandType.None;
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
            }
        }

        struct MoveToResourceJob : IJobForEachWithEntity<SpatialEntityId, CollectCommand, EntityTransform.Component, 
            PositionSchema.LinearVelocity.Component, CollisionSchema.BoxCollider.Component, CommandListener>
        {
            [WriteOnly]
            public NativeQueue<CollectPayload>.ParallelWriter occupyPayloads;

            public void Execute(Entity entity, int jobIndex, ref SpatialEntityId spatialEntityId, ref CollectCommand collectCommand, 
                ref EntityTransform.Component entityTransform, ref PositionSchema.LinearVelocity.Component linearVelocityComponent,
                [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider, ref CommandListener commandListener)
            {
                Vector3f sameY = new Vector3f(collectCommand.destination.X, entityTransform.Position.Y, collectCommand.destination.Z);
                Vector3f direction = sameY - entityTransform.Position;
                float distance = HelperFunctions.Distance(sameY, entityTransform.Position);


                // Just magnitude prob fine, if all uniform, they won't be uniform though.
                float minDistance = boxCollider.Dimensions.ToUnityVector().magnitude;
                // When should I mark IsAtResource
                if (!collectCommand.IsCollecting && !collectCommand.IsAtResource)
                {
                    if (distance < minDistance)
                    {
                        collectCommand.IsAtResource = true;
                        linearVelocityComponent.Velocity = Vector3f.Zero;
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

        struct GetTargetPositionsJob : IJobForEach<SpatialEntityId, Enemy, EntityTransform.Component>
        {
            public NativeHashMap<EntityId, Vector3f>.ParallelWriter attackeePositions;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Enemy enemy, 
                [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                // Maybe add clickable and clicked.
                if (attackeePositions.TryAdd(spatialEntityId.EntityId, entityTransform.Position))
                {
                }
            }
        }

        // So what I want to do is actually act upon the AttackPayload stuff.
        // Only those entities do I want to check if line of sight, since that is when they are prepping to attack.
        // Let default reroute collision syttem handle it for most part.

        struct MoveToAttackTargetJob : IJobForEachWithEntity<SpatialEntityId, AttackCommand, PositionSchema.LinearVelocity.Component,
            EntityTransform.Component, CombatStats>
        {

            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            [ReadOnly]
            public NativeHashMap<EntityId, Vector3f> attackerToAttackeePosition;
            public float deltaTime;

            public NativeQueue<AttackPayload>.ParallelWriter attackPayloads;

            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId spatialEntityId, ref AttackCommand attackCommand, 
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent, [ReadOnly] ref EntityTransform.Component entityTransform, 
                [ReadOnly] ref CombatStats combatStats)
            {

                if (attackerToAttackeePosition.TryGetValue(attackCommand.target, out Vector3f targetPosition))
                {
                    Vector3f sameY = new Vector3f(targetPosition.X, entityTransform.Position.Y, targetPosition.Z);
                    Vector3f direction = sameY - entityTransform.Position;
                    float distance = HelperFunctions.Distance(sameY, entityTransform.Position);
                    if (distance <= combatStats.attackRange)
                    {
                        if (combatStats.attackCooldown == 0)
                        {
                            attackPayloads.Enqueue(new AttackPayload
                            {
                                attackerId = spatialEntityId.EntityId,
                                positionToAttack = targetPosition,
                                startingPosition = entityTransform.Position + (linearVelocityComponent.Velocity * deltaTime),
                                attackeeId = attackCommand.target
                            });
                            attackCommand.attacking = true;
                        }
                        linearVelocityComponent.Velocity = Vector3f.Zero;
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
                    linearVelocityComponent.Velocity = Vector3f.Zero;
                }
            }
        }
        #endregion

        #region Build Command Jobs
        struct MoveToBuildLocationJob: IJobForEach<SpatialEntityId, BuildCommand, PositionSchema.LinearVelocity.Component,
            EntityTransform.Component>
        {
            public NativeHashMap<EntityId, BuildCommand>.ParallelWriter entitiesBuilding;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, ref BuildCommand buildCommand, ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
            [ReadOnly] ref EntityTransform.Component entityTransformComponent )
            {
                float distance = HelperFunctions.Distance(buildCommand.buildLocation, entityTransformComponent.Position);
                if (distance <= buildCommand.minDistanceToBuild && !buildCommand.isBuilding){
                    buildCommand.isBuilding = true;
                    entitiesBuilding.TryAdd(spatialEntityId.EntityId, buildCommand);
                }
                else{
                    linearVelocityComponent.Velocity = buildCommand.buildLocation - entityTransformComponent.Position;
                    buildCommand.isBuilding = false;
                }
            }
        }
        #endregion

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
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
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadOnly<CombatStats>(),
                ComponentType.Exclude<RerouteComponent>()
                );
            attackQuery.SetFilter(PositionSchema.LinearVelocity.ComponentAuthority.Authoritative);

            enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Enemy>(),
                ComponentType.ReadOnly<EntityTransform.Component>()
                );
            authVelocityGroup = new ComponentType[7]
            {
                ComponentType.ReadWrite<CommandListener>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>(),
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                null,
                null
            };

            pendingCollects = new NativeQueue<CollectPayload>(Allocator.Persistent);
            pendingOccupy = new NativeQueue<CollectPayload>(Allocator.Persistent);

            spawnRequestSystem = World.GetExistingSystem<SpawnRequestSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            positionSystem = World.GetExistingSystem<PositionSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            resourceRequestSystem = World.GetExistingSystem<ResourceRequestSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();

            unitCollisionMappings = new Dictionary<EntityId, List<EntityId>>();
            interruptedGroup = GetEntityQuery(ComponentType.ReadOnly<CommandInterrupt>(), ComponentType.ReadOnly<SpatialEntityId>());
           // enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
            friendlyQuery = GetEntityQuery(ComponentType.ReadOnly<CommandListener>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            pendingCollects.Dispose();
            pendingOccupy.Dispose();
        }

        protected override void OnUpdate()
        {
            RunCommandJobs();
            ProcessInterruptedCommands();
        }
        

        private void RunCommandJobs()
        {
            float deltaTime = Time.deltaTime;

            // Run tick cooldown and getting enemy positions jobs in background while running move, build, and collect jobs.
            CommonJobs.ClientJobs.TickAttackCooldownJob tickAttackCooldownJob = new CommonJobs.ClientJobs.TickAttackCooldownJob
            {
                deltaTime = deltaTime
            };
            JobHandle tickAttackCoolDownHandle = tickAttackCooldownJob.Schedule(this);
            NativeHashMap<EntityId, Vector3f> attackeePositions = new NativeHashMap<EntityId, Vector3f>(enemyQuery.CalculateEntityCount() , Allocator.TempJob);
            GetTargetPositionsJob getTargetPositionsJob = new GetTargetPositionsJob
            {
                attackeePositions = attackeePositions.AsParallelWriter()
            };

            // It's read only thoughh
            JobHandle getTargetPositionsHandle = getTargetPositionsJob.Schedule(enemyQuery);

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

            JobHandle moveToDestHandle = moveCommandJob.Schedule(entityQuery);
            tickAttackCoolDownHandle.Complete();
            moveToDestHandle.Complete();

            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<CollectCommand>();
            authVelocityGroup[authVelocityGroup.Length - 2] = ComponentType.ReadOnly<SpatialEntityId>();

            // Might need to keep doing 
            entityQueryDesc = new EntityQueryDesc
            {
                All = authVelocityGroup
            };

            MoveToResourceJob moveToResourceJob = new MoveToResourceJob
            {

                occupyPayloads = pendingOccupy.AsParallelWriter()
            };
            entityQuery = GetEntityQuery(entityQueryDesc);
            JobHandle moveToCollectHandle = moveToResourceJob.Schedule(entityQuery, getTargetPositionsHandle);

            getTargetPositionsHandle.Complete();

            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<BuildCommand>();
            authVelocityGroup[authVelocityGroup.Length - 2] = ComponentType.ReadOnly<SpatialEntityId>();
            entityQuery = GetEntityQuery(entityQueryDesc);

            NativeHashMap<EntityId, BuildCommand> buildingUnits = new NativeHashMap<EntityId, BuildCommand>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            MoveToBuildLocationJob moveToBuildLocation = new MoveToBuildLocationJob{
                entitiesBuilding = buildingUnits.AsParallelWriter(),
                
            };

            moveToCollectHandle.Complete();

            JobHandle moveToBuildHandle = moveToBuildLocation.Schedule(entityQuery);

            var potentialTargetEntities = attackeePositions.GetKeyArray(Allocator.TempJob);

            // Need to remove any keys that are respawning.
            foreach(var potentialTargetId in potentialTargetEntities)
            {
                workerSystem.TryGetEntity(potentialTargetId, out Entity targetEntity);

                //CHeck if respawning
                if (EntityManager.HasComponent<SpawnSchema.RespawnMetadata.Component>(targetEntity) && EntityManager.GetComponentData<SpawnSchema.PendingRespawn.Component>(targetEntity).RespawnActive)
                {
                    Debug.Log("here??");
                    attackeePositions.Remove(potentialTargetId);
                }
            }
            potentialTargetEntities.Dispose();
            // Maybe make this a member variale instead of local. For now it's fine.
            NativeQueue<AttackPayload> attackPayloads = new NativeQueue<AttackPayload>(Allocator.TempJob);
            MoveToAttackTargetJob moveToAttackTargetJob = new MoveToAttackTargetJob
            {
                deltaTime = deltaTime,
                attackerToAttackeePosition = attackeePositions,
                attackPayloads = attackPayloads.AsParallelWriter(),
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };

            moveToBuildHandle.Complete();

            // Run moving to attack command job in background while process collect command requests.
            // and sending build requests from build commands.
            JobHandle moveToAttackHandle = moveToAttackTargetJob.Schedule(attackQuery);
            RunCollectCommandRequests();
            moveToAttackHandle.Complete();
          
            attackeePositions.Dispose();

            // maybe even do this run first as easily one of the biggest
            Queue<PendingAttack> pendingAttacks = ProcessAttackPayloads(attackPayloads);
            SpawnBuildings(buildingUnits);
            buildingUnits.Dispose();
            // Spawn Buildings
            // Rename this to SpawnStructures.
            RunBuildCommandRequests();

            ProcessPendingAttacks(pendingAttacks);
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

        private void RunBuildCommandRequests(){

            
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
                                    EntityTransform.Component resourceTransform = entityManager.GetComponentData<EntityTransform.Component>(potentialResourceEntity);

                                    float currDistance = HelperFunctions.Distance(resourceTransform.Position, collectCommand.destination);
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
                        GameObject invaderObject = GameObject.FindGameObjectWithTag("MainCamera");
                        PointRequestSystem pointRequestSystem = World.GetExistingSystem<PointRequestSystem>();

                        // Could I use call back here for something?
                        // point update already an event itself, just incase will add log.
                        pointRequestSystem.AddPointRequest(new MdgSchema.Common.Point.PointRequest
                        {
                            EntityUpdating = invaderObject.GetComponent<LinkedEntityComponent>().EntityId,
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
        private void HandleStructureSpawned(EntityId spawned) 
        {

        }
        private void HandleBuildResponses()
        {

        }
        #endregion
       

        // Main issue with this is that it's doing too much.
        // Tbh, for stuff like animations, it should be diff system also acting on CommandInterrupt.
        private void ProcessInterruptedCommands()
        {
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
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.Exclude<StructureSchema.Trap.Component>()
            );

            while (attackPayloads.Count > 0)
            {
                AttackPayload attackPayload = attackPayloads.Dequeue();
                int initialCapacity = enemyQuery.CalculateEntityCount() + attackQuery.CalculateEntityCount();
                NativeQueue<CommonJobs.ClientJobs.RaycastHit> entitiesOnLineOfSight = new NativeQueue<CommonJobs.ClientJobs.RaycastHit>(Allocator.TempJob);

                CommonJobs.ClientJobs.Raycast raycastJob = new CommonJobs.ClientJobs.Raycast
                {
                    startPoint = attackPayload.positionToAttack - attackPayload.startingPosition,
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

            // Some extra time to ru this one.
        }

        private void ProcessPendingAttacks(Queue<PendingAttack> pendingAttacks){

              while (pendingAttacks.Count > 0)
            {
                PendingAttack pendingAttack = pendingAttacks.Peek();
                AttackPayload attackPayload = pendingAttack.attackPayload;
                pendingAttack.jobHandle.Complete();

                workerSystem.TryGetEntity(attackPayload.attackerId, out Entity attackerEntity);
                workerSystem.TryGetEntity(attackPayload.attackeeId, out Entity targetEntity);
                // O(n) time checking distance faster than converting to sortable and sorting
                // and implementing own auto sorting native contiainer.
                float closestInLineOfSight = Mathf.Infinity;
                CommonJobs.ClientJobs.RaycastHit? closestEntity = null;

                while (pendingAttack.InLineOfSight.Count > 0)
                {
                    CommonJobs.ClientJobs.RaycastHit raycastHit = pendingAttack.InLineOfSight.Dequeue();
                    Debug.Log("Hit with raycast " + raycastHit.entityId);
                    float distanceFromHit = HelperFunctions.Distance(attackPayload.startingPosition, raycastHit.position);
                    if ( distanceFromHit < closestInLineOfSight)
                    {
                        // First check if it is trap, / floored. Lol. TECHNICALLY, floored should just be ignored in line of sight.
                        // for now just check if trap then ignore.
                        closestInLineOfSight = distanceFromHit;
                        closestEntity = raycastHit;
                    }
                }

                // If in line of sight AND its first one in line of sight, ie: no blocking it then continue on with attack.
                // Confirmed in range and headed towards that position so line of sight must be filled, main question is what's in between.
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
                    projectileConfig.linearVelocity = sameYTarget - attackPayload.startingPosition;

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
                    Vector3f currentVelocity = attackPayload.positionToAttack - attackPayload.startingPosition;

                    float angleOfTravel = Mathf.Atan2(currentVelocity.Z, currentVelocity.X);
                    float magnitude = HelperFunctions.Magnitude(currentVelocity);
                    // See if slightly left of vector or not.
                    // HelperFunctions.IsLeftOfVector(attackPayload.positionToAttack, )
                    //Meh just try to go 45 degrees.
                    //angleOfTravel += 45 * Mathf.Deg2Rad;
                    angleOfTravel += Time.deltaTime;
                    Debug.Log("Not in light of sight. Adding reroute component here");
                    Vector3f newVelocity = new Vector3f(Mathf.Cos(angleOfTravel), attackPayload.startingPosition.Y, Mathf.Sin(angleOfTravel)) * magnitude;
                    PostUpdateCommands.AddComponent(attackerEntity, new RerouteComponent
                    {
                        applied = false,
                        subDestination = newVelocity,
                        destination = currentVelocity
                    });
                }
                pendingAttack.InLineOfSight.Dispose();
                pendingAttacks.Dequeue();
            }
        }

        #endregion

        private void SpawnBuildings(NativeHashMap<EntityId, BuildCommand> buildingUnits)
        {
            var builderIds = buildingUnits.GetKeyArray(Allocator.TempJob);

            foreach(EntityId builderId in builderIds){
                // I should puysh whats here and let autocompelte hep now.
            }
            builderIds.Dispose();
        }
    }
}