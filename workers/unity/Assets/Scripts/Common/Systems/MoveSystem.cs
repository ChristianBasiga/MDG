using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Improbable;
using Unity.Mathematics;
using Improbable.Gdk.Core;


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
                ComponentType.ReadWrite<Position.Component>(),
                ComponentType.ReadOnly<Position.ComponentAuthority>()
            );
            authPosQuery.SetFilter(Position.ComponentAuthority.Authoritative);

        }
        protected override void OnUpdate()
        {
            bool movedForawrd = Input.GetKey(KeyCode.W);
            if (movedForawrd)
            {
                // Mayhaps only update actual position down line.
                Entities.With(authPosQuery).ForEach((ref Position.Component position, ref Unity.Transforms.Translation translation) =>
                {
                        Debug.LogError("Moving forward");
                        position.Coords = new Coordinates(position.Coords.X + 1, position.Coords.Y, position.Coords.Z);
                        //Vector3 vector3 = position.Coords.ToUnityVector();
                        //translation.Value = new float3(vector3.x, vector3.y, vector3.z);
                });
            }

        }
    }
}