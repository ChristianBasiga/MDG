using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public class Testing : MonoBehaviour
{
    // Start is called before the first frame update
    public Mesh mesh;
    public Material material;
    Entity entity;
    void Start()
    {
        /*
        EntityManager entityManager = World.Active.EntityManager;
        entity = entityManager.CreateEntity(typeof(Translation), typeof(LocalToWorld), typeof(RenderMesh), typeof (Improbable.Position.Component), typeof(Scale));
        entityManager.SetSharedComponentData<RenderMesh>(entity, new RenderMesh
        {
            mesh = mesh,
            material = material
        });
        entityManager.SetComponentData(entity, new Scale { Value = 50.0f });*/
      //  World.Active.GetOrCreateSystem<MoveSystem>();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
