using System.Collections;
using System.Linq;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.Events;

namespace CuteDuckGame
{
    /// <summary>
    /// 간단한 오리게임 세션 관리자
    /// - 3초 타이머
    /// - 오리 프리팹 생성
    /// - 기본 UI 업데이트
    /// </summary>
    public class GameSessionManager : NetworkBehaviour
    {
        [Header("게임 설정")]
        [SerializeField] private float duckSpawnCycle = 3f;
        [SerializeField] private int maxDucksAtOnce = 20;
        
        [Header("오리 프리팹")]
        [SerializeField] private GameObject duckPrefab;  // Inspector에서 직접 할당
        
        [Header("스폰 설정")]
        //[SerializeField] private Vector3 spawnCenterOffset = Vector3.zero;
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private bool useARPosition = true;
        
        [Header("UI 연결")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        
        [Header("Unity Events")]
        [SerializeField] private UnityEvent<int> OnDuckSpawnCycleComplete;
        [SerializeField] private UnityEvent<float> OnTimerUpdated;

        // 네트워크 동기화 변수들
        [Networked] public float ServerTimer { get; set; }
        [Networked] public int ConnectedPlayers { get; set; }
        [Networked] public bool ShouldSpawnDucks { get; set; }
        [Networked] public bool IsGameActive { get; set; }

        // 로컬 변수들
        private bool lastSpawnState = false;
        private int currentDuckCount = 0;  // 현재 생성된 오리 수 추적
        private Vector3 dynamicSpawnCenter;
        
        // Action 이벤트
        public static System.Action<int> OnDucksSpawned;
        public static System.Action<float> OnGameTimerChanged;
        
        // ==============================================
        // Fusion2 생명주기
        // ==============================================
        
        public override void Spawned()
        {
            Debug.Log($"[GameSessionManager] Spawned - HasStateAuthority: {Object.HasStateAuthority}");
            
            if (Object.HasStateAuthority)
            {
                ServerTimer = duckSpawnCycle;
                IsGameActive = true;
                ConnectedPlayers = Runner.ActivePlayers.Count();
            }
            
            InitializeComponents();
            UpdateSpawnCenter();
        }
        
        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && IsGameActive)
            {
                UpdateTimer();
                UpdatePlayerCount();
            }
            
            CheckDuckSpawnState();
        }
        
        void Update()
        {
            if (Object != null && Object.IsValid)
            {
                UpdateUI();
            }
        }
        
        // ==============================================
        // 스폰 센터 업데이트
        // ==============================================
        
        /// AR 위치를 기반으로 스폰 센터 업데이트
        private void UpdateSpawnCenter()
        {
            dynamicSpawnCenter = StaticData.GetCurrentSpawnPosition();
            Debug.Log($"[GameSessionManager] AR 위치 기반 스폰 센터: {dynamicSpawnCenter}");
        }
        
        // ==============================================
        // 타이머 시스템
        // ==============================================
        
        private void UpdateTimer()
        {
            ServerTimer -= Runner.DeltaTime;
            
            // 이벤트 발생
            OnTimerUpdated?.Invoke(ServerTimer);
            OnGameTimerChanged?.Invoke(ServerTimer);
            
            if (ServerTimer <= 0f)
            {
                Debug.Log("[GameSessionManager] 타이머 완료! 오리 생성!");
                ShouldSpawnDucks = true;
                ServerTimer = duckSpawnCycle;
                
                // 3초 후 생성 중단
                StartCoroutine(StopSpawning());
            }
        }
        
        private IEnumerator StopSpawning()
        {
            yield return new WaitForSeconds(3f);
            ShouldSpawnDucks = false;
        }
        
        private void UpdatePlayerCount()
        {
            int currentCount = Runner.ActivePlayers.Count();
            if (ConnectedPlayers != currentCount)
            {
                ConnectedPlayers = currentCount;
                Debug.Log($"[GameSessionManager] 플레이어 수: {ConnectedPlayers}명");
            }
        }
        
        // ==============================================
        // 오리 생성 시스템
        // ==============================================
        
        private void CheckDuckSpawnState()
        {
            if (ShouldSpawnDucks != lastSpawnState)
            {
                lastSpawnState = ShouldSpawnDucks;
                
                if (ShouldSpawnDucks)
                {
                    SpawnDucks();
                }
            }
        }
        
        private void SpawnDucks()
        {
            if (duckPrefab == null)
            {
                Debug.LogWarning("[GameSessionManager] 오리 프리팹이 할당되지 않았습니다!");
                return;
            }
            
            // 플레이어 수에 따른 오리 개수 계산
            int duckCount = Mathf.Clamp(ConnectedPlayers / 2 + 1, 1, 8);
            
            Debug.Log($"[GameSessionManager] 오리 {duckCount}마리 생성 시작! 스폰 센터: {dynamicSpawnCenter}");
            
            // 오리 생성
            for (int i = 0; i < duckCount; i++)
            {
                SpawnSingleDuck();
            }
            
            // 이벤트 발생
            OnDuckSpawnCycleComplete?.Invoke(duckCount);
            OnDucksSpawned?.Invoke(duckCount);
        }
        
        private void SpawnSingleDuck()
        {
            if (duckPrefab == null) return;
    
            if (currentDuckCount >= maxDucksAtOnce)
            {
                return;
            }
    
            // 동적 스폰 센터 기반으로 위치 설정
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0,
                Random.Range(-spawnRadius, spawnRadius)
            );
    
            Vector3 spawnPos = dynamicSpawnCenter + randomOffset;
    
            // 각 축별 랜덤 회전 설정 (더 직관적)
            Vector3 randomEulerAngles = new Vector3(
                Random.Range(0f, 360f),    // X축 회전 (0~360도)
                Random.Range(0f, 360f),    // Y축 회전 (0~360도)
                Random.Range(0f, 360f)     // Z축 회전 (0~360도)
            );
    
            Quaternion randomRotation = Quaternion.Euler(randomEulerAngles);
    
            // 오리 생성
            Runner.Spawn(duckPrefab, spawnPos, randomRotation);
            currentDuckCount++;
        }
        
        // ==============================================
        // 자동 설정
        // ==============================================
        
        private void InitializeComponents()
        {
            // UI 컴포넌트 자동 찾기
            if (timerText == null)
                timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
            if (playerCountText == null)
                playerCountText = GameObject.Find("PlayerCountText")?.GetComponent<TextMeshProUGUI>();
            
            Debug.Log($"[GameSessionManager] 초기화 완료 - Timer: {timerText != null}, PlayerCount: {playerCountText != null}");
        }
        
        // ==============================================
        // UI 업데이트
        // ==============================================
        
        private void UpdateUI()
        {
            if (Object == null || !Object.IsValid)
            {
                if (timerText != null) timerText.text = "--";
                if (playerCountText != null) playerCountText.text = "접속자: --명";
                return;
            }
            
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(ServerTimer);
                timerText.text = $"{seconds:00}";
            }
            
            if (playerCountText != null)
            {
                playerCountText.text = $"진짜 오리 {ConnectedPlayers}마리가 숨었다!?";
            }
        }
    
    }
}