using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Factory component responsible for creating all UGUIML UI elements
/// Extracted from UGUIML.cs to improve modularity and maintainability
/// </summary>
public class UGUIMLElementFactory : MonoBehaviour
{
    #region Dependencies - Injected by UGUIML
    
    private UGUIML parentUGUIML;
    private UGUIMLResources guiResources;
    
    // Element dictionaries (references to UGUIML dictionaries)
    private Dictionary<string, Button> buttons;
    private Dictionary<string, CanvasGroup> panels;
    private Dictionary<string, TMP_Text> textElements;
    private Dictionary<string, RawImage> images;
    private Dictionary<string, UGUIMLElement> allElements;
    private Dictionary<string, ScrollRect> scrollViews;
    private Dictionary<string, Slider> progressBars;
    private Dictionary<string, ToggleGroup> toggleGroups;
    private Dictionary<string, Toggle> toggles;
    private Dictionary<string, TMP_InputField> inputFields;
    private Dictionary<string, TMP_Dropdown> dropdowns;
    private Dictionary<string, HorizontalLayoutGroup> horizontalLayouts;
    private Dictionary<string, VerticalLayoutGroup> verticalLayouts;
    private Dictionary<string, GridLayoutGroup> gridLayouts;
    private Dictionary<string, Canvas> nestedCanvases;
    private Dictionary<string, GraphicRaycaster> nestedRaycasters;
    
    #endregion

    #region Factory Initialization
    
    /// <summary>
    /// Initialize the factory with dependencies from UGUIML
    /// </summary>
    public void Initialize(UGUIML uguiml, UGUIMLResources resources, 
        Dictionary<string, Button> buttonsDict,
        Dictionary<string, CanvasGroup> panelsDict,
        Dictionary<string, TMP_Text> textDict,
        Dictionary<string, RawImage> imagesDict,
        Dictionary<string, UGUIMLElement> allElementsDict,
        Dictionary<string, ScrollRect> scrollViewsDict,
        Dictionary<string, Slider> progressBarsDict,
        Dictionary<string, ToggleGroup> toggleGroupsDict,
        Dictionary<string, Toggle> togglesDict,
        Dictionary<string, TMP_InputField> inputFieldsDict,
        Dictionary<string, TMP_Dropdown> dropdownsDict,
        Dictionary<string, HorizontalLayoutGroup> horizontalLayoutsDict,
        Dictionary<string, VerticalLayoutGroup> verticalLayoutsDict,
        Dictionary<string, GridLayoutGroup> gridLayoutsDict,
        Dictionary<string, Canvas> nestedCanvasesDict,
        Dictionary<string, GraphicRaycaster> nestedRaycastersDict)
    {
        parentUGUIML = uguiml;
        guiResources = resources;
        buttons = buttonsDict;
        panels = panelsDict;
        textElements = textDict;
        images = imagesDict;
        allElements = allElementsDict;
        scrollViews = scrollViewsDict;
        progressBars = progressBarsDict;
        toggleGroups = toggleGroupsDict;
        toggles = togglesDict;
        inputFields = inputFieldsDict;
        dropdowns = dropdownsDict;
        horizontalLayouts = horizontalLayoutsDict;
        verticalLayouts = verticalLayoutsDict;
        gridLayouts = gridLayoutsDict;
        nestedCanvases = nestedCanvasesDict;
        nestedRaycasters = nestedRaycastersDict;
    }
    
    #endregion

    #region Main Factory Method
    
