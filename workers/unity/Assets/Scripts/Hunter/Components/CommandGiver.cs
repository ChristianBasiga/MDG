using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Hunter.Commands;
using Improbable.Gdk.Core;
using Unity.Mathematics;

namespace MDG.Hunter.Components
{
    // Command Giver only added as component
    // when gives a command.
    // It should include the command given.
    // then it will apply to all clickables that are clicked by this command giver.
    // much cleaner than now, so actual creation of commands will be done prior to iterating through clickables.
    [RemoveAtEndOfTick]
    public struct CommandGiver : IComponentData
    {
        public CommandMetadata commandMetadata;
    }

    // Should it be client only component?
    // or should select -> CommandGiver be jobified.
    // Replacing mouse input component. Uneccesarry.
    [RemoveAtEndOfTick]
    public struct Selection : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float3 Scale;
    }
}