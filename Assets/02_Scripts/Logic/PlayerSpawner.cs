using Fusion;
using UnityEngine;
using UnityEngine.Events;
using CuteDuckGame;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("플레이어 설정")] [Tooltip("플레이어 캐릭터 프리팹 (NetworkObject 포함)")]
    public GameObject PlayerPrefab;

    private Vector3 spawnPosAdditionalOffset = new Vector3(0, 5, 0);
    
    [Header("Unity Events")] [SerializeField]
    private UnityEvent<PlayerRef> OnPlayerSpawned;

    [SerializeField] private UnityEvent<Vector3> OnSpawnPositionUsed;

    // Action 이벤트
    public static System.Action<PlayerRef, Vector3> OnPlayerSpawnedAtPosition;

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] PlayerJoined 호출됨! Player: {player}, LocalPlayer: {Runner?.LocalPlayer}");

        Debug.Log($"[PlayerSpawner] Runner 상태 - IsRunning: {Runner.IsRunning}, IsClient: {Runner.IsClient}");

        if (player == Runner.LocalPlayer)
        {
            Debug.Log($"[PlayerSpawner] 로컬 플레이어 감지! 스폰 시작: {player}");


            // 스폰 위치 계산 시도
            if (!TryGetSpawnPosition(player, out Vector3 spawnPosition))
            {
                Debug.LogError("[PlayerSpawner] 유효한 스폰 위치를 찾을 수 없습니다! Player 스폰 중단");
                return;
            }

            Debug.Log($"[PlayerSpawner] 계산된 스폰 위치: {spawnPosition}");
            
            


            // 플레이어 스폰
            NetworkObject playerObject = Runner.Spawn(PlayerPrefab,
                spawnPosition+spawnPosAdditionalOffset,
                Quaternion.identity,
                player); // InputAuthority 설정

            Debug.Log(
                $"[PlayerSpawner] 플레이어 스폰 성공! Player: {player} at {spawnPosition}, NetworkObject: {playerObject?.name}");

            // Map의 자식으로 설정 (선택사항)
            SetPlayerAsMapChild(playerObject, spawnPosition);

            // 이벤트 발생
            OnPlayerSpawned?.Invoke(player);
            OnSpawnPositionUsed?.Invoke(spawnPosition);
            OnPlayerSpawnedAtPosition?.Invoke(player, spawnPosition);

            Debug.Log("[PlayerSpawner] 스폰 이벤트 발생 완료");
        }
        else
        {
            Debug.Log($"[PlayerSpawner] 다른 플레이어 참가: {player} (로컬 플레이어 아님)");
        }
    }

    private bool TryGetSpawnPosition(PlayerRef player, out Vector3 spawnPosition)
    {
        Debug.Log("[PlayerSpawner] TryGetSpawnPosition() 시작");

        // 배치된 Map 찾기
        GameObject placedMap = GetPlacedMap();
        Debug.Log($"[PlayerSpawner] 배치된 Map 발견: {placedMap.name}");

        // 원하는 로컬 오프셋(로컬 좌표에서의 위치)을 정의
        // 필요에 따라 Vector3(x, y, z) 형태로 값을 변경하세요.
        Vector3 localOffset = Vector3.zero;

        // 로컬 포지션을 월드 포지션으로 변환
        Vector3 finalPosition = placedMap.transform.TransformPoint(localOffset);

        Debug.Log($"[PlayerSpawner] Map 기준 스폰 - Map위치: {placedMap.transform.position}, " +
                  $"로컬오프셋: {localOffset}, 최종위치: {finalPosition}");

        spawnPosition = finalPosition;

        return true;
    }

    /// 배치된 Map GameObject 찾기
    private GameObject GetPlacedMap()
    {
        // GameObject.FindWithTag("GameMap")를 사용하여 Map 찾기
        GameObject map = GameObject.FindWithTag("GameMap");
        Debug.Log($"[PlayerSpawner] 배치된 Map 발견: {map.name}");
        return map;
    }

    /// Player를 Map의 자식으로 설정 (선택사항)
    private void SetPlayerAsMapChild(NetworkObject playerObject, Vector3 worldSpawnPosition)
    {
        GameObject placedMap = GetPlacedMap();
        // World position을 Map의 local position으로 변환
        Vector3 localPosition = placedMap.transform.InverseTransformPoint(worldSpawnPosition);

        // Player를 Map의 자식으로 설정
        playerObject.transform.SetParent(placedMap.transform);
        playerObject.transform.localPosition = localPosition;

        Debug.Log($"[PlayerSpawner] Player를 Map({placedMap.name})의 자식으로 설정 - 로컬위치: {localPosition}");
    }
}