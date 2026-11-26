using Game.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Displays the active weapon name, icon, ammo count, and optional reload progress.
    /// </summary>
    public sealed class ActiveWeaponHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private TMP_Text _weaponNameText;
        [SerializeField] private Image _weaponIconImage;
        [SerializeField] private TMP_Text _ammoText;

        [Header("Optional Reload Bar")]
        [SerializeField] private GameObject _reloadRoot;
        [SerializeField] private Image _reloadFillImage;
        [SerializeField] private Slider _reloadSlider;

        [Header("Formatting & Colors")]
        [SerializeField] private string _ammoFormat = "{0}/{1}";
        [SerializeField] private string _noAmmoText = "--";
        [SerializeField] private Color _normalAmmoColor = Color.white;
        [SerializeField] private Color _emptyAmmoColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _reloadingColor = new Color(1f, 0.8f, 0.25f, 1f);

        private IWeapon _subscribedWeapon;
        private bool _isReloadingVisual;
        private float _reloadDuration;
        private float _reloadElapsed;

        private void OnEnable()
        {
            if (_weaponController == null)
            {
                return;
            }

            _weaponController.WeaponChanged += HandleWeaponChanged;
            BindWeapon(_weaponController.ActiveWeapon);
        }

        private void OnDisable()
        {
            if (_weaponController != null)
            {
                _weaponController.WeaponChanged -= HandleWeaponChanged;
            }

            UnbindCurrentWeapon();
            SetReloadVisualActive(false, 0f);
        }

        private void Update()
        {
            if (!_isReloadingVisual)
            {
                return;
            }

            _reloadElapsed += Time.unscaledDeltaTime;
            float normalized = _reloadDuration > 0f ? Mathf.Clamp01(_reloadElapsed / _reloadDuration) : 1f;
            ApplyReloadProgress(normalized);
        }

        private void HandleWeaponChanged(IWeapon newWeapon)
        {
            BindWeapon(newWeapon);
        }

        private void BindWeapon(IWeapon weapon)
        {
            UnbindCurrentWeapon();
            _subscribedWeapon = weapon;

            if (_subscribedWeapon == null)
            {
                ClearWeaponDisplay();
                return;
            }

            _subscribedWeapon.AmmoChanged += HandleAmmoChanged;
            _subscribedWeapon.ReloadStarted += HandleReloadStarted;
            _subscribedWeapon.ReloadFinished += HandleReloadFinished;

            ApplyWeaponIdentity(_subscribedWeapon);
            ApplyAmmo(_subscribedWeapon.CurrentAmmo, _subscribedWeapon.MaxAmmo);

            if (_subscribedWeapon.IsReloading && _subscribedWeapon.Config != null)
            {
                HandleReloadStarted(_subscribedWeapon.Config.ReloadDuration);
            }
            else
            {
                SetReloadVisualActive(false, 0f);
            }
        }

        private void UnbindCurrentWeapon()
        {
            if (_subscribedWeapon == null)
            {
                return;
            }

            _subscribedWeapon.AmmoChanged -= HandleAmmoChanged;
            _subscribedWeapon.ReloadStarted -= HandleReloadStarted;
            _subscribedWeapon.ReloadFinished -= HandleReloadFinished;
            _subscribedWeapon = null;
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            ApplyAmmo(currentAmmo, maxAmmo);
        }

        private void HandleReloadStarted(float duration)
        {
            _reloadDuration = Mathf.Max(0f, duration);
            _reloadElapsed = 0f;
            SetReloadVisualActive(true, _reloadDuration > 0f ? 0f : 1f);

            if (_subscribedWeapon != null)
            {
                ApplyAmmo(_subscribedWeapon.CurrentAmmo, _subscribedWeapon.MaxAmmo);
            }
        }

        private void HandleReloadFinished()
        {
            SetReloadVisualActive(false, 1f);

            if (_subscribedWeapon != null)
            {
                ApplyAmmo(_subscribedWeapon.CurrentAmmo, _subscribedWeapon.MaxAmmo);
            }
        }

        private void ApplyWeaponIdentity(IWeapon weapon)
        {
            WeaponConfig config = weapon.Config;

            if (_weaponNameText != null)
            {
                _weaponNameText.text = config != null ? config.DisplayName : string.Empty;
            }

            if (_weaponIconImage != null)
            {
                Sprite icon = config != null ? config.Icon : null;
                _weaponIconImage.sprite = icon;
                _weaponIconImage.enabled = icon != null;
            }
        }

        private void ApplyAmmo(int currentAmmo, int maxAmmo)
        {
            if (_ammoText == null || _subscribedWeapon == null)
            {
                return;
            }

            if (!_subscribedWeapon.UsesAmmo)
            {
                _ammoText.text = _noAmmoText;
                _ammoText.color = _normalAmmoColor;
                return;
            }

            _ammoText.text = string.Format(_ammoFormat, Mathf.Max(0, currentAmmo), Mathf.Max(0, maxAmmo));

            if (_subscribedWeapon.IsReloading || _isReloadingVisual)
            {
                _ammoText.color = _reloadingColor;
            }
            else
            {
                _ammoText.color = currentAmmo <= 0 ? _emptyAmmoColor : _normalAmmoColor;
            }
        }

        private void ClearWeaponDisplay()
        {
            if (_weaponNameText != null)
            {
                _weaponNameText.text = string.Empty;
            }

            if (_weaponIconImage != null)
            {
                _weaponIconImage.sprite = null;
                _weaponIconImage.enabled = false;
            }

            if (_ammoText != null)
            {
                _ammoText.text = string.Empty;
            }

            SetReloadVisualActive(false, 0f);
        }

        private void SetReloadVisualActive(bool active, float normalizedProgress)
        {
            _isReloadingVisual = active;

            if (_reloadRoot != null)
            {
                _reloadRoot.SetActive(active);
            }

            ApplyReloadProgress(normalizedProgress);
        }

        private void ApplyReloadProgress(float normalizedProgress)
        {
            if (_reloadFillImage != null)
            {
                _reloadFillImage.fillAmount = normalizedProgress;
            }

            if (_reloadSlider != null)
            {
                _reloadSlider.value = normalizedProgress;
            }
        }
    }
}
