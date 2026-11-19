using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShip : MonoBehaviour
{
    [Header("Player Index & Field")]
    [SerializeField] private int playerIndex = 0;
    [SerializeField] private PlayField myField;

    [Header("Character Data (SO)")]
    [SerializeField] private CharacterDataSO characterData;

    [Header("Hitbox & Visual")]
    [SerializeField] private SpriteRenderer hitboxMarkerRenderer;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private GameObject chargeEffect;

    [Header("Health")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float invincibilityDuration = 1.0f;
    private float currentHP;
    private float lastDamageTime = -999f;
    private Coroutine blinkCoroutine;

    [Header("Movement")]
    [SerializeField] private float normalSpeed = 6.0f;
    [SerializeField] private float slowSpeed = 3.0f;

    [Header("Energy & Charge Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float autoEnergyPerSec = 2.5f;
    [SerializeField] private float chargeSpeed = 25.0f;
    private float currentEnergy = 0f;
    private float currentChargeProgress = 0f;
    private bool isCharging = false;
    private float currentPower = 1.0f;
    private float shotDamageMultiplier = 1.0f;

    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int maxBombCount = 3;
    [SerializeField] private float bombCooldown = 4.0f;
    [SerializeField] private float bombEnergyPenalty = 60f;
    private int currentBombCount;
    private float lastBombTime = -999f;

    [Header("Lv3 Engage Burst")]
    [SerializeField] private GameObject engageBurstPrefab;

    [Header("Common Shot Pooling")]
    [SerializeField] private PlayerBullet commonBulletPrefab;
    [SerializeField] private Transform commonShotPosLeft;
    [SerializeField] private Transform commonShotPosRight;
    [SerializeField] private float commonShotInterval = 0.08f;
    [SerializeField] private float commonBulletSpeed = 22f;
    [SerializeField] private float commonShotDamage = 8.0f;

    [Header("Options")]
    [SerializeField] private List<PlayerOptionBase> options = new List<PlayerOptionBase>();
    private float optionShotTimer = 0f;
    private EngageBossBase activeSpawnedBoss;

    [Header("Defeat & Visual Effects")]
    [SerializeField] private GameObject defeatExplosionPrefab;
    [SerializeField] private Collider2D playerCollider;
    private bool isLastStand = false;
    private bool isDefeated = false;

    private IObjectPool<PlayerBullet> bulletPool;
    private Vector2 moveInput;
    private bool isSlowPressed;
    private bool isShotPressed;
    private Coroutine commonShotCoroutine;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float CurrentChargeProgress => currentChargeProgress;
    public float CurrentPower => currentPower;
    public int CurrentBombCount => currentBombCount;
    public int MaxBombCount => maxBombCount;
    public int PlayerIndex => playerIndex;
    public bool IsLastStand => isLastStand;
    public CharacterDataSO CharacterData => characterData;
    public event Action<PlayerShip> OnBombExecuted;
    public event Action<PlayerShip> OnAttackExecuted;
    public float ShotDamageMultiplier => shotDamageMultiplier;
    public float OptionDamageMultiplier => optionDamageMultiplier;
    private float optionDamageMultiplier = 1.0f;

    private void Awake()
    {
        currentHP = maxHP;
        currentBombCount = maxBombCount;
        bulletPool = new ObjectPool<PlayerBullet>(
            createFunc: () => { var b = Instantiate(commonBulletPrefab); b.SetPool(bulletPool); return b; },
            actionOnGet: b => { if (b) b.gameObject.SetActive(true); },
            actionOnRelease: b => { if (b) b.gameObject.SetActive(false); },
            actionOnDestroy: b => { if (b) Destroy(b.gameObject); },
            defaultCapacity: 40, maxSize: 100
        );
    }

    private void Start()
    {
        if (chargeEffect) chargeEffect.SetActive(false);
        if (hitboxMarkerRenderer) hitboxMarkerRenderer.enabled = false;
    }

    private void Update()
    {
        float speed = isSlowPressed ? slowSpeed : normalSpeed;
        Vector3 newPos = transform.position + (Vector3)(moveInput * (speed * Time.deltaTime));
        if (myField) transform.position = myField.ClampPosition(newPos);

        if (hitboxMarkerRenderer) hitboxMarkerRenderer.enabled = isSlowPressed;
        foreach (var opt in options) opt?.UpdatePosition(isSlowPressed);

        if (!isCharging)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + autoEnergyPerSec * Time.deltaTime);
        }
        else if (currentChargeProgress < currentEnergy)
        {
            currentChargeProgress = Mathf.Min(currentEnergy, currentChargeProgress + chargeSpeed * Time.deltaTime);
        }

        if (isCharging || !isShotPressed || !characterData || !characterData.optionBulletPrefab) return;
        optionShotTimer -= Time.deltaTime;
        if (optionShotTimer <= 0f)
        {
            optionShotTimer = characterData.optionShotInterval;
            foreach (var opt in options)
            {
                opt?.FireOptionShot(characterData.optionBulletPrefab, characterData.optionBulletSpeed, myField, optionDamageMultiplier);
            }
        }
    }

    // --- 入力ハンドラ (PlayerInput & CPU 共通受付) ---
    public void OnMove(InputValue value) => SetMoveInput(value.Get<Vector2>());
    public void OnSlow(InputValue value) => SetSlowInput(value.Get<float>() > 0.5f);
    public void OnShot(InputValue value) => SetShotInput(value.Get<float>() > 0.5f);
    public void OnCharge(InputValue value) => SetChargeInput(value.Get<float>() > 0.5f);
    public void OnBomb(InputValue value) { if (value.Get<float>() > 0.5f) ExecuteBombDirect(); }
    public void OnPause(InputValue value) { if (value.isPressed) PauseManager.Instance?.TogglePause(); }

    public void SetMoveInput(Vector2 move) => moveInput = move;
    public void SetSlowInput(bool slow) => isSlowPressed = slow;

    public void SetShotInput(bool pressed)
    {
        if (PauseManager.IsPaused) { isShotPressed = false; return; }
        isShotPressed = pressed;
        if (isShotPressed && !isCharging && commonShotCoroutine == null) commonShotCoroutine = StartCoroutine(CommonShotRoutine());
        else if (!isShotPressed && commonShotCoroutine != null) { StopCoroutine(commonShotCoroutine); commonShotCoroutine = null; }
    }

    public void SetChargeInput(bool pressed)
    {
        if (PauseManager.IsPaused) { ResetChargeVisual(); return; }

        if (pressed)
        {
            isCharging = true;
            currentChargeProgress = 0f;
            if (chargeEffect) chargeEffect.SetActive(true);
            SoundManager.Instance?.PlayPlayerChargeSE();
            if (commonShotCoroutine != null) { StopCoroutine(commonShotCoroutine); commonShotCoroutine = null; }
        }
        else if (isCharging)
        {
            ResetChargeVisual();
            ExecuteChargeAttack(currentChargeProgress);
            currentChargeProgress = 0f;
            if (isShotPressed && commonShotCoroutine == null) commonShotCoroutine = StartCoroutine(CommonShotRoutine());
        }
    }

    private void ResetChargeVisual()
    {
        isCharging = false;
        if (chargeEffect) chargeEffect.SetActive(false);
    }

    public void ExecuteBombDirect()
    {
        if (PauseManager.IsPaused || isCharging || currentBombCount <= 0 || Time.time < lastBombTime + bombCooldown) return;
        lastBombTime = Time.time;
        currentBombCount--;

        // 設定された固定値分だけエナジーを減少（0未満にはしない）
        currentEnergy = Mathf.Max(0f, currentEnergy - bombEnergyPenalty);
        OnBombExecuted?.Invoke(this);
        if (bombPrefab)
        {
            var bObj = Instantiate(bombPrefab, transform.position, Quaternion.identity);
            var cb = bObj.GetComponent<CommonBomb>();
            if (cb) cb.OwnerField = myField;
            SoundManager.Instance?.PlayPlayerBombSE();
        }
        TriggerInvincibility();
    }

    // --- チャージ攻撃 ---
    private void ExecuteChargeAttack(float chargedAmount)
    {
        PlayField opponent = myField ? myField.OpponentField : null;
        float attackPower = currentPower;
        bool isBossAlive = activeSpawnedBoss != null && !activeSpawnedBoss.IsDead;

        CharacterDataSO mainChar = CharacterSelectManager.Instance != null 
            ? CharacterSelectManager.Instance.GetCharacterData(playerIndex) 
            : characterData;

        AssistantDataSO assistChar = AssistantManager.Instance != null 
            ? AssistantManager.Instance.GetAssistantData(playerIndex) 
            : null;

        if (chargedAmount >= 100f)
        {
            if (isBossAlive)
            {
                CutinManager.Instance?.PlayCutin(playerIndex, 2, mainChar);
                ExecuteLv2Attack(opponent, attackPower);
                currentEnergy = Mathf.Max(0f, currentEnergy - 70f);
            }
            else
            {
                CutinManager.Instance?.PlayCutin(playerIndex, 3, mainChar, assistChar);
                currentEnergy = Mathf.Max(0f, currentEnergy - 100f);
                if (isLastStand) { isLastStand = false; currentHP = 5.0f; ApplyLastStandVisual(false); }

                GameObject burst = Instantiate(engageBurstPrefab ? engageBurstPrefab : bombPrefab, transform.position, Quaternion.identity);
                if (burst.TryGetComponent<CommonBomb>(out var cb)) cb.OwnerField = myField;

                if (opponent && characterData && characterData.engageAttackPrefab)
                {
                    var bossObj = Instantiate(characterData.engageAttackPrefab);
                    activeSpawnedBoss = bossObj.GetComponent<EngageBossBase>();
                    activeSpawnedBoss?.Initialize(opponent, attackPower);
                    AssistantManager.Instance?.SpawnAssistBoss(playerIndex, opponent, activeSpawnedBoss);
                }
                currentPower = Mathf.Min(9.0f, currentPower + 1.0f);
            }
            TriggerInvincibility();
            OnAttackExecuted?.Invoke(this);
        }
        else if (chargedAmount >= 70f)
        {
            CutinManager.Instance?.PlayCutin(playerIndex, 2, mainChar);
            currentEnergy = Mathf.Max(0f, currentEnergy - 70f);
            ExecuteLv2Attack(opponent, attackPower);
            TriggerInvincibility();
            OnAttackExecuted?.Invoke(this);
        }
        else if (chargedAmount >= 40f)
        {
            CutinManager.Instance?.PlayCutin(playerIndex, 1, mainChar);
            currentEnergy = Mathf.Max(0f, currentEnergy - 40f);
            if (opponent && characterData && characterData.normalAttackPrefab) SpawnAttackEmitter(characterData.normalAttackPrefab, opponent, attackPower);
            currentPower = Mathf.Min(9.0f, currentPower + 0.5f);
            TriggerInvincibility();
            OnAttackExecuted?.Invoke(this);
        }
    }

    private void ExecuteLv2Attack(PlayField opponent, float power)
    {
        if (opponent && characterData && characterData.specialAttackPrefab) SpawnAttackEmitter(characterData.specialAttackPrefab, opponent, power);
        currentPower = Mathf.Min(9.0f, currentPower + 0.5f);
    }

    private void SpawnAttackEmitter(GameObject prefab, PlayField targetField, float powerToUse)
    {
        if (Instantiate(prefab).TryGetComponent<IAttackEmitter>(out var emitter)) emitter.Initialize(targetField, powerToUse);
    }

    private IEnumerator CommonShotRoutine()
    {
        while (isShotPressed && !isCharging)
        {
            SpawnBullet(commonShotPosLeft);
            SpawnBullet(commonShotPosRight);
            yield return new WaitForSeconds(commonShotInterval);
        }
        commonShotCoroutine = null;
    }

    private void SpawnBullet(Transform spawnPos)
    {
        if (!spawnPos || bulletPool == null) return;
        PlayerBullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(spawnPos.position, Quaternion.identity);
        bullet.SetSpeed(commonBulletSpeed);
        bullet.SetField(myField);
        bullet.SetDamage(commonShotDamage * shotDamageMultiplier);
    }

    // --- ダメージ・耐久・無敵 ---
    public void TakeDamage(float damage)
    {
        if (isDefeated || (BattleGameManager.Instance && BattleGameManager.Instance.IsGameOver) || Time.time < lastDamageTime + invincibilityDuration) return;

        if (isLastStand && currentHP <= 0f)
        {
            lastDamageTime = Time.time;
            Defeat();
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
        AddEnergy(8.0f);

        if (currentHP <= 0f)
        {
            isLastStand = true;
            ApplyLastStandVisual(true);
            SoundManager.Instance?.PlayPlayerLastStandSE();
        }
        else
        {
            SoundManager.Instance?.PlayPlayerHitSE();
        }

        TriggerInvincibility();
    }

    private void TriggerInvincibility()
    {
        lastDamageTime = Time.time;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(InvincibilityBlink());
    }

    private IEnumerator InvincibilityBlink()
    {
        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            if (playerSprite) playerSprite.enabled = !playerSprite.enabled;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }
        if (playerSprite) playerSprite.enabled = true;
        blinkCoroutine = null;
    }

    private void Defeat()
    {
        isDefeated = true;
        SoundManager.Instance?.PlayPlayerDefeatSE();
        if (defeatExplosionPrefab) Instantiate(defeatExplosionPrefab, transform.position, Quaternion.identity);
        BattleGameManager.Instance?.OnPlayerDefeated(this);
        gameObject.SetActive(false);
    }

    public void ApplyHandicapStatus(float power, int bombCount, float invincibilityTime)
    {
        currentPower = power;
        maxBombCount = bombCount;
        currentBombCount = bombCount;
        invincibilityDuration = invincibilityTime;
    }

    // --- 各種パラメータ更新・補助 ---
    public void AddEnergy(float amount) => currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    public void AddEnergyPercent(float percent) => AddEnergy(maxEnergy * (percent / 100f));
    public void HealHPPercent(float percent) { if (!isLastStand) currentHP = Mathf.Min(maxHP, currentHP + maxHP * (percent / 100f)); }
    public void SetInvincibleState() { if (playerCollider) playerCollider.enabled = false; }
    private void ApplyLastStandVisual(bool active) { if (playerSprite) playerSprite.color = active ? new Color(1.0f, 0.5f, 0.5f, 1.0f) : Color.white; }

    public void SetupPlayer(int index, PlayField field) { playerIndex = index; myField = field; }
    public void SetShotDamageMultiplier(float multiplier)
    {
        shotDamageMultiplier = multiplier;
    }
    public void SetOptionDamageMultiplier(float multiplier)
    {
        optionDamageMultiplier = multiplier;
    }
    public void AddMaxBombCount(int amount)
    {
        maxBombCount += amount;
        currentBombCount = maxBombCount;
    }
    public void ReduceEP(int amount)
    {
        bombEnergyPenalty -= amount;
    }
    public void AddChargeSpeed(float amount)
    {
        chargeSpeed += amount;
    }
    public void AddNormalSpeed(float amount)
    {
        normalSpeed += amount;
    }
    public void ReduceNormalSpeed(float amount, float minSpeed = 4.0f)
    {
        normalSpeed = Mathf.Max(minSpeed, normalSpeed - amount);
    }

    public void AddAutoEnergyPerSec(float amount)
    {
        autoEnergyPerSec += amount;
    }

    public void HealHP(float amount)
    {
        if (!isLastStand && !isDefeated)
        {
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }
    }

    public void ReduceEnergy(float amount)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
    }

    public void AddInvincibilityDuration(float amount)
    {
        invincibilityDuration += amount;
    }
}