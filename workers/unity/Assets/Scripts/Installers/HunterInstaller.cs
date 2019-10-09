using MDG.Hunter.Monobehaviours;
using UnityEngine;
using System.Collections.Generic;
using Zenject;
using MdgSchema.Units;
using Unity.Rendering;

namespace MDG.Installers {
    public class HunterInstaller : MonoInstaller<HunterInstaller>
    {
        Camera mainSceneCamera;
        public Mesh mesh;
        public Material material;
        Dictionary<UnitTypes, RenderMesh> unitTypesToRenderMesh;
        
        // Do DI later, not neccesarry.
        /*
        public override void InstallBindings()
        {
            /*
            Container.Bind<CameraController>().FromNewComponentOnNewGameObject().AsSingle();
              Container.Bind<Camera>().FromInstance(GetSceneCamera()).WhenInjectedInto<CameraController>();
            Container.Bind<CameraController.Settings>().AsSingle().WithArguments(
                new Vector2(Screen.width * 0.2f, Screen.height * 0.2f),
                new Vector2(100, 100),
                10.0f,
                100.0f
            ).WhenInjectedInto<CameraController>().Lazy();
            
            //Install bindings for unit creation.
           // Container.Bind<Dictionary<UnitTypes, RenderMesh>>().FromInstance(GetMeshesForUnits()).AsSingle().WhenInjectedInto<UnitCreationRequestSystem>();

        }

        private Dictionary<UnitTypes, RenderMesh> GetMeshesForUnits()
        {
            if (unitTypesToRenderMesh == null)
            {
                //Down the road, load from perhaps scriptable objects / resources.
                unitTypesToRenderMesh = new Dictionary<UnitTypes, RenderMesh>()
                {
                    {
                        UnitTypes.WORKER,
                        new RenderMesh{
                            mesh = mesh,
                            material = material
                        }
                    }
                };
            }
            return unitTypesToRenderMesh;
        }

        private Camera GetSceneCamera()
        {
            if (mainSceneCamera == null)
            {
                GameObject gameObject = GameObject.Find("HunterCamera");
                mainSceneCamera = gameObject.GetComponent<Camera>();
            }
            return mainSceneCamera;
        }
    */
    }
}