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
using PointSchema = MdgSchema.Common.Point;
using MDG.Common.Systems.Point;
using Improbable.Gdk.Subscriptions;
using MDG.Common.Systems.Position;
using MDG.Common.Datastructures;
using MDG.Common;
using MDG.Common.Systems.Spawn;
using MDG.DTO;

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

        public struct CollectPayload
        {
            public EntityId resourceId;
            public EntityId requestingOccupant;
        }

        public struct AttackPayload
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



        public struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityTransform.Component, PositionSchema.LinearVelocity.Component,
            CollisionSchema.BoxCollider.Component, CommandListener>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref MoveCommand moveCommand, [ReadOnly] ref EntityTransform.Component entityTransform,
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                ref CommandListener commandListener)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);

                Vector3f direction = moveCommand.destination - entityTransform.Position;
                float distance = HelperFunctions.Distance(moveCommand.destination, entityTransform.Position);

                if (!moveCommand.applied)
                {
                    linearVelocityComponent.Velocity = direction;
                    moveCommand.applied = true;
                }
                else if (distance <= boxCollider.Dimensions.ToUnityVector().magnitude)
                {
                    Debug.Log("Finished moving");
                    linearVelocityComponent.Velocity = Vector3f.Zero;
                    commandListener.CommandType = Commands.CommandType.None;
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
            }
        }

        public struct MoveToResourceJob : IJobForEachWithEntity<SpatialEntityId, CollectCommand, EntityTransform.Component, 
            PositionSchema.LinearVelocity.Component, CollisionSchema.BoxCollider.Component, CommandListener>
        {
            // There is chance that the resource moving to is gone before get there.
            // In that case collect command needs to removed on not just those ready to collect but on all.
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            [WriteOnly]
            public NativeQueue<CollectPayload>.ParallelWriter occupyPayloads;

            public void Execute(Entity entity, int jobIndex, ref SpatialEntityId spatialEntityId, ref CollectCommand collectCommand, 
                ref EntityTransform.Component entityTransform, ref PositionSchema.LinearVelocity.Component linearVelocityComponent,
                [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider, ref CommandListener commandListener)
            {
                Vector3f direction = collectCommand.destination - entityTransform.Position;
                // Min distance should take really take into account collider, for now I'll fudg vaue
                // DOwn line pass in NativeArray of BoxCollider. to more accurate.
                const float bufferRoom = 1.0f;
                // Just magnitude prob fine, if all uniform, they won't be uniform though.
                float minDistance = boxCollider.Dimensions.ToUnityVector().magnitude + bufferRoom;
                // When should I mark IsAtResource
                if (!collectCommand.IsCollecting && !collectCommand.IsAtResource)
                {
                    float distance = direction.ToUnityVector().magnitude;

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

        public struct GetTargetPositionsJob : IJobForEach<SpatialEntityId, Enemy, EntityTransform.Component>
        {
            public NativeHashMap<EntityId, Vector3f>.ParallelWriter attackeePositions;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Enemy enemy, 
                [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                // Maybe add clickable and clicked.
                if (attackeePositions.TryAdd(spatialEntityId.EntityId, entityTransform.Position))
                {
                    Debug.Log("Adding spatial id to map " + spatialEntityId.EntityId);
                }
            }
        }

        // So what I want to do is actually act upon the AttackPayload stuff.
        // Only those entities do I want to check if line of sight, since that is when they are prepping to attack.
        // Let default reroute collision syttem handle it for most part.

        public struct MoveToAttackTargetJob : IJobForEachWithEntity<SpatialEntityId, AttackCommand, PositionSchema.LinearVelocity.Component,
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
                    float distance = HelperFunctions.Distance(targetPosition, entityTransform.Position);
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
                        // Issue is this happens. since no longer in range.

                        // If no longer in range, stop attacking, start following again.
                        attackCommand.attacking = false;
                        Vector3f velocity = targetPosition - entityTransform.Position;
                        linearVelocityComponent.Velocity = velocity;
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

        protected override void OnCreate()
        {
            base.OnCreate();

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
            RunCommandRequests();
            ProcessInterruptedCommands();
        }

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




        // Pretty unorgnaized and interwined sections for the purpose of parralelization
        private void RunCommandJobs()
        {
            float deltaTime = Time.deltaTime;

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

            JobHandle getTargetPositionsHandle = getTargetPositionsJob.Schedule(enemyQuery);

            MoveCommandJob moveCommandJob = new MoveCommandJob
            {
                deltaTime = deltaTime,
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
            moveCommandJob.Schedule(entityQuery).Complete();

            authVelocityGroup[authVelocityGroup.Length - 1] = ComponentType.ReadWrite<CollectCommand>();
            authVelocityGroup[authVelocityGroup.Length - 2] = ComponentType.ReadOnly<SpatialEntityId>();

            MoveToResourceJob moveToResourceJob = new MoveToResourceJob
            {
                deltaTime = deltaTime,
                occupyPayloads = pendingOccupy.AsParallelWriter()
            };
            entityQuery = GetEntityQuery(entityQueryDesc);
            // For each pending collect, send Collect request.
            getTargetPositionsHandle.Complete();

            var potentialTargetEntities = attackeePositions.GetKeyArray(Allocator.TempJob);

            // Need to remove any keys that are respawning.
            foreach(var potentialTargetId in potentialTargetEntities)
            {
                workerSystem.TryGetEntity(potentialTargetId, out Entity targetEntity);

                //CHeck if respawning
                if (EntityManager.HasComponent<SpawnSchema.RespawnMetadata.Component>(targetEntity) && EntityManager.GetComponentData<SpawnSchema.PendingRespawn.Component>(targetEntity).RespawnActive)
                {
                    attackeePositions.Remove(potentialTargetId);
                }
            }
            potentialTargetEntities.Dispose();

            moveToResourceJob.Schedule(entityQuery).Complete();

            tickAttackCoolDownHandle.Complete();
            // Maybe make this a member variale instead of local. For now it's fine.
            NativeQueue<AttackPayload> attackPayloads = new NativeQueue<AttackPayload>(Allocator.TempJob);
            MoveToAttackTargetJob moveToAttackTargetJob = new MoveToAttackTargetJob
            {
                deltaTime = deltaTime,
                attackerToAttackeePosition = attackeePositions,
                attackPayloads = attackPayloads.AsParallelWriter(),
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };

            moveToAttackTargetJob.Schedule(attackQuery).Complete();
            attackeePositions.Dispose();

       


            // Maybe list instead of queue, that way ca
            Queue<PendingAttack> pendingAttacks = new Queue<PendingAttack>(attackQuery.CalculateEntityCount());
            // Later check range or melee
            while (attackPayloads.Count > 0)
            {
                AttackPayload attackPayload = attackPayloads.Dequeue();
                


                // Check if within line of sight first. Simply running ray cast job and checking if front of list
                // is the target. If not, reroute. (Down line MAYBE check if what hit was also enemy. If so change target.
                // BUT that is more specifics / polish.

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
                    jobHandle = raycastJob.Schedule(this),
                    InLineOfSight = entitiesOnLineOfSight,
                });
            }
            attackPayloads.Dispose();

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

                    float distanceFromHit = HelperFunctions.Distance(attackPayload.startingPosition, raycastHit.position);
                    if ( distanceFromHit < closestInLineOfSight)
                    {
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

                    Debug.Log($"Starting position = {attackPayload.startingPosition} position to attack: {attackPayload.positionToAttack}");
                    Vector3f sameYTarget = new Vector3f(attackPayload.positionToAttack.X, attackPayload.startingPosition.Y, attackPayload.positionToAttack.Z);

                    // WOO them magic nums. Gotta update this.
                    // will retrieve this from scriptable object instead of hardcoding the nums here.
                    // Scriptable Object
                    ProjectileConfig projectileConfig = new ProjectileConfig
                    {
                        startingPosition = attackPayload.startingPosition,
                        linearVelocity = sameYTarget - attackPayload.startingPosition,
                        maximumHits = 1,
                        damage = 1,
                        projectileId = 1,
                        dimensions = new Vector3f(15, 0, 15),
                        lifeTime = 5.0f,
                    };

                    byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);

                    WeaponMetadata weaponMetadata = new WeaponMetadata
                    {
                        weaponType = MdgSchema.Common.Weapon.WeaponType.Projectile,
                        wielderId = attackPayload.attackerId.Id,
                        attackCooldown = 1.0f
                    };
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
                    // Otherwise if was not in line of sight apply reroute component.
                    // Maybe instead of applying reroute component
                    // Sub destination could be angle from 

                    // This is so EXTRA, but good.
                    Vector3f currentVelocity = attackPayload.positionToAttack - attackPayload.startingPosition;

                    float angleOfTravel = Mathf.Atan2(currentVelocity.Z, currentVelocity.X);
                    float magnitude = HelperFunctions.Magnitude(currentVelocity);
                    // See if slightly left of vector or not.
                    // HelperFunctions.IsLeftOfVector(attackPayload.positionToAttack, )
                    //Meh just try to go 45 degrees.
                    //angleOfTravel += 45 * Mathf.Deg2Rad;
                    angleOfTravel += deltaTime;
                    Debug.Log("Adding reroute component here");
                    Vector3f newVelocity = new Vector3f(Mathf.Cos(angleOfTravel), 0, Mathf.Sin(angleOfTravel)) * magnitude;
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

        private void RunCommandRequests()
        {
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

        }

        // Main issue with this is that it's doing too much.
        // Tbh, for stuff like animations, it should be diff system also acting on CommandInterrupt.
        private void ProcessInterruptedCommands()
        {
           Entities.With(interruptedGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId,  ref CommandInterrupt commandInterrupted) =>
           {
               switch (commandInterrupted.interrupting)
               {
                   // Fall through because I am treating everything as a 'resource' so units focusing on an enemy
                   // are 'occupying' it. When unit is disarming a trap, it is 'occupying' the resource.
                   // Collect in itself is very generic just 'health' but health could mean anything.
                   case Commands.CommandType.Attack:
                      
                   case Commands.CommandType.Collect:
                       resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                       {
                           OccupantId = spatialEntityId.EntityId,
                           ResourceId = commandInterrupted.target.Value,
                           ResourceRequestType = ResourceRequestType.RELEASE
                       });
                       break;
                   case Commands.CommandType.Move:
                       break;
               }
           });
        }
    }
}