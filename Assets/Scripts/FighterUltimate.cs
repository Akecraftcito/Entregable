using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public sealed class FighterUltimate : MonoBehaviour
{
    // --- NVO: Variable estática para saber si HAY CUALQUIER ULTIMATE EN CURSO ---
    public static bool IsAnyUltimateExecuting { get; private set; } = false;

    [Header("Video de la Ultimate")]
    [SerializeField] private VideoClip ultimateVideo;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Camera targetCamera;

    [Header("Sonido y Hitbox")]
    [SerializeField] private AudioSource ultimateAudioSource;
    [SerializeField] private AudioClip ultimateSound;
    [SerializeField] private UltimateHitbox ultimateHitbox;
    [SerializeField, Min(1f)] private float damage = 100f;

    [Header("Ajustes de Cooldown")]
    [SerializeField] private float cooldownDuration = 10f;
    private float cooldownTimer = 0f;

    private FighterController owner;
    private bool isExecuting;

    public bool IsExecuting => isExecuting;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    public bool HasRequiredAssets => ultimateVideo != null;

    // --- Se añade '!IsAnyUltimateExecuting' para bloquear la activación de otros personajes ---
    public bool CanActivate => owner != null
        && owner.IsUltimateCharged
        && !owner.isDefeated
        && !owner.isStunned
        && !owner.IsBeingGrabbed
        && !isExecuting
        && !IsOnCooldown
        && !IsAnyUltimateExecuting
        && HasRequiredAssets;

    private void Awake()
    {
        owner = GetComponent<FighterController>();

        // 1. Buscar Hitbox en los hijos
        if (ultimateHitbox == null)
        {
            ultimateHitbox = FindUltimateHitboxRecursive(transform);
        }

        // 2. Crear Hitbox gigante si no existe
        if (ultimateHitbox == null)
        {
            Debug.LogWarning($"[FighterUltimate] Creando UltimateHitbox ampliada en {gameObject.name}...");
            
            GameObject autoHitboxObj = new GameObject("UltimateHitbox");
            autoHitboxObj.transform.SetParent(transform, false);
            autoHitboxObj.transform.localPosition = new Vector3(0f, 0.5f, 1.5f); 

            BoxCollider collider = autoHitboxObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.size = new Vector3(4.0f, 4.0f, 5.0f); 

            ultimateHitbox = autoHitboxObj.AddComponent<UltimateHitbox>();
            autoHitboxObj.SetActive(false);
        }

        if (ultimateHitbox != null)
        {
            ultimateHitbox.SetOwner(this);
        }

        // 3. Configurar la cámara si no fue asignada
        if (targetCamera == null) targetCamera = Camera.main;

        // 4. Configurar VideoPlayer (Unificado)
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = targetCamera;
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;

        // 5. AudioSource
        if (ultimateAudioSource == null) ultimateAudioSource = GetComponent<AudioSource>();
        if (ultimateAudioSource == null) ultimateAudioSource = gameObject.AddComponent<AudioSource>();
        ultimateAudioSource.playOnAwake = false;
        ultimateAudioSource.spatialBlend = 0f;
        ultimateAudioSource.ignoreListenerPause = true;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, ultimateAudioSource);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private UltimateHitbox FindUltimateHitboxRecursive(Transform currentParent)
    {
        UltimateHitbox hitbox = currentParent.GetComponent<UltimateHitbox>();
        if (hitbox != null) return hitbox;

        if (currentParent.name.Equals("UltimateHitbox", System.StringComparison.OrdinalIgnoreCase))
        {
            hitbox = currentParent.gameObject.GetComponent<UltimateHitbox>();
            if (hitbox == null) hitbox = currentParent.gameObject.AddComponent<UltimateHitbox>();
            return hitbox;
        }

        foreach (Transform child in currentParent)
        {
            UltimateHitbox result = FindUltimateHitboxRecursive(child);
            if (result != null) return result;
        }

        return null;
    }

    public bool TryActivate()
    {
        if (owner == null) owner = GetComponent<FighterController>();

        if (!CanActivate)
        {
            if (IsOnCooldown)
            {
                Debug.LogWarning($"[FighterUltimate] La Ultimate de {gameObject.name} está en cooldown. Tiempo restante: {cooldownTimer:F1}s");
            }
            else
            {
                Debug.LogWarning($"[FighterUltimate] No se pudo activar la Ultimate en {gameObject.name}. ¿Video cargado? {ultimateVideo != null} | Carga: {owner?.IsUltimateCharged}");
            }
            return false;
        }

        if (!owner.ConsumeUltimateCharge()) return false;

        isExecuting = true;
        StartCoroutine(CheckHitAndExecuteRoutine());
        return true;
    }

    private IEnumerator CheckHitAndExecuteRoutine()
    {
        FighterController victimDetected = null;

        if (ultimateHitbox != null)
        {
            ultimateHitbox.gameObject.SetActive(true);
            ultimateHitbox.CheckForVictimInstant();

            float timer = 0f;
            while (timer < 0.25f)
            {
                if (ultimateHitbox.HasHitVictim(out victimDetected))
                {
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            ultimateHitbox.gameObject.SetActive(false);
        }

        // Respaldo si la hitbox física falló pero hay un oponente activo en escena
        if (victimDetected == null)
        {
            FighterController[] allFighters = FindObjectsOfType<FighterController>();
            foreach (var fighter in allFighters)
            {
                if (fighter != owner && !fighter.isDefeated)
                {
                    victimDetected = fighter;
                    break;
                }
            }
        }

        if (victimDetected == null)
        {
            Debug.LogWarning("[FighterUltimate] Ataque cancelado: No se encontró ningún oponente vivo en la escena.");
            isExecuting = false;
            yield break;
        }

        yield return StartCoroutine(PlayVideoSequenceRoutine(victimDetected));
    }

    private IEnumerator PlayVideoSequenceRoutine(FighterController targetVictim)
    {
        float previousTimeScale = Time.timeScale;
        IsAnyUltimateExecuting = true; // Activar bandera global

        // Pausar todos los AudioSources activos de la pelea excepto el de la Ultimate
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        System.Collections.Generic.List<AudioSource> pausedSources = new System.Collections.Generic.List<AudioSource>();

        foreach (AudioSource source in allAudioSources)
        {
            if (source != ultimateAudioSource && source.isPlaying)
            {
                source.Pause();
                pausedSources.Add(source);
            }
        }

        try
        {
            Time.timeScale = 0f;

            if (targetCamera == null) targetCamera = Camera.main;
            videoPlayer.targetCamera = targetCamera;
            videoPlayer.clip = ultimateVideo;

            if (ultimateAudioSource != null)
            {
                ultimateAudioSource.volume = 1f;
                ultimateAudioSource.mute = false;
            }

            videoPlayer.Prepare();

            float prepareTimeout = Time.realtimeSinceStartup + 4f;
            while (!videoPlayer.isPrepared && Time.realtimeSinceStartup < prepareTimeout)
            {
                yield return null;
            }

            if (videoPlayer.isPrepared)
            {
                Debug.Log("[FighterUltimate] Reproduciendo Vídeo de Ultimate con audio propio activo.");

                videoPlayer.Play();

                if (ultimateSound != null)
                {
                    ultimateAudioSource.PlayOneShot(ultimateSound);
                }

                yield return new WaitForSecondsRealtime((float)ultimateVideo.length);
            }
            else
            {
                Debug.LogError("[FighterUltimate] Error: El vídeo MP4 no pudo cargarse a tiempo.");
            }

            // Aplicar daño e inmovilizar al objetivo con Stun de 2 segundos
            if (targetVictim != null && owner != null && !owner.isDefeated)
            {
                targetVictim.TakeHit(damage, true, owner.transform.position, ignoreBlocking: true, customStunDuration: 2.0f);
            }
        }
        finally
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            // Reanudar los sonidos pausados
            foreach (AudioSource source in pausedSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }

            Time.timeScale = previousTimeScale;
            cooldownTimer = cooldownDuration;
            isExecuting = false;
            IsAnyUltimateExecuting = false; // Liberar bandera global al finalizar
        }
    }
}