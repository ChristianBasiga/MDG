using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using MdgSchema.Common;
using MdgSchema.Common.Collision;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using CollisionSchema = MdgSchema.Common.Collision;
using PointSchema = MdgSchema.Common.Point;
using StatSchema = MdgSchema.Common.Stats;
using WeaponSchema = MdgSchema.Common.Weapon;

namespace MDG.Common.Systems.Weapon
{

    // Jobify this system later.
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [AlwaysUpdateSystem]
    public class WeaponSystem : ComponentSystem
    {

        JobHandle updateHitsJobHandle;
        ComponentUpdateSystem componentUpdateSystem;
        CommandSystem commandSystem;
        WorkerSystem workerSystem;
        Point.PointRequestSystem pointRequestSystem;

        EntityQuery weaponCollisionQuery;
        EntityQuery updateWeaponHitCountQuery;

        Queue<DamageRequestPayload> queuedDamagedRequests; 

        struct DamageRequestPayload
        {
            public EntityId weapon_id;
            // Could make this optional so not all deaths result in points.
            public PointSchema.Point.Component pointComponent;
            public StatSchema.Stats.DamageEntity.Request request;
            // Purely for not having to re get weapon entity, etc.
            public WeaponSchema.Weapon.Component weaponComponent;
        }
        Dictionary<long, DamageRequestPayload> pendingDamageRequests;

        NativeHashMap<EntityId, int> weaponIdToHitsThisFrame;
        NativeQueue<EntityId> destroyedWeapons;

        /// <summary>
        /// ToDo: Store weapons that killed, get wielder to give points directly. Figure out flow for that.
        /// </summary>

        protected override void OnCreate()
        {
            base.OnCreate();
            queuedDamagedRequests = new Queue<DamageRequestPayload>();
            pendingDamageRequests = new Dictionary<long, DamageRequestPayload>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            pointRequestSystem = World.GetExistingSystem<Point.PointRequestSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();

            weaponCollisionQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CollisionSchema.Collision.Component>(),
                ComponentType.ReadOnly<WeaponSchema.Weapon.Component>(),
                ComponentType.ReadOnly<WeaponSchema.Damage.Component>()
                );

            updateWeaponHitCountQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<WeaponSchema.Weapon.Component>(),
                ComponentType.ReadWrite<WeaponSchema.Damage.Component>(),
                ComponentType.ReadOnly<WeaponSchema.Damage.ComponentAuthority>()

                );
            updateWeaponHitCountQuery.SetFilter(WeaponSchema.Damage.ComponentAuthority.Authoritative);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (destroyedWeapons.IsCreated)
            {
                destroyedWeapons.Dispose();
            }

