using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace VamTimeline
{
    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public abstract class TargetFrameBase<T> : MonoBehaviour, ITargetFrame
        where T : IAnimationTargetWithCurves
    {
        protected readonly StyleBase style = new StyleBase();
        protected IAtomPlugin plugin;
        protected AtomAnimationClip clip;
        protected T target;
        protected UIDynamicToggle toggle;
        protected Text valueText;
        private GameObject _expand;
        private int _ignoreNextToggleEvent;
        private bool _expanded;
        private float _originalHeight;

        public UIDynamic Container => gameObject.GetComponent<UIDynamic>();

        public TargetFrameBase()
        {
        }

        public void Bind(IAtomPlugin plugin, AtomAnimationClip clip, T target)
        {
            this.plugin = plugin;
            this.clip = clip;
            this.target = target;

            CreateToggle(plugin);
            toggle.label = target.name;

            CreateCustom();

            valueText = CreateValueText();

            _expand = CreateExpand();
            var expandListener = _expand.AddComponent<Clickable>();
            expandListener.onClick.AddListener(pointerEvent => ToggleExpanded());

            this.plugin.animation.TimeChanged.AddListener(this.OnTimeChanged);
            OnTimeChanged(this.plugin.animation.Time);

            target.onAnimationKeyframesModified.AddListener(OnAnimationKeyframesModified);

            OnAnimationKeyframesModified();
        }

        private void ToggleExpanded()
        {
            var ui = GetComponent<UIDynamic>();
            var expandSize = 100f;
            if (!_expanded)
            {
                ui.height += expandSize;
                _expanded = true;
                _expand.GetComponent<Text>().text = "\u02C5";
            }
            else
            {
                ui.height -= expandSize;
                _expanded = false;
                _expand.GetComponent<Text>().text = "\u02C3";
            }
        }

        private void OnAnimationKeyframesModified()
        {
            SetTime(plugin.animation.Time, true);
        }

        private void CreateToggle(IAtomPlugin plugin)
        {
            if (toggle != null) return;

            var ui = Instantiate(plugin.manager.configurableTogglePrefab.transform);
            ui.SetParent(transform, false);

            toggle = ui.GetComponent<UIDynamicToggle>();

            var rect = ui.gameObject.GetComponent<RectTransform>();
            rect.StretchParent();

            toggle.backgroundImage.raycastTarget = false;

            var label = toggle.labelText;
            label.fontSize = 26;
            label.alignment = TextAnchor.UpperLeft;
            label.raycastTarget = false;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin += new Vector2(-3f, 0f);
            labelRect.offsetMax += new Vector2(0f, -5f);

            var checkbox = toggle.toggle.image.gameObject;
            var toggleRect = checkbox.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(0, 1);
            toggleRect.anchoredPosition = new Vector2(29f, -30f);

            ui.gameObject.SetActive(true);

            toggle.toggle.onValueChanged.AddListener(ToggleKeyframeInternal);
        }

        private void ToggleKeyframeInternal(bool on)
        {
            if (_ignoreNextToggleEvent > 0) return;
            ToggleKeyframe(on);
        }

        protected Text CreateValueText()
        {
            var go = new GameObject();
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(300f, 40f);
            rect.anchoredPosition = new Vector2(-150f - 48f, 20f - 54f);

            var text = go.AddComponent<Text>();
            text.alignment = TextAnchor.LowerRight;
            text.fontSize = 20;
            text.font = style.Font;
            text.color = style.FontColor;
            text.raycastTarget = false;

            return text;
        }

        private GameObject CreateExpand()
        {
            var go = new GameObject();
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(30f, 52f);
            rect.anchoredPosition += new Vector2(-22f, -30f);

            // var image = go.AddComponent<Image>();
            // image.color = new Color(0.95f, 0.95f, 0.98f);
            // image.raycastTarget = true;

            // var child = new GameObject();
            // child.transform.SetParent(go.transform, false);
            // child.AddComponent<RectTransform>().StretchParent();

            var text = go.AddComponent<Text>();
            text.font = style.Font;
            text.color = new Color(0.6f, 0.6f, 0.7f);
            text.text = "\u02C3";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 40;

            return go;
        }

        public void Update()
        {
            if (plugin.animation.IsPlaying())
                SetTime(plugin.animation.Time, false);
        }

        private void OnTimeChanged(float time)
        {
            SetTime(time, true);
        }

        public virtual void SetTime(float time, bool stopped)
        {
            if (stopped)
            {
                toggle.toggle.interactable = time > 0 && time < clip.animationLength;
                SetToggle(target.GetLeadCurve().KeyframeBinarySearch(time) != -1);
            }
            else
            {
                toggle.toggle.interactable = false;
                if (valueText.text != "")
                    valueText.text = "";
            }
        }

        protected void SetToggle(bool on)
        {
            if (toggle.toggle.isOn == on) return;
            Interlocked.Increment(ref _ignoreNextToggleEvent);
            try
            {
                toggle.toggle.isOn = on;
            }
            finally
            {
                Interlocked.Decrement(ref _ignoreNextToggleEvent);
            }
        }

        protected abstract void CreateCustom();
        public abstract void ToggleKeyframe(bool enable);

        public void OnDestroy()
        {
            plugin.animation.TimeChanged.RemoveListener(OnTimeChanged);
            target.onAnimationKeyframesModified.RemoveListener(OnAnimationKeyframesModified);
        }
    }
}
