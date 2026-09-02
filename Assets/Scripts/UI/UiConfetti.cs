using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class UiConfetti : MonoBehaviour
    {
        const string TweenId = "confetti";

        [SerializeField] Sprite[] _sprites;
        [SerializeField, Min(1)] int _poolSize = 48;
        [SerializeField, Min(1)] int _burstCount = 40;
        [SerializeField] Vector2 _pieceSize = new Vector2(56f, 56f);
        [SerializeField] Vector2 _durationRange = new Vector2(1.4f, 2.2f);
        [SerializeField] Vector2 _fallDistance = new Vector2(900f, 1400f);
        [SerializeField] Vector2 _spreadX = new Vector2(-120f, 120f);
        [SerializeField] Vector2 _scaleRange = new Vector2(0.45f, 1.1f);
        [SerializeField, Min(0f)] float _stagger = 0.03f;

        readonly List<Image> _pool = new List<Image>();
        RectTransform _root;
        bool _built;

        void Awake()
        {
            _root = transform as RectTransform;
            BuildPool();
        }

        void OnDisable()
        {
            KillTweens();
        }

        public void Play()
        {
            if (_sprites == null || _sprites.Length == 0)
            {
                Debug.LogError("UiConfetti: serialized field '_sprites' is empty.", this);
                return;
            }

            if (!_built)
                BuildPool();

            Stop();
            gameObject.SetActive(true);

            int n = Mathf.Min(_burstCount, _pool.Count);
            for (int i = 0; i < n; i++)
                Launch(_pool[i], i * _stagger);
        }

        public void Stop()
        {
            KillTweens();
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    _pool[i].gameObject.SetActive(false);
            }
        }

        void BuildPool()
        {
            if (_built || _root == null)
                return;

            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(_root, false);
                rt.sizeDelta = _pieceSize;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                go.SetActive(false);
                _pool.Add(image);
            }

            _built = true;
        }

        void Launch(Image image, float delay)
        {
            if (image == null)
                return;

            RectTransform rt = image.rectTransform;
            image.sprite = _sprites[Random.Range(0, _sprites.Length)];
            Color color = image.color;
            color.a = 1f;
            image.color = color;

            float width = _root.rect.width > 1f ? _root.rect.width : 1080f;
            float startX = Random.Range(-width * 0.48f, width * 0.48f);
            rt.anchoredPosition = new Vector2(startX, Random.Range(24f, 80f));
            rt.localScale = Vector3.one * Random.Range(_scaleRange.x, _scaleRange.y);
            rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            image.gameObject.SetActive(true);

            float duration = Random.Range(_durationRange.x, _durationRange.y);
            Vector2 end = new Vector2(
                startX + Random.Range(_spreadX.x, _spreadX.y),
                -Random.Range(_fallDistance.x, _fallDistance.y));
            Vector3 endRot = new Vector3(0f, 0f, rt.localEulerAngles.z + Random.Range(180f, 540f));

            Sequence seq = DOTween.Sequence();
            seq.SetId(TweenId);
            seq.SetUpdate(true);
            seq.SetLink(image.gameObject);
            seq.SetDelay(delay);
            seq.Append(rt.DOAnchorPos(end, duration).SetEase(Ease.InQuad));
            seq.Join(rt.DOLocalRotate(endRot, duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            seq.Insert(duration * 0.55f, image.DOFade(0f, duration * 0.45f).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                if (image != null)
                    image.gameObject.SetActive(false);
            });
        }

        void KillTweens()
        {
            DOTween.Kill(TweenId);
        }
    }
}
