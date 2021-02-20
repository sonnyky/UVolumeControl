using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Audio;

namespace Tinker
{

    public class VolumeSlider : MonoBehaviour
    {
        private string m_SettingsFilePath;
        private float m_SavedVolumeValue; 
        private AudioSource m_TargetAudioSource;

        private AudioMixer m_Mixer;
        private AudioMixerGroup m_BgmGroup;

        public float m_SliderValue;
        private string m_Text = "0";
        private bool m_EditMode = false;
        private bool m_CanEditVolume = true;

        Rect windowRect = new Rect(20, 20, 600, 100);

        private void Start()
        {
            m_TargetAudioSource = GetComponent<AudioSource>();
            m_Mixer = Resources.Load("Mixer/MainMixer") as AudioMixer;

            if (m_TargetAudioSource == null || m_Mixer == null)
            {
                Debug.LogError("AudioSource is null or Mixer is null");
            }

            m_BgmGroup = m_Mixer.FindMatchingGroups("BGM")[0];
            m_TargetAudioSource.outputAudioMixerGroup = m_BgmGroup;

            // Load saved settings

            if (!Directory.Exists(Application.persistentDataPath + "/VolumeSettings"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/VolumeSettings");
            }

            m_SettingsFilePath = Application.persistentDataPath + "/VolumeSettings/bgm_control.json";
            LoadVolumeSettings();
            SetVolume();
            m_Text = m_SliderValue.ToString();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                m_EditMode = !m_EditMode;
            }
        }

        void OnGUI()
        {
            if (!m_EditMode) return;
            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "BGM Control");


        }

        void DrawWindow(int windowID)
        {
            GUI.skin.horizontalSlider.fixedHeight = 30f;
            GUI.skin.horizontalSliderThumb.fixedHeight = 30f;
            GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 16;

            if (m_CanEditVolume)
            {
                float slider = -1f;
                slider = GUILayout.HorizontalSlider(m_SliderValue, 0.0001f, 1.0f, GUILayout.Width(600), GUILayout.Height(30));

                if(slider != m_SliderValue)
                {
                    m_SliderValue = slider;
                    m_Text = slider.ToString();
                }

                string valString = "null";
                GUILayout.BeginHorizontal();
                GUILayout.Label("Volume");
                valString = GUILayout.TextField(m_Text);
                GUILayout.EndHorizontal();

                if(valString != m_Text)
                {
                    m_Text = valString;
                    if (float.TryParse(valString, out slider))
                    {
                        if (slider == 0) slider = 0.0001f;
                        m_Text = valString;
                        m_SliderValue = slider;
                    }
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply"))
                {
                    SaveVolumeSettings();
                }
                if (GUILayout.Button("Revert"))
                {
                    m_SliderValue = m_SavedVolumeValue;
                    m_Text = m_SliderValue.ToString();
                }
                GUILayout.EndHorizontal();

                if (GUI.changed)
                {
                    SetVolume();
                }
            }
            else
            {
                GUILayout.Label("Cannot adjust volume in this mode");
            }
        }

        public void SetVolume()
        {
            m_Mixer.SetFloat("BgmVolume", LinearToDecibel(m_SliderValue));
        }

        private float DecibelToLinear(float dB)
        {
            float linear = Mathf.Pow(10.0f, dB / 20.0f);

            return linear;
        }

        private float LinearToDecibel(float linear)
        {
            float dB;

            if (linear != 0)
                dB = 20.0f * Mathf.Log10(linear);
            else
                dB = -144.0f;

            return dB;
        }

        VolumeSettings CreateVolumeSettings()
        {
            VolumeSettings settings = new VolumeSettings();
            settings.BgmVolume = m_SliderValue;
            return settings;
        }

        void SaveVolumeSettings()
        {
            VolumeSettings settings = CreateVolumeSettings();
            settings.BgmVolume = m_SliderValue;
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(m_SettingsFilePath);
            bf.Serialize(file, settings);
            file.Close();
            m_SavedVolumeValue = m_SliderValue;
        }

        void LoadVolumeSettings()
        {
            if (File.Exists(m_SettingsFilePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(m_SettingsFilePath, FileMode.Open);
                VolumeSettings save = (VolumeSettings)bf.Deserialize(file);
                file.Close();
                m_SliderValue = save.BgmVolume;
            }
            else
            {
                m_SliderValue = 1f;
            }
            m_SavedVolumeValue = m_SliderValue;
        }

        public void ReleaseMixerGroupFromAudioSource()
        {
            m_CanEditVolume = false;
            m_TargetAudioSource.volume = 0f;
            StartCoroutine("_ReleaseMixerGroupFromAudioSource");
        }

        IEnumerator _ReleaseMixerGroupFromAudioSource()
        {
            yield return new WaitForSeconds(0.1f);
            m_TargetAudioSource.outputAudioMixerGroup = null;
            m_TargetAudioSource.volume = m_SliderValue;
        }

        public void ReattachMixerGroupToAudioSource()
        {
            m_TargetAudioSource.volume = 0f;
            StartCoroutine("_ReattachMixerGroupToAudioSource");
        }

        public AudioMixerGroup GetAudioMixerGroup()
        {
            return m_BgmGroup;
        }

        IEnumerator _ReattachMixerGroupToAudioSource()
        {
            yield return new WaitForSeconds(0.1f);
            m_TargetAudioSource.outputAudioMixerGroup = m_BgmGroup;
            m_TargetAudioSource.volume = 1f;
            m_CanEditVolume = true;
        }
    }
}