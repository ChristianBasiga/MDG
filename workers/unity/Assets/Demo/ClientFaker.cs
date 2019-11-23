using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For demo purposes, anything that doesn't yet have UI set up, will be activated through key strokes
/// these will invoke the same functions normally the buttons would. I should have been testing like this like I was at the start. Wtf is wrong with me
/// too obsessed with automatic / 'proper' testing and getting things done bullsiht.
/// </summary>
public class ClientFaker : MonoBehaviour
{
    LinkedEntityComponent linkedEntityComponent;
    // Start is called before the first frame update
    void Start()
    {
        linkedEntityComponent = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<LinkedEntityComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
