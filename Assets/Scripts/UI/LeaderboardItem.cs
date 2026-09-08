using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        Coroutine _photoRoutine;
        UnityWebRequest _photoRequest;
        Sprite _runtimeSprite;

        void Awake()
        {
            Validate();
        }

        void OnDestroy()
        {
            CancelPhotoLoad();
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

            _rankText.text = entry.Rank > 0 ? entry.Rank.ToString() : "-";
            _userNameText.text = entry.DisplayName;
            _scoreText.text = entry.ScoreLabel;
            ApplyIcon(entry);
        }

        void ApplyIcon(LeaderboardEntry entry)
        {
            CancelPhotoLoad();
            if (_countryIcon == null)
                return;

            if (entry.CountryIcon != null)
            {
                _countryIcon.enabled = true;
                _countryIcon.sprite = entry.CountryIcon;
                return;
            }

            _countryIcon.enabled = false;
            _countryIcon.sprite = null;
            if (string.IsNullOrEmpty(entry.PhotoUrl) || !isActiveAndEnabled)
                return;

            _photoRoutine = StartCoroutine(LoadPhoto(entry.PhotoUrl));
        }

        IEnumerator LoadPhoto(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            _photoRequest = request;
            yield return request.SendWebRequest();

            if (_photoRequest != request)
                yield break;

            _photoRequest = null;
            _photoRoutine = null;

            try
            {
                if (_countryIcon == null || request.result != UnityWebRequest.Result.Success)
                    yield break;

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                    yield break;

                _runtimeSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                _countryIcon.sprite = _runtimeSprite;
                _countryIcon.enabled = true;
            }
            finally
            {
                request.Dispose();
            }
        }

        void CancelPhotoLoad()
        {
            if (_photoRoutine != null)
            {
                StopCoroutine(_photoRoutine);
                _photoRoutine = null;
            }

            if (_photoRequest != null)
            {
                _photoRequest.Abort();
                _photoRequest.Dispose();
                _photoRequest = null;
            }

            if (_runtimeSprite != null)
            {
                Texture texture = _runtimeSprite.texture;
                Destroy(_runtimeSprite);
                if (texture != null)
                    Destroy(texture);

                _runtimeSprite = null;
            }
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
