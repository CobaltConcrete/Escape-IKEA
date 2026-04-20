using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InstructionsOverlayUI : MonoBehaviour
{
    private const string CloseButtonName = "CloseInstructionsButton";

    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.1f, 0.96f);
    [SerializeField] private Color textColor = Color.white;

    private void Awake()
    {
        Transform existingCanvas = transform.Find("InstructionsCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas.gameObject);

        BuildOverlay();
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("InstructionsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backdrop = CreateImage("Backdrop", canvasObject.transform, backdropColor);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        GameObject panel = CreateImage("Panel", canvasObject.transform, panelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 500f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.transform.SetAsLastSibling();

        GameObject title = CreateText("Title", panel.transform, "INSTRUCTIONS", 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(64f, -92f);
        titleRect.offsetMax = new Vector2(-64f, -24f);

        string instructions =
            "Search each room and collect every item on your shopping list.\n" +
            "Once the list is complete, find the final boss, defeat her, and escape the store.\n\n" +
            "WASD - Move\n" +
            "Click - Attack\n" +
            "F - Interact / pick up nearby loot\n" +
            "E - Inventory\n" +
            "I - Shopping List\n" +
            "K - Drop equipped item\n" +
            "L or Right Shift - Dash when you have the dash belt\n" +
            "P - Pause menu\n" +
            "H - Open these instructions";

        GameObject body = CreateText("InstructionText", panel.transform, instructions, 26, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(64f, 48f);
        bodyRect.offsetMax = new Vector2(-64f, -120f);

        GameObject closeButton = CreateButton(CloseButtonName, panel.transform, "X");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(54f, 54f);
        closeRect.anchoredPosition = new Vector2(-34f, -34f);
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private GameObject CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateImage(objectName, parent, new Color(0.92f, 0.92f, 0.92f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        GameObject labelObject = CreateText("Text", buttonObject.transform, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelObject.GetComponent<Text>().color = new Color(0.12f, 0.12f, 0.14f, 1f);

        return buttonObject;
    }

    private GameObject CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);

        Text uiText = go.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.alignment = alignment;
        uiText.color = textColor;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return go;
    }
}
