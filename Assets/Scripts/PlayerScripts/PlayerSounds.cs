using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSounds : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip jump;
    public AudioClip land;
    public AudioClip whipSwing;
    public AudioClip whipHit;
    public AudioClip hurt;
    public AudioClip death;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    public float minAirTimeForLanding = 0.15f;

    AudioSource source;
    PlayerMovement movement;
    PlayerAttack attack;
    PlayerHealth health;

    float airTime;
    int lastHealth;
    bool wasDead;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (movement != null)
            movement.OnJump += PlayJump;

        if (attack != null)
        {
            attack.OnSwing += PlayWhipSwing;
            attack.OnHitEnemy += PlayWhipHit;
        }
    }

    void OnDisable()
    {
        if (movement != null)
            movement.OnJump -= PlayJump;

        if (attack != null)
        {
            attack.OnSwing -= PlayWhipSwing;
            attack.OnHitEnemy -= PlayWhipHit;
        }
    }

    void Start()
    {
        if (health != null)
            lastHealth = health.Health;
    }

    void Update()
    {
        UpdateLanding();
        UpdateHealth();
    }

    void UpdateLanding()
    {
        if (movement == null)
            return;

        if (movement.IsGrounded())
        {
            if (airTime >= minAirTimeForLanding && Time.timeSinceLevelLoad > 0.5f)
                Play(land);
            airTime = 0f;
        }
        else
        {
            airTime += Time.deltaTime;
        }
    }

    void UpdateHealth()
    {
        if (health == null)
            return;

        if (health.IsDead)
        {
            if (!wasDead)
            {
                wasDead = true;
                Play(death);
            }
        }
        else if (health.Health < lastHealth)
        {
            Play(hurt);
        }

        lastHealth = health.Health;
    }

    void PlayJump() => Play(jump);
    void PlayWhipSwing() => Play(whipSwing);
    void PlayWhipHit() => Play(whipHit);

    void Play(AudioClip clip)
    {
        if (clip != null)
            source.PlayOneShot(clip, volume * GameAudio.Sound);
    }

#if UNITY_EDITOR
    void Reset()
    {
        jump = FindClip("Boing");
        land = FindClip("Character Fall");
        whipSwing = FindClip("Whip");
        hurt = FindClip("Pain Scream");
        death = FindClip("Death Collapse");
    }

    static AudioClip FindClip(string search)
    {
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(search + " t:AudioClip"))
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
#endif
}