    /// <summary>
    /// Create a UI element based on XML node type
    /// </summary>
    public void CreateUIElement(XmlNode node, Transform parent)
    {
        try
        {
            string elementType = UGUIMLParser.GetNormalizedElementType(node.Name);
            string elementName = GetAttribute(node, "name", "");

            if (string.IsNullOrEmpty(elementName))
            {
                Debug.LogWarning($"UGUIMLElementFactory: Element of type '{elementType}' has no name attribute!");
                return;
            }

            GameObject elementObject = new GameObject(elementName);
            elementObject.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rectTransform = elementObject.AddComponent<RectTransform>();
            
            // Add UGUIMLElement component
            UGUIMLElement uguimlElement = elementObject.AddComponent<UGUIMLElement>();
            uguimlElement.Initialize(parentUGUIML, node);

            // Configure RectTransform early to avoid layout recalculation
            ConfigureRectTransform(rectTransform, node);

            // Create specific element type
            switch (elementType)
            {
                case "canvas":
                    CreateNestedCanvas(elementObject, node);
                    break;
                case "panel":
                    CreatePanel(elementObject, node);
                    break;
                case "text":
                    CreateText(elementObject, node);
                    break;
                case "button":
                    CreateButton(elementObject, node);
                    break;
                case "image":
                    CreateImage(elementObject, node);
                    break;
                case "scrollview":
                    CreateScrollView(elementObject, node);
                    break;
                case "progressbar":
                    CreateProgressBar(elementObject, node);
                    break;
                case "togglegroup":
                    CreateToggleGroup(elementObject, node);
                    break;
                case "toggle":
                    CreateToggle(elementObject, node);
                    break;
                case "inputfield":
                    CreateInputField(elementObject, node);
                    break;
                case "dropdown":
                    CreateDropdown(elementObject, node);
                    break;
                case "horizontallayout":
                    CreateHorizontalLayout(elementObject, node);
                    break;
                case "verticallayout":
                    CreateVerticalLayout(elementObject, node);
                    break;
                case "gridlayout":
                    CreateGridLayout(elementObject, node);
                    break;
                default:
                    Debug.LogWarning($"UGUIMLElementFactory: Unknown element type '{elementType}'");
                    break;
            }

            // Add to all elements dictionary
            allElements[elementName] = uguimlElement;

            // Parse child nodes - use content area for ScrollView
            Transform parentForChildren = elementObject.transform;
            if (elementType == "scrollview")
            {
                // Find the Content object for ScrollView children
                Transform content = elementObject.transform.Find("Viewport/Content");
                if (content != null)
                {
                    parentForChildren = content;
                }
            }
            
            // Parse child nodes
            ParseChildNodes(node, parentForChildren);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIMLElementFactory: Exception creating element - {e.Message}\nStack: {e.StackTrace}");
            throw;
        }
    }
    
    #endregion

    #region Basic Element Creation Methods
    
    public void CreatePanel(GameObject panelObject, XmlNode node)
    {
        try
        {
            if (panelObject == null || node == null) return;

            CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelObject.AddComponent<CanvasGroup>();
            }
            
            int bindId = UGUIMLParser.GetIntAttribute(node, "bindId", -1);
            
            float alpha = UGUIMLParser.GetFloatAttribute(node, "alpha", 1f);
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = UGUIMLParser.GetBoolAttribute(node, "interactable", alpha > 0f);
            canvasGroup.blocksRaycasts = UGUIMLParser.GetBoolAttribute(node, "blocksRaycasts", alpha > 0f);

            string panelName = GetAttribute(node, "name", "");
            if (panels == null) return;
            panels[panelName] = canvasGroup;

            if (guiResources != null && guiResources.panels != null && !guiResources.panels.ContainsKey(panelName))
            {
                guiResources.panels[panelName] = canvasGroup;
            }

            string backgroundColor = GetAttribute(node, "backgroundColor", "");
            bool needsBackground = !string.IsNullOrEmpty(backgroundColor) || (bindId == -1 && guiResources != null && guiResources.DefaultPanelBackground != null);
            
            if (needsBackground)
            {
                Image backgroundImage = panelObject.AddComponent<Image>();
                
                if (bindId == -1 && guiResources != null)
                {
                    if (guiResources.DefaultPanelBackground != null)
                    {
                        backgroundImage.sprite = guiResources.DefaultPanelBackground;
                        backgroundImage.type = Image.Type.Sliced;
                    }
                    backgroundImage.color = guiResources.DefaultPanelColor;
                }
                
                if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color color))
                {
                    backgroundImage.color = color;
                }
                
