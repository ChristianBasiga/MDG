using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Subscriptions;
using MDG.ClientSide.UserInterface;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class StatSystemTests
    {
        [UnityTest, Order(1)]
        public IEnumerator SceneValidation()
        {
            SceneManager.LoadScene("DevelopmentScene");
            GameObject uiManager = null;

            yield return new WaitUntil(() =>
            {
                uiManager = GameObject.Find("ClientWorker");
                return uiManager != null;

            });
            yield return new WaitForSeconds(2.0f);
            
            uiManager.GetComponent<UIManager>().SelectRole("Hunter");
            yield return new WaitUntil(() =>
            {
                return GameObject.Find("Hunter_Spawned") != null && GameObject.FindGameObjectWithTag("Unit") != null;
            });
            
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest, Order(2)]
        public IEnumerator StatPanelUpdatedCorrectly()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
