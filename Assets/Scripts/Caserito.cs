using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Caserito : MonoBehaviour
{
    public static readonly List<Caserito> All = new();

    [Header("Behavior")]
    public float moveSpeed     = 2f;
    public float catchDistance = 1.1f;
    public float watchFov      = 65f;
    public float repairFov     = 28f;
    public float activateDelay = 4f;

    [Header("Sounds — leave empty to use generated metallic sounds")]
    public AudioClip footstepClip;
    public AudioClip freezeClip;
    public AudioClip catchClip;

    // ── Components ─────────────────────────────────────────────────────────
    Rigidbody    _rb;
    Animator     _anim;
    MeshRenderer _bodyRend;
    MeshRenderer _eyeRend;
    Material     _eyeMat;
    Material     _bodyMat;
    AudioSource  _stepSrc;   // for footstep PlayOneShot
    AudioSource  _eventSrc;  // for freeze / catch events

    // ── Cached generated clips ─────────────────────────────────────────────
    AudioClip _genStep;
    AudioClip _genFreeze;
    AudioClip _genCatch;

    // ── State ──────────────────────────────────────────────────────────────
    enum State { Idle, Moving, Frozen }
    State     _state = State.Idle;
    Transform _player;
    Camera    _cam;
    bool      _activated;
    bool      _dead;

    // Anti-stuck
    Vector3 _lastPos;
    float   _stuckTimer;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        All.Add(this);
        _rb = GetComponent<Rigidbody>();
        _rb.constraints            = RigidbodyConstraints.FreezeRotation;
        _rb.linearDamping          = 8f;
        _rb.angularDamping         = 999f;
        _rb.useGravity             = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnDestroy() => All.Remove(this);

    void Start()
    {
        _player = GameObject.FindWithTag("Player")?.transform;
        _cam    = Camera.main;

        _anim = GetComponentInChildren<Animator>();
        _anim?.Play("Idle", 0, Random.value);   // start mid-cycle so 3 caseritos don't sync

        GrabVisuals();
        BuildAudio();
        StartCoroutine(ActivateAfterDelay());
        StartCoroutine(FootstepCoroutine());
    }

    // ── Visuals ────────────────────────────────────────────────────────────
    void GrabVisuals()
    {
        var bodyChild = transform.Find("Body");
        if (bodyChild != null)
        {
            if (bodyChild.TryGetComponent(out MeshRenderer mr))
            {
                // Primitive capsule fallback
                _bodyMat = new Material(mr.sharedMaterial);
                _bodyMat.EnableKeyword("_EMISSION");
                _bodyMat.SetColor("_EmissionColor", Color.black);
                mr.material = _bodyMat;
                _bodyRend = mr;
            }
            else
            {
                // FBX model: use first SkinnedMeshRenderer found in children
                var smr = bodyChild.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    _bodyMat = new Material(smr.sharedMaterial);
                    _bodyMat.EnableKeyword("_EMISSION");
                    _bodyMat.SetColor("_EmissionColor", Color.black);
                    smr.material = _bodyMat;
                }
            }
        }
        if (transform.Find("Eye")?.TryGetComponent(out MeshRenderer eR) == true)
        {
            _eyeMat = new Material(eR.sharedMaterial);
            _eyeMat.EnableKeyword("_EMISSION");
            _eyeMat.SetColor("_EmissionColor", Color.black);
            eR.material = _eyeMat;
            _eyeRend = eR;
        }
    }

    // ── Audio setup ────────────────────────────────────────────────────────
    void BuildAudio()
    {
        // Generate metallic sounds so the game works with zero audio assets
        _genStep   = MakeFootstep();
        _genFreeze = MakeClick();
        _genCatch  = MakeScream();

        if (!footstepClip) footstepClip = _genStep;
        if (!freezeClip)   freezeClip   = _genFreeze;
        if (!catchClip)    catchClip     = _genCatch;

        _stepSrc  = Make3DSource(minD: 1f, maxD: 20f, vol: 0.85f);
        _eventSrc = Make3DSource(minD: 1f, maxD: 18f, vol: 1.00f);
    }

    AudioSource Make3DSource(float minD, float maxD, float vol)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.spatialBlend = 1f;
        s.rolloffMode  = AudioRolloffMode.Linear;
        s.minDistance  = minD;
        s.maxDistance  = maxD;
        s.volume       = vol;
        s.loop         = false;
        s.playOnAwake  = false;
        return s;
    }

    // ── Procedural audio generation ────────────────────────────────────────
    // Metallic thud — heavy footstep of a broken animatronic
    static AudioClip MakeFootstep()
    {
        const int sr = 44100;
        const float dur = 0.20f;
        int n = (int)(sr * dur);
        var d = new float[n];
        var rng = new System.Random(42);
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / sr;
            float env = Mathf.Exp(-t * 30f);
            float low = Mathf.Sin(Mathf.PI * 2f * 55f * t);                  // thud
            float mid = Mathf.Sin(Mathf.PI * 2f * 850f * t) * 0.35f;         // metallic
            float nz  = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.18f;       // crunch
            d[i] = Mathf.Clamp((low + mid + nz) * env * 0.9f, -1f, 1f);
        }
        var c = AudioClip.Create("step", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    // Mechanical click — gears locking when frozen
    static AudioClip MakeClick()
    {
        const int sr = 44100;
        const float dur = 0.10f;
        int n = (int)(sr * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / sr;
            float env = Mathf.Exp(-t * 90f);
            float hi  = Mathf.Sin(Mathf.PI * 2f * 1400f * t);
            float lo  = Mathf.Sin(Mathf.PI * 2f * 400f * t) * 0.5f;
            d[i] = Mathf.Clamp((hi + lo) * env * 0.95f, -1f, 1f);
        }
        var c = AudioClip.Create("click", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    // Distorted screech — jump scare catch sound
    static AudioClip MakeScream()
    {
        const int sr = 44100;
        const float dur = 0.70f;
        int n = (int)(sr * dur);
        var d = new float[n];
        var rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / sr;
            float env = Mathf.Exp(-t * 5f);
            float freq = Mathf.Lerp(380f, 90f, t * 1.4f);
            float wave = Mathf.Sin(Mathf.PI * 2f * freq * t);
            float nz   = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.45f;
            d[i] = Mathf.Clamp((wave + nz) * env * 1.2f, -1f, 1f);
        }
        var c = AudioClip.Create("catch", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    // ── Activation ─────────────────────────────────────────────────────────
    IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(activateDelay);
        _lastPos   = transform.position;
        _activated = true;
    }

    // ── Footstep coroutine — rhythmic, not looped ──────────────────────────
    IEnumerator FootstepCoroutine()
    {
        while (!_dead)
        {
            if (_activated && _state == State.Moving)
            {
                _stepSrc.PlayOneShot(footstepClip, 0.85f);
                yield return new WaitForSeconds(0.55f);   // ~109 BPM slow walk
            }
            else
            {
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    // ── Vision check ───────────────────────────────────────────────────────
    bool IsBeingWatched()
    {
        if (_cam == null) return false;
        float halfFov  = (RepairMinigame.IsActive ? repairFov : watchFov) * 0.5f;
        Vector3 camPos = _cam.transform.position;
        Vector3 target = transform.position + Vector3.up * 1.4f;
        Vector3 dir    = target - camPos;

        if (Vector3.Angle(_cam.transform.forward, dir) > halfFov) return false;

        if (Physics.Raycast(camPos, dir.normalized, out RaycastHit hit,
                dir.magnitude - 0.05f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }
        return true;
    }

    // ── Update — vision, state, rotation ───────────────────────────────────
    void Update()
    {
        if (!_activated || _dead || _player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)    return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted) return;

        bool watched = IsBeingWatched();
        if (watched  && _state != State.Frozen) EnterFrozen();
        if (!watched && _state != State.Moving)  EnterMoving();

        if (_state == State.Moving)
        {
            FacePlayer();
            CheckCatch();
            TickStuck();
        }
    }

    // ── FixedUpdate — physics velocity ─────────────────────────────────────
    void FixedUpdate()
    {
        if (!_activated || _dead || _player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted) return;

        float vy = _rb.linearVelocity.y;

        if (_state == State.Moving)
        {
            Vector3 dir   = FlatDir(_player.position - transform.position);
            float   speed = RepairMinigame.IsActive ? moveSpeed * 0.18f : moveSpeed;
            if (dir.sqrMagnitude > 0.01f)
                _rb.linearVelocity = dir * speed + Vector3.up * vy;
        }
        else
        {
            _rb.linearVelocity = Vector3.up * vy;
        }
    }

    // ── State transitions ──────────────────────────────────────────────────
    void EnterFrozen()
    {
        _state = State.Frozen;
        _anim?.SetBool("Moving", false);
        SetEyeGlow(false);
        SetBodyPulse(0f);
        _eventSrc.PlayOneShot(freezeClip);
    }

    void EnterMoving()
    {
        _state = State.Moving;
        _anim?.SetBool("Moving", true);
        SetEyeGlow(true);
    }

    // ── Movement helpers ───────────────────────────────────────────────────
    void FacePlayer()
    {
        Vector3 dir = FlatDir(_player.position - transform.position);
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
    }

    void CheckCatch()
    {
        if (FlatDist(_player.position) < catchDistance) Catch();
    }

    void TickStuck()
    {
        float moved = FlatDist(_lastPos);
        _stuckTimer = moved < 0.04f ? _stuckTimer + Time.deltaTime : 0f;
        if (_stuckTimer > 1.8f)
        {
            Vector3 side = Vector3.Cross(
                FlatDir(_player.position - transform.position), Vector3.up);
            _rb.AddForce(side * 3.5f, ForceMode.VelocityChange);
            _stuckTimer = 0f;
        }
        _lastPos = transform.position;
    }

    // ── Body pulse (proximity effect) ──────────────────────────────────────
    void LateUpdate()
    {
        if (_activated && !_dead && _player != null && _state == State.Moving)
            SetBodyPulse(1f - Mathf.Clamp01(FlatDist(_player.position) / 6f));
    }

    // ── CATCH — jump scare then game over ──────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!_dead && other.CompareTag("Player")) Catch();
    }

    void Catch()
    {
        if (_dead) return;
        _dead = true;
        _rb.linearVelocity = Vector3.zero;

        // Lock player immediately
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.enabled = false;

        StartCoroutine(JumpScareSequence());
    }

    IEnumerator JumpScareSequence()
    {
        // Maximize eye glow for scare moment
        if (_eyeMat != null)
        {
            _eyeMat.color = new Color(1f, 0f, 0f);
            _eyeMat.SetColor("_EmissionColor", new Color(10f, 0.2f, 0.2f));
        }

        // Harsh audio hit
        _eventSrc.PlayOneShot(catchClip, 1.5f);

        // Camera shake
        StartCoroutine(ShakeCamera(0.50f, 18f));

        // Red screen flash using HUD if available
        GameHUD.Instance?.TriggerCatchFlash();

        yield return new WaitForSecondsRealtime(0.85f);

        // Now trigger the game over
        GameManager.Instance?.TriggerCaseritoGameOver();
    }

    IEnumerator ShakeCamera(float dur, float mag)
    {
        var cam = Camera.main?.transform;
        if (!cam) yield break;
        var orig = cam.localPosition;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float s = mag * (1f - t / dur) * 0.01f;
            cam.localPosition = orig + new Vector3(
                Random.Range(-s, s), Random.Range(-s, s), 0f);
            yield return null;
        }
        cam.localPosition = orig;
    }

    // ── Visuals ────────────────────────────────────────────────────────────
    void SetEyeGlow(bool on)
    {
        if (_eyeMat == null) return;
        _eyeMat.color = on ? new Color(1f, 0.04f, 0.04f) : new Color(0.10f, 0f, 0f);
        _eyeMat.SetColor("_EmissionColor",
            on ? new Color(4f, 0.1f, 0.1f) : Color.black);
    }

    void SetBodyPulse(float t)
        => _bodyMat?.SetColor("_EmissionColor", new Color(t * 0.85f, 0f, 0f));

    // ── Utils ──────────────────────────────────────────────────────────────
    static Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    float FlatDist(Vector3 to)
    {
        Vector3 d = to - transform.position; d.y = 0f; return d.magnitude;
    }

    // ── Public query for HUD tension indicator ─────────────────────────────
    // Returns true if any caserito is moving and within warning range
    public static bool AnyApproaching(Transform player, float range = 8f)
    {
        foreach (var c in All)
            if (c._activated && !c._dead && c._state == State.Moving)
                if (c.FlatDist(player.position) < range) return true;
        return false;
    }
}
