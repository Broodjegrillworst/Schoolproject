using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoProtocol
{
    [System.Serializable]
    public sealed class EchoProtocolFrame
    {
        public float time;
        public Vector3 position;
        public int facing;
        public int interactionSequence;
    }

    public sealed class EchoProtocolRecording2D : MonoBehaviour
    {
        [SerializeField] private EchoProtocolController2D player;
        [SerializeField] private Transform visualTemplate;
        [SerializeField] private int maximumEchoes = 3;
        [SerializeField] private float maximumRecordingSeconds = 18f;
        private readonly List<EchoProtocolFrame> frames = new List<EchoProtocolFrame>(900);
        private readonly List<EchoProtocolActor2D> echoes = new List<EchoProtocolActor2D>(3);
        private bool recording;
        private float recordingStartedAt;

        public bool IsRecording => recording;
        public float RecordingDuration => recording ? Time.time - recordingStartedAt : 0f;
        public int EchoCount => echoes.Count;

        private void Awake()
        {
            if (player == null) player = GetComponent<EchoProtocolController2D>();
            if (visualTemplate == null && player != null) visualTemplate = player.transform.Find("Visual");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (recording) FinishRecording();
                else BeginRecording();
            }
            if (keyboard.backspaceKey.wasPressedThisFrame) RemoveOldestEcho();

            if (recording)
            {
                CaptureFrame();
                if (Time.time - recordingStartedAt >= maximumRecordingSeconds) FinishRecording();
            }
        }

        private void BeginRecording()
        {
            if (player == null) return;
            frames.Clear();
            recording = true;
            recordingStartedAt = Time.time;
            CaptureFrame();
            EchoProtocolHUD2D.ShowMessage("RECORDING // ACTION TRACE OPEN", 1.4f);
        }

        private void CaptureFrame()
        {
            if (player == null) return;
            frames.Add(new EchoProtocolFrame
            {
                time = Time.time - recordingStartedAt,
                position = player.transform.position,
                facing = player.Facing,
                interactionSequence = player.InteractionSequence
            });
        }

        private void FinishRecording()
        {
            recording = false;
            if (frames.Count < 2 || Time.time - recordingStartedAt < 0.35f)
            {
                frames.Clear();
                EchoProtocolHUD2D.ShowMessage("TRACE TOO SHORT // RECORD AT LEAST 0.4 SECONDS", 1.7f);
                return;
            }

            if (visualTemplate == null) visualTemplate = player.transform.Find("Visual");
            GameObject actorObject = new GameObject("Echo // recorded action trace");
            actorObject.transform.position = frames[0].position;
            Rigidbody2D body = actorObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            CapsuleCollider2D collider = actorObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.64f, 1.42f);
            collider.isTrigger = true;
            EchoProtocolActor2D actor = actorObject.AddComponent<EchoProtocolActor2D>();
            actor.Initialize(frames.ToArray(), visualTemplate);
            echoes.Add(actor);

            while (echoes.Count > maximumEchoes)
            {
                EchoProtocolActor2D oldest = echoes[0];
                echoes.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }
            EchoProtocolHUD2D.ShowMessage("TRACE SEALED // ECHO REPLAYING " + frames.Count + " SAMPLES", 2f);
            frames.Clear();
        }

        private void RemoveOldestEcho()
        {
            for (int i = 0; i < echoes.Count; i++)
            {
                if (echoes[i] == null) continue;
                Destroy(echoes[i].gameObject);
                echoes.RemoveAt(i);
                EchoProtocolHUD2D.ShowMessage("OLDEST ECHO PURGED", 1.1f);
                return;
            }
        }
    }
}
