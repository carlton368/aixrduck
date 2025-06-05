using UnityEngine;
using System;

namespace CuteDuckGame
{
    public static class StaticData
    {
        public static string CurrentRoomName = "123";
        
        // AR 위치 관련 추가
        public static Vector3 _spawnPos;
        
        // 이벤트 - Action 사용
        public static Action<Vector3> OnSpawnPositionChanged;
        
        /// 스폰 위치 설정
        public static void SetSpawnPos(Vector3 pos)
        {
            _spawnPos = pos;
            
            Debug.Log($"[StaticData] 스폰 위치 설정: {pos}");
            
            // 이벤트 발생
            OnSpawnPositionChanged?.Invoke(pos);
        }
        
        /// 현재 스폰 위치 반환
        public static Vector3 GetCurrentSpawnPosition()
        {
            return _spawnPos;
        }
    }
}