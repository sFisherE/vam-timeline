using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VamTimeline
{
    public class ScreensManager : MonoBehaviour
    {
        public class ScreenChangedEvent : UnityEvent<string> { }

        public static ScreensManager Configure(GameObject go)
        {
            var content = VamPrefabFactory.CreateScrollRect(go);
            return content.gameObject.AddComponent<ScreensManager>();
        }

        public readonly ScreenChangedEvent onScreenChanged = new ScreenChangedEvent();
        private IAtomPlugin _plugin;
        private ScreenBase _current;
        private bool _uiRefreshScheduled;
        private bool _uiRefreshInProgress;
        private bool _uiRefreshInvalidated;
        private string _currentScreen;
        private string _defaultScreen;
        private Coroutine _uiRefreshCoroutine;

        public void Bind(IAtomPlugin plugin)
        {
            _plugin = plugin;
        }

        public void ChangeScreen(string screen)
        {
            _currentScreen = _defaultScreen = screen;
            _plugin.lockedJSON.val = screen == PerformanceScreen.ScreenName;
            if (!enabled) return;
            onScreenChanged.Invoke(screen);
            RefreshCurrentUI();
        }

        private List<string> ListAvailableScreens()
        {
            var list = new List<string>();
            if (_plugin.animation == null || _plugin.animation.current == null) return list;
            return list;
        }

        public string GetDefaultScreen()
        {
            if (_defaultScreen != null)
                return _defaultScreen;
            else if (_plugin?.animation == null || _plugin?.lockedJSON?.val == true)
                return PerformanceScreen.ScreenName;
            else
                return TargetsScreen.ScreenName;
        }

        public void UpdateLocked(bool isLocked)
        {
            if (isLocked)
            {
                if (_currentScreen != PerformanceScreen.ScreenName)
                    ChangeScreen(PerformanceScreen.ScreenName);
            }
            else
            {
                if (_currentScreen == PerformanceScreen.ScreenName)
                    ChangeScreen(GetDefaultScreen());
            }
        }

        public void RefreshCurrentUI()
        {
            if (_plugin.animation == null) return;

            if (_uiRefreshInProgress)
                _uiRefreshInvalidated = true;
            else if (!_uiRefreshScheduled)
                _uiRefreshCoroutine = StartCoroutine(RefreshCurrentUIDeferred(_currentScreen));
        }

        private IEnumerator RefreshCurrentUIDeferred(string screen)
        {
            _uiRefreshScheduled = true;

            // Let every event trigger a UI refresh
            yield return 0;

            _uiRefreshScheduled = false;

            // Cannot proceed
            if (_plugin == null || _plugin.animation == null || _plugin.animation.current == null) yield break;

            // Same UI, just refresh
            if (_current != null && _current.screenId == screen)
            {
                _uiRefreshCoroutine = null;
                yield break;
            }

            // UI Change
            _uiRefreshInProgress = true;

            // Dispose previous
            if (_current != null)
            {
                try
                {
                    Destroy(_current.gameObject);
                }
                catch (Exception exc)
                {
                    SuperController.LogError($"VamTimeline.{nameof(ScreensManager)}.{nameof(RefreshCurrentUIDeferred)} (while removing {_current.screenId}): {exc}");
                }

                _current = null;
            }

            yield return 0;

            var screenContainer = CreateScreenContainer();

            switch (screen)
            {
                case SettingsScreen.ScreenName:
                    _current = screenContainer.AddComponent<SettingsScreen>();
                    break;
                case AddRemoveTargetsScreen.ScreenName:
                    _current = screenContainer.AddComponent<AddRemoveTargetsScreen>();
                    break;
                case TargetsScreen.ScreenName:
                    _current = screenContainer.AddComponent<TargetsScreen>();
                    break;
                case AnimationsScreen.ScreenName:
                    _current = screenContainer.AddComponent<AnimationsScreen>();
                    break;
                case BulkScreen.ScreenName:
                    _current = screenContainer.AddComponent<BulkScreen>();
                    break;
                case AdvancedKeyframeToolsScreen.ScreenName:
                    _current = screenContainer.AddComponent<AdvancedKeyframeToolsScreen>();
                    break;
                case MocapScreen.ScreenName:
                    _current = screenContainer.AddComponent<MocapScreen>();
                    break;
                case MoreScreen.ScreenName:
                    _current = screenContainer.AddComponent<MoreScreen>();
                    break;
                case EditAnimationScreen.ScreenName:
                    _current = screenContainer.AddComponent<EditAnimationScreen>();
                    break;
                case AddAnimationScreen.ScreenName:
                    _current = screenContainer.AddComponent<AddAnimationScreen>();
                    break;
                case ManageAnimationsScreen.ScreenName:
                    _current = screenContainer.AddComponent<ManageAnimationsScreen>();
                    break;
                case PerformanceScreen.ScreenName:
                    _current = screenContainer.AddComponent<PerformanceScreen>();
                    break;
                case HelpScreen.ScreenName:
                    _current = screenContainer.AddComponent<HelpScreen>();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown screen {screen}");
            }

            try
            {
                _current.transform.SetParent(transform, false);
                _current.onScreenChangeRequested.AddListener(ChangeScreen);
                _current.Init(_plugin);
            }
            catch (Exception exc)
            {
                SuperController.LogError($"VamTimeline.{nameof(ScreensManager)}.{nameof(RefreshCurrentUIDeferred)} (while initializing {_current.screenId}): {exc}");
            }

            yield return 0;

            _uiRefreshInProgress = false;

            _uiRefreshCoroutine = null;

            if (_uiRefreshInvalidated)
            {
                _uiRefreshInvalidated = false;
                _uiRefreshScheduled = true;
                _uiRefreshCoroutine = StartCoroutine(RefreshCurrentUIDeferred(_currentScreen));
            }
        }

        private GameObject CreateScreenContainer()
        {
            var go = new GameObject();
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1);

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.childControlHeight = true;
            group.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        public void OnEnable()
        {
            if (_plugin == null) return;
            ChangeScreen(GetDefaultScreen());
        }

        public void OnDisable()
        {
            Destroy(_current?.gameObject);
            _current = null;
            _currentScreen = null;
            if (_uiRefreshCoroutine != null) StopCoroutine(_uiRefreshCoroutine);
            _uiRefreshInProgress = false;
            _uiRefreshInvalidated = false;
            _uiRefreshScheduled = false;
        }

        public void OnDestroy()
        {
            onScreenChanged.RemoveAllListeners();
        }
    }
}

