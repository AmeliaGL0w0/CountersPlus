using IPA.Utilities;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace CountersPlus.Utils
{
    internal sealed class SharedCoroutineStarter : MonoBehaviour
    {
        private static SharedCoroutineStarter _instance;

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
            }
        }

        public static async Task<Coroutine> Run(IEnumerator routine)
        {
            await UnityGame.SwitchToMainThreadAsync();

            if (_instance == null)
                return null;

            return _instance.StartCoroutine(routine);
        }

        public static async Task Stop(Coroutine coroutine)
        {
            if (coroutine == null)
                return;

            await UnityGame.SwitchToMainThreadAsync();

            if (_instance == null)
                return;

            _instance.StopCoroutine(coroutine);
        }
    }
}