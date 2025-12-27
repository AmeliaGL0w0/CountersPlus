using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CountersPlus.Utils
{
    internal sealed class SharedCoroutineStarter : MonoBehaviour
    {
        private static SharedCoroutineStarter _instance;
        private static readonly List<IEnumerator> _queuedRoutines = new List<IEnumerator>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                lock (_queuedRoutines)
                {
                    foreach (var routine in _queuedRoutines)
                    {
                        StartCoroutine(routine);
                    }
                    _queuedRoutines.Clear();
                }
            }
        }

        private void Update()
        {
            // Process any queued routines on the main thread
            lock (_queuedRoutines)
            {
                if (_queuedRoutines.Count > 0)
                {
                    foreach (var routine in _queuedRoutines)
                    {
                        StartCoroutine(routine);
                    }
                    _queuedRoutines.Clear();
                }
            }
        }

        public static void Run(IEnumerator routine)
        {
            // Enqueue the routine, it will run during Update on main thread
            lock (_queuedRoutines)
            {
                _queuedRoutines.Add(routine);
            }
        }

        public static void Stop(Coroutine coroutine)
        {
            if (_instance != null && coroutine != null)
                _instance.StopCoroutine(coroutine);
        }
    }
}