using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Player;


namespace MDG.Synchronization
{
    //This would be attached to the non authoritative entities.
    public class PlayerSynchronizerReader : MonoBehaviour
    {

        [Require] private PlayerTransformReader playerTransformReader;
        // Start is called before the first frame update
        void Start()
        {
            playerTransformReader.OnUpdate += SynchronizePlayer;
        }

        void SynchronizePlayer(PlayerTransform.Update update)
        {

            transform.position = update.Position.Value.ToUnityVector();
            transform.rotation = Quaternion.Euler(update.Rotation.Value.ToUnityVector());

        }
    }
}