using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class CharacterSelectManager : MonoBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    [System.Serializable]
    public class CharacterEntry
    {
        public string characterId;
        public string characterName;
        public GameObject shipPrefab;
    }

    [Header("使用可能キャラクターリスト")]
    [SerializeField] private List<CharacterEntry> characterList = new List<CharacterEntry>();

    [Header("対戦フィールド参照")]
    [SerializeField] private PlayField field1P;
    [SerializeField] private PlayField field2P;

    [Header("初期スポーン座標")]
    [SerializeField] private Vector3 spawnPos1P = new Vector3(-4.0f, -3.5f, 0f);
    [SerializeField] private Vector3 spawnPos2P = new Vector3(4.0f, -3.5f, 0f);

    [Header("HUD参照")]
    [SerializeField] private GameHUD hud1P;
    [SerializeField] private GameHUD hud2P;

    [Header("テスト用初期選択")]
    [SerializeField] private int p1SelectedCharIndex = 3;
    [SerializeField] private int p2SelectedCharIndex = 1;

    [Header("CPU 操作設定")]
    [SerializeField] private bool p1IsCPU = false;
    [SerializeField] private CPUDifficulty p1CpuDifficulty = CPUDifficulty.Normal;

    [SerializeField] private bool p2IsCPU = false;
    [SerializeField] private CPUDifficulty p2CpuDifficulty = CPUDifficulty.Normal;

    public int P1CharIndex => p1SelectedCharIndex;
    public int P2CharIndex => p2SelectedCharIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (BattleSettingData.Instance != null && BattleSettingData.Instance.IsConfigured)
        {
            var data = BattleSettingData.Instance;
            p1SelectedCharIndex = data.P1Data.mainCharIndex;
            p1IsCPU = data.P1Data.isCPU;
            p1CpuDifficulty = data.P1Data.cpuDifficulty;

            // ★ アーケードモード時：事前一括抽選された敵データをステージに応じて取得
            if (data.IsArcadeMode)
            {
                p2IsCPU = true;

                int stageIndex = Mathf.Clamp(data.CurrentArcadeStage - 1, 0, 2);
                var opponentData = data.ArcadeOpponents[stageIndex];

                p2SelectedCharIndex = opponentData.mainCharIndex;
                int p2AssistantIndex = opponentData.assistCharIndex;

                // ステージ進行に応じた難易度設定 (1: Easy / 2: Normal / 3: Hard)
                p2CpuDifficulty = data.CurrentArcadeStage switch
                {
                    1 => CPUDifficulty.Easy,
                    2 => CPUDifficulty.Normal,
                    _ => CPUDifficulty.Hard
                };

                // BattleSettingData 側にも反映
                data.P2Data.mainCharIndex = p2SelectedCharIndex;
                data.P2Data.assistCharIndex = p2AssistantIndex;
                data.P2Data.isCPU = true;
                data.P2Data.cpuDifficulty = p2CpuDifficulty;
            }
            else
            {
                p2SelectedCharIndex = data.P2Data.mainCharIndex;
                p2IsCPU = data.P2Data.isCPU;
                p2CpuDifficulty = data.P2Data.cpuDifficulty;
            }
        }
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        var gamepads = Gamepad.all;
        List<InputDevice> p1Devices = new List<InputDevice>();
        List<InputDevice> p2Devices = new List<InputDevice>();

        if (Keyboard.current != null)
        {
            p1Devices.Add(Keyboard.current);
        }

        if (gamepads.Count >= 2)
        {
            p1Devices.Add(gamepads[0]);
            p2Devices.Add(gamepads[1]);
        }
        else if (gamepads.Count == 1)
        {
            if (p2IsCPU) p1Devices.Add(gamepads[0]);
            else p2Devices.Add(gamepads[0]);
        }

        // --- 1P 生成 ---
        PlayerShip ship1 = null;
        if (characterList.Count > p1SelectedCharIndex)
        {
            GameObject p1Prefab = characterList[p1SelectedCharIndex].shipPrefab;
            ship1 = InstantiatePlayer(p1Prefab, spawnPos1P, 0, field1P, p1Devices.ToArray());
        }

        // --- 2P 生成 ---
        PlayerShip ship2 = null;
        if (characterList.Count > p2SelectedCharIndex)
        {
            GameObject p2Prefab = characterList[p2SelectedCharIndex].shipPrefab;
            ship2 = InstantiatePlayer(p2Prefab, spawnPos2P, 1, field2P, p2Devices.ToArray());
        }

        // サポートスキルの初期適用
        AssistantManager assistMgr = AssistantManager.Instance != null ? AssistantManager.Instance : FindAnyObjectByType<AssistantManager>();
        if (assistMgr != null)
        {
            assistMgr.ApplyInitialSupport(ship1, ship2);
        }

        // ハンデコマンドの最終上書き適用 (ワンマッチ時)
        if (BattleSettingData.Instance != null)
        {
            var bsd = BattleSettingData.Instance;

            if (bsd.P1Handicap && ship1 != null)
            {
                ship1.ApplyHandicapStatus(9.0f, 9, 3.0f);
            }

            if (bsd.P2Handicap && ship2 != null)
            {
                ship2.ApplyHandicapStatus(9.0f, 9, 3.0f);
            }
        }

        // HUD初期化
        if (hud1P != null && ship1 != null) hud1P.SetTargetPlayer(ship1);
        if (hud2P != null && ship2 != null) hud2P.SetTargetPlayer(ship2);

        // ★ BGM再生仕様の制御
        AudioClip p1Bgm = characterList.Count > p1SelectedCharIndex ? characterList[p1SelectedCharIndex].shipPrefab.GetComponent<PlayerShip>()?.CharacterData?.themeBGM : null;
        AudioClip p2Bgm = characterList.Count > p2SelectedCharIndex ? characterList[p2SelectedCharIndex].shipPrefab.GetComponent<PlayerShip>()?.CharacterData?.themeBGM : null;

        AudioClip pickedBGM;

        if (BattleSettingData.Instance != null && BattleSettingData.Instance.IsArcadeMode)
        {
            // アーケードモード時は、必ず2P(CPU)のテーマBGMを直接参照して再生
            pickedBGM = (p2Bgm != null) ? p2Bgm : p1Bgm;
        }
        else
        {
            // ワンマッチ等の通常時は50%のランダム抽選
            pickedBGM = (Random.value < 0.5f) ? (p1Bgm != null ? p1Bgm : p2Bgm) : (p2Bgm != null ? p2Bgm : p1Bgm);
        }

        if (pickedBGM != null) SoundManager.Instance?.PlayBGM(pickedBGM);
    }

    private PlayerShip InstantiatePlayer(GameObject prefab, Vector3 spawnPos, int index, PlayField field, InputDevice[] devices)
    {
        if (prefab == null) return null;

        GameObject playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        playerObj.name = $"Player_{index + 1}P";

        bool isCPU = (index == 0) ? p1IsCPU : p2IsCPU;

        if (isCPU)
        {
            CPUShip cpu = playerObj.AddComponent<CPUShip>();
            cpu.SetDifficulty((index == 0) ? p1CpuDifficulty : p2CpuDifficulty);
        }
        else
        {
            PlayerInput pInput = playerObj.GetComponent<PlayerInput>();
            if (pInput != null && devices != null && devices.Length > 0)
            {
                pInput.user.UnpairDevices();
                foreach (var device in devices)
                {
                    if (device != null)
                    {
                        InputUser.PerformPairingWithDevice(device, pInput.user);
                    }
                }

                if (pInput.actions != null)
                {
                    pInput.actions.bindingMask = null;
                }
            }
        }

        PlayerShip ship = playerObj.GetComponent<PlayerShip>();
        if (ship != null)
        {
            ship.SetupPlayer(index, field);
        }

        return ship;
    }

    public CharacterDataSO GetCharacterData(int playerIndex)
    {
        int idx = (playerIndex == 0) ? p1SelectedCharIndex : p2SelectedCharIndex;
        if (idx >= 0 && idx < characterList.Count && characterList[idx].shipPrefab != null)
        {
            return characterList[idx].shipPrefab.GetComponent<PlayerShip>()?.CharacterData;
        }
        return null;
    }
}