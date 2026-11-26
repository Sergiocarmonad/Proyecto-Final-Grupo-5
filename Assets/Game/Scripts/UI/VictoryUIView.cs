using Game.Flow;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Presentation bridge that displays final level metrics when the victory sequence starts.
    /// </summary>
    public sealed class VictoryUIView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private Camera _victoryCamera;
        [SerializeField] private LevelFlowController _levelFlowController;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI _trashText;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _bonusPointsText;

        [Header("Formatting")]
        [SerializeField] private string _trashFormat = "{0}/{1}";
        [SerializeField] private string _healthMissingFormat = "{0}%";
        [SerializeField] private string _bonusPointsFormat = "{0}";

        private void Start()
        {
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }

            SetVictoryCameraActive(false);

            if (_levelFlowController != null)
            {
                _levelFlowController.VictorySequenceStarted += HandleVictorySequenceStarted;
            }
        }

        private void OnDisable()
        {
            SetVictoryCameraActive(false);
        }

        private void OnDestroy()
        {
            if (_levelFlowController != null)
            {
                _levelFlowController.VictorySequenceStarted -= HandleVictorySequenceStarted;
            }
        }

        private void HandleVictorySequenceStarted(LevelResult result)
        {
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(true);
            }

            SetVictoryCameraActive(true);

            if (_trashText != null)
            {
                _trashText.text = string.Format(_trashFormat, result.TrashCollected, result.TotalTrash);
            }

            if (_healthText != null)
            {
                int missingHealthPercent = Mathf.RoundToInt(result.HealthMissingNormalized * 100f);
                _healthText.text = string.Format(_healthMissingFormat, missingHealthPercent);
            }

            if (_bonusPointsText != null)
            {
                _bonusPointsText.text = string.Format(_bonusPointsFormat, result.BonusPoints);
            }

            // Hook: trigger your victory UI animations, Timeline playback, audio, or celebration VFX here.
        }

        private void SetVictoryCameraActive(bool active)
        {
            if (_victoryCamera != null)
            {
                _victoryCamera.gameObject.SetActive(active);
            }
        }
    }
}
