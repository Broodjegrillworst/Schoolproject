using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolHUD2D : MonoBehaviour
    {
        private static EchoProtocolHUD2D instance;
        [SerializeField] private EchoProtocolRecording2D recording;
        private string message = "FACILITY LOGGING ACTIVE // ALL MOVEMENT IS RETAINED";
        private float messageUntil;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private Font font;

        public void Configure(EchoProtocolRecording2D source) => recording = source;

        public static void ShowMessage(string text, float seconds)
        {
            if (instance == null) return;
            instance.message = text;
            instance.messageUntil = Time.unscaledTime + seconds;
        }

        private void Awake()
        {
            instance = this;
            font = Font.CreateDynamicFontFromOSFont("Consolas", 16);
        }

        private void Start()
        {
            ShowMessage("SHIFT 041 // YOU ARE ALONE IN THE FACILITY. THE FACILITY IS STILL RECORDING.", 4.5f);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.78f, 0.9f, 0.86f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 14, normal = { textColor = new Color(0.68f, 0.75f, 0.73f) } };
            smallStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 12, normal = { textColor = new Color(0.46f, 0.59f, 0.57f) } };
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawPanel(new Rect(20f, 18f, 390f, 104f), new Color(0.015f, 0.026f, 0.032f, 0.82f));
            GUI.Label(new Rect(38f, 28f, 350f, 27f), "ECHO PROTOCOL  //  TEST WING", titleStyle);
            GUI.Label(new Rect(38f, 59f, 350f, 22f), "RESEARCH COMPLEX  /  ACCESS LEVEL 01", smallStyle);
            string state = recording != null && recording.IsRecording
                ? "RECORDING  " + recording.RecordingDuration.ToString("00.0") + "s  //  PRESS R TO SEAL"
                : "ECHOES RETAINED  " + (recording != null ? recording.EchoCount.ToString("00") : "00") + "  //  TRACE READY";
            GUI.Label(new Rect(38f, 84f, 350f, 23f), state, bodyStyle);

            float bottomY = Screen.height - 76f;
            DrawPanel(new Rect(20f, bottomY, 560f, 54f), new Color(0.015f, 0.026f, 0.032f, 0.76f));
            GUI.Label(new Rect(38f, bottomY + 8f, 530f, 22f), "A / D MOVE     SPACE JUMP     R RECORD / RELEASE     E INTERACT     BACKSPACE PURGE", smallStyle);
            GUI.Label(new Rect(38f, bottomY + 29f, 530f, 18f), "ECHOES REPLAY YOUR MOVEMENT AND INTERACTION TRACE. THEY CAN HOLD SYSTEMS OPEN.", smallStyle);

            if (Time.unscaledTime < messageUntil)
            {
                float alpha = Mathf.Clamp01(Mathf.Min((messageUntil - Time.unscaledTime) * 2.5f, 1f));
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawPanel(new Rect(Screen.width * 0.5f - 300f, 28f, 600f, 42f), new Color(0.02f, 0.03f, 0.035f, 0.9f));
                GUI.Label(new Rect(Screen.width * 0.5f - 280f, 38f, 560f, 26f), message, bodyStyle);
                GUI.color = Color.white;
            }
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color prior = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.37f, 0.5f, 0.48f, color.a * 0.8f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
            GUI.color = prior;
        }
    }
}
