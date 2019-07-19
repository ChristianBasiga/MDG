using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Improbable.Gdk.Subscriptions;
using Mdg.Player;

namespace MDG.Synchronization
{
    //Sends updates to other workers to update players they don't have authority over.
    public class PlayerSynchronizerWriter : MonoBehaviour
    {
        //Can send update of transform by use of events in controller.
        [Require] private PlayerTransformWriter playerTransformWriter;


        private void Start()
        {

            PlayerMove playerMove = GetComponent<PlayerMove>();
            playerMove.OnPlayerMove += SynchronizeTransforms;
        }



        //Will have more general one later.
        private void SynchronizeTransforms(Vector3 pos, Vector3 rot)
        {
            PlayerTransform.Update update = new PlayerTransform.Update
            {
                Position = Vector3f.FromUnityVector(pos),
                Rotation = Vector3f.FromUnityVector(rot)
            };

            playerTransformWriter.SendUpdate(update);
        }

    }
}