using BeatSaberOnline.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberOnline.Views.Menus
{
    public class ScoreboardEntry
    {
        public string name;
        public int score;
        public int combo;
        public TextMeshProUGUI text;
        public ulong clientIndex;
        public int place;

        public ScoreboardEntry(ulong clientIndex, string name)
        {
            this.clientIndex = clientIndex;
            this.name = name;
        }

        public void UpdateText(int place)
        {
            this.place = place;
            if (this.text)
                this.text.text = $"{place+1}.  <align=left>{name} - [{combo} combo]<line-height=0>\r\n<align=right>{score}<line-height=1em>";
        }
    }

    public class Scoreboard : MonoBehaviour
    {
        private readonly Comparison<ScoreboardEntry> _scoreComparison = new Comparison<ScoreboardEntry>((x, y) => y.score - x.score);

        private ObjectPool<TextMeshProUGUI> _textPool;
        private Canvas _canvas;
        private readonly Vector3 _basePosition = new Vector3(0f, 4f, 5f);
        private float _scale = 2.0f;
        private float _width = 200f;
        private float _padding = 4f;
        private float _lineSpacing = 2f;

        private float _fontSize = 12f;
        private Color _fontColor = Color.white;

        private Image _background;
        private float _backgroundHeight = 0f;
        private Color _backgroundColor = Color.black.ColorWithAlpha(0.8f);
        
        public Dictionary<ulong, ScoreboardEntry> _scoreboardEntries = new Dictionary<ulong, ScoreboardEntry>();

        private Material _noGlowMaterial = null;
        public Material NoGlowMaterial
        {
            get
            {
                if (!_noGlowMaterial)
                {
                    Material material = Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name == "UINoGlow").FirstOrDefault();
                    if (material)
                        _noGlowMaterial = Material.Instantiate(material);
                }
                return _noGlowMaterial;
            }
        }

        public static Scoreboard Instance = null;
        public bool disabled = true;
        public static void OnLoad()
        {
            if (Instance) return;

            Instance = new GameObject("CustomScoreboard").AddComponent<Scoreboard>();
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            var collider = gameObject.AddComponent<MeshCollider>();
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
            _canvas.GetComponent<RectTransform>().localScale = new Vector3(0.012f * _scale, 0.012f * _scale, 0.012f * _scale);

            _background = new GameObject("ScoreboardBackground").AddComponent<Image>();
            _background.rectTransform.SetParent(gameObject.transform, false);
            _background.color =  _backgroundColor;
            _background.rectTransform.pivot = new Vector2(0, 0);
            _background.rectTransform.sizeDelta = new Vector2(_width + _padding, 0);
            _background.rectTransform.localPosition = new Vector3(0 - (_width + _padding) / 2, 0, 0);
            _background.material = NoGlowMaterial;
            _textPool = new ObjectPool<TextMeshProUGUI>(0, 
            // FirstAlloc
            (text) =>
            {
                var canvasScaler = text.gameObject.GetComponent<CanvasScaler>();
                if (canvasScaler == null)
                    canvasScaler = text.gameObject.AddComponent<CanvasScaler>();
                canvasScaler.dynamicPixelsPerUnit = 100;
                text.rectTransform.SetParent(gameObject.transform, false);
                text.rectTransform.localPosition = new Vector3(0, 0, 0);
                text.rectTransform.localRotation = new Quaternion(0, 0, 0, 0);
                text.rectTransform.pivot = new Vector2(0, 0);
                text.rectTransform.sizeDelta = new Vector2(_width, 1);
                text.enableWordWrapping = false;
                text.richText = true;
                text.fontSize = _fontSize;
                text.overflowMode = TextOverflowModes.Overflow;
                text.alignment = TextAlignmentOptions.Left;
                text.color = _fontColor;
            },
            // OnAlloc
            (text) =>
            {
                text.material = NoGlowMaterial;
                text.enabled = true;
            },
            // OnFree
            (text) =>
            {
                text.text = "";
                text.enabled = false;
            });

            // Set the scoreboard position
            _canvas.transform.position = _basePosition;
            _canvas.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        }
        

        public void RemoveScoreboardEntry(ulong clientIndex)
        {
            ScoreboardEntry entry = _scoreboardEntries[clientIndex];
            if (entry == null) return;
            entry.text.text = "";
            _textPool.Free(entry.text);
            _scoreboardEntries.Remove(clientIndex);
            UpdateScoreboardUI();
        }


        public void RemoveAll()
        {
            for (int i = 0; i < _scoreboardEntries.Count; i++)
            {
                _scoreboardEntries.Values.ToArray()[i].text.text = "";
            }
            UpdateScoreboardUI();
            _scoreboardEntries.Clear();
        }

        public void UpsertScoreboardEntry(ulong clientIndex, string name, int score = 0, int combo = 0)
        {
            if (disabled) { return; }
                ScoreboardEntry entry;
                if (!_scoreboardEntries.ContainsKey(clientIndex))
                {
                    entry = new ScoreboardEntry(clientIndex, name);
                    entry.place = 0;
                    entry.score = 0;
                    entry.place = _scoreboardEntries.Count + 1;
                    entry.text = _textPool.Alloc();
                    entry.text.text = name;
                    _scoreboardEntries.Add(clientIndex, entry);
                }
                else
                {
                    entry = _scoreboardEntries[clientIndex];
                    entry.score = score;
                    entry.combo = combo;
                    entry.UpdateText(entry.place);
                }
                List<KeyValuePair<ulong, ScoreboardEntry>> sorted = _scoreboardEntries.ToList();
                sorted.Sort((pair1, pair2) => pair2.Value.score - pair1.Value.score);
                _scoreboardEntries = sorted.ToDictionary(pair => pair.Key, pair => pair.Value);
                UpdateScoreboardUI();
        }
        private void UpdateScoreboardUI()
        {
                if (_scoreboardEntries.Count > 0)
                {
                    for (int i = 0; i < _scoreboardEntries.Count; i++)
                    {
                        // Only update the text if their place changed
                        if (i != _scoreboardEntries[_scoreboardEntries.Keys.ToArray()[i]].place)
                        {
                            _scoreboardEntries[_scoreboardEntries.Keys.ToArray()[i]].UpdateText(i);
                        }
                    }

                    // Update the position of each scoreboard entry
                    float currentYValue = 0;
                    float initialYValue = currentYValue;
                    KeyValuePair<ulong, ScoreboardEntry>[] _tmpArray = _scoreboardEntries.ToArray();

                    for (int i = 0; i < _tmpArray.Length; i++)
                    {
                        if (_tmpArray[i].Value.text.text != "")
                        {
                            _tmpArray[i].Value.text.transform.localPosition = new Vector3(-_width / 2, currentYValue - _tmpArray[i].Value.text.preferredHeight * 0.6f / 2 - 1, 0);
                            currentYValue -= (_tmpArray[i].Value.text.preferredHeight * 0.6f + (i < _scoreboardEntries.Count() - 1 ? _lineSpacing + 1.5f : 0));
                        }
                    }
                    //_width = maxWidth;
                    _backgroundHeight = (initialYValue - currentYValue) + _padding * 2;
                    _background.rectTransform.sizeDelta = new Vector2(_width + _padding * 2, _backgroundHeight);
                    _background.rectTransform.position = _canvas.transform.TransformPoint(new Vector3(-_width / 2 - _padding, (initialYValue - _backgroundHeight + _padding), 0.1f));
                    _canvas.transform.position = _basePosition;
                    _canvas.transform.position = _canvas.transform.TransformPoint(new Vector3(0, _backgroundHeight));
                }
        }
    }
}
