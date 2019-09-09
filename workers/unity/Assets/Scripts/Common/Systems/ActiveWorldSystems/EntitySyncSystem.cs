using Unity.Entities;
using MDG.Common.Components;
using Improbable.Gdk.Core;
using Unity.Collections;
using Improbable;
using Unity.Transforms;
using Unity.Rendering;
using MDG.Common.MonoBehaviours;
using UnityEngine;
using Unity.Jobs;
using MdgSchema.Units;

namespace MDG.Common.Systems
{
    /// Later, move to this to act in ActiveWorld, and just query client world, so can jobify everything.
    /// query client world for job, no jobifying for now
    /// <summary>
    /// Syncing renderable entities to be stored in active world with those that exist in client world.
    /// </summary>
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class EntitySyncSystem : ComponentSystem
    {

        JobHandle entitySyncJobHandle;
        JobHandle movementSyncJobHandle;
        NativeHashMap<EntityId, Entity> entityIdToRenderedEntities;
        EntityQuery pendingRenderQuery;
        EntityQuery translationSyncGroup;
        //Move this to archtypes file under common later.
        EntityArchetype renderableEntityArchtype;
        EndSimulationEntityCommandBufferSystem simulationEntityCommandBufferSystem;
        // I wish I could inject values in systems
        MeshFactory meshFactory;
        World clientWorld;

        // Should actually
        public struct TransformPositionSyncJob : IJobForEachWithEntity<SpatialEntityId, Improbable.Position.Component, Rendered>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, Entity> entityIdToRenderedEntities;
            [ReadOnly]
            public EntityCommandBuffer.Concurrent commandBuffer;

            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId id, [ReadOnly] [ChangedFilter] ref Improbable.Position.Component position, [ReadOnly] ref Rendered rendered)
            {
                if (entityIdToRenderedEntities.TryGetValue(id.EntityId, out Entity renderedEntity)) {
                    
                    Vector3 vector3 = position.Coords.ToUnityVector();
                    Debug.LogError(vector3);
                    // So it's not setting it for some reason.
                    Debug.LogError(renderedEntity != Entity.Null);
                    commandBuffer.SetComponent(jobIndex, renderedEntity, new Translation {
                        Value = new Unity.Mathematics.float3(vector3.x, vector3.y, vector3.z)
                    });
                    //commandBuffer.SetComponent(jobIndex, renderedEntity, new Translation { Value = translation.Value });
                }
            }
        }

        public struct EntitySyncJob : IJobForEachWithEntity<SpatialEntityId, RenderPending, Improbable.Position.Component>
        {
            public NativeHashMap<EntityId, Entity>.ParallelWriter entityIdToRenderedEntities;
            [ReadOnly]
            public EntityCommandBuffer.Concurrent activeCommandBuffer;
            [ReadOnly]
            public EntityArchetype renderArchType;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref RenderPending renderPending, [ReadOnly] ref Improbable.Position.Component position)
            {
                Entity renderedEntity = activeCommandBuffer.CreateEntity(jobIndex, renderArchType);
                entityIdToRenderedEntities.TryAdd(spatialEntityId.EntityId, renderedEntity);

                try
                {
                    RenderMesh renderInfo = MeshFactory.Instance.GetMeshRender(renderPending.meshToRender);
                    // Render pending will have initial mesh, location or let it be set later on? Hmm I don't want them spawned in wrong place for even a frame.
                    // Position Sync will have change filter, so chance that applying that will skip initial step.
                    activeCommandBuffer.SetSharedComponent(jobIndex, renderedEntity, renderInfo);
                    Vector3 vector3 = position.Coords.ToUnityVector();
                    activeCommandBuffer.SetComponent(jobIndex, renderedEntity, new Unity.Transforms.Translation
                    {
                        Value = new Unity.Mathematics.float3(vector3.x, vector3.y, vector3.z)
                    });
                    activeCommandBuffer.SetComponent(jobIndex, renderedEntity, new Scale { Value = 50.0f });
                }
                catch (System.Exception err)
                {
                    UnityEngine.Debug.LogError(err.Message);
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            pendingRenderQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                // Render Pending, this was it, this is what broke it, truly.
                // I'm still in the same issue.
                // 
                //ComponentType.ReadOnly<RenderPending>(),
                ComponentType.ReadOnly<Unit.Component>(),
                ComponentType.ReadOnly<Improbable.Position.Component>()
            );
            translationSyncGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Rendered>(),
                ComponentType.ReadOnly<Translation>()
            );
            translationSyncGroup.SetFilterChanged(typeof(Translation));

            //Values for this archtype are needed in render pending
            renderableEntityArchtype = World.Active.EntityManager.CreateArchetype(
                typeof(RenderMesh),
                typeof(Translation),
                typeof(LocalToWorld),
                typeof(Scale)
            );

            // Inject this later
            meshFactory = GameObject.Find("MeshFactory").GetComponent<MeshFactory>();
            clientWorld = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().Worker.World;
            simulationEntityCommandBufferSystem = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            entityIdToRenderedEntities = new NativeHashMap<EntityId, Entity>(10, Allocator.Persistent);
        }

        public void DestroyEntity(EntityId entityId)
        {
            if (entityIdToRenderedEntities.TryGetValue(entityId, out Entity entity))
            {
                World.Active.EntityManager.DestroyEntity(entity);
                entityIdToRenderedEntities.Remove(entityId);
            }
        }

        protected override void OnDestroy()
        {
            entityIdToRenderedEntities.Dispose();
            base.OnDestroy();
        }
        
        protected override void OnUpdate()
        {

            // Main thread not threaded for this si fine, for now. I guess. Can't do as job until I figure that out.

            // This is fine not jobified, for now. Cause won't be able to access factory. I'll make it a singleton later.
            /*
            Entities.With(pendingRenderQuery).ForEach(( Entity entity, ref SpatialEntityId spatialEntityId, ref Improbable.Position.Component position) =>
            {
                // When would it ever NOT contain the fucking key.
                if (!entityIdToRenderedEntities.ContainsKey(spatialEntityId.EntityId))
                {
                    EntityManager activeEntityManager = World.Active.EntityManager;
                    Entity renderedEntity = activeEntityManager.CreateEntity(renderableEntityArchtype);
                    bool added = entityIdToRenderedEntities.TryAdd(spatialEntityId.EntityId, renderedEntity);
                    try
                    {
                        RenderMesh renderInfo = meshFactory.GetMeshRender(MeshTypes.UNIT);
                        // Render pending will have initial mesh, location or let it be set later on? Hmm I don't want them spawned in wrong place for even a frame.
                        // Position Sync will have change filter, so chance that applying that will skip initial step.
                        activeEntityManager.SetSharedComponentData(renderedEntity, renderInfo);
                        Vector3 vector3 = position.Coords.ToUnityVector();
                        // Here is core, position should be with respect to it, not keep resetting.
                        activeEntityManager.SetComponentData(renderedEntity, new Unity.Transforms.Translation
                        {
                            Value = new Unity.Mathematics.float3(vector3.x, vector3.y, vector3.z)
                        });
                        activeEntityManager.SetComponentData(renderedEntity, new Scale { Value = 50.0f });
                        PostUpdateCommands.AddComponent<Rendered>(entity);
                    }
                    catch (System.Exception err)
                    {
                        UnityEngine.Debug.LogError(err.Message);
                    }
                }
                
            });*/
            TransformPositionSyncJob transformPositionSyncJob = new TransformPositionSyncJob
            {
                entityIdToRenderedEntities = entityIdToRenderedEntities,
                commandBuffer = simulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            transformPositionSyncJob.Schedule(this).Complete();
            
            
            // Ideally I want to apply changed filter but this is fine.
            // Unfortuanetly can't do as job. Again Unless I get reference to client world. Hmm, I COULD  do that.

            /*
            EntityCommandBuffer entityCommandBuffer = simulationEntityCommandBufferSystem.CreateCommandBuffer();
            EntityCommandBuffer.Concurrent concurrent = entityCommandBuffer.ToConcurrent();

            EntitySyncJob entitySyncJob = new EntitySyncJob
            {
                entityIdToRenderedEntities = entityIdToRenderedEntities.AsParallelWriter(),
                activeCommandBuffer = concurrent,
                renderArchType = renderableEntityArchtype
            };
            entitySyncJobHandle = entitySyncJob.Schedule(this);
            entitySyncJobHandle.Complete();
            entityCommandBuffer.Playback(simulationEntityCommandBufferSystem.EntityManager);
            */
            /*
            Entities.With(translationSyncGroup).ForEach((ref SpatialEntityId spatialEntityId, ref Translation position) =>
            {
                Debug.LogError("here");
                if (entityIdToRenderedEntities.TryGetValue(spatialEntityId.EntityId, out Entity entity))
                {
                    World.Active.EntityManager.SetComponentData(entity, new Unity.Transforms.Translation
                    {
                        Value = position.Value
                    });
                }
            });*/
        }
        
        /*
         * 
         * Re-do these but entityquery jobs and have this system run in active world.
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!entityIdToRenderedEntities.IsCreated) return inputDeps;
            EntityCommandBuffer entityCommandBuffer = simulationEntityCommandBufferSystem.CreateCommandBuffer();
            EntitySyncJob entitySyncJob = new EntitySyncJob
            {
                entityIdToRenderedEntities = entityIdToRenderedEntities.AsParallelWriter(),
                activeCommandBuffer = entityCommandBuffer.ToConcurrent(),
                renderArchType = renderableEntityArchtype
            };
            entitySyncJobHandle = entitySyncJob.Schedule(this);
            entitySyncJobHandle.Complete();

            entityCommandBuffer.Playback(simulationEntityCommandBufferSystem.EntityManager);
            TransformPositionSyncJob transformPositionSyncJob = new TransformPositionSyncJob
            {
                entityIdToRenderedEntities = entityIdToRenderedEntities,
                commandBuffer = entityCommandBuffer.ToConcurrent()
            };
            movementSyncJobHandle = transformPositionSyncJob.Schedule(this);
            movementSyncJobHandle.Complete();
            return movementSyncJobHandle;
        }*/
    }
}