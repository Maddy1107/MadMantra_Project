using System.Collections;
using UnityEngine;
using MadMantra.Audio;

namespace MadMantra.Environment
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        private Light targetLight;
        private float originalIntensity;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            originalIntensity = targetLight.intensity;
        }

        public void Flicker(float duration = 3.5f)
        {
            StopAllCoroutines();
            StartCoroutine(FlickerRoutine(duration));
        }

        private IEnumerator FlickerRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Slower, more atmospheric pulse intervals
                float step = Random.Range(0.12f, 0.28f);
                elapsed += step;

                // Modulate intensity between dim struggling hum and bright surges
                targetLight.intensity = Random.Range(originalIntensity * 0.2f, originalIntensity * 1.4f);
                targetLight.enabled = Random.value > 0.15f;
                if (!targetLight.enabled) AudioManager.Instance?.PlayLightFlicker();

                yield return new WaitForSeconds(step);
            }

            // Restore cleanly to original steady power
            targetLight.enabled = true;
            targetLight.intensity = originalIntensity;
        }
    }
}
