using System;
using System.Collections.Generic;
using Game.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays the GTA-style weapon wheel slots and hover highlight from WeaponWheelController events.
    /// </summary>
    public sealed class WeaponWheelHudView : MonoBehaviour
    {
        [Serializable]
        private struct WheelSlotWidgets
        {
            public RectTransform Root;
            public Image BackgroundImage;
            public Image IconImage;
            public TMP_Text LabelText;
        }

        [Header("References")]
        [SerializeField] private WeaponWheelController _wheelController;
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private GameObject _wheelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _hoveredWeaponNameText;
        [SerializeField] private WheelSlotWidgets[] _slots = Array.Empty<WheelSlotWidgets>();

        [Header("Visuals & Animation")]
        [SerializeField] private Color _normalSlotColor = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        [SerializeField] private Color _equippedSlotColor = new Color(0.2f, 0.5f, 0.75f, 0.9f);
        [SerializeField] private Color _hoveredSlotColor = new Color(0.95f, 0.75f, 0.2f, 0.95f);
        [SerializeField, Min(1f)] private float _hoveredSlotScale = 1.1f;
        [SerializeField, Min(0f)] private float _fadeSpeed = 14f;

        private int _hoveredIndex = -1;
        private float _targetAlpha;
        private float _currentAlpha;

        private void OnEnable()
        {
            if (_wheelController == null)
            {
                return;
            }

            _wheelController.WheelOpened += HandleWheelOpened;
            _wheelController.WheelClosed += HandleWheelClosed;
            _wheelController.HoveredSlotChanged += HandleHoveredSlotChanged;

            if (_weaponController != null)
            {
                _weaponController.WeaponChanged += HandleWeaponChanged;
            }

            SyncInitialState();
        }

        private void OnDisable()
        {
            if (_wheelController != null)
            {
                _wheelController.WheelOpened -= HandleWheelOpened;
                _wheelController.WheelClosed -= HandleWheelClosed;
                _wheelController.HoveredSlotChanged -= HandleHoveredSlotChanged;
            }

            if (_weaponController != null)
            {
                _weaponController.WeaponChanged -= HandleWeaponChanged;
            }
        }

        private void Update()
        {
            if (_canvasGroup == null || _fadeSpeed <= 0f || Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                return;
            }

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);
            _canvasGroup.alpha = _currentAlpha;

            if (_wheelRoot != null && _currentAlpha <= 0f && _targetAlpha <= 0f)
            {
                _wheelRoot.SetActive(false);
            }
        }

        private void SyncInitialState()
        {
            PopulateSlots();
            _hoveredIndex = _wheelController.HoveredIndex;
            SetWheelVisible(_wheelController.IsOpen, snapFade: true);
            UpdateHoveredWeaponLabel(GetWeaponAtIndex(_hoveredIndex));
            RefreshSlotHighlights();
        }

        private void HandleWheelOpened()
        {
            PopulateSlots();
            _hoveredIndex = _wheelController.HoveredIndex;
            SetWheelVisible(true, snapFade: false);
            UpdateHoveredWeaponLabel(GetWeaponAtIndex(_hoveredIndex));
            RefreshSlotHighlights();
        }

        private void HandleWheelClosed()
        {
            _hoveredIndex = -1;
            UpdateHoveredWeaponLabel(null);
            RefreshSlotHighlights();
            SetWheelVisible(false, snapFade: true);
        }

        private void HandleHoveredSlotChanged(int slotIndex, IWeapon hoveredWeapon)
        {
            _hoveredIndex = slotIndex;
            UpdateHoveredWeaponLabel(hoveredWeapon);
            RefreshSlotHighlights();
        }

        private void HandleWeaponChanged(IWeapon _)
        {
            RefreshSlotHighlights();
        }

        private void PopulateSlots()
        {
            IReadOnlyList<WeaponBase> weapons = _wheelController != null ? _wheelController.Weapons : null;
            int weaponCount = weapons != null ? weapons.Count : 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                WheelSlotWidgets slot = _slots[i];
                WeaponConfig config = i < weaponCount && weapons[i] != null ? weapons[i].Config : null;

                if (slot.IconImage != null)
                {
                    Sprite icon = config != null ? config.Icon : null;
                    slot.IconImage.sprite = icon;
                    slot.IconImage.enabled = icon != null;
                }

                if (slot.LabelText != null)
                {
                    slot.LabelText.text = config != null ? config.DisplayName : string.Empty;
                }
            }
        }

        private void RefreshSlotHighlights()
        {
            IReadOnlyList<WeaponBase> weapons = _wheelController != null ? _wheelController.Weapons : null;
            int weaponCount = weapons != null ? weapons.Count : 0;
            IWeapon activeWeapon = _weaponController != null ? _weaponController.ActiveWeapon : null;

            for (int i = 0; i < _slots.Length; i++)
            {
                WheelSlotWidgets slot = _slots[i];
                bool isHovered = i == _hoveredIndex;
                bool isEquipped = activeWeapon != null && i < weaponCount && ReferenceEquals(weapons[i], activeWeapon);

                if (slot.BackgroundImage != null)
                {
                    if (isHovered)
                    {
                        slot.BackgroundImage.color = _hoveredSlotColor;
                    }
                    else if (isEquipped)
                    {
                        slot.BackgroundImage.color = _equippedSlotColor;
                    }
                    else
                    {
                        slot.BackgroundImage.color = _normalSlotColor;
                    }
                }

                if (slot.Root != null)
                {
                    float scale = isHovered ? _hoveredSlotScale : 1f;
                    slot.Root.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private void UpdateHoveredWeaponLabel(IWeapon hoveredWeapon)
        {
            if (_hoveredWeaponNameText == null)
            {
                return;
            }

            WeaponConfig config = hoveredWeapon != null ? hoveredWeapon.Config : null;
            _hoveredWeaponNameText.text = config != null ? config.DisplayName : string.Empty;
        }

        private IWeapon GetWeaponAtIndex(int index)
        {
            if (_wheelController == null)
            {
                return null;
            }

            IReadOnlyList<WeaponBase> weapons = _wheelController.Weapons;
            if (weapons == null || index < 0 || index >= weapons.Count)
            {
                return null;
            }

            return weapons[index];
        }

        private void SetWheelVisible(bool visible, bool snapFade)
        {
            if (visible && _wheelRoot != null)
            {
                _wheelRoot.SetActive(true);
            }

            _targetAlpha = visible ? 1f : 0f;

            if (_canvasGroup == null || _fadeSpeed <= 0f || snapFade)
            {
                _currentAlpha = _targetAlpha;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = _currentAlpha;
                }

                if (!visible && _wheelRoot != null)
                {
                    _wheelRoot.SetActive(false);
                }
            }
        }
    }
}
