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
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting;

        public static SharedCoroutineStarter instance
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
                    return _instance;
                }
            }
        }

        protected void Awake()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this;
                    _applicationIsQuitting = false;
                    DontDestroyOnLoad(gameObject);
                }
                else if (_instance != this)
                {
                    Plugin.Logger.Warn("[Singleton] Duplicate instance of " + nameof(SharedCoroutineStarter) + " detected. Destroying duplicate.");
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            lock (_lock)
            {
                if (_instance == this)
                {
                    _applicationIsQuitting = true;
                    _instance = null;
                }
            }
        }
    }
}