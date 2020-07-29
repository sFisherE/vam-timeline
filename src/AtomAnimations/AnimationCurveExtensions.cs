using System;
using System.Runtime.CompilerServices;

namespace VamTimeline
{
    public static class AnimationCurveExtensions
    {
        #region Keyframes control

        public static void Reverse(this BezierAnimationCurve curve)
        {
            if (curve.length < 2) return;
            var currentLength = curve.GetKeyframe(curve.length - 1).time;

            var keys = curve.keys.ToList();

            while (curve.length > 0)
                curve.RemoveKey(0);

            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                curve.AddKey((currentLength - key.time).Snap(), key.value);
            }
        }

        #endregion

        #region Curves

        [MethodImpl(256)]
        public static int KeyframeBinarySearch(this BezierAnimationCurve curve, float time, bool returnClosest = false)
        {
            if (time == 0) return 0;
            if (time == curve.GetKeyframe(curve.length - 1).time) return curve.length - 1;
            var timeSmall = time - 0.0001f;
            var timeLarge = time + 0.0001f;

            var left = 0;
            var right = curve.length - 1;

            while (left <= right)
            {
                var middle = left + (right - left) / 2;

                var keyTime = curve.GetKeyframe(middle).time;
                if (keyTime > timeLarge)
                {
                    right = middle - 1;
                }
                else if (curve.GetKeyframe(middle).time < timeSmall)
                {
                    left = middle + 1;
                }
                else
                {
                    return middle;
                }
            }
            if (!returnClosest) return -1;
            if (left > right)
            {
                var tmp = left;
                left = right;
                right = tmp;
            }
            var avg = curve.GetKeyframe(left).time + ((curve.GetKeyframe(right).time - curve.GetKeyframe(left).time) / 2f);
            if (time - avg < 0) return left; else return right;
        }

        public static void ApplyCurveType(this BezierAnimationCurve curve, int key, string curveType, bool loop)
        {
            // TODO: Make this work again
            return;
            /*
            if (curveType == CurveTypeValues.LeaveAsIs) return;

            var keyframe = curve.GetKeyframe(key);
            VamKeyframe? before;
            if (key > 0)
                before = curve.GetKeyframe(key - 1);
            else if (loop && curve.length > 2)
                before = new VamKeyframe(curve.GetKeyframe(curve.length - 2).time - curve.GetKeyframe(curve.length - 1).time, curve.GetKeyframe(curve.length - 2).value);
            else
                before = null;
            VamKeyframe? next;
            if (key < curve.length - 1)
                next = curve.GetKeyframe(key + 1);
            else if (loop && curve.length > 2)
                next = new VamKeyframe(curve.GetKeyframe(curve.length - 1).time + curve.GetKeyframe(1).time, curve.GetKeyframe(1).value);
            else
                next = null;

            keyframe.weightedMode = WeightedMode.None;
            keyframe.inWeight = 0.333333f;
            keyframe.outWeight = 0.333333f;

            switch (curveType)
            {
                case null:
                case "":
                    return;
                case CurveTypeValues.Flat:
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = 0f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.FlatLong:
                    keyframe.weightedMode = WeightedMode.Both;
                    keyframe.inTangent = 0f;
                    keyframe.inWeight = 0.5f;
                    keyframe.outTangent = 0f;
                    keyframe.outWeight = 0.5f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Linear:
                    keyframe.inTangent = CalculateLinearTangent(before, keyframe);
                    keyframe.outTangent = CalculateLinearTangent(keyframe, next);
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Bounce:
                    // Increasing kinetic energy
                    keyframe.inTangent = CalculateLinearTangent(before, keyframe) * 2f;
                    // Lower coefficient of restitution
                    keyframe.outTangent = -keyframe.inTangent * 0.8f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Smooth:
                    curve.SmoothTangents(key, 0f);
                    break;
                case CurveTypeValues.LinearFlat:
                    keyframe.inTangent = CalculateLinearTangent(before, keyframe);
                    keyframe.outTangent = 0f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.FlatLinear:
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = CalculateLinearTangent(keyframe, next);
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Constant:
                    keyframe.inTangent = Mathf.Infinity;
                    keyframe.outTangent = Mathf.Infinity;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.CopyPrevious:
                    if (before != null)
                    {
                        keyframe.value = before.Value.value;
                        keyframe.inTangent = 0f;
                        keyframe.outTangent = 0f;
                        curve.MoveKey(key, keyframe);
                    }
                    break;
                default:
                    throw new NotSupportedException($"Curve type {curveType} is not supported");
            }
            */
        }

        public static void SmoothNeighbors(this BezierAnimationCurve curve, int key)
        {
            throw new NotImplementedException();
            // if (key == -1) return;
            // curve.SmoothTangents(key, 1f);
            // if (key > 0) curve.SmoothTangents(key - 1, 1f);
            // if (key < curve.length - 1) curve.SmoothTangents(key + 1, 1f);
        }

        public static void SmoothLoop(this BezierAnimationCurve curve)
        {
            // TODO: Not useful anymore?
//             if (curve.length == 0) return;

//             var keyframe = curve.GetKeyframe(0);

//             if (curve.length <= 2)
//             {
//                 keyframe.inTangent = 0f;
//                 keyframe.outTangent = 0f;
//             }
//             else
//             {
//                 var inTangent = CalculateLinearTangent(curve.GetKeyframe(curve.length - 2).value, keyframe.value, curve.GetKeyframe(curve.length - 2).time, curve.GetKeyframe(curve.length - 1).time);
//                 var outTangent = CalculateLinearTangent(keyframe, curve.GetKeyframe(1));
//                 var tangent = (inTangent + outTangent) / 2f;
//                 keyframe.inTangent = tangent;
//                 keyframe.outTangent = tangent;
//             }

//             keyframe.inWeight = 0.33f;
//             keyframe.outWeight = 0.33f;
//             curve.MoveKey(0, keyframe);

// keyframe = keyframe.Clone();
//             keyframe.time = curve.GetKeyframe(curve.length - 1).time;
//             curve.MoveKey(curve.length - 1, keyframe);
        }

        [MethodImpl(256)]
        public static float CalculateFixedMirrorTangent(VamKeyframe from, VamKeyframe to, float strength = 0.8f)
        {
            var tangent = CalculateLinearTangent(from, to);
            if (tangent > 0)
                return strength;
            else if (tangent < 0)
                return -strength;
            else
                return 0;
        }

        [MethodImpl(256)]
        public static float CalculateLinearTangent(VamKeyframe from, VamKeyframe to)
        {
            if (from == null || to == null) return 0f;
            return (float)((from.value - (double)to.value) / (from.time - (double)to.time));
        }

        [MethodImpl(256)]
        public static float CalculateLinearTangent(float fromValue, float toValue, float fromTime, float toTime)
        {
            return (float)((fromValue - (double)toValue) / (fromTime - (double)toTime));
        }

        #endregion

        #region Snapshots

        public static void SetKeySnapshot(this BezierAnimationCurve curve, float time, VamKeyframe keyframe)
        {
            if (curve.length == 0)
            {
                curve.AddKey(time, keyframe.value);
                return;
            }

            var index = curve.KeyframeBinarySearch(time);
            if (index == -1)
            {
                curve.AddKey(time, keyframe.value);
            }
            else
            {
                keyframe = keyframe.Clone();
                keyframe.time = time;
                curve.MoveKey(index, keyframe);
            }
        }

        #endregion
    }
}
