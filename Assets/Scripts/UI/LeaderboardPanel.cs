using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] LeaderboardItem _itemPrefab;
        [SerializeField] RectTransform _contentRoot;
        [SerializeField] LeaderboardItem _playerItem;
        [SerializeField] Button _backButton;
        [SerializeField] Button _homeButton;

        readonly List<LeaderboardItem> _spawnedItems = new List<LeaderboardItem>();
        bool _isValid;
        bool _listenersBound;

        void Awake()
        {
            Validate();
            SetPlayerEntry(null);
        }

        void OnEnable()
        {
            BindListeners();
        }

        void OnDisable()
        {
            UnbindListeners();
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Initialize(IReadOnlyList<LeaderboardEntry> entries, LeaderboardEntry playerEntry = null)
        {
            if (!RequiredRefs.CanAct(_isValid, "Initialize", this))
                return;

            if (entries == null)
            {
                Debug.LogError("LeaderboardPanel: Initialize received a null entries list.", this);
                return;
            }

            ClearSpawned();
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardItem item = Instantiate(_itemPrefab, _contentRoot);
                item.Bind(entries[i]);
                _spawnedItems.Add(item);
            }

            SetPlayerEntry(playerEntry);
        }

        public void SetPlayerEntry(LeaderboardEntry playerEntry)
        {
            if (_playerItem == null)
                return;

            if (playerEntry == null)
            {
                _playerItem.gameObject.SetActive(false);
                return;
            }

            _playerItem.gameObject.SetActive(true);
            _playerItem.Bind(playerEntry);
        }

        void Validate()
        {
            _isValid = true;
            _isValid &= RequiredRefs.Assign(_itemPrefab, nameof(_itemPrefab), this);
            _isValid &= RequiredRefs.Assign(_contentRoot, nameof(_contentRoot), this);
            _isValid &= RequiredRefs.Assign(_playerItem, nameof(_playerItem), this);
            _isValid &= RequiredRefs.Assign(_backButton, nameof(_backButton), this);
            _isValid &= RequiredRefs.Assign(_homeButton, nameof(_homeButton), this);
        }

        void BindListeners()
        {
            if (_listenersBound)
                return;

            if (_backButton != null)
                _backButton.onClick.AddListener(Close);
            if (_homeButton != null)
                _homeButton.onClick.AddListener(Close);

            _listenersBound = true;
        }

        void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_backButton != null)
                _backButton.onClick.RemoveListener(Close);
            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(Close);
            _listenersBound = false;
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                    Destroy(_spawnedItems[i].gameObject);
            }

            _spawnedItems.Clear();
        }
    }
}
