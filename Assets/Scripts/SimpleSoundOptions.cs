using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SimpleSoundOptions : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private string masterParamName = "MasterVolume";

    [Header("UI Controls")]
    [SerializeField] private Slider masterVolumeSlider;

    private const string PREF_MASTER_VOL = "Pref_Master_Volume";

    void Start()
    {
        if (masterVolumeSlider == null) return;

        // 1. Cargar el volumen guardado (por defecto a 0.8)
        float savedVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.8f);

        // 2. Configurar el Slider
        masterVolumeSlider.minValue = 0.0001f; // Evita el 0 absoluto para el cálculo logarítmico
        masterVolumeSlider.maxValue = 1f;
        masterVolumeSlider.value = savedVolume;

        // 3. Aplicar el volumen inicial y escuchar cambios
        SetMasterVolume(savedVolume);
        masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void OnVolumeChanged(float value)
    {
        SetMasterVolume(value);
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, value);
    }

    private void SetMasterVolume(float linearValue)
    {
        if (mainMixer == null) return;

        // Convierte el valor del Slider (0.0001 a 1.0) a Decibeles (-80 dB a 0 dB)
        float dB = Mathf.Log10(Mathf.Clamp(linearValue, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat(masterParamName, dB);
    }
}