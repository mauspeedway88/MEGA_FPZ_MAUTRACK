using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Starter.Shooter
{
    public class InfoSceneManager : MonoBehaviour
    {
        private static InfoSceneManager _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnSceneLoaded()
        {
            if (SceneManager.GetActiveScene().name.Contains("05") || SceneManager.GetActiveScene().name.Contains("info"))
            {
                if (GameObject.FindObjectOfType<InfoSceneManager>() == null)
                {
                    GameObject go = new GameObject("AUTO_InfoSceneManager");
                    go.AddComponent<InfoSceneManager>();
                    Debug.Log("[InfoSceneManager] Auto-spawned in scene.");
                }
            }
        }

        public TextMeshProUGUI WinnerNameText;
        public TextMeshProUGUI LoserNameText;

        private void Awake()
        {
            _instance = this;
            // Prevent destruction if needed, but we only want it in this scene
        }

        private void Start()
        {
            Debug.Log("[InfoSceneManager] Start running...");
            SetupUI();
            StartCoroutine(InfiniteUpdateLoop());
        }

        private void SetupUI()
        {
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("EmergencyCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Winner Text
            if (WinnerNameText == null)
                WinnerNameText = CreateText(canvas.transform, "WINNER_DISPLAY", new Vector2(400, 0), Color.green);

            // Create Loser Text
            if (LoserNameText == null)
                LoserNameText = CreateText(canvas.transform, "LOSER_DISPLAY", new Vector2(-400, 0), Color.red);
                
            // Check for Guest Session - Warning
            var pm = Starter.PlayFabIntegration.PlayFabManager.Instance;
            if (pm != null && pm.IsGuestSession)
            {
                var warningText = CreateText(canvas.transform, "GUEST_WARNING", new Vector2(0, -300), Color.yellow);
                warningText.fontSize = 60;
                warningText.text = "REGISTRATE PARA GANAR MONEDAS";
            }
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 80;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.text = "WAITING...";
            
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 200);
            rect.anchoredPosition = pos;
            
            return text;
        }

        private IEnumerator InfiniteUpdateLoop()
        {
            while (true)
            {
                string w = PlayerPrefs.GetString("LastWinner", "---");
                string l = PlayerPrefs.GetString("LastLoser", "---");

                if (WinnerNameText != null) WinnerNameText.text = "GANADOR:\n" + w;
                if (LoserNameText != null) LoserNameText.text = "PERDEDOR:\n" + l;

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnGUI()
        {
            // Massive debug text at top
            GUIStyle style = new GUIStyle();
            style.fontSize = 40;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperCenter;
            
            string w = PlayerPrefs.GetString("LastWinner", "NOT_FOUND");
            string l = PlayerPrefs.GetString("LastLoser", "NOT_FOUND");
            
            GUI.Label(new Rect(0, 50, Screen.width, 100), $"Winner: {w} | Loser: {l}", style);
        }
    }
}





