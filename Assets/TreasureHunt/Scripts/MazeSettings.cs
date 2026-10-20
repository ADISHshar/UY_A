using UnityEngine;
using UnityEngine.Serialization;

namespace TreasureHunt
{
    [CreateAssetMenu(menuName = "Treasure Hunt/Maze Settings")]
    public sealed class MazeSettings : ScriptableObject
    {
        [Range(5, 31)] public int width = 11;
        [Range(5, 31)] public int height = 11;
        public int seed = 17421;
        [Range(3, 8)] public float cellSize = 4;
        [Range(0, 30)] public int traps = 10;
        [Range(0, 8)] public int guards = 3;
        [Range(1, 5)] public int lives = 3;
        [Range(2, 12)] public float sightRange = 7;
        [Range(30, 120)] public float sightAngle = 65;
        [Header("Keys and doors")]
        [Range(2,5)] public int doors = 3;
        [Header("Sound and hearing")]
        [Range(2,12)] public float sprintHearingRange = 5.5f;
        [Range(1,8)] public float investigateSeconds = 4;
        [Header("Hazards")]
        [Range(0,20)] public int lasers = 6;
        [Range(0,20)] public int wallSpikes = 5;
        [Min(1)] public float wallSpikeCycle = 2.1f;
        [FormerlySerializedAs("crushers"),Range(0,20)] public int collapsingTiles = 5;
        [Min(2)] public float spikeCycle = 4.2f;
        [Range(.3f,2)] public float spikeWarning = .65f;
        [FormerlySerializedAs("crusherDelay"),Range(.5f,3)] public float collapseDelay = 1.2f;
        [Min(.5f)] public float tileRespawnDelay = 4f;
        [Range(.5f,3)] public float damageImmunity = 1.25f;
    }
}
