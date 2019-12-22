using Improbable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.ScriptableObjects.Game
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public Vector3[] InvaderUnitSpawnPoints;
        public Vector3[] SpawnStructureSpawnPoints;
        public Vector3[] DefenderSpawnPoints;
        public Vector3 WorldDimensions;
        public Vector3 InvaderSpawnPoint;
        public int CapicityPerRegion;
        public float GameTime;
        public int MinimumPlayers;
    }
}