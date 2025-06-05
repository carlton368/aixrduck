using Fusion;
using UnityEngine;
using UnityEngine.Events;
using CuteDuckGame;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("플레이어 설정")]
    [Tooltip("플레이어 캐릭터 프리팹 (NetworkObject 포함)")]
    public GameObject PlayerPrefab;
    
    [Header("Unity Events")]
    [SerializeField] private UnityEvent<PlayerRef> OnPlayerSpawned;
    [SerializeField] private UnityEvent<Vector3> OnSpawnPositionUsed;

    // Action 이벤트
    public static System.Action<PlayerRef, Vector3> OnPlayerSpawnedAtPosition;

    // 디버그용 추가
    private void Awake()
    {
        Debug.Log("[PlayerSpawner] Awake() - PlayerSpawner 초기화됨");
        
        // 프리팹 할당 체크
        if (PlayerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] PlayerPrefab이 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
        else
        {
            Debug.Log($"[PlayerSpawner] PlayerPrefab 할당됨: {PlayerPrefab.name}");
            
            // NetworkObject 컴포넌트 체크
            NetworkObject netObj = PlayerPrefab.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("[PlayerSpawner] PlayerPrefab에 NetworkObject 컴포넌트가 없습니다!");
            }
            else
            {
                Debug.Log("[PlayerSpawner] PlayerPrefab에 NetworkObject 컴포넌트 확인됨");
            }
        }
    }

    private void Start()
    {
        Debug.Log("[PlayerSpawner] Start() - PlayerSpawner 시작");
        
        // NetworkRunner 상태 체크
        if (Runner == null)
        {
            Debug.LogWarning("[PlayerSpawner] NetworkRunner가 null입니다. 아직 초기화되지 않았을 수 있습니다.");
        }
        else
        {
            Debug.Log($"[PlayerSpawner] NetworkRunner 상태: IsRunning={Runner.IsRunning}, IsClient={Runner.IsClient}");
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] PlayerJoined 호출됨! Player: {player}, LocalPlayer: {Runner?.LocalPlayer}");
        
        // NetworkRunner 상태 재확인
        if (Runner == null)
        {
            Debug.LogError("[PlayerSpawner] NetworkRunner가 null입니다!");
            return;
        }
        
        Debug.Log($"[PlayerSpawner] Runner 상태 - IsRunning: {Runner.IsRunning}, IsClient: {Runner.IsClient}");
        
        // 로컬 플레이어만 스폰 (호스트 또는 클라이언트 자기 자신)
        if (player == Runner.LocalPlayer)
        {
            Debug.Log($"[PlayerSpawner] 로컬 플레이어 감지! 스폰 시작: {player}");
            
            if (PlayerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] PlayerPrefab이 null입니다! 스폰 불가능!");
                return;
            }
            
            Vector3 spawnPosition = GetSpawnPosition(player);
            Debug.Log($"[PlayerSpawner] 계산된 스폰 위치: {spawnPosition}");
            
            try
            {
                // 플레이어 스폰
                NetworkObject playerObject = Runner.Spawn(PlayerPrefab,
                             spawnPosition,
                             Quaternion.identity,
                             player); // InputAuthority 설정
                
                Debug.Log($"[PlayerSpawner] 플레이어 스폰 성공! Player: {player} at {spawnPosition}, NetworkObject: {playerObject?.name}");
                
                // 이벤트 발생
                OnPlayerSpawned?.Invoke(player);
                OnSpawnPositionUsed?.Invoke(spawnPosition);
                OnPlayerSpawnedAtPosition?.Invoke(player, spawnPosition);
                
                Debug.Log("[PlayerSpawner] 스폰 이벤트 발생 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerSpawner] 플레이어 스폰 실패! Exception: {e.Message}");
                Debug.LogError($"[PlayerSpawner] StackTrace: {e.StackTrace}");
            }
        }
        else
        {
            Debug.Log($"[PlayerSpawner] 다른 플레이어 참가: {player} (로컬 플레이어 아님)");
        }
    }
    
    /// 스폰 위치 계산
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        Debug.Log("[PlayerSpawner] GetSpawnPosition() 시작");
        
        Vector3 basePosition;
        
        basePosition = StaticData.GetCurrentSpawnPosition();
        Debug.Log($"[PlayerSpawner] AR 위치 사용: {basePosition}");
        
        
        // 최종 스폰 위치
        Vector3 finalPosition = basePosition;
        
        Debug.Log($"[PlayerSpawner] 최종 스폰 위치 계산: base={basePosition},final={finalPosition}");
        
        return finalPosition;
    }
    // 추가 디버그 메서드
    private void Update()
    {
        // 30초마다 상태 체크
        if (Time.time % 30f < Time.deltaTime)
        {
            CheckStatus();
        }
    }

    private void CheckStatus()
    {
        Debug.Log($"[PlayerSpawner] 상태 체크 - Runner: {(Runner != null ? "있음" : "없음")}, " +
                 $"IsRunning: {Runner?.IsRunning}");
        
        if (Runner != null && Runner.IsRunning)
        {
            Debug.Log($"[PlayerSpawner] 현재 플레이어들: LocalPlayer={Runner.LocalPlayer}");
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[PlayerSpawner] OnDestroy() - PlayerSpawner 파괴됨");
    }
}