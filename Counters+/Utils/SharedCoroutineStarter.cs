using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CountersPlus.Utils
{
    // Mostly a copy-paste of PersistentSingleton<T> from game version < 1.31.0.
    internal class SharedCoroutineStarter : MonoBehaviour
    {
        private static SharedCoroutineStarter? _instance;
        private static object _lock = new object();
        private static bool _applicationIsQuitting;
        private readonly List<Action> _mainThreadActions = new List<Action>();

        public static SharedCoroutineStarter? instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton] Instance '" + nameof(SharedCoroutineStarter) + "' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<SharedCoroutineStarter>();

                        if (FindObjectsOfType<SharedCoroutineStarter>().Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject obj = new GameObject();
                            _instance = obj.AddComponent<SharedCoroutineStarter>();
                            obj.name = nameof(SharedCoroutineStarter);
                            DontDestroyOnLoad(obj);
                        }
                    }

                    return _instance;
                }
            }
        }

        public void StartCoroutineThreadSafe(IEnumerator coroutine)
        {
            lock (_mainThreadActions)
            {
                _mainThreadActions.Add(() => StartCoroutine(coroutine));
            }
        }

        protected void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

        protected void Update()
        {
            lock (_mainThreadActions)
            {
                if (_mainThreadActions.Count > 0)
                {
                    foreach (var action in _mainThreadActions)
                    {
                        action?.Invoke();
                    }
                    _mainThreadActions.Clear();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            _applicationIsQuitting = true;
        }
    }
}