using UnityEngine;
using System.Collections;
using System.Linq;
using VamTimeline.Tests.Framework;

namespace VamTimeline.Tests.Specs
{
    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public class AnimationTests
    {
        public IEnumerable EmptyAnimation(TestContext context)
        {
            context.Assert(context.animation.clips.Count, 1, "Only one clip");
            context.Assert(context.animation.state.clips.Count, 1, "Only one clip state");
            context.animation.PlayAll();
            yield return 0f;
            context.Assert(context.animation.state.isPlaying, "Play should set isPlaying to true");
            context.Assert(context.animation.state.clips[0].enabled, "Clips is enabled");
            context.animation.StopAll();
            yield return 0f;
            context.Assert(!context.animation.state.isPlaying, "Stop should set isPlaying to false");
            context.Assert(!context.animation.state.clips[0].enabled, "Clip is disabled");
        }
    }
}