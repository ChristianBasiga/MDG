using UnityEngine;
using MDG.Common.Components;
using UnityEngine.Rendering;
using Unity.Rendering;
using System.Linq;
namespace MDG.Common.MonoBehaviours
{


    public class MeshFactory : MonoBehaviour
    {
        RenderInfo[] meshesCanRender;
        static MeshFactory instance;

        public static MeshFactory Instance { get {


                return instance;

        } }


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }

        private void Start()
        {
            meshesCanRender = Resources.LoadAll<RenderInfo>("ScriptableObjects");
            // Debug this later.
        }

        public RenderMesh GetMeshRender(MeshTypes meshType)
        {
            RenderInfo renderInfo = meshesCanRender.FirstOrDefault((RenderInfo r) => { return r.renderFor == meshType; });
            if (renderInfo == null)
            {
                throw new System.Exception($"No render info exists for mesh type {meshType.ToString()}.");
            }

            return new RenderMesh
            {
                mesh = renderInfo.mesh,
                material = renderInfo.material,
                layer = renderInfo.layer,
                subMesh = renderInfo.subMesh,
                receiveShadows = renderInfo.receiveShadows,
                castShadows = renderInfo.castShadows
            };
        }

    }
}