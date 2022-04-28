using System.Collections.Generic;
using System.Linq;

namespace VamTimeline
{
    public class AtomAnimationsClipsIndex
    {
        public class IndexedSegment
        {
            public readonly Dictionary<string, List<AtomAnimationClip>> layersMap = new Dictionary<string, List<AtomAnimationClip>>();
            public readonly List<List<AtomAnimationClip>> layers = new List<List<AtomAnimationClip>>();
            public readonly List<string> layerNames = new List<string>();

            public void Add(AtomAnimationClip clip)
            {
                List<AtomAnimationClip> layer;
                if (!layersMap.TryGetValue(clip.animationLayer, out layer))
                {
                    layer = new List<AtomAnimationClip>();
                    layersMap.Add(clip.animationLayer, layer);
                    layers.Add(layer);
                    layerNames.Add(clip.animationLayer);
                }
                layer.Add(clip);
            }
        }

        public IEnumerable<string> clipNames => _clipsByName.Keys;
        public string mainLayerNameQualified { get; private set; }

        private readonly List<AtomAnimationClip> _clips;

        public readonly Dictionary<string, IndexedSegment> segments = new Dictionary<string, IndexedSegment>();
        public readonly List<string> segmentNames = new List<string>();
        public readonly IList<List<AtomAnimationClip>> clipsGroupedByLayer = new List<List<AtomAnimationClip>>();
        private readonly Dictionary<string, List<AtomAnimationClip>> _clipsByLayerNameQualified = new Dictionary<string, List<AtomAnimationClip>>();
        private readonly Dictionary<string, List<AtomAnimationClip>> _clipsByName = new Dictionary<string, List<AtomAnimationClip>>();
        private readonly Dictionary<string, List<AtomAnimationClip>> _clipsBySet = new Dictionary<string, List<AtomAnimationClip>>();
        private readonly Dictionary<string, List<AtomAnimationClip>> _clipsBySetByLayer = new Dictionary<string, List<AtomAnimationClip>>();
        private readonly Dictionary<FreeControllerV3Ref, List<FreeControllerV3AnimationTarget>> _clipsByController = new Dictionary<FreeControllerV3Ref, List<FreeControllerV3AnimationTarget>>();
        private readonly Dictionary<JSONStorableFloatRef, List<JSONStorableFloatAnimationTarget>> _clipsByFloatParam = new Dictionary<JSONStorableFloatRef, List<JSONStorableFloatAnimationTarget>>();
        private readonly List<AtomAnimationClip> _emptyClipList = new List<AtomAnimationClip>();
        private bool _paused;

        public AtomAnimationsClipsIndex(List<AtomAnimationClip> clips)
        {
            _clips = clips;
        }

        public void StartBulkUpdates()
        {
            _paused = true;
        }

        public void EndBulkUpdates()
        {
            _paused = false;
            Rebuild();
        }

