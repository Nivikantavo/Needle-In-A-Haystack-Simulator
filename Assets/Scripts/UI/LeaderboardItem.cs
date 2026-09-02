using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class LeaderboardItem : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _rankText;
        [SerializeField] Image _countryIcon;
        [SerializeField] TextMeshProUGUI _userNameText;
        [SerializeField] TextMeshProUGUI _scoreText;

        bool _isValid;

        void Awake()
        {
            Validate();
        }

        public void Bind(LeaderboardEntry entry)
        {
            EnsureValid();
            if (!RequiredRefs.CanAct(_isValid, "Bind", this))
                return;

            if (entry == null)
            {
                Debug.LogError("LeaderboardItem: Bind received a null entry.", this);
                return;
            }

            _rankText.text = entry.Rank.ToString();
            _userNameText.text = entry.DisplayName;
            _scoreText.text = entry.ScoreLabel;

            if (_countryIcon == null)
                return;

            if (entry.CountryIcon == null)
            {
                _countryIcon.enabled = false;
                return;
            }

            _countryIcon.enabled = true;
            _countryIcon.sprite = entry.CountryIcon;
        }

        void EnsureValid()
        {
            if (!_isValid)
                Validate();
        }

        void Validate()
        {
            _isValid = true;
            _isValid &= RequiredRefs.Assign(_rankText, nameof(_rankText), this);
            _isValid &= RequiredRefs.Assign(_countryIcon, nameof(_countryIcon), this);
            _isValid &= RequiredRefs.Assign(_userNameText, nameof(_userNameText), this);
            _isValid &= RequiredRefs.Assign(_scoreText, nameof(_scoreText), this);
        }
    }
}
