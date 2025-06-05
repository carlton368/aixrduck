using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace CuteDuckGame
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")] [SerializeField] private GameObject arScanPanel;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject gamePanel;

        [Header("UI Components")] [SerializeField]
        private EventSystem eventSystem;

        [SerializeField] private Button playButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI phaseMessageText;

        public static UIManager Instance;
        public static Action OnGameStartRequested;
        public static Action OnGameLeaveRequested;

        private void Awake()
        {
            Instance = this;

            // GameFlowController 이벤트 구독
            GameFlowController.OnPhaseChanged += OnPhaseChanged;
            GameFlowController.OnPhaseMessage += OnPhaseMessageChanged;
        }

        private void OnPhaseChanged(GamePhase newPhase)
        {
            // 모든 패널 비활성화
            HideAllPanels();

            // 단계별 UI 활성화
            switch (newPhase)
            {
                case GamePhase.ARInitializing:
                case GamePhase.ARScanning:
                    ShowPanel("ARScanPanel");
                    EnablePlayButton(false);
                    break;

                case GamePhase.Ready:
                    ShowPanel("ARScanPanel");
                    EnablePlayButton(true);
                    break;

                case GamePhase.Connecting:
                    ShowPanel("ConnectionPanel");
                    break;

                case GamePhase.Playing:
                    ShowPanel("GamePanel");
                    EnableLeaveButton(true);
                    break;
            }
        }

        private void OnPhaseMessageChanged(string message)
        {
            if (phaseMessageText != null)
            {
                phaseMessageText.text = message;
            }
        }

        public void ShowPanel(string panelName)
        {
            HideAllPanels();

            switch (panelName)
            {
                case "ARScanPanel":
                    arScanPanel?.SetActive(true);
                    break;
                case "TitlePanel":
                    titlePanel?.SetActive(true);
                    break;
                case "ConnectionPanel":
                    connectionPanel?.SetActive(true);
                    break;
                case "GamePanel":
                    gamePanel?.SetActive(true);
                    break;
            }
        }

        private void HideAllPanels()
        {
            arScanPanel?.SetActive(false);
            titlePanel?.SetActive(false);
            connectionPanel?.SetActive(false);
            gamePanel?.SetActive(false);
        }

        public void EnablePlayButton(bool enabled)
        {
            if (playButton)
            {
                playButton.interactable = enabled;
            }
        }

        public void EnableLeaveButton(bool enabled)
        {
            if (leaveButton != null)
            {
                leaveButton.gameObject.SetActive(enabled);
            }
        }

        public void OnPlayButtonClicked()
        {
            Debug.Log("[UIManager] 플레이 버튼 클릭됨");
            OnGameStartRequested?.Invoke();
        }

        public void OnLeaveButtonClicked()
        {
            Debug.Log("[UIManager] 나가기 버튼 클릭됨");
            OnGameLeaveRequested?.Invoke();
        }

        public void ToggleInteraction(bool isOn)
        {
            eventSystem.enabled = isOn;
        }

        private void OnDestroy()
        {
            GameFlowController.OnPhaseChanged -= OnPhaseChanged;
            GameFlowController.OnPhaseMessage -= OnPhaseMessageChanged;
        }
    }
}