        public void Rebuild()
        {
            segments.Clear();
            clipsGroupedByLayer.Clear();
            _clipsByLayerNameQualified.Clear();
            _clipsByName.Clear();
            _clipsBySet.Clear();
            _clipsBySetByLayer.Clear();
            _clipsByController.Clear();
            _clipsByFloatParam.Clear();
            segmentNames.Clear();
            segmentNames.Add(AtomAnimationClip.DefaultAnimationSegment);

            if (_clips == null || _clips.Count == 0) return;

            mainLayerNameQualified = _clips[0].animationLayerQualified;

            foreach (var clip in _clips)
            {
                {
                    IndexedSegment sequence;
                    if (!segments.TryGetValue(clip.animationSegment, out sequence))
                    {
                        sequence = new IndexedSegment();
                        segments.Add(clip.animationSegment, sequence);
                        segmentNames.Add(clip.animationSegment);
                    }
                    sequence.Add(clip);
                }

                {
                    List<AtomAnimationClip> nameClips;
                    if (!_clipsByName.TryGetValue(clip.animationName, out nameClips))
                    {
                        nameClips = new List<AtomAnimationClip>();
                        _clipsByName.Add(clip.animationName, nameClips);
                    }
                    nameClips.Add(clip);
                }

                if(clip.animationSet != null)
                {
                    List<AtomAnimationClip> setClips;
                    if (!_clipsBySet.TryGetValue(clip.animationSet, out setClips))
                    {
                        setClips = new List<AtomAnimationClip>();
                        _clipsBySet.Add(clip.animationSet, setClips);
                    }
                    setClips.Add(clip);

                    #warning Review this
                    List<AtomAnimationClip> setByLayerClips;
                    if (!_clipsBySetByLayer.TryGetValue(clip.animationSetQualified, out setByLayerClips))
                    {
                        setByLayerClips = new List<AtomAnimationClip>();
                        _clipsBySetByLayer.Add(clip.animationSetQualified, setByLayerClips);
                    }
                    if (setByLayerClips.All(c => c.animationLayerQualified != clip.animationLayerQualified))
                        setByLayerClips.Add(clip);
                }
            }

            // TODO: Why?
            if (_paused) return;

            foreach (var clip in _clips)
            {
                {
                    List<AtomAnimationClip> layerClips;
                    if (!_clipsByLayerNameQualified.TryGetValue(clip.animationLayerQualified, out layerClips))
                    {
                        layerClips = new List<AtomAnimationClip>();
                        clipsGroupedByLayer.Add(layerClips);
                        _clipsByLayerNameQualified.Add(clip.animationLayerQualified, layerClips);
                    }
                    layerClips.Add(clip);
                }

                foreach (var target in clip.targetControllers)
                {
                    List<FreeControllerV3AnimationTarget> byController;
                    if (!_clipsByController.TryGetValue(target.animatableRef, out byController))
                    {
                        byController = new List<FreeControllerV3AnimationTarget>();
                        _clipsByController.Add(target.animatableRef, byController);
                    }
                    byController.Add(target);
                }

                foreach (var target in clip.targetFloatParams)
                {
                    List<JSONStorableFloatAnimationTarget> byFloatParam;
                    if (!_clipsByFloatParam.TryGetValue(target.animatableRef, out byFloatParam))
                    {
                        byFloatParam = new List<JSONStorableFloatAnimationTarget>();
                        _clipsByFloatParam.Add(target.animatableRef, byFloatParam);
                    }
                    byFloatParam.Add(target);
                }
            }
        }

        public IList<AtomAnimationClip> ByLayer(string layerNameQualified)
        {
            List<AtomAnimationClip> clips;
            return _clipsByLayerNameQualified.TryGetValue(layerNameQualified, out clips) ? clips : _emptyClipList;
        }

        public IEnumerable<KeyValuePair<FreeControllerV3Ref, List<FreeControllerV3AnimationTarget>>> ByController()
        {
            return _clipsByController;
        }

        public IEnumerable<KeyValuePair<JSONStorableFloatRef, List<JSONStorableFloatAnimationTarget>>> ByFloatParam()
        {
            return _clipsByFloatParam;
        }

        #warning Scope to sequence?
        public IList<AtomAnimationClip> ByName(string name)
        {
            List<AtomAnimationClip> clips;
            return _clipsByName.TryGetValue(name, out clips) ? clips : _emptyClipList;
        }

        #warning Delete?
        public IList<AtomAnimationClip> BySet(string set)
        {
            List<AtomAnimationClip> clips;
            return _clipsBySet.TryGetValue(set, out clips) ? clips : _emptyClipList;
        }

        public IList<AtomAnimationClip> GetSiblingsByLayer(AtomAnimationClip clip)
        {
            List<AtomAnimationClip> clips;
            var result = clip.animationSet != null
                ? _clipsBySetByLayer.TryGetValue(clip.animationSet, out clips)
                    ? clips
                    : _emptyClipList
                : ByName(clip.animationName);
            for (var i = 0; i < result.Count; i++)
            {
                if (result[i].animationLayerQualified != clip.animationLayerQualified) continue;
                result[i] = clip;
                break;
            }
            return result;
        }
    }
}
