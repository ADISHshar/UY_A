using UnityEngine;

namespace TreasureHunt
{
    [CreateAssetMenu(menuName="Treasure Hunt/Audio Bank")]
    public sealed class HuntAudioBank : ScriptableObject
    {
        public AudioClip[] playerSteps, guardSteps;
        public AudioClip pickup, unlock, locked, damage, victory, defeat, alert;
        public AudioClip spikes, laser, pressureClick, crusherWindup, crusherImpact, ambience;
    }
}
