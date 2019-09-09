using MDG.Common.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//  For now add here.
[CreateAssetMenu]
public class RenderInfo : ScriptableObject
{
    public Mesh mesh;
    public Material material;
    public int subMesh;
    public int layer;
    public bool receiveShadows;
    public ShadowCastingMode castShadows;
    public MeshTypes renderFor;
}
