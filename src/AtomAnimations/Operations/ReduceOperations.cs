﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VamTimeline
{
    public class ReduceSettings
    {
        public int fps;
        public bool avgToSnap;
        public bool simplify;
        public float minMeaningfulDistance;
        public float minMeaningfulRotation;
        public float minMeaningfulFloatParamRangeRatio;
    }

    public class ReduceOperations
    {
        public struct Progress
        {
            public float startTime;
            public float nowTime;
            public float stepsDone;
            public float stepsTotal;
            public float timeLeft => ((nowTime - startTime) / stepsDone) * (stepsTotal - stepsDone);
        }

        private readonly AtomAnimationClip _clip;
        private readonly ReduceSettings _settings;

        public ReduceOperations(AtomAnimationClip clip, ReduceSettings settings)
        {
            _clip = clip;
            _settings = settings;
        }

        public IEnumerator ReduceKeyframes(List<ICurveAnimationTarget> targets, Action<Progress> progress, Action callback)
        {
            SuperController.LogMessage($"Timeline: Reducing {targets.Count} targets. Please wait...");

            var steps = targets.Count;
            var startTime = Time.realtimeSinceStartup;
            var done = 0;

            foreach (var target in targets.OfType<FreeControllerAnimationTarget>())
            {
                var initialFrames = target.x.length;
                var initialTime = Time.realtimeSinceStartup;
                target.StartBulkUpdates();
                try
                {
                    var enumerator = Process(new ControllerTargetReduceProcessor(target, _settings));
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                    SuperController.LogMessage($"Timeline: Reduced {target.controller.name} from {initialFrames} frames to {target.x.length} frames in {Time.realtimeSinceStartup - initialTime:0.00}s");
                }
                finally
                {
                    target.dirty = true;
                    target.EndBulkUpdates();
                    progress?.Invoke(new Progress
                    {
                        startTime = startTime,
                        nowTime = Time.realtimeSinceStartup,
                        stepsTotal = steps,
                        stepsDone = ++done
                    });
                }
                yield return 0;
            }

            foreach (var target in targets.OfType<FloatParamAnimationTarget>())
            {
                var initialFrames = target.value.length;
                var initialTime = Time.realtimeSinceStartup;
                target.StartBulkUpdates();
                try
                {
                    var enumerator = Process(new FloatParamTargetReduceProcessor(target, _settings));
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                    SuperController.LogMessage($"Timeline: Reduced {target.GetShortName()} from {initialFrames} frames to {target.value.length} frames in {Time.realtimeSinceStartup - initialTime:0.00}s");
                }
                finally
                {
                    target.dirty = true;
                    target.EndBulkUpdates();
                    progress?.Invoke(new Progress
                    {
                        startTime = startTime,
                        nowTime = Time.realtimeSinceStartup,
                        stepsTotal = steps,
                        stepsDone = ++done
                    });
                }
                yield return 0;
            }

            callback?.Invoke();
        }

        public interface ITargetReduceProcessor
        {
            ICurveAnimationTarget target { get; }
            void Branch();
            void Commit();
            ReducerBucket CreateBucket(int from, int to);
            void CopyToBranch(int key);
            void AverageToBranch(float keyTime, int fromKey, int toKey);
        }

        public struct ReducerBucket
        {
            public int from;
            public int to;
            public int keyWithLargestDelta;
            public float largestDelta;
        }

        public abstract class TargetReduceProcessorBase<T> where T : class, ICurveAnimationTarget
        {
            public readonly T target;
            public readonly ReduceSettings settings;
            protected T branch;

            protected TargetReduceProcessorBase(T target, ReduceSettings settings)
            {
                this.target = target;
                this.settings = settings;
            }

            public void Branch()
            {
                branch = target.Clone(false) as T;
            }

            public void Commit()
            {
                target.RestoreFrom(branch);
                branch = null;
            }

            public virtual ReducerBucket CreateBucket(int from, int to)
            {
                return new ReducerBucket
                {
                    from = from,
                    to = to,
                    keyWithLargestDelta = -1
                };
            }
        }

        public class ControllerTargetReduceProcessor : TargetReduceProcessorBase<FreeControllerAnimationTarget>, ITargetReduceProcessor
        {
            ICurveAnimationTarget ITargetReduceProcessor.target => base.target;

            public ControllerTargetReduceProcessor(FreeControllerAnimationTarget target, ReduceSettings settings)
                : base(target, settings)
            {
            }

            public void CopyToBranch(int key)
            {
                var time = target.x.keys[key].time;
                branch.SetSnapshot(time, target.GetSnapshot(time));
                var branchKey = branch.x.KeyframeBinarySearch(time);
                branch.SmoothNeighbors(branchKey);
            }

            public void AverageToBranch(float keyTime, int fromKey, int toKey)
            {
                var position = Vector3.zero;
                var rotationCum = Vector4.zero;
                var firstRotation = target.GetKeyframeRotation(fromKey);
                var duration = target.x.GetKeyframeByKey(toKey).time - target.x.GetKeyframeByKey(fromKey).time;
                for (var key = fromKey; key < toKey; key++)
                {
                    var frameDuration = target.x.GetKeyframeByKey(key + 1).time - target.x.GetKeyframeByKey(key).time;
                    var weight = frameDuration / duration;
                    position += target.GetKeyframePosition(key) * weight;
                    QuaternionUtil.AverageQuaternion(ref rotationCum, target.GetKeyframeRotation(key), firstRotation, weight);
                }
                branch.SetKeyframe(keyTime, position, target.GetKeyframeRotation(fromKey), CurveTypeValues.SmoothLocal);

            }

            public override ReducerBucket CreateBucket(int from, int to)
            {
                var bucket = base.CreateBucket(from, to);
                for (var i = from; i <= to; i++)
                {
                    var time = target.x.keys[i].time;

                    var positionDiff = Vector3.Distance(
                        branch.EvaluatePosition(time),
                        target.EvaluatePosition(time)
                    );
                    var rotationAngle = Quaternion.Angle(
                        branch.EvaluateRotation(time),
                        target.EvaluateRotation(time)
                    );
                    // This is an attempt to compare translations and rotations
                    // TODO: Normalize the values, investigate how to do this with settings
                    var normalizedPositionDistance = settings.minMeaningfulDistance > 0 ? positionDiff / settings.minMeaningfulDistance : 1f;
                    var normalizedRotationAngle = settings.minMeaningfulRotation > 0 ? rotationAngle / settings.minMeaningfulRotation : 1f;
                    var delta = normalizedPositionDistance + normalizedRotationAngle;
                    if (delta > bucket.largestDelta)
                    {
                        bucket.largestDelta = delta;
                        bucket.keyWithLargestDelta = i;
                    }
                }
                return bucket;
            }
        }

        public class FloatParamTargetReduceProcessor : TargetReduceProcessorBase<FloatParamAnimationTarget>, ITargetReduceProcessor
        {
            ICurveAnimationTarget ITargetReduceProcessor.target => base.target;

            public FloatParamTargetReduceProcessor(FloatParamAnimationTarget target, ReduceSettings settings)
                : base(target, settings)
            {
            }


            public void CopyToBranch(int key)
            {
                var branchKey = branch.value.SetKeyframe(target.value.keys[key].time, target.value.keys[key].value, CurveTypeValues.SmoothLocal);
                branch.value.SmoothNeighbors(branchKey);
            }

            public void AverageToBranch(float keyTime, int fromKey, int toKey)
            {
                var timeSum = 0f;
                var valueSum = 0f;
                for (var key = fromKey; key < toKey; key++)
                {
                    var frame = target.value.GetKeyframeByKey(key);
                    valueSum += frame.value;
                    timeSum += target.value.GetKeyframeByKey(key + 1).time - frame.time;
                }
                branch.SetKeyframe(keyTime, valueSum / timeSum, false);
            }

            public override ReducerBucket CreateBucket(int from, int to)
            {
                var bucket = base.CreateBucket(from, to);
                for (var i = from; i <= to; i++)
                {
                    var time = target.value.keys[i].time;
                    // TODO: Normalize the delta values based on range
                    float delta;
                    if (settings.minMeaningfulFloatParamRangeRatio > 0)
                        delta = Mathf.Abs(
                            branch.value.Evaluate(time) -
                            target.value.Evaluate(time)
                        ) / (target.floatParam.max - target.floatParam.min) / settings.minMeaningfulFloatParamRangeRatio;
                    else
                        delta = 1f;
                    if (delta > bucket.largestDelta)
                    {
                        bucket.largestDelta = delta;
                        bucket.keyWithLargestDelta = i;
                    }
                }
                return bucket;
            }
        }

        protected IEnumerator Process(ITargetReduceProcessor processor)
        {
            var maxFramesPerSecond = (float) _settings.fps;
            var minFrameDistance = Mathf.Max(1f / maxFramesPerSecond, 0.001f);
            var animationLength = processor.target.GetLeadCurve().GetLastFrame().time;
            var maxIterations = (int)(animationLength * 10);

            // STEP 1: Average keyframes based on the desired FPS
            if (_settings.avgToSnap && maxFramesPerSecond <= 50)
            {
                var avgTimeRange = minFrameDistance / 2f;
                var lead = processor.target.GetLeadCurve();
                var toKey = 0;
                processor.Branch();
                SuperController.LogMessage($"{minFrameDistance:0.000} {animationLength:0.000}");
                for (var keyTime = 0f; keyTime <= animationLength; keyTime += minFrameDistance)
                {
                    var fromKey = toKey;
                    while (toKey < lead.length - 1 && lead.keys[toKey].time < keyTime + avgTimeRange)
                    {
                        toKey++;
                    }

                    if (toKey - fromKey > 0)
                        processor.AverageToBranch(keyTime.Snap(), fromKey, toKey);
                }
                processor.Commit();
            }

            // STEP 2: Apply to the curve, adjust end time

            // STEP 3: Run the buckets algorithm to find flat and linear curves (mostly flat ones)

            // STEP 4: Run the reduce algo

            if (_settings.simplify)
            {
                processor.Branch();

                var buckets = new List<ReducerBucket>
                {
                    processor.CreateBucket(1, processor.target.GetLeadCurve().length - 2)
                };

                for (var iteration = 0; iteration < maxIterations; iteration++)
                {
                    // Scan for largest difference with curve
                    var bucketWithLargestDelta = -1;
                    var keyWithLargestDelta = -1;
                    var largestDelta = 0f;
                    for (var bucketIndex = 0; bucketIndex < buckets.Count; bucketIndex++)
                    {
                        var bucket = buckets[bucketIndex];
                        if (bucket.largestDelta > largestDelta)
                        {
                            largestDelta = bucket.largestDelta;
                            keyWithLargestDelta = bucket.keyWithLargestDelta;
                            bucketWithLargestDelta = bucketIndex;
                        }
                    }

                    // Cannot find large enough diffs, exit
                    if (keyWithLargestDelta == -1) break;
                    if (largestDelta < 1f) break; // TODO: Configurable pos and rot weight, pos and rot min change inside bucket scan

                    processor.CopyToBranch(keyWithLargestDelta);

                    var bucketToSplitIndex = bucketWithLargestDelta;

                    if (bucketToSplitIndex > -1)
                    {
                        // Split buckets and exclude the scanned keyframe, we never have to scan it again.
                        var bucketToSplit = buckets[bucketToSplitIndex];
                        buckets.RemoveAt(bucketToSplitIndex);
                        if (bucketToSplit.to - keyWithLargestDelta + 1 > 2)
                            buckets.Insert(bucketToSplitIndex, processor.CreateBucket(keyWithLargestDelta + 1, bucketToSplit.to));
                        if (keyWithLargestDelta - 1 - bucketToSplit.from > 2)
                            buckets.Insert(bucketToSplitIndex, processor.CreateBucket(bucketToSplit.from, keyWithLargestDelta - 1));
                    }

                    yield return 0;
                }

                processor.Commit();
            }
        }
    }
}
