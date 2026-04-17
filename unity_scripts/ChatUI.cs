using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Master controller — wires together GeminiService, PabloAnimator and the chat UI.
/// Attach to a root GameObject in the scene along with GeminiService and PabloAnimator.
public class ChatUI : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private GeminiService gemini;
    [SerializeField] private PabloAnimator pablo;

    [Header("UI — Chat panel")]
    [SerializeField] private ScrollRect      scrollRect;
    [SerializeField] private Transform       messageContainer; // Content child of ScrollRect
    [SerializeField] private TMP_InputField  inputField;
    [SerializeField] private Button          sendButton;
    [SerializeField] private GameObject      thinkingIndicator; // "..." dots object

    [Header("Message Bubble Prefabs")]
    [SerializeField] private GameObject userBubblePrefab;  // right-aligned, blue
    [SerializeField] private GameObject pabloBubblePrefab; // left-aligned, dark

    [Header("Colors")]
    [SerializeField] private Color userBubbleColor  = new Color(0.18f, 0.6f, 0.96f);
    [SerializeField] private Color pabloBubbleColor = new Color(0.22f, 0.22f, 0.25f);

    private bool _busy;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        sendButton.onClick.AddListener(OnSend);
        inputField.onSubmit.AddListener(_ => OnSend());

        if (thinkingIndicator) thinkingIndicator.SetActive(false);

        // Welcome message from Pablo after a short delay
        StartCoroutine(WelcomeDelay());
    }

    private IEnumerator WelcomeDelay()
    {
        yield return new WaitForSeconds(1.2f);
        ShowPabloMessage("Eyyy\nQué pasa tronk 👋", "happy");
    }

    // ── Send flow ──────────────────────────────────────────────────────────────

    private void OnSend()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || _busy) return;

        inputField.text = "";
        inputField.ActivateInputField();

        AddUserBubble(text);
        StartCoroutine(AskPablo(text));
    }

    private IEnumerator AskPablo(string userMsg)
    {
        _busy = true;
        sendButton.interactable = false;

        // Pablo goes to thinking mode
        pablo.PlayEmotion("thinking");
        if (thinkingIndicator) thinkingIndicator.SetActive(true);

        PabloResponse response = null;
        yield return gemini.Ask(userMsg, r => response = r);

        if (thinkingIndicator) thinkingIndicator.SetActive(false);

        if (response != null)
        {
            pablo.PlayEmotion(response.emotion);
            // Split by \n — Pablo sends several short messages
            string[] lines = response.text.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                yield return new WaitForSeconds(0.35f); // slight delay between messages
                ShowPabloMessage(trimmed, response.emotion);
            }

            // After talking, return to idle after a moment
            yield return new WaitForSeconds(2.5f);
            pablo.ReturnToIdle();
        }
        else
        {
            ShowPabloMessage("Jajjaaj eso no me ha llegao", "happy");
            pablo.ReturnToIdle();
        }

        _busy = false;
        sendButton.interactable = true;
    }

    // ── Bubble creation ────────────────────────────────────────────────────────

    private void AddUserBubble(string text)
    {
        var go   = CreateBubble(userBubblePrefab, text, userBubbleColor, TextAlignmentOptions.Right);
        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot     = new Vector2(1f, 0f);
        }
        ScrollToBottom();
    }

    private void ShowPabloMessage(string text, string emotion)
    {
        var go = CreateBubble(pabloBubblePrefab, text, pabloBubbleColor, TextAlignmentOptions.Left);
        _ = emotion; // available for future emoji prefixes etc.
        ScrollToBottom();
    }

    private GameObject CreateBubble(GameObject prefab, string text, Color bgColor, TextAlignmentOptions align)
    {
        GameObject go;

        if (prefab != null)
        {
            go = Instantiate(prefab, messageContainer);
        }
        else
        {
            // Build bubble dynamically if no prefab assigned
            go = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasRenderer),
                                typeof(Image), typeof(HorizontalLayoutGroup));

            var img = go.GetComponent<Image>();
            img.color = bgColor;

            // Rounded look via sprite (optional — works with a simple square if no sprite)
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(18, 18, 10, 10);
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var textGo  = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize  = 22;
            tmp.color     = Color.white;
            tmp.alignment = align;
            tmp.text      = text;

            var le = textGo.AddComponent<LayoutElement>();
            le.preferredWidth  = 480;
            le.flexibleWidth   = 1;

            go.transform.SetParent(messageContainer, false);
        }

        // If prefab has a TMP child, set text
        var existingTmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (existingTmp != null) existingTmp.text = text;

        // Set bg color if image present
        var existingImg = go.GetComponent<Image>();
        if (existingImg != null) existingImg.color = bgColor;

        return go;
    }

    private void ScrollToBottom()
    {
        // Needs one frame for layout to update
        StartCoroutine(ScrollNextFrame());
    }

    private IEnumerator ScrollNextFrame()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // ── Thinking indicator ────────────────────────────────────────────────────
    // The ThinkingIndicator GameObject should contain a TMP text that animates dots.
    // This optional coroutine drives it if you use a TMP text child.

    private Coroutine _dotsCo;

    private void OnEnable()  => _dotsCo = StartCoroutine(AnimateDots());
    private void OnDisable() { if (_dotsCo != null) StopCoroutine(_dotsCo); }

    private IEnumerator AnimateDots()
    {
        if (thinkingIndicator == null) yield break;
        var tmp = thinkingIndicator.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) yield break;

        string[] frames = { ".", "..", "..." };
        int i = 0;
        while (true)
        {
            if (thinkingIndicator.activeSelf)
                tmp.text = frames[i % frames.Length];
            i++;
            yield return new WaitForSeconds(0.4f);
        }
    }
}
