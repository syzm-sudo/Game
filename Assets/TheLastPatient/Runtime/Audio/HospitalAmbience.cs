using UnityEngine;

namespace TheLastPatient
{
    public sealed class HospitalAmbience : MonoBehaviour
    {
        public Light[] hospitalLights;
        public AudioSource ambience;
        public AudioSource effects;
        private AudioClip hum, step, door, click;
        private void Awake()
        {
            hum = Tone("Ventilation", 48, 4, 0.17f, 0.04f);
            step = Tone("Footstep", 95, 0.11f, 0.2f, 0.35f);
            door = Tone("Door", 67, 0.55f, 0.33f, 0.35f);
            click = Tone("Tape click", 230, 0.09f, 0.15f, 0.2f);
            ambience.clip = hum; ambience.loop = true; ambience.volume = 0.35f; ambience.Play();
        }
        private static AudioClip Tone(string name, float frequency, float seconds, float volume, float noise)
        {
            const int rate = 22050;
            var data = new float[Mathf.CeilToInt(rate * seconds)];
            var random = new System.Random(13);
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = name == "Ventilation" ? 1 : Mathf.Pow(1 - (float)i / data.Length, 2);
                data[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t) + ((float)random.NextDouble() * 2 - 1) * noise) * volume * envelope;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        public void SetHospitalLights(bool enabled)
        { if (hospitalLights != null) foreach (var light in hospitalLights) if (light != null) light.enabled = enabled; }
        public void ApplyState() => SetHospitalLights(!GameDirector.Instance.story.HasFlag(StoryIds.Inside));
        public void Footstep(bool quiet) => effects.PlayOneShot(step, quiet ? 0.17f : 0.5f);
        public void DoorSound() => effects.PlayOneShot(door, 0.7f);
        public void TapeClick() => effects.PlayOneShot(click, 0.65f);
        private void OnDestroy()
        { if (hum != null) Destroy(hum); if (step != null) Destroy(step); if (door != null) Destroy(door); if (click != null) Destroy(click); }
    }
}
