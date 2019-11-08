using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using WeaponSchema = MdgSchema.Common.Weapon;
using CollisionSchema = MdgSchema.Common.Collision;
using StatSchema = MdgSchema.Common.Stats;
using PointSchema = MdgSchema.Common.Point;
using MDG.Common.Components.Weapon;
using MdgSchema.Common.Collision;
using Improbable.Gdk.Core.Commands;
using Unity.Jobs;

namespace MDG.Common.Systems.Weapon
{
    // Should separate files into clientside adn server side to make that clear.
    // Cause that's essentially ALL in my head and in diagrams right now lmao.
    // This sits on client side btw. COULD sit on server side
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

        struct DamageRequestPayload
        {
            public EntityId weapon_id;
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

            ProcessDamageRequestResponses();
            ProcessWeaponCollisions();

            UpdateWeaponHitCountJob updateWeaponHitCountJob = new UpdateWeaponHitCountJob
            {
                idToDamage = weaponIdToHitsThisFrame,
                destroyedWeapons = destroyedWeapons.AsParallelWriter()
            };

            updateHitsJobHandle = updateWeaponHitCountJob.Schedule(updateWeaponHitCountQuery);

            updateHitsJobHandle.Complete();
            weaponIdToHitsThisFrame.Dispose();

            while (destroyedWeapons.Count > 0)
            {
                EntityId destroyedWeaponId = destroyedWeapons.Dequeue();
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
                {
                    EntityId = destroyedWeaponId
                });
            }
            destroyedWeapons.Dispose();
        }

        // Update UnitRerouteSystem later to also work off like this isntead of off events
        // entityQuery faster than my query yo.
        private void ProcessWeaponCollisions() {
            
            Entities.With(weaponCollisionQuery).ForEach((Entity entity, ref SpatialEntityId spatialEntityId, ref WeaponSchema.Weapon.Component weaponComponent, ref WeaponSchema.Damage.Component damageComponent,
                ref CollisionSchema.Collision.Component collisionComponent) =>
            {
                int currentHits = damageComponent.Hits;
                foreach (KeyValuePair<EntityId, CollisionPoint> entityIdToCollision in collisionComponent.Collisions)
                {
                    if (workerSystem.TryGetEntity(entityIdToCollision.Value.CollidingWith, out Entity collidedEntity))
                    {
                        // If collidee not enemy, and what collision hit is enemy on respective client. This makes it so enemies not hitting each other 
                        // on other clients.
                        if (!EntityManager.HasComponent<Enemy>(entity) && EntityManager.HasComponent<Enemy>(collidedEntity))
                        {
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
                            long requestId = commandSystem.SendCommand(request);
                            pendingDamageRequests.Add(requestId, new DamageRequestPayload
                            {
                                weapon_id = spatialEntityId.EntityId,
                                weaponComponent = weaponComponent,
                                request = request
                            });
                        }
                    }
                }
                weaponIdToHitsThisFrame[spatialEntityId.EntityId] = currentHits;
            });
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
                                workerSystem.TryGetEntity(requestSent.request.TargetEntityId, out Entity killedEntity);
                                PointSchema.Point.Component pointComponent = EntityManager.GetComponentData<PointSchema.Point.Component>(killedEntity);

                                // So 2 ways. Add points to unit, then that gets sent to invader
                                // or invader points is sum of all points of units?
                                // Or maybe instead make the wielder the invader.

                                pointRequestSystem.AddPointRequest(new PointSchema.PointRequest
                                {
                                    EntityUpdating = weaponComponent.WielderId,
                                    PointUpdate = pointComponent.Value
                                }, OnGainKillPoints);
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

        private void OnGainKillPoints(PointSchema.PointResponse pointResponse)
        {
        }
    }
}