            if (weaponIdToHitsThisFrame.IsCreated)
            {
                weaponIdToHitsThisFrame.Dispose();
            }
        }

        struct UpdateWeaponHitCountJob : IJobForEach<SpatialEntityId,
            WeaponSchema.Weapon.Component, WeaponSchema.Damage.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> idToDamage;
            public NativeQueue<EntityId>.ParallelWriter destroyedWeapons;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref WeaponSchema.Weapon.Component weaponComponent, 
                ref WeaponSchema.Damage.Component damageComponent)
            {
                if (idToDamage.TryGetValue(spatialEntityId.EntityId, out int hits))
                {
                    damageComponent.Hits += hits;
                    if (damageComponent.Hits >= weaponComponent.Durability)
                    {
                        destroyedWeapons.Enqueue(spatialEntityId.EntityId);
                    }
                }
                
            }
        }

        protected override void OnUpdate()
        {
            int entityCount = updateWeaponHitCountQuery.CalculateEntityCount();
            weaponIdToHitsThisFrame = new NativeHashMap<EntityId, int>(entityCount, Allocator.TempJob);
            destroyedWeapons = new NativeQueue<EntityId>(Allocator.TempJob);

            SendQueuedDamageRequests();
            ProcessDamageRequestResponses();
            ProcessWeaponCollisions();
            UpdateWeaponHitCountJob updateWeaponHitCountJob = new UpdateWeaponHitCountJob
            {
                idToDamage = weaponIdToHitsThisFrame,
                destroyedWeapons = destroyedWeapons.AsParallelWriter()
            };
            updateHitsJobHandle = updateWeaponHitCountJob.Schedule(updateWeaponHitCountQuery);

            updateHitsJobHandle.Complete();
            weaponIdToHitsThisFrame.Dispose(updateHitsJobHandle);

            while (destroyedWeapons.Count > 0)
            {
                UnityEngine.Debug.Log("destroyed weapon on hit");
                EntityId destroyedWeaponId = destroyedWeapons.Dequeue();
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
                {
                    EntityId = destroyedWeaponId
                });
            }
            destroyedWeapons.Dispose();
        }

        private void ProcessWeaponCollisions() {
            
            Entities.With(weaponCollisionQuery).ForEach((Entity entity, ref SpatialEntityId spatialEntityId, ref WeaponSchema.Weapon.Component weaponComponent, ref WeaponSchema.Damage.Component damageComponent,
                ref CollisionSchema.Collision.Component collisionComponent) =>
            {

                int currentHits = damageComponent.Hits;
                UnityEngine.Debug.Log("checking triggers of entity " + spatialEntityId.EntityId);

                foreach (KeyValuePair<EntityId, CollisionPoint> entityIdToCollision in collisionComponent.Triggers)
                {
                    // Later don't query worker to be in view, just use component update system
                    // to get snapshot.
                    if (workerSystem.TryGetEntity(entityIdToCollision.Value.CollidingWith, out Entity collidedEntity))
                    {

                        // If collidee not enemy, and what collision hit is enemy on respective client. This makes it so enemies not hitting each other 
                        // on other clients.
                        if (!EntityManager.HasComponent<Enemy>(entity) && EntityManager.HasComponent<Enemy>(collidedEntity))
                        {
                            UnityEngine.Debug.Log("collided with an enemy");
                            currentHits += 1;
                            // Send damage request to entity hit.
                            UnityEngine.Debug.Log($"Sending damage request to {entityIdToCollision.Value.CollidingWith}");
                            StatSchema.Stats.DamageEntity.Request request = new StatSchema.Stats.DamageEntity.Request
                            {
                                Payload = new StatSchema.DamageRequest
                                {
                                    Damage = weaponComponent.BaseDamage
                                },
                                TargetEntityId = entityIdToCollision.Key,
                            };

                            if (componentUpdateSystem.HasComponent(PointSchema.Point.ComponentId, entityIdToCollision.Key))
                            {
                                PointSchema.Point.Component pointComponent = EntityManager.GetComponentData<PointSchema.Point.Component>(collidedEntity);
                                var payload = new DamageRequestPayload
                                {
                                    weapon_id = spatialEntityId.EntityId,
                                    weaponComponent = weaponComponent,
                                    request = request,
                                    pointComponent = pointComponent
                                };
                                long requestId = commandSystem.SendCommand(request);
                                pendingDamageRequests.Add(requestId, payload);
                               // queuedDamagedRequests.Enqueue(payload);
                            }
                            else
                            {
                                UnityEngine.Debug.Log("no point component");
                            }
                        }
                    }
                }
                weaponIdToHitsThisFrame[spatialEntityId.EntityId] = currentHits;
            });
        }


        private void SendQueuedDamageRequests()
        {
            // Ideally I sum the total damge done
            while(queuedDamagedRequests.Count > 0)
            {
                var request = queuedDamagedRequests.Dequeue();
                if (componentUpdateSystem.HasComponent(PointSchema.Point.ComponentId, request.request.TargetEntityId))
                {
                    long requestId = commandSystem.SendCommand(request.request);
                    pendingDamageRequests.Add(requestId, request);
                }
                else
                {
                    UnityEngine.Debug.Log("no point component");
                }
            }
        }
        
        private void ProcessDamageRequestResponses()
        {
            var damageResponses = commandSystem.GetResponses<StatSchema.Stats.DamageEntity.ReceivedResponse>();
            for (int i = 0; i < damageResponses.Count; ++i)
            {
                UnityEngine.Debug.Log("Recieved damage response");
                ref readonly var damageResponse = ref damageResponses[i];
                if (pendingDamageRequests.TryGetValue(damageResponse.RequestId, out DamageRequestPayload requestSent))
                {
                    switch (damageResponse.StatusCode)
                    {
                        case Improbable.Worker.CInterop.StatusCode.Success:
                            UnityEngine.Debug.Log($"Applied damage to entity with id {requestSent.request.TargetEntityId}");
                            StatSchema.DamageResponse responsePayload = damageResponse.ResponsePayload.Value;
                            pendingDamageRequests.Remove(damageResponse.RequestId);
                            if (responsePayload.AlreadyDead)
                            {
                                // If was already dead before hit. Decrease amount of hits
                                if (weaponIdToHitsThisFrame.TryGetValue(requestSent.weapon_id, out int calculatedHits))
                                {
                                    weaponIdToHitsThisFrame[requestSent.weapon_id] = calculatedHits - 1;
                                }
                            }
                            else if (responsePayload.Killed)
                            {
                                WeaponSchema.Weapon.Component weaponComponent = requestSent.weaponComponent;
                                PointSchema.PointRequest pointRequestPayload = new PointSchema.PointRequest { PointUpdate = requestSent.pointComponent.Value };

                                workerSystem.TryGetEntity(weaponComponent.WielderId, out Entity wielderEntity);
                                if (EntityManager.HasComponent<Owner.Component>(wielderEntity))
                                {
                                    pointRequestPayload.EntityUpdating = EntityManager.GetComponentData<Owner.Component>(wielderEntity).OwnerId;
                                }
                                else
                                {
                                    pointRequestPayload.EntityUpdating = weaponComponent.WielderId;
                                }

                                UnityEngine.Debug.Log($"Adding points to entity {pointRequestPayload.EntityUpdating}");

                                pointRequestSystem.AddPointRequest(pointRequestPayload, OnGainKillPoints);
                            }
                            else
                            {
                                // We did damage.
                                UnityEngine.Debug.Log("We've dealt damage");
                            }

                            break;

                        case Improbable.Worker.CInterop.StatusCode.Timeout:
                            // If damage requests time out, more checks must be done before simply
                            // resending or just be ignored server side.
                            UnityEngine.Debug.Log("timed out");
                            break;
                        default:
                            UnityEngine.Debug.LogError(damageResponse.Message);
                            break;
                    }
                }
            }
        }

        private void OnGainKillPoints(PointSchema.Point.UpdatePoints.ReceivedResponse pointResponse)
        {
        }
    }
}