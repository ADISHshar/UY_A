using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class EditorReviewRunner
    {
        static readonly Queue<IEnumerator> queue=new Queue<IEnumerator>();
        static bool subscribed;
        public static void Enqueue(IEnumerator review)
        {
            queue.Enqueue(review);
            if(!subscribed) { subscribed=true; EditorApplication.update+=Tick; }
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(queue.Count==0) { subscribed=false;EditorApplication.update-=Tick;Debug.Log("UI AND MOTION PREVIEWS COMPLETE");return; }
            var review=queue.Peek();
            try { if(!review.MoveNext()) { (review as IDisposable)?.Dispose();queue.Dequeue(); } }
            catch(Exception error) { (review as IDisposable)?.Dispose();queue.Dequeue();Debug.LogException(error); }
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
