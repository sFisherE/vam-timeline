using System.Collections.Generic;

namespace VamTimeline
{
    public class OptionsScreen : ScreenBase
    {
        public const string ScreenName = "Settings";
        private JSONStorableBool _lockedJSON;
        private JSONStorableBool _syncWithPeersJSON;
        private JSONStorableBool _syncSubsceneOnlyJSON;
        private JSONStorableFloat _snapJSON;
        private JSONStorableBool _autoKeyframeAllControllersJSON;
        private JSONStorableBool _showPaths;
        private JSONStorableBool _ignoreSequencingJSON;
        private JSONStorableStringChooser _serializationModeJSON;

        public override string screenId => ScreenName;

        #region Init

        public override void Init(IAtomPlugin plugin, object arg)
        {
            base.Init(plugin, arg);

            // Right side

            CreateChangeScreenButton("<b><</b> <i>Back</i>", MoreScreen.ScreenName);

            InitLockedUI();

            prefabFactory.CreateHeader("Playback Options", 1);

            InitUseRealTimeUI();
            InitLiveParenting();
            InitForceBlendInTime();

            InitDisableSync();
            InitIgnoreSequencing();
#if (VAM_GT_1_20)
            if (!ReferenceEquals(plugin.containingAtom.containingSubScene, null))
                InitSyncSubsceneOnly();
#endif

            prefabFactory.CreateHeader("Edit Options", 1);

            InitSnapUI();

            InitAutoKeyframeUI();

            prefabFactory.CreateHeader("UI Options", 1);

            InitShowPathsUI();

            prefabFactory.CreateHeader("Serialization Options", 1);

            InitSerializationModeUI();

            animationEditContext.onEditorSettingsChanged.AddListener(OnEditorSettingsChanged);
        }

        private void InitLockedUI()
        {
            _lockedJSON = new JSONStorableBool("Lock (ignore in-game edits)", animationEditContext.locked, val => animationEditContext.locked = val);
            prefabFactory.CreateToggle(_lockedJSON);
        }

        private void InitUseRealTimeUI()
        {
            var timeModes = new List<string> {TimeModes.UnityTime.ToString(), TimeModes.RealTime.ToString()};
            var timeModeDisplay = new List<string>
            {
                "Game time (slows with low fps)",
                "Real time (better for audio sync)"
            };
            if (animation.timeMode == TimeModes.RealTimeLegacy)
            {
              timeModes.Add(TimeModes.RealTimeLegacy.ToString());
              timeModeDisplay.Add("Real time (legacy, only use for old scenes)");
            }
            var timeTypeJSON = new JSONStorableStringChooser("Time mode", timeModes, TimeModes.UnityTime.ToString(), "Time mode")
            {
                displayChoices = timeModeDisplay,
                valNoCallback = animation.timeMode.ToString(),
                setCallbackFunction = val => animation.timeMode = int.Parse(val)
            };
            prefabFactory.CreatePopup(timeTypeJSON, true, true);
        }

        private void InitForceBlendInTime()
        {
            var forceBlendTimeJSON = new JSONStorableBool("Ignore blend speed", animation.forceBlendTime, val => animation.forceBlendTime = val);
            prefabFactory.CreateToggle(forceBlendTimeJSON);
        }

        private void InitLiveParenting()
        {
            var liveParentingJSON = new JSONStorableBool("Always-on parenting", animation.liveParenting, val => animation.liveParenting = val);
            prefabFactory.CreateToggle(liveParentingJSON);
        }

        private void InitDisableSync()
        {
            _syncWithPeersJSON = new JSONStorableBool("Sync with other atoms", animation.syncWithPeers, val => animation.syncWithPeers = val);
            prefabFactory.CreateToggle(_syncWithPeersJSON);
        }

        private void InitSyncSubsceneOnly()
        {
            _syncSubsceneOnlyJSON = new JSONStorableBool("Send sync in subscene only", animation.syncSubsceneOnly, val => animation.syncSubsceneOnly = val);
            prefabFactory.CreateToggle(_syncSubsceneOnlyJSON);
        }

        private void InitSnapUI()
        {
            _snapJSON = new JSONStorableFloat("Snap", 0.001f, val => animationEditContext.snap = val.Snap(), 0.1f, 1f)
            {
                valNoCallback = animationEditContext.snap
            };
            var snapUI = prefabFactory.CreateSlider(_snapJSON);
            snapUI.valueFormat = "F3";
        }

        private void InitAutoKeyframeUI()
        {
            _autoKeyframeAllControllersJSON = new JSONStorableBool("Keyframe all controllers at once", animationEditContext.autoKeyframeAllControllers, val => animationEditContext.autoKeyframeAllControllers = val);
            prefabFactory.CreateToggle(_autoKeyframeAllControllersJSON);
        }

        private void InitShowPathsUI()
        {
            _showPaths = new JSONStorableBool(
                "Show selected controllers paths", animationEditContext.showPaths,
                val => animationEditContext.showPaths = val);
            prefabFactory.CreateToggle(_showPaths);
        }

        private void InitIgnoreSequencing()
        {
            _ignoreSequencingJSON = new JSONStorableBool("Disable sequencing", animation.pauseSequencing, val => animation.pauseSequencing = val);
            prefabFactory.CreateToggle(_ignoreSequencingJSON);
        }

        private void InitSerializationModeUI()
        {
            _serializationModeJSON = new JSONStorableStringChooser("SerializationMode",new List<string>
            {
                "0",
                "1"
            }, "Optimized", "Serialization Mode")
            {
                displayChoices = new List<string>
                {
                    "Readable (easier to edit)",
                    "Optimized (smaller file)",
                },
                setCallbackFunction = val => animation.serializeMode = int.Parse(val),
                valNoCallback = animation.serializeMode.ToString()
            };
            prefabFactory.CreatePopup(_serializationModeJSON, false, false, upwards: true, popupPanelHeight: 180f);
        }

        #endregion

        private void OnEditorSettingsChanged(string _)
        {
            _lockedJSON.valNoCallback = animationEditContext.locked;
            _snapJSON.valNoCallback = animationEditContext.snap;
            _autoKeyframeAllControllersJSON.valNoCallback = animationEditContext.autoKeyframeAllControllers;
        }
    }
}

