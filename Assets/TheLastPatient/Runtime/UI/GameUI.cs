using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TheLastPatient
{
    public sealed class GameUI : MonoBehaviour
    {
        public bool IsModal { get; private set; }
        public bool IsPause { get; private set; }
        public bool IsEnding { get; private set; }
        private Canvas canvas;
        private TMP_Text chapter, mission, interaction, battery, speaker, subtitle, toast;
        private GameObject modal, dialoguePanel;
        private TMP_Text modalTitle, modalText;
        private ScrollRect documentScroll;
        private Transform buttons;
        private float toastUntil;
        private Color ink = new Color(0.83f, 0.86f, 0.82f);

        private void Awake()
        {
            var root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            chapter = Text("Chapter", root.transform, 22);
            Place(chapter.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -38), new Vector2(900, 36));
            chapter.color = new Color(0.44f, 0.68f, 0.66f);
            mission = Text("Mission", root.transform, 29);
            Place(mission.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -80), new Vector2(1250, 75));
            var crosshair = Text("Crosshair", root.transform, 25);
            crosshair.text = "+"; crosshair.alignment = TextAlignmentOptions.Center;
            Place(crosshair.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(32, 32));
            interaction = Text("Interaction", root.transform, 26);
            interaction.alignment = TextAlignmentOptions.Center;
            Place(interaction.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -66), new Vector2(1000, 60));
            battery = Text("Battery", root.transform, 22);
            Place(battery.rectTransform, Vector2.one, Vector2.one, new Vector2(-45, -38), new Vector2(340, 42));
            battery.alignment = TextAlignmentOptions.Right;
            var help = Text("Controls", root.transform, 19);
            help.text = "WASD · Maus   /   E Interaktion   /   F Licht   /   Shift Sprint   /   Strg Ducken   /   J Journal   /   Esc Pause";
            Place(help.rectTransform, Vector2.zero, Vector2.zero, new Vector2(46, 24), new Vector2(1760, 35));
            help.color = new Color(0.6f, 0.63f, 0.63f);
            toast = Text("Notification", root.transform, 25);
            toast.alignment = TextAlignmentOptions.Center;
            Place(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(1450, 80));
            dialoguePanel = Panel("Subtitles", root.transform, new Color(0.015f, 0.025f, 0.03f, 0.94f));
            Place(dialoguePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 82), new Vector2(1390, 194));
            speaker = Text("Speaker", dialoguePanel.transform, 23);
            speaker.color = new Color(0.51f, 0.8f, 0.74f);
            Place(speaker.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(35, -16), new Vector2(1250, 35));
            subtitle = Text("Line", dialoguePanel.transform, 30);
            Place(subtitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(35, -59), new Vector2(1320, 95));
            var advance = Text("Advance", dialoguePanel.transform, 17);
            advance.text = "LEERTASTE · Text anzeigen / nächste Zeile";
            Place(advance.rectTransform, Vector2.zero, Vector2.zero, new Vector2(35, 10), new Vector2(900, 25));
            dialoguePanel.SetActive(false);
            modal = Panel("Modal", root.transform, new Color(0.018f, 0.03f, 0.035f, 0.98f));
            Place(modal.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1120, 780));
            modalTitle = Text("Title", modal.transform, 37);
            Place(modalTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -38), new Vector2(1010, 70));
            modalTitle.color = new Color(0.57f, 0.8f, 0.72f);
            var viewport = Panel("Reading area", modal.transform, Color.clear);
            Place(viewport.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -131), new Vector2(1010, 435));
            viewport.AddComponent<RectMask2D>();
            modalText = Text("Body", viewport.transform, 25);
            Place(modalText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1010, 435));
            modalText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            documentScroll = viewport.AddComponent<ScrollRect>();
            documentScroll.viewport = viewport.GetComponent<RectTransform>(); documentScroll.content = modalText.rectTransform;
            documentScroll.horizontal = false; documentScroll.movementType = ScrollRect.MovementType.Clamped;
            documentScroll.scrollSensitivity = 25;
            var row = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            row.transform.SetParent(modal.transform, false);
            Place(row.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(54, 34), new Vector2(1010, 168));
            var layout = row.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            buttons = row.transform;
            modal.SetActive(false);
        }

        private TMP_Text Text(string name, Transform parent, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size; text.color = ink; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private void Button(string label, Action action)
        {
            var go = Panel(label, buttons, new Color(0.12f, 0.22f, 0.23f));
            var layout = go.AddComponent<LayoutElement>(); layout.preferredHeight = 46;
            var button = go.AddComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(0.7f, 0.9f, 0.82f); button.colors = colors;
            var text = Text("Label", go.transform, 23); text.text = label; text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            button.onClick.AddListener(() => action?.Invoke());
        }
        private void OpenModal(string title, string text)
        {
            foreach (Transform child in buttons) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            modalTitle.text = title; modalText.text = text;
            IsModal = true; modal.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(modalText.rectTransform);
            documentScroll.verticalNormalizedPosition = 1;
            GameDirector.Instance.UpdateCursor();
        }
        public void CloseModal()
        {
            if (IsEnding) return;
            IsModal = IsPause = false; modal.SetActive(false); Time.timeScale = 1;
            GameDirector.Instance.UpdateCursor();
        }
        public void ResetPanels()
        { IsEnding = false; CloseModal(); HideDialogue(); }
        public void SetMission(string title, string goal) { chapter.text = "BLACKWOOD  /  " + title.ToUpperInvariant(); mission.text = goal; }
        public void SetInteraction(string text) => interaction.text = text;
        public void SetBattery(bool owned, float charge, bool on) => battery.text = owned ? "LICHT " + (on ? "AN" : "AUS") + "  ·  " + Mathf.CeilToInt(charge) + " %" : string.Empty;
        public void Toast(string text) { toast.text = text; toastUntil = Time.unscaledTime + 4; }
        private void Update() { if (toast != null && Time.unscaledTime > toastUntil) toast.text = string.Empty; }
        public void ShowLine(string name, string text)
        { dialoguePanel.SetActive(true); speaker.text = name; subtitle.text = text; subtitle.maxVisibleCharacters = 0; }
        public void Reveal(int characters) => subtitle.maxVisibleCharacters = characters;
        public void HideDialogue() => dialoguePanel.SetActive(false);
        public void ShowChoices(DialogueChoice[] choices, Action<DialogueChoice> choose)
        {
            OpenModal("Eine Erinnerung", "Wie möchtest du antworten?");
            foreach (var entry in choices)
            { var choice = entry; Button(choice.text, () => { CloseModal(); choose(choice); }); }
        }
        public void ShowDocument(string title, string text, string actionLabel, Action take)
        {
            OpenModal(title, text);
            if (!string.IsNullOrEmpty(actionLabel)) Button(actionLabel, () => { CloseModal(); take?.Invoke(); });
            Button("Akte schließen · Esc", CloseModal);
        }
        public void ShowJournal()
        {
            var state = GameDirector.Instance.story.State;
            string entries = "Daniel Carter · Blackwood · 2013\n\n";
            entries += state.inventory.Contains(StoryIds.HasFlashlight) ? "[x] Taschenlampe (mit Not-Dynamo)\n" : "– Noch keine Taschenlampe\n";
            entries += state.inventory.Contains(StoryIds.WardKey) ? "[x] Schlüssel zur Patientenstation\n" : "– Schlüssel fehlt\n";
            entries += "\nBEWEISE\n";
            entries += state.documents.Contains(StoryIds.ReceptionFile)
                ? "Empfangsakte: 1998 wurden Geschwister aufgenommen. Unter der Rückseite steckt ein Stationsschlüssel.\n\n" : "Noch keine Akten gefunden.\n\n";
            entries += state.documents.Contains(StoryIds.FirstTape) ? "Tonband: Emily warnt Daniel vor dem, den sie zurückgelassen haben." : "Das Tonband am Empfang wurde noch nicht vollständig gehört.";
            OpenModal("Journal & Inventar", entries); Button("Zurück · J / Esc", CloseModal);
        }
        public void ShowPause()
        {
            IsPause = true; Time.timeScale = 0;
            OpenModal("THE LAST PATIENT", "Kapitel 1 · Die Ankunft\n\nWASD: Bewegen   ·   Maus: Umsehen\nShift: Sprinten   ·   Strg: Ducken\nE: Interagieren   ·   F: Taschenlampe\nLeertaste: Dialog weiter   ·   Esc: Cutscene überspringen\nJ: Journal   ·   F5: Speichern   ·   F9: Laden");
            Button("Weiterspielen", CloseModal);
            Button("Checkpoint laden", () => GameDirector.Instance.Load());
            Button("Neu beginnen", () => GameDirector.Instance.NewGame());
        }
        public void ShowEnding()
        {
            IsEnding = true; Time.timeScale = 0;
            OpenModal("Kapitel 1 · Die Ankunft", "Die Tür zur Patientenstation steht offen.\n\nAus Zimmer 7 klingt ein altes Radio. Daniel kennt die Melodie, aber nicht den Ort, an dem er sie zuerst gehört hat.\n\nEMILY: „Du warst nicht der Einzige, Daniel.“\n\nHier endet der spielbare Ausschnitt. Kapitel 2–5 und die drei Enden sind für die nächsten Entwicklungsphasen vorgesehen.");
            Button("Kapitel 1 erneut spielen", () => GameDirector.Instance.NewGame());
            Button("Spiel beenden", () => Application.Quit());
        }
    }
}