                backgroundImage.raycastTarget = UGUIMLParser.GetBoolAttribute(node, "raycastTarget", false);
                backgroundImage.maskable = UGUIMLParser.GetBoolAttribute(node, "maskable", false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIMLElementFactory CreatePanel: Exception - {e.Message}");
            throw;
        }
    }

    public void CreateText(GameObject textObject, XmlNode node)
    {
        TMP_Text textComponent = textObject.AddComponent<TextMeshProUGUI>();
        
        int bindId = UGUIMLParser.GetIntAttribute(node, "bindId", -1);
        
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultFont != null)
            {
                textComponent.font = guiResources.DefaultFont;
            }
            textComponent.color = guiResources.DefaultTextColor;
            textComponent.fontSize = guiResources.DefaultFontSize;
        }
        
        textComponent.text = GetAttribute(node, "text", "");
        
        float fontSize = UGUIMLParser.GetFloatAttribute(node, "fontSize", textComponent.fontSize);
        textComponent.fontSize = fontSize;
        
        string colorHex = GetAttribute(node, "color", "");
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            textComponent.color = color;
        }

        string alignment = GetAttribute(node, "alignment", "center");
        textComponent.alignment = UGUIMLParser.ParseTextAlignment(alignment);

        textComponent.raycastTarget = UGUIMLParser.GetBoolAttribute(node, "raycastTarget", false);
        textComponent.maskable = UGUIMLParser.GetBoolAttribute(node, "maskable", false);

        string textName = GetAttribute(node, "name", "");
        textElements[textName] = textComponent;

        if (bindId >= 0 && guiResources != null)
        {
            if (guiResources.resources != null && bindId < guiResources.resources.Count)
            {
                guiResources.resources[bindId].resourceType = GUIResourceTypes.Text;
                guiResources.resources[bindId].bindings.Add(textObject.AddComponent<UGUIMLElement>());
            }
            else
            {
                string count = guiResources.resources != null ? guiResources.resources.Count.ToString() : "null";
                Debug.LogWarning($"UGUIMLElementFactory: Text bind ID {bindId} is out of range. Resources count: {count}");
            }
        }
    }

    public void CreateButton(GameObject buttonObject, XmlNode node)
    {
        Image buttonImage = buttonObject.AddComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        
        int bindId = UGUIMLParser.GetIntAttribute(node, "bindId", -1);
        
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultButtonBackground != null)
            {
                buttonImage.sprite = guiResources.DefaultButtonBackground;
                buttonImage.type = Image.Type.Sliced;
            }
            buttonImage.color = guiResources.DefaultButtonColor;
        }
        
        string backgroundColor = GetAttribute(node, "backgroundColor", "");
        if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color bgColor))
        {
            buttonImage.color = bgColor;
        }

        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = buttonImage.color;
        colorBlock.highlightedColor = Color.Lerp(buttonImage.color, Color.white, 0.2f);
        colorBlock.pressedColor = Color.Lerp(buttonImage.color, Color.black, 0.2f);
        colorBlock.selectedColor = colorBlock.highlightedColor;
        colorBlock.disabledColor = Color.Lerp(buttonImage.color, Color.gray, 0.5f);
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.1f;
        button.colors = colorBlock;

        buttonImage.raycastTarget = true;
        buttonImage.maskable = UGUIMLParser.GetBoolAttribute(node, "maskable", false);
        button.targetGraphic = buttonImage;

        string buttonName = GetAttribute(node, "name", "");
        buttons[buttonName] = button;

        string buttonText = GetAttribute(node, "text", "");
        if (!string.IsNullOrEmpty(buttonText))
        {
            CreateButtonText(buttonObject, buttonText, node);
        }
    }

    public void CreateImage(GameObject imageObject, XmlNode node)
    {
        RawImage rawImage = imageObject.AddComponent<RawImage>();
        
        int bindId = UGUIMLParser.GetIntAttribute(node, "bindId", -1);
        
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultImageTexture != null)
            {
                rawImage.texture = guiResources.DefaultImageTexture;
            }
        }
        
        string colorHex = GetAttribute(node, "color", "");
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            rawImage.color = color;
        }

        rawImage.raycastTarget = UGUIMLParser.GetBoolAttribute(node, "raycastTarget", false);
        rawImage.maskable = UGUIMLParser.GetBoolAttribute(node, "maskable", false);

        string imageName = GetAttribute(node, "name", "");
        images[imageName] = rawImage;

        if (bindId >= 0 && guiResources != null)
        {
            Debug.Log($"UGUIMLElementFactory: Image binding to ID {bindId} - binding system needs to be implemented in UGUIMLResources");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private void CreateButtonText(GameObject buttonObject, string buttonText, XmlNode node)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TMP_Text textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = buttonText;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = UGUIMLParser.GetFloatAttribute(node, "fontSize", 14f);
        textComponent.raycastTarget = false;
        textComponent.maskable = UGUIMLParser.GetBoolAttribute(node, "maskable", false);
        
        string textColor = GetAttribute(node, "textColor", "#000000");
        if (ColorUtility.TryParseHtmlString(textColor, out Color color))
        {
            textComponent.color = color;
        }
    }
    
    private void ConfigureRectTransform(RectTransform rectTransform, XmlNode node)
    {
        string positionAttr = GetAttribute(node, "position", "");
        if (string.IsNullOrEmpty(positionAttr))
        {
            positionAttr = GetAttribute(node, "anchoredPosition", "0,0");
        }
        Vector2 position = UGUIMLParser.ParseVector2(positionAttr);
        rectTransform.anchoredPosition = position;

        string sizeAttr = GetAttribute(node, "size", "");
        if (string.IsNullOrEmpty(sizeAttr))
        {
            float width = UGUIMLParser.GetFloatAttribute(node, "width", 100f);
            float height = UGUIMLParser.GetFloatAttribute(node, "height", 100f);
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        else
        {
            Vector2 size = UGUIMLParser.ParseVector2(sizeAttr);
            rectTransform.sizeDelta = size;
        }

        Vector2 anchorMin = UGUIMLParser.ParseVector2(GetAttribute(node, "anchorMin", "0.5,0.5"));
        Vector2 anchorMax = UGUIMLParser.ParseVector2(GetAttribute(node, "anchorMax", "0.5,0.5"));
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        Vector2 pivot = UGUIMLParser.ParseVector2(GetAttribute(node, "pivot", "0.5,0.5"));
        rectTransform.pivot = pivot;

        Vector3 scale = UGUIMLParser.ParseVector3(GetAttribute(node, "scale", "1,1,1"));
        rectTransform.localScale = scale;
    }
    
    private string GetAttribute(XmlNode node, string attributeName, string defaultValue)
    {
        return UGUIMLParser.GetAttribute(node, attributeName, defaultValue);
    }
    
    /// <summary>
    /// Parse child nodes of an XML element
    /// </summary>
    private void ParseChildNodes(XmlNode node, Transform parent)
    {
        if (node == null || parent == null) return;
        
        foreach (XmlNode childNode in node.ChildNodes)
        {
            if (childNode.NodeType == XmlNodeType.Element)
            {
                CreateUIElement(childNode, parent);
            }
        }
    }
    
    #endregion

    #region Complex Element Creation Methods
    
    public void CreateNestedCanvas(GameObject canvasObject, XmlNode node)
    {
        try
        {
            Canvas nestedCanvas = canvasObject.AddComponent<Canvas>();
            
            string renderMode = GetAttribute(node, "renderMode", "overlay").ToLower();
            switch (renderMode)
            {
                case "overlay":
                    nestedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
                case "camera":
                    nestedCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    string cameraName = GetAttribute(node, "camera", "");
                    if (!string.IsNullOrEmpty(cameraName))
                    {
                        Camera cam = GameObject.Find(cameraName)?.GetComponent<Camera>();
                        if (cam != null) nestedCanvas.worldCamera = cam;
                    }
                    break;
                case "world":
                    nestedCanvas.renderMode = RenderMode.WorldSpace;
                    break;
                default:
                    nestedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
            }

            nestedCanvas.sortingOrder = UGUIMLParser.GetIntAttribute(node, "sortingOrder", 0);
            
            string sortingLayer = GetAttribute(node, "sortingLayer", "");
            if (!string.IsNullOrEmpty(sortingLayer))
            {
                nestedCanvas.sortingLayerName = sortingLayer;
            }

            nestedCanvas.pixelPerfect = UGUIMLParser.GetBoolAttribute(node, "pixelPerfect", false);

            if (nestedCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                nestedCanvas.planeDistance = UGUIMLParser.GetFloatAttribute(node, "planeDistance", 100f);
            }

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = UGUIMLParser.GetBoolAttribute(node, "ignoreReversedGraphics", true);
            raycaster.blockingObjects = UGUIMLParser.GetBoolAttribute(node, "blockingObjects", false) ? 
                GraphicRaycaster.BlockingObjects.All : GraphicRaycaster.BlockingObjects.None;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UGUIMLParser.ParseVector2(GetAttribute(node, "referenceResolution", "1920,1080"));

            string canvasName = GetAttribute(node, "name", "");
            nestedCanvases[canvasName] = nestedCanvas;
            nestedRaycasters[canvasName] = raycaster;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIMLElementFactory CreateNestedCanvas: Exception - {e.Message}");
            throw;
        }
    }

    public void CreateScrollView(GameObject scrollObject, XmlNode node)
    {
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        
        Image backgroundImage = scrollObject.AddComponent<Image>();
        string backgroundColor = GetAttribute(node, "backgroundColor", "");
        if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color bgColor))
        {
            backgroundImage.color = bgColor;
        }
        else
        {
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }
        backgroundImage.raycastTarget = UGUIMLParser.GetBoolAttribute(node, "raycastTarget", true);
        
        // Create viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image maskImage = viewport.AddComponent<Image>();
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
        
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Create content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        
        bool isHorizontal = UGUIMLParser.GetBoolAttribute(node, "horizontal", false);
        bool isVertical = UGUIMLParser.GetBoolAttribute(node, "vertical", true);
        
        if (isVertical && !isHorizontal)
        {
            contentRect.anchorMin = Vector2.left;
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
        }
        else if (isHorizontal && !isVertical)
        {
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.left;
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.sizeDelta = new Vector2(300, 0);
        }
        else
        {
            contentRect.anchorMin = Vector2.left;
            contentRect.anchorMax = Vector2.left;
            contentRect.pivot = Vector2.left;
            contentRect.sizeDelta = new Vector2(300, 300);
        }
        
        contentRect.anchoredPosition = Vector2.zero;
        
        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        if (isVertical) sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (isHorizontal) sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        if (isVertical && !isHorizontal)
        {
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }
        else if (isHorizontal && !isVertical)
        {
            HorizontalLayoutGroup layoutGroup = content.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;
        }
        
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = isHorizontal;
        scrollRect.vertical = isVertical;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = UGUIMLParser.GetFloatAttribute(node, "elasticity", 0.1f);
        scrollRect.inertia = UGUIMLParser.GetBoolAttribute(node, "inertia", true);
        scrollRect.decelerationRate = UGUIMLParser.GetFloatAttribute(node, "deceleration", 0.135f);
        
        string scrollName = GetAttribute(node, "name", "");
        scrollViews[scrollName] = scrollRect;
    }

    public void CreateProgressBar(GameObject progressObject, XmlNode node)
    {
        Slider slider = progressObject.AddComponent<Slider>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(progressObject.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "backgroundColor", ""), out Color bgColor) ? 
                       bgColor : (guiResources != null ? guiResources.DefaultProgressBarBackgroundColor : new Color(0.2f, 0.3f, 0.4f));
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(progressObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.left;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "fillColor", ""), out Color fillColor) ? 
                         fillColor : (guiResources != null ? guiResources.DefaultProgressBarFillColor : new Color(0.2f, 0.6f, 0.9f));
        
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = UGUIMLParser.GetFloatAttribute(node, "minValue", 0f);
        slider.maxValue = UGUIMLParser.GetFloatAttribute(node, "maxValue", 1f);
        slider.value = UGUIMLParser.GetFloatAttribute(node, "value", 0f);
        slider.interactable = UGUIMLParser.GetBoolAttribute(node, "interactable", false);
        
        string progressName = GetAttribute(node, "name", "");
        progressBars[progressName] = slider;
    }

    public void CreateToggleGroup(GameObject groupObject, XmlNode node)
    {
        ToggleGroup toggleGroup = groupObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = UGUIMLParser.GetBoolAttribute(node, "allowSwitchOff", false);
        
        string groupName = GetAttribute(node, "name", "");
        toggleGroups[groupName] = toggleGroup;
    }

    public void CreateToggle(GameObject toggleObject, XmlNode node)
    {
        Toggle toggle = toggleObject.AddComponent<Toggle>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObject.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(10, 0);
        bgRect.sizeDelta = new Vector2(20, 20);
        
        Image bgImage = background.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultCheckboxBackground != null)
        {
            bgImage.sprite = guiResources.DefaultCheckboxBackground;
            bgImage.type = Image.Type.Sliced;
        }
        bgImage.color = guiResources != null ? guiResources.DefaultCheckboxColor : Color.white;
        
        // Create checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = Vector2.zero;
        checkRect.anchoredPosition = Vector2.zero;
        
        Image checkImage = checkmark.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultCheckboxCheckmark != null)
        {
            checkImage.sprite = guiResources.DefaultCheckboxCheckmark;
            checkImage.type = Image.Type.Simple;
        }
        checkImage.color = guiResources != null ? guiResources.DefaultCheckmarkColor : new Color(0.2f, 0.6f, 0.9f);
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = UGUIMLParser.GetBoolAttribute(node, "isOn", false);
        
        string groupName = GetAttribute(node, "group", "");
        if (!string.IsNullOrEmpty(groupName) && toggleGroups.ContainsKey(groupName))
        {
            toggle.group = toggleGroups[groupName];
        }
        
        string toggleName = GetAttribute(node, "name", "");
        toggles[toggleName] = toggle;
    }

    public void CreateInputField(GameObject inputObject, XmlNode node)
    {
        TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
        
        Image backgroundImage = inputObject.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultInputFieldBackground != null)
        {
            backgroundImage.sprite = guiResources.DefaultInputFieldBackground;
            backgroundImage.type = Image.Type.Sliced;
        }
        backgroundImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "backgroundColor", ""), out Color bgColor) ? 
                               bgColor : (guiResources != null ? guiResources.DefaultInputFieldColor : new Color(0.17f, 0.24f, 0.31f));
        
        // Create text area with mask
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObject.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);
        
        RectMask2D textMask = textArea.AddComponent<RectMask2D>();
        
        // Create text component
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);
        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TMP_Text textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.fontSize = UGUIMLParser.GetFloatAttribute(node, "fontSize", 14f);
        textComponent.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "textColor", "#FFFFFF"), out Color textColor) ? textColor : Color.white;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        
        inputField.targetGraphic = backgroundImage;
        inputField.textComponent = textComponent;
        inputField.text = GetAttribute(node, "text", "");
        inputField.characterLimit = UGUIMLParser.GetIntAttribute(node, "characterLimit", 0);
        
        string inputName = GetAttribute(node, "name", "");
        inputFields[inputName] = inputField;
    }

    public void CreateDropdown(GameObject dropdownObject, XmlNode node)
    {
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        
        Image backgroundImage = dropdownObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.17f, 0.24f, 0.31f);
        
        // Create label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(dropdownObject.transform, false);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);
        
        TMP_Text labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = UGUIMLParser.GetFloatAttribute(node, "fontSize", 14f);
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        
        dropdown.captionText = labelText;
        dropdown.targetGraphic = backgroundImage;
        
        string optionsStr = GetAttribute(node, "options", "");
        if (!string.IsNullOrEmpty(optionsStr))
        {
            string[] optionArray = optionsStr.Split(',');
            dropdown.options.Clear();
            foreach (string option in optionArray)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option.Trim()));
            }
        }
        
        dropdown.value = UGUIMLParser.GetIntAttribute(node, "value", 0);
        dropdown.RefreshShownValue();
        
        string dropdownName = GetAttribute(node, "name", "");
        dropdowns[dropdownName] = dropdown;
    }
    
    #endregion

    #region Layout Group Creation Methods
    
    public void CreateHorizontalLayout(GameObject layoutObject, XmlNode node)
    {
        HorizontalLayoutGroup layout = layoutObject.AddComponent<HorizontalLayoutGroup>();
        
        layout.spacing = UGUIMLParser.GetFloatAttribute(node, "spacing", 0f);
        layout.childAlignment = UGUIMLParser.ParseTextAnchor(GetAttribute(node, "childAlignment", "MiddleCenter"));
        layout.reverseArrangement = UGUIMLParser.GetBoolAttribute(node, "reverse", false);
        layout.childControlWidth = UGUIMLParser.GetBoolAttribute(node, "controlChildWidth", true);
        layout.childControlHeight = UGUIMLParser.GetBoolAttribute(node, "controlChildHeight", true);
        layout.childForceExpandWidth = UGUIMLParser.GetBoolAttribute(node, "forceExpandWidth", true);
        layout.childForceExpandHeight = UGUIMLParser.GetBoolAttribute(node, "forceExpandHeight", false);
        
        Vector4 padding = UGUIMLParser.ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        bool autoSize = UGUIMLParser.GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        string layoutName = GetAttribute(node, "name", "");
        horizontalLayouts[layoutName] = layout;
    }

    public void CreateVerticalLayout(GameObject layoutObject, XmlNode node)
    {
        VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
        
        layout.spacing = UGUIMLParser.GetFloatAttribute(node, "spacing", 0f);
        layout.childAlignment = UGUIMLParser.ParseTextAnchor(GetAttribute(node, "childAlignment", "UpperCenter"));
        layout.reverseArrangement = UGUIMLParser.GetBoolAttribute(node, "reverse", false);
        layout.childControlWidth = UGUIMLParser.GetBoolAttribute(node, "controlChildWidth", true);
        layout.childControlHeight = UGUIMLParser.GetBoolAttribute(node, "controlChildHeight", true);
        layout.childForceExpandWidth = UGUIMLParser.GetBoolAttribute(node, "forceExpandWidth", false);
        layout.childForceExpandHeight = UGUIMLParser.GetBoolAttribute(node, "forceExpandHeight", true);
        
        Vector4 padding = UGUIMLParser.ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        bool autoSize = UGUIMLParser.GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        string layoutName = GetAttribute(node, "name", "");
        verticalLayouts[layoutName] = layout;
    }

    public void CreateGridLayout(GameObject layoutObject, XmlNode node)
    {
        GridLayoutGroup layout = layoutObject.AddComponent<GridLayoutGroup>();
        
        layout.cellSize = UGUIMLParser.ParseVector2(GetAttribute(node, "cellSize", "100,100"));
        layout.spacing = UGUIMLParser.ParseVector2(GetAttribute(node, "spacing", "0,0"));
        layout.startCorner = UGUIMLParser.ParseGridCorner(GetAttribute(node, "startCorner", "UpperLeft"));
        layout.startAxis = UGUIMLParser.ParseGridAxis(GetAttribute(node, "startAxis", "Horizontal"));
        layout.childAlignment = UGUIMLParser.ParseTextAnchor(GetAttribute(node, "childAlignment", "UpperLeft"));
        layout.constraint = UGUIMLParser.ParseGridConstraint(GetAttribute(node, "constraint", "FixedColumnCount"));
        layout.constraintCount = UGUIMLParser.GetIntAttribute(node, "constraintCount", 1);
        
        Vector4 padding = UGUIMLParser.ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        bool autoSize = UGUIMLParser.GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        string layoutName = GetAttribute(node, "name", "");
        gridLayouts[layoutName] = layout;
    }
    
    #endregion
} 