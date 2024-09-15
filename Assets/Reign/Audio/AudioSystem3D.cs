using System.Collections.Generic;
using UnityEngine;

namespace Reign.Audio
{
    [RequireComponent(typeof(AudioListener))]
    [DefaultExecutionOrder(32000)]
    public class AudioSystem3D : MonoBehaviour
    {
        public static AudioSystem3D singleton { get; private set; }
        internal new Transform transform;

        private HashSet<AudioListener3D> listeners;
        private HashSet<AudioSource3D> sources;
        private Dictionary<AudioSource, GameObject> unitySources;

        private void Awake()
        {
            // validate only one instance lives
            if (singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);

            transform = GetComponent<Transform>();
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            listeners = new HashSet<AudioListener3D>();
            sources = new HashSet<AudioSource3D>();
            unitySources = new Dictionary<AudioSource, GameObject>();
        }

		private void LateUpdate()
        {
            foreach (var listener in listeners)
            {
                if (!listener.isActiveAndEnabled)
                {
                    foreach (var source in listener.sources)
                    {
                        var unitySource = source.Value;
                        if (unitySource.isPlaying) unitySource.Stop();// stop playing unity audio if needed
                    }
                    continue;
                }

                var pos = listener.transform.position;
                var rotInv = Quaternion.Inverse(listener.transform.rotation);
                foreach (var source in listener.sources)
                {
                    var reignSource = source.Key;
                    var unitySource = source.Value;

                    // validate audio is active
                    if (!reignSource.isActiveAndEnabled)
                    {
                        if (unitySource.isPlaying) unitySource.Stop();// stop playing unity audio if source is disabled
                        continue;
                    }
                    if (!unitySource.isPlaying) continue;

                    // move unity audio source into reletive position to unity audio source
                    var srcTransform = reignSource.transform;
                    var dstTransform = unitySource.transform;
                    dstTransform.position = rotInv * (srcTransform.position - pos);
                    dstTransform.rotation = rotInv * srcTransform.rotation;

                    // update pitch
                    unitySource.pitch = Time.timeScale;

                    // update audio setting changes
                    if (reignSource.lastClip != reignSource.clip) unitySource.clip = reignSource.clip;
                    if (reignSource.lastAudioMixerGroup != reignSource.audioMixerGroup) unitySource.outputAudioMixerGroup = reignSource.audioMixerGroup;
                    if (reignSource.lastVolume != reignSource.volume) unitySource.volume = reignSource.volume;
                }
            }

            foreach (var source in sources)
            {
                source.lastClip = source.clip;
                source.lastAudioMixerGroup = source.audioMixerGroup;
                source.lastVolume = source.volume;
            }
        }

        private static void AddSourceToListener(AudioListener3D listener, AudioSource3D source)
        {
            var unitySourceObject = new GameObject("Audio Source");
            unitySourceObject.transform.parent = singleton.transform;
            var unitySource = unitySourceObject.AddComponent<AudioSource>();
            unitySource.clip = source.clip;
            unitySource.outputAudioMixerGroup = source.audioMixerGroup;
            unitySource.loop = source.loop;
            unitySource.spatialBlend = 1;
            unitySource.volume = source.volume;
            unitySource.rolloffMode = source.volumeRolloff;
            unitySource.minDistance = source.minDistance;
            unitySource.maxDistance = source.maxDistance;
            if (source.playOnAwake) unitySource.Play();
            singleton.unitySources.Add(unitySource, unitySourceObject);
            listener.sources.Add(source, unitySource);
        }

        public static void AddAudioListener(AudioListener3D listener)
        {
            singleton.listeners.Add(listener);

            // add existing audio-sources to listener
            foreach (var source in singleton.sources)
            {
                if (!listener.sources.ContainsKey(source)) AddSourceToListener(listener, source);
            }
        }

        public static void RemoveAudioListener(AudioListener3D listener)
        {
            singleton.listeners.Remove(listener);

            // destroy unity-audio-source game-objects
            foreach (var source in listener.sources)
            {
                var unitySource = source.Value;
                if (singleton.unitySources.ContainsKey(unitySource))
                {
                    var unitySourceObject = singleton.unitySources[unitySource];
                    Destroy(unitySourceObject);
                }
            }
        }

        public static void AddAudioSource(AudioSource3D source)
        {
            singleton.sources.Add(source);

            // add audio-source to all listeners
            foreach (var listener in singleton.listeners)
            {
                if (!listener.sources.ContainsKey(source)) AddSourceToListener(listener, source);
            }
        }

        public static void RemoveAudioSource(AudioSource3D source)
        {
            singleton.sources.Remove(source);

            // remove source from all listeners
            foreach (var listener in singleton.listeners)
            {
                if (listener.sources.ContainsKey(source))
                {
                    // destroy unity-audio-source game-object
                    var unitySource = listener.sources[source];
                    if (singleton.unitySources.ContainsKey(unitySource))
                    {
                        var unitySourceObject = singleton.unitySources[unitySource];
                        Destroy(unitySourceObject);
                    }

                    // remove source from listener
                    listener.sources.Remove(source);
                }
            }
        }

        public static void PlayAudio(AudioSource3D source)
        {
            foreach (var listener in singleton.listeners)
            foreach (var listernSource in listener.sources)
            {
                if (listernSource.Key == source)
                {
                    listernSource.Key.Play();
                    break;
                }
            }
        }

        public static void PlayAudioOneShot(AudioSource3D source, AudioClip clip)
        {
            foreach (var listener in singleton.listeners)
            foreach (var listernSource in listener.sources)
            {
                if (listernSource.Key == source)
                {
                    listernSource.Value.PlayOneShot(clip);
                    break;
                }
            }
        }

        public static void PauseAudio(AudioSource3D source)
        {
            foreach (var listener in singleton.listeners)
            foreach (var listernSource in listener.sources)
            {
                if (listernSource.Key == source)
                {
                    listernSource.Key.Pause();
                    break;
                }
            }
        }

        public static void StopAudio(AudioSource3D source)
        {
            foreach (var listener in singleton.listeners)
            foreach (var listernSource in listener.sources)
            {
                if (listernSource.Key == source)
                {
                    listernSource.Key.Stop();
                    break;
                }
            }
        }
    }
}