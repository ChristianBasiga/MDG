using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Common.Components;
// I should look more into NUnit and other testing frameworks to fully utilize this past the simple tests.
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MDG_Testing
{
    [TestFixture]
    [Category("Spawn System Tests")]
    public class SpawnSystemUnitTests : ECSTestsFixture
    {
      
        [UnityTest]
        IEnumerator TestCorrectEntitySpawned()
        {
            yield return new WaitForSeconds(10.0f);

            Button button = GameObject.Find("Hunter").GetComponent<Button>();
            button.OnPointerClick(null);
            button.OnSubmit(null);
            button.OnSelect(null);

        }
    }
}