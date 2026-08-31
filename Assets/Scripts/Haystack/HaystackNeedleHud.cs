using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.Haystack
{
    [DisallowMultipleComponent]
    public sealed class HaystackNeedleHud : MonoBehaviour
    {
        [SerializeField] HaystackSystem _haystack;
        [SerializeField] GameObject _labelObject;
        [SerializeField] string _foundText = "Needle found!";
        [SerializeField, Min(0.5f)] float _displaySeconds = 5f;

        float _hideAt = -1f;

        public HaystackSystem Haystack { get => _haystack; set => _haystack = value; }
        public GameObject LabelObject { get => _labelObject; set => _labelObject = value; }

        void Awake()
        {
            if (_haystack == null)
                _haystack = FindFirstObjectByType<HaystackSystem>();
            EnsureLabel();
            Hide();
        }

        void OnEnable()
        {
            if (_haystack != null)
                _haystack.NeedleFound += OnNeedleFound;
        }

        void OnDisable()
        {
            if (_haystack != null)
                _haystack.NeedleFound -= OnNeedleFound;
        }

        void Update()
        {
            if (_labelObject == null || !_labelObject.activeSelf)
                return;
            if (_hideAt >= 0f && Time.unscaledTime >= _hideAt)
                Hide();
        }

        void OnNeedleFound(int _)
        {
            if (_labelObject == null)
                EnsureLabel();
            if (_labelObject == null)
                return;

            SetLabelText(_foundText);
            _labelObject.SetActive(true);
            _hideAt = Time.unscaledTime + _displaySeconds;
        }

        void Hide()
        {
            _hideAt = -1f;
            if (_labelObject != null)
                _labelObject.SetActive(false);
        }

        void EnsureLabel()
        {
            if (_labelObject != null)
                return;

            Transform child = transform.Find("NeedleFoundLabel");
            if (child != null)
                _labelObject = child.gameObject;
        }

        void SetLabelText(string value)
        {
            if (_labelObject == null)
                return;

            var tmp = _labelObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            var legacy = _labelObject.GetComponent<Text>();
            if (legacy != null)
                legacy.text = value;
        }
    }
}
