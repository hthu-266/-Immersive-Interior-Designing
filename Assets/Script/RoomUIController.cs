using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoomUIController : MonoBehaviour
{
    [Header("Existing Room Inputs")]
    public TMP_InputField widthInput;
    public TMP_InputField lengthInput;
    public FloorController floorController;

    [Header("Runtime UI")]
    public bool buildMissingControls = true;
    public bool createDeleteButtonOnCanvas = true;
    public TMP_Text statusText;
    public TMP_Text floorPresetLabel;
    public Button previousFloorButton;
    public Button nextFloorButton;
    public Button toggleWallsButton;
    public Button rotateLeftButton;
    public Button rotateRightButton;
    public Button clearSelectionButton;
    public Button deleteSelectedButton;

    [Header("Interaction")]
    public FurnitureInteractionController interactionController;

    void Awake()
    {
        ResolveReferences();
        EnsureInteractionController();
    }

    void Start()
    {
        ResolveReferences();
        EnsureInteractionController();
        InitializeRoomInputs();

        if (buildMissingControls)
        {
            BuildRuntimeControls();
        }

        ResolveSceneDeleteButton();
        EnsureDeleteButton();
        WireRuntimeControls();
        RefreshUI();
    }

    void OnDestroy()
    {
        if (interactionController != null)
        {
            interactionController.SelectionChanged -= HandleSelectionChanged;
        }
    }

    public void OnGenerateRoom()
    {
        if (floorController == null)
        {
            SetStatus("Missing floor controller");
            return;
        }

        if (!TryReadDimension(widthInput, "width", out float width) ||
            !TryReadDimension(lengthInput, "length", out float length))
        {
            return;
        }

        floorController.SetFloorSize(width, length);

        if (interactionController != null)
        {
            interactionController.ClampAllFurnitureToRoom();
        }

        RefreshUI();
    }

    public void OnPreviousFloorMaterial()
    {
        if (floorController == null)
        {
            return;
        }

        floorController.SelectPreviousFloorMaterial();
        RefreshUI();
    }

    public void OnNextFloorMaterial()
    {
        if (floorController == null)
        {
            return;
        }

        floorController.SelectNextFloorMaterial();
        RefreshUI();
    }

    public void OnToggleWalls()
    {
        if (floorController == null)
        {
            return;
        }

        floorController.ToggleWalls();
        RefreshUI();
    }

    public void OnRotateSelectedLeft()
    {
        if (interactionController == null || interactionController.SelectedFurniture == null)
        {
            return;
        }

        interactionController.RotateSelected(-interactionController.SelectedFurniture.rotationStep);
        RefreshUI();
    }

    public void OnRotateSelectedRight()
    {
        if (interactionController == null || interactionController.SelectedFurniture == null)
        {
            return;
        }

        interactionController.RotateSelected(interactionController.SelectedFurniture.rotationStep);
        RefreshUI();
    }

    public void OnClearSelection()
    {
        if (interactionController == null)
        {
            return;
        }

        interactionController.ClearSelection();
        RefreshUI();
    }

    public void OnDeleteSelected()
    {
        if (interactionController == null || interactionController.SelectedFurniture == null)
        {
            return;
        }

        interactionController.DeleteSelected();
        RefreshUI();
    }

    void ResolveReferences()
    {
        if (floorController == null)
        {
            floorController = FindFirstObjectByType<FloorController>();
        }

        if (interactionController == null)
        {
            interactionController = FindFirstObjectByType<FurnitureInteractionController>();
        }
    }

    void EnsureInteractionController()
    {
        if (interactionController == null)
        {
            interactionController = GetComponent<FurnitureInteractionController>();
        }

        if (interactionController == null)
        {
            interactionController = FindFirstObjectByType<FurnitureInteractionController>();
        }

        if (interactionController == null)
        {
            interactionController = gameObject.AddComponent<FurnitureInteractionController>();
        }

        if (interactionController == null)
        {
            Debug.LogError("RoomUIController could not create a FurnitureInteractionController.", this);
            return;
        }

        interactionController.floorController = floorController;
        interactionController.statusText = statusText;
        interactionController.SelectionChanged -= HandleSelectionChanged;
        interactionController.SelectionChanged += HandleSelectionChanged;
    }

    void InitializeRoomInputs()
    {
        if (floorController == null)
        {
            return;
        }

        if (widthInput != null && string.IsNullOrWhiteSpace(widthInput.text))
        {
            widthInput.text = FormatNumber(floorController.Width);
        }

        if (lengthInput != null && string.IsNullOrWhiteSpace(lengthInput.text))
        {
            lengthInput.text = FormatNumber(floorController.Length);
        }
    }

    bool TryReadDimension(TMP_InputField input, string label, out float value)
    {
        value = 0f;

        if (input == null)
        {
            SetStatus("Missing " + label + " input");
            return false;
        }

        string text = input.text.Trim();
        bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
            float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

        if (!parsed || value <= 0f)
        {
            SetStatus("Invalid " + label);
            return false;
        }

        return true;
    }

    void BuildRuntimeControls()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null || statusText != null)
        {
            return;
        }

        if (canvas.transform.localScale.sqrMagnitude < 0.0001f)
        {
            canvas.transform.localScale = Vector3.one;
        }

        RectTransform panel = CreatePanel(canvas.transform);
        statusText = CreateText(panel, "Status", "No furniture selected", 15f, FontStyles.Bold);
        floorPresetLabel = CreateText(panel, "Floor Preset", "", 14f, FontStyles.Normal);

        RectTransform floorRow = CreateRow(panel, "Floor Row");
        previousFloorButton = CreateButton(floorRow, "Previous Floor", "Prev");
        nextFloorButton = CreateButton(floorRow, "Next Floor", "Next");

        toggleWallsButton = CreateButton(panel, "Toggle Walls", "Walls");

        RectTransform rotateRow = CreateRow(panel, "Rotate Row");
        rotateLeftButton = CreateButton(rotateRow, "Rotate Left", "Rotate -");
        rotateRightButton = CreateButton(rotateRow, "Rotate Right", "Rotate +");

        clearSelectionButton = CreateButton(panel, "Deselect Furniture", "Deselect");
        deleteSelectedButton = CreateButton(panel, "Delete Selected Furniture", "Delete");
    }

    RectTransform CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Runtime Room Controls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.SetParent(parent, false);
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-16f, -16f);
        panel.sizeDelta = new Vector2(260f, 276f);

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.09f, 0.1f, 0.82f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel;
    }

    RectTransform CreateRow(Transform parent, string name)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.SetParent(parent, false);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 32f;

        return row;
    }

    TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 24f;

        return label;
    }

    Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.20f, 0.24f, 0.27f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.34f, 0.38f, 1f);
        colors.pressedColor = new Color(0.12f, 0.16f, 0.19f, 1f);
        colors.disabledColor = new Color(0.12f, 0.13f, 0.14f, 0.55f);
        button.colors = colors;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 32f;

        TMP_Text buttonText = CreateText(buttonObject.transform, "Label", label, 13f, FontStyles.Bold);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);
        buttonText.alignment = TextAlignmentOptions.Center;

        return button;
    }

    void EnsureDeleteButton()
    {
        if (deleteSelectedButton != null)
        {
            return;
        }

        if (!createDeleteButtonOnCanvas)
        {
            return;
        }

        Transform parent = clearSelectionButton != null
            ? clearSelectionButton.transform.parent
            : null;

        if (parent == null)
        {
            return;
        }

        deleteSelectedButton = CreateButton(parent, "Delete Selected Furniture", "Delete");
        deleteSelectedButton.transform.SetSiblingIndex(clearSelectionButton.transform.GetSiblingIndex() + 1);
    }

    void ResolveSceneDeleteButton()
    {
        if (deleteSelectedButton != null)
        {
            return;
        }

        Transform preferredRoot = clearSelectionButton != null
            ? clearSelectionButton.transform.parent
            : null;

        deleteSelectedButton = FindDeleteButton(preferredRoot);
        if (deleteSelectedButton != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        deleteSelectedButton = canvas != null
            ? FindDeleteButton(canvas.transform)
            : FindDeleteButton(null);
    }

    Button FindDeleteButton(Transform root)
    {
        Button[] buttons = root != null
            ? root.GetComponentsInChildren<Button>(true)
            : FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null || button == clearSelectionButton)
            {
                continue;
            }

            if (IsDeleteButtonName(button.gameObject.name))
            {
                return button;
            }
        }

        return null;
    }

    bool IsDeleteButtonName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string lowerName = objectName.ToLowerInvariant();
        return lowerName.Contains("delete") ||
            lowerName.Contains("remove") ||
            lowerName.Contains("xoa");
    }

    void WireRuntimeControls()
    {
        if (interactionController != null)
        {
            interactionController.statusText = statusText;
        }

        if (previousFloorButton != null)
        {
            previousFloorButton.onClick.RemoveListener(OnPreviousFloorMaterial);
            previousFloorButton.onClick.AddListener(OnPreviousFloorMaterial);
        }

        if (nextFloorButton != null)
        {
            nextFloorButton.onClick.RemoveListener(OnNextFloorMaterial);
            nextFloorButton.onClick.AddListener(OnNextFloorMaterial);
        }

        if (toggleWallsButton != null)
        {
            toggleWallsButton.onClick.RemoveListener(OnToggleWalls);
            toggleWallsButton.onClick.AddListener(OnToggleWalls);
        }

        if (rotateLeftButton != null)
        {
            rotateLeftButton.onClick.RemoveListener(OnRotateSelectedLeft);
            rotateLeftButton.onClick.AddListener(OnRotateSelectedLeft);
        }

        if (rotateRightButton != null)
        {
            rotateRightButton.onClick.RemoveListener(OnRotateSelectedRight);
            rotateRightButton.onClick.AddListener(OnRotateSelectedRight);
        }

        if (clearSelectionButton != null)
        {
            clearSelectionButton.onClick.RemoveListener(OnClearSelection);
            clearSelectionButton.onClick.AddListener(OnClearSelection);
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.onClick.RemoveListener(OnDeleteSelected);
            deleteSelectedButton.onClick.AddListener(OnDeleteSelected);
        }
    }

    void RefreshUI()
    {
        if (floorController != null)
        {
            if (floorPresetLabel != null)
            {
                floorPresetLabel.text = "Floor: " + floorController.CurrentPresetName;
            }

            SetButtonText(toggleWallsButton, floorController.wallsEnabled ? "Walls: On" : "Walls: Off");
        }

        bool hasSelection = interactionController != null && interactionController.SelectedFurniture != null;
        SetFurnitureButtonsInteractable(hasSelection);

        if (hasSelection)
        {
            SetStatus("Selected: " + interactionController.SelectedFurniture.DisplayName);
        }
        else if (floorController != null)
        {
            SetStatus("Room: " + FormatNumber(floorController.Width) + " x " + FormatNumber(floorController.Length));
        }
    }

    void HandleSelectionChanged(MovableFurniture selectedFurniture)
    {
        RefreshUI();
    }

    void SetFurnitureButtonsInteractable(bool interactable)
    {
        if (rotateLeftButton != null)
        {
            rotateLeftButton.interactable = interactable;
        }

        if (rotateRightButton != null)
        {
            rotateRightButton.interactable = interactable;
        }

        if (clearSelectionButton != null)
        {
            clearSelectionButton.interactable = interactable;
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.interactable = interactable;
        }
    }

    void SetButtonText(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    string FormatNumber(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
