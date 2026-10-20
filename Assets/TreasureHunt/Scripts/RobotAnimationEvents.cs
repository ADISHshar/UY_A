using UnityEngine;
namespace TreasureHunt
{
    public sealed class RobotAnimationEvents : MonoBehaviour
    {
        public RobotAnimation driver;
        public void FootContact(AnimationEvent entry)
        {
            if(driver==null || entry.animatorClipInfo.weight<.45f) return;
            var clip=entry.animatorClipInfo.clip;
            driver.AnimationFootContact(entry.intParameter,clip!=null && clip.name.Contains("Run"));
        }
    }
}
