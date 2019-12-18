using MDG.Common.MonoBehaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureSynchronizer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<HealthSynchronizer>().OnHealthBarUpdated += OnHealthUpdated;
    }

    private void OnHealthUpdated(int pct)
    {
        if (pct <= 0)
        {
            Destroy(gameObject);
        }
    }

}
