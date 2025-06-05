using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace CuteDuckGame
{
    public class RaycastWithTrackableTypes : MonoBehaviour
    {
        [Header("AR 컴포넌트")] [SerializeField] private GameObject indicator;
        [Header("유저별 맵 프리팹")] [SerializeField] private GameObject mapPrefab;
        
        private GameObject placededMap;
        [SerializeField] private ARRaycastManager raycastManager;

        private List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private Vector3 currentSelectedPosition = Vector3.zero;
        private bool hasValidPosition = false;
        private bool indicatorEnabled = true;

        public static Action<Vector3> OnARPositionChanged;
        public static Action<bool> OnARPositionValidityChanged;

        private void Start()
        {
            indicator.SetActive(true);
            raycastManager = GetComponent<ARRaycastManager>();
            
            // mapPrefab 할당 확인
            if (mapPrefab == null)
            {
                Debug.LogError("[RaycastWithTrackableTypes] mapPrefab이 할당되지 않았습니다!");
            }
        }

        private void Update()
        {
            if (indicatorEnabled)
            {
                DetectGround();

                if (indicator.activeInHierarchy && Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    
                    // UI 터치 차단 확인
                    if (IsPointerOverUIObject(touch.position))
                    {
                        Debug.Log("UI 요소를 터치했습니다. 맵 생성을 건너뜁니다.");
                        return;
                    }
                    
                    Debug.Log("화면 터치 발생");
                    if (touch.phase == TouchPhase.Began)
                    {
                        Debug.Log("화면 터치 시작");
                        if (!placededMap)
                            CreateMap();
                        else
                        {
                            ChangeMapPosition();
                            Debug.Log("맵 위치 변경");
                        }
                        StaticData.SetSpawnPos(currentSelectedPosition);
                    }

                    if (touch.phase == TouchPhase.Ended)
                    {
                        Debug.Log("화면 터치 끝");
                    }
                }
            }
        }

        private void ChangeMapPosition()
        {
            placededMap.transform.position = currentSelectedPosition;
        }

        private void CreateMap()
        {
            Debug.Log("맵 생성 시작");
            
            // null 체크 추가
            if (mapPrefab == null)
            {
                Debug.LogError("[RaycastWithTrackableTypes] mapPrefab이 null입니다! Inspector에서 할당해주세요.");
                return;
            }
            
            Vector3 spawnPosition = StaticData.GetCurrentSpawnPosition();
            Quaternion spawnRotation = indicator != null ? indicator.transform.rotation : Quaternion.identity;
            
            placededMap = Instantiate(mapPrefab, spawnPosition, spawnRotation);
            Debug.Log($"맵 생성 완료: {placededMap.name} at {spawnPosition}");
        }

        private bool IsPointerOverUIObject(Vector2 touchPosition)
        {
            // EventSystem이 준비되지 않았으면 false 반환
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[RaycastWithTrackableTypes] EventSystem이 아직 초기화되지 않았습니다.");
                return false;
            }
    
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = touchPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void DetectGround()
        {
            Vector2 screenPoint = new Vector2(Screen.width / 2, Screen.height / 2);

            if (raycastManager.Raycast(screenPoint, hits, TrackableType.Planes))
            {
                indicator.SetActive(true);
                indicator.transform.position = hits[0].pose.position;
                indicator.transform.rotation = hits[0].pose.rotation;
                indicator.transform.position += indicator.transform.up * 0.1f;

                Vector3 newPosition = hits[0].pose.position;
                currentSelectedPosition = newPosition;
                OnARPositionChanged?.Invoke(currentSelectedPosition);

                if (!hasValidPosition)
                {
                    hasValidPosition = true;
                    OnARPositionValidityChanged?.Invoke(true);
                }
            }
            else
            {
                indicator.SetActive(false);

                if (hasValidPosition)
                {
                    hasValidPosition = false;
                    OnARPositionValidityChanged?.Invoke(false);
                }
            }
        }

        public void SetIndicatorEnabled(bool enabled)
        {
            indicatorEnabled = enabled;
            if (!enabled && indicator != null)
                indicator.SetActive(false);
        }
    }
}