using Fusion;
using UnityEngine;
using UnityEngine.Events;
using CuteDuckGame;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("플레이어 설정")]
    [Tooltip("플레이어 캐릭터 프리팹 (NetworkObject 포함)")]
    public GameObject PlayerPrefab;
    
    [Header("스폰 설정")]
    [Tooltip("Map 기준 로컬 스폰 반경")]
    public float spawnRadius = 2f;
    [Tooltip("Map 기준 스폰 높이 오프셋")]
    public float spawnHeightOffset = 50f;
    
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
            
            // 스폰 위치 계산 시도
            if (!TryGetSpawnPosition(player, out Vector3 spawnPosition))
            {
                Debug.LogError("[PlayerSpawner] 유효한 스폰 위치를 찾을 수 없습니다! Player 스폰 중단");
                return;
            }
            
            Debug.Log($"[PlayerSpawner] 계산된 스폰 위치: {spawnPosition}");
            
            try
            {
                // 플레이어 스폰
                NetworkObject playerObject = Runner.Spawn(PlayerPrefab,
                             spawnPosition,
                             Quaternion.identity,
                             player); // InputAuthority 설정
                
                Debug.Log($"[PlayerSpawner] 플레이어 스폰 성공! Player: {player} at {spawnPosition}, NetworkObject: {playerObject?.name}");
                
                // Map의 자식으로 설정 (선택사항)
                SetPlayerAsMapChild(playerObject, spawnPosition);
                
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
    
    /// 스폰 위치 계산 - Map 기준 로컬 포지션 사용
    private bool TryGetSpawnPosition(PlayerRef player, out Vector3 spawnPosition)
    {
        Debug.Log("[PlayerSpawner] TryGetSpawnPosition() 시작");
        
        // 배치된 Map 찾기
        GameObject placedMap = GetPlacedMap();
        
        if (placedMap != null)
        {
            Debug.Log($"[PlayerSpawner] 배치된 Map 발견: {placedMap.name}");
            
            // Map 기준 랜덤 로컬 포지션 계산
            Vector3 localSpawnOffset = new Vector3(
                UnityEngine.Random.Range(-spawnRadius, spawnRadius),
                spawnHeightOffset,
                UnityEngine.Random.Range(-spawnRadius, spawnRadius)
            );
            
            // 로컬 포지션을 월드 포지션으로 변환
            Vector3 finalPosition = placedMap.transform.TransformPoint(localSpawnOffset);
            
            Debug.Log($"[PlayerSpawner] Map 기준 스폰 - Map위치: {placedMap.transform.position}, " +
                     $"로컬오프셋: {localSpawnOffset}, 최종위치: {finalPosition}");
            
            spawnPosition = finalPosition;
            return true;
        }
        
        Debug.LogError("[PlayerSpawner] 배치된 Map을 찾을 수 없습니다! 스폰 불가능!");
        spawnPosition = Vector3.zero;
        return false;
    }
    
    /// 배치된 Map GameObject 찾기
    private GameObject GetPlacedMap()
    {
        // GameObject.FindWithTag("GameMap")를 사용하여 Map 찾기
        GameObject map = GameObject.FindWithTag("GameMap");
        
        if (map != null)
        {
            Debug.Log($"[PlayerSpawner] 배치된 Map 발견: {map.name}");
            return map;
        }
        
        Debug.LogWarning("[PlayerSpawner] Map을 찾을 수 없습니다!");
        return null;
    }
    
    /// Player를 Map의 자식으로 설정 (선택사항)
    private void SetPlayerAsMapChild(NetworkObject playerObject, Vector3 worldSpawnPosition)
    {
        if (playerObject == null) return;
        
        GameObject placedMap = GetPlacedMap();
        if (placedMap != null)
        {
            // World position을 Map의 local position으로 변환
            Vector3 localPosition = placedMap.transform.InverseTransformPoint(worldSpawnPosition);
            
            // Player를 Map의 자식으로 설정
            playerObject.transform.SetParent(placedMap.transform);
            playerObject.transform.localPosition = localPosition;
            
            Debug.Log($"[PlayerSpawner] Player를 Map({placedMap.name})의 자식으로 설정 - 로컬위치: {localPosition}");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Map을 찾을 수 없어 Player를 자식으로 설정하지 못했습니다.");
        }
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
        
        // Map 상태 체크
        GameObject map = GameObject.FindWithTag("GameMap");
        Debug.Log($"[PlayerSpawner] Map 상태: {(map != null ? $"발견됨({map.name})" : "없음")}");
    }

    private void OnDestroy()
    {
        Debug.Log("[PlayerSpawner] OnDestroy() - PlayerSpawner 파괴됨");
    }
}