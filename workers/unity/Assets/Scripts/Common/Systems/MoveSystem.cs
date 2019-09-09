using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Improbable;
using Unity.Mathematics;
using Improbable.Gdk.Core;
using MdgSchema.Common;

namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class MoveSystem : ComponentSystem
    {
        EntityQuery authPosQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            authPosQuery = GetEntityQuery(
                ComponentType.ReadWrite<EntityTransform.Component>(),
                ComponentType.ReadOnly<EntityTransform.ComponentAuthority>()
            );
            authPosQuery.SetFilter(EntityTransform.ComponentAuthority.Authoritative);

        }
        protected override void OnUpdate()
        {
            bool movedForawrd = Input.GetKey(KeyCode.W);
            if (movedForawrd)
            {
                // Mayhaps only update actual position down line.
                Entities.With(authPosQuery).ForEach((ref EntityTransform.Component position) =>
                {
                    Debug.LogError("Moving forward");
                    position.Position = position.Position + new Vector3f(position.Position.X + 1, 0, 0);
                    //Vector3 vector3 = position.Coords.ToUnityVector();
                    //translation.Value = new float3(vector3.x, vector3.y, vector3.z);
                });
            }

        }
    }
}