using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main component that parses XML markup to create Unity uGUI hierarchies
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UGUIML : MonoBehaviour
{
    [Header("XML Configuration")]
    [SerializeField] private TextAsset xmlFile;
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool clearCanvasOnLoad = true;

    [Header("Runtime References")]
    private Canvas targetCanvas;
    private UGUIMLResources guiResources;

    [Header("UI Element Dictionaries")]
    public Dictionary<string, Button> buttons = new Dictionary<string, Button>();
    public Dictionary<string, CanvasGroup> panels = new Dictionary<string, CanvasGroup>();
    public Dictionary<string, TMP_Text> textElements = new Dictionary<string, TMP_Text>();
    public Dictionary<string, RawImage> images = new Dictionary<string, RawImage>();
    public Dictionary<string, UGUIMLElement> allElements = new Dictionary<string, UGUIMLElement>();
    
    [Header("Rich Component Dictionaries")]
    public Dictionary<string, ScrollRect> scrollViews = new Dictionary<string, ScrollRect>();
    public Dictionary<string, Slider> progressBars = new Dictionary<string, Slider>();
    public Dictionary<string, ToggleGroup> toggleGroups = new Dictionary<string, ToggleGroup>();
    public Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();
    public Dictionary<string, TMP_InputField> inputFields = new Dictionary<string, TMP_InputField>();
    public Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>();
    
    [Header("Layout Groups")]
    public Dictionary<string, HorizontalLayoutGroup> horizontalLayouts = new Dictionary<string, HorizontalLayoutGroup>();
    public Dictionary<string, VerticalLayoutGroup> verticalLayouts = new Dictionary<string, VerticalLayoutGroup>();
    public Dictionary<string, GridLayoutGroup> gridLayouts = new Dictionary<string, GridLayoutGroup>();

    [Header("Nested Canvases")]
    public Dictionary<string, Canvas> nestedCanvases = new Dictionary<string, Canvas>();
    public Dictionary<string, GraphicRaycaster> nestedRaycasters = new Dictionary<string, GraphicRaycaster>();

    private XmlDocument xmlDocument;

    public Canvas TargetCanvas => targetCanvas;
    public bool IsLoaded { get; private set; }
    public TextAsset XmlFile => xmlFile;

    #region Unity Lifecycle

    private void Awake()
    {
        // Initialize dictionaries if they're null (safety check)
        if (buttons == null) buttons = new Dictionary<string, Button>();
        if (panels == null) panels = new Dictionary<string, CanvasGroup>();
        if (textElements == null) textElements = new Dictionary<string, TMP_Text>();
        if (images == null) images = new Dictionary<string, RawImage>();
        if (allElements == null) allElements = new Dictionary<string, UGUIMLElement>();
        
        // Initialize rich component dictionaries
        if (scrollViews == null) scrollViews = new Dictionary<string, ScrollRect>();
        if (progressBars == null) progressBars = new Dictionary<string, Slider>();
        if (toggleGroups == null) toggleGroups = new Dictionary<string, ToggleGroup>();
        if (toggles == null) toggles = new Dictionary<string, Toggle>();
        if (inputFields == null) inputFields = new Dictionary<string, TMP_InputField>();
        if (dropdowns == null) dropdowns = new Dictionary<string, TMP_Dropdown>();
        
        // Initialize layout dictionaries
        if (horizontalLayouts == null) horizontalLayouts = new Dictionary<string, HorizontalLayoutGroup>();
        if (verticalLayouts == null) verticalLayouts = new Dictionary<string, VerticalLayoutGroup>();
        if (gridLayouts == null) gridLayouts = new Dictionary<string, GridLayoutGroup>();

        // Initialize nested canvas dictionaries
        if (nestedCanvases == null) nestedCanvases = new Dictionary<string, Canvas>();
        if (nestedRaycasters == null) nestedRaycasters = new Dictionary<string, GraphicRaycaster>();

        targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogError("UGUIML: Canvas component not found! This component requires a Canvas component on the same GameObject.");
            return;
        }

        // Ensure the main canvas has proper scaler settings for UGUIML layouts
        EnsureCanvasScalerConfiguration();

        guiResources = UGUIMLResources.Singleton;
        if (guiResources == null)
        {
            Debug.LogError("UGUIML: UGUIMLResources singleton not found! Make sure UGUIMLResources is active in the scene.");
        }
    }

    private void Start()
    {
        if (autoLoadOnStart && xmlFile != null)
        {
            LoadXML();
        }
    }

    /// <summary>
    /// Ensure the main canvas has proper CanvasScaler configuration for UGUIML layouts
    /// </summary>
    private void EnsureCanvasScalerConfiguration()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            Debug.Log("UGUIML: Added CanvasScaler component to main canvas");
        }

        // Set default UGUIML-optimized scaler settings if not already configured properly
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
            scaler.referenceResolution != new Vector2(1920, 1080))
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balanced scaling between width and height
            
            Debug.Log($"UGUIML: Configured main canvas scaler with 1920x1080 reference resolution and 0.5 match ratio");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Load and parse the XML file to create UI hierarchy
    /// </summary>
    public void LoadXML()
    {
        if (xmlFile == null)
        {
            Debug.LogError("UGUIML: No XML file assigned!");
            return;
        }

        LoadXML(xmlFile.text);
    }

    /// <summary>
    /// Load and parse XML from string
    /// </summary>
    /// <param name="xmlContent">XML content as string</param>
    public void LoadXML(string xmlContent)
    {
        try
        {
            // Find UGUIMLResources if not assigned
            if (guiResources == null)
            {
                guiResources = UGUIMLResources.Singleton;
                if (guiResources == null)
                {
                    Debug.LogWarning("UGUIML: UGUIMLResources.Singleton not found. Element binding and default resources will not work.");
                }
                else
                {
                    Debug.Log($"UGUIML: Found UGUIMLResources. Default resources available: " +
                             $"ButtonSprite={guiResources.DefaultButtonBackground != null}, " +
                             $"PanelSprite={guiResources.DefaultPanelBackground != null}, " +
                             $"ImageTexture={guiResources.DefaultImageTexture != null}, " +
                             $"Font={guiResources.DefaultFont != null}");
                }
            }
            
            // Validate required components
            if (targetCanvas == null)
            {
                Debug.LogError("UGUIML: Target Canvas is null! Make sure this GameObject has a Canvas component.");
                return;
            }

            if (string.IsNullOrEmpty(xmlContent))
            {
                Debug.LogError("UGUIML: XML content is null or empty!");
                return;
            }

            if (clearCanvasOnLoad)
            {
                ClearCanvas();
            }

            xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlContent);

            XmlNode rootNode = xmlDocument.SelectSingleNode("UGUIML");
            if (rootNode != null)
            {
                ParseNode(rootNode, targetCanvas.transform);
                IsLoaded = true;
                Debug.Log($"UGUIML: Successfully loaded {allElements.Count} UI elements");
            }
            else
            {
                Debug.LogError("UGUIML: Root 'UGUIML' node not found in XML!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UGUIML: Error parsing XML - {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Clear all UI elements from the canvas
    /// </summary>
    public void ClearCanvas()
    {
        // Remove bindings from UGUIMLResources
        foreach (var element in allElements.Values)
        {
            element.RemoveBinding();
        }

        // Clear dictionaries
        buttons.Clear();
        panels.Clear();
        textElements.Clear();
        images.Clear();
        allElements.Clear();
        
        // Clear rich component dictionaries
        scrollViews.Clear();
        progressBars.Clear();
        toggleGroups.Clear();
        toggles.Clear();
        inputFields.Clear();
        dropdowns.Clear();
        
        // Clear layout dictionaries
        horizontalLayouts.Clear();
        verticalLayouts.Clear();
        gridLayouts.Clear();

        // Clear nested canvas dictionaries
        nestedCanvases.Clear();
        nestedRaycasters.Clear();

        // Destroy child GameObjects
        if (targetCanvas != null)
        {
            for (int i = targetCanvas.transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(targetCanvas.transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(targetCanvas.transform.GetChild(i).gameObject);
                }
            }
        }

        IsLoaded = false;
    }

    /// <summary>
    /// Get UI element by name and type
    /// </summary>
    public T GetElement<T>(string elementName) where T : Component
    {
        if (allElements.TryGetValue(elementName, out UGUIMLElement element))
        {
            return element.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// Get UGUIMLElement by name
    /// </summary>
    public UGUIMLElement GetUGUIMLElement(string elementName)
    {
        allElements.TryGetValue(elementName, out UGUIMLElement element);
        return element;
    }

    /// <summary>
    /// Get ScrollView by name
    /// </summary>
    public ScrollRect GetScrollView(string scrollViewName)
    {
        scrollViews.TryGetValue(scrollViewName, out ScrollRect scrollView);
        return scrollView;
    }

    /// <summary>
    /// Get ProgressBar by name
    /// </summary>
    public Slider GetProgressBar(string progressBarName)
    {
        progressBars.TryGetValue(progressBarName, out Slider progressBar);
        return progressBar;
    }

    /// <summary>
    /// Get Toggle by name
    /// </summary>
    public Toggle GetToggle(string toggleName)
    {
        toggles.TryGetValue(toggleName, out Toggle toggle);
        return toggle;
    }

    /// <summary>
    /// Get InputField by name
    /// </summary>
    public TMP_InputField GetInputField(string inputFieldName)
    {
        inputFields.TryGetValue(inputFieldName, out TMP_InputField inputField);
        return inputField;
    }

    /// <summary>
    /// Get Dropdown by name
    /// </summary>
    public TMP_Dropdown GetDropdown(string dropdownName)
    {
        dropdowns.TryGetValue(dropdownName, out TMP_Dropdown dropdown);
        return dropdown;
    }

    /// <summary>
    /// Set progress bar value with optional animation
    /// </summary>
    public void SetProgressBarValue(string progressBarName, float value, bool animate = false)
    {
        if (progressBars.TryGetValue(progressBarName, out Slider progressBar))
        {
            if (animate && Application.isPlaying)
            {
                StartCoroutine(AnimateProgressBar(progressBar, value));
            }
            else
            {
                progressBar.value = value;
            }
        }
    }

    private System.Collections.IEnumerator AnimateProgressBar(Slider progressBar, float targetValue)
    {
        float startValue = progressBar.value;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            progressBar.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        progressBar.value = targetValue;
    }

    /// <summary>
    /// External command handler reference for bridging to other components
    /// </summary>
    [Header("External Command Handler")]
    [SerializeField] private MonoBehaviour externalCommandHandler;

    /// <summary>
    /// Set external command handler component that contains command methods
    /// </summary>
    public void SetExternalCommandHandler(MonoBehaviour handler)
    {
        externalCommandHandler = handler;
        Debug.Log($"UGUIML: External command handler set to {handler?.name ?? "null"}");
    }

    /// <summary>
    /// Get the external command handler for method invocation
    /// </summary>
    public MonoBehaviour GetExternalCommandHandler()
    {
        return externalCommandHandler;
    }

    /// <summary>
    /// Play animation on an element by name with parameters
    /// </summary>
    /// <param name="elementName">Name of the element to animate</param>
    /// <param name="animationType">Type of animation (fadein, fadeout, moveto, scale, slideoffscreen, bounce)</param>
    /// <param name="parameters">Animation parameters (varies by type)</param>
    public void PlayAnimationOnElement(string elementName, string animationType, params string[] parameters)
    {
        var element = GetUGUIMLElement(elementName);
        if (element == null)
        {
            Debug.LogWarning($"UGUIML: Element '{elementName}' not found for animation!");
            return;
        }

        float speed = parameters.Length > 0 && float.TryParse(parameters[0], out float s) ? s : 1f;

        switch (animationType.ToLower())
        {
            case "fadein":
                element.FadeIn(speed);
                break;
            case "fadeout":
                element.FadeOut(speed);
                break;
            case "fade":
                if (parameters.Length > 1 && float.TryParse(parameters[1], out float alpha))
                {
                    element.AnimateFade(alpha, speed);
                }
                break;
            case "moveto":
                if (parameters.Length >= 3 && 
                    float.TryParse(parameters[1], out float x) && 
                    float.TryParse(parameters[2], out float y))
                {
                    element.AnimateToPosition(new Vector2(x, y), speed);
                }
                break;
            case "movetooriginal":
                element.AnimateToOriginalPosition(speed);
                break;
            case "scale":
                if (parameters.Length > 1 && float.TryParse(parameters[1], out float scale))
                {
                    element.AnimateScale(Vector3.one * scale, speed);
                }
                break;
            case "slideoffscreen":
                OffScreenDirection direction = OffScreenDirection.Left;
                if (parameters.Length > 1)
                {
                    System.Enum.TryParse(parameters[1], true, out direction);
                }
                element.AnimateOffScreen(direction, speed);
                break;
            case "bounce":
                StartCoroutine(BounceElementAnimation(element, speed));
                break;
            case "show":
                element.FadeIn(speed);
                element.AnimateToOriginalPosition(speed);
                element.AnimateScale(Vector3.one, speed);
                break;
            case "hide":
                element.FadeOut(speed);
                element.AnimateOffScreen(OffScreenDirection.Left, speed);
                break;
            default:
                Debug.LogWarning($"UGUIML: Unknown animation type '{animationType}' for element '{elementName}'");
                break;
        }

      //  Debug.Log($"UGUIML: Playing '{animationType}' animation on element '{elementName}'");
    }

    /// <summary>
    /// Bounce animation coroutine for elements
    /// </summary>
    private System.Collections.IEnumerator BounceElementAnimation(UGUIMLElement element, float speed)
    {
        Vector3 originalScale = element.RectTransform.localScale;
        float bounceScale = 1.2f;
        
        // Scale up
        element.AnimateScale(originalScale * bounceScale, speed * 2f);
        yield return new UnityEngine.WaitForSeconds(0.1f / speed);
        
        // Scale back down
        element.AnimateScale(originalScale, speed * 2f);
    }

    /// <summary>
    /// Invoke an element's event/command by simulating the event
    /// </summary>
    /// <param name="elementName">Name of the element</param>
    /// <param name="eventType">Type of event to trigger (click, valuechanged, etc.)</param>
    /// <param name="parameters">Optional parameters for the event</param>
    public void InvokeElementCommand(string elementName, string eventType, params string[] parameters)
    {
        var element = GetUGUIMLElement(elementName);
        if (element == null)
        {
            Debug.LogWarning($"UGUIML: Element '{elementName}' not found for command invocation!");
            return;
        }

        // Find matching event handlers
        var matchingHandlers = element.EventHandlers.FindAll(h => 
            h.eventType.Equals(eventType, System.StringComparison.OrdinalIgnoreCase));

        if (matchingHandlers.Count == 0)
        {
            Debug.LogWarning($"UGUIML: No '{eventType}' event handlers found for element '{elementName}'");
            return;
        }

        // Execute all matching handlers
        foreach (var handler in matchingHandlers)
        {
            // Use reflection to call the private ExecuteEventHandler method
            var executeMethod = typeof(UGUIMLElement).GetMethod("ExecuteEventHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (executeMethod != null)
            {
                executeMethod.Invoke(element, new object[] { handler, parameters });
            }
        }

        Debug.Log($"UGUIML: Invoked '{eventType}' command on element '{elementName}'");
    }

    /// <summary>
    /// Built-in command methods that can be called from XML without external scripts
    /// </summary>
    #region Built-in Commands

    public void ShowElement(string elementName)
    {
        PlayAnimationOnElement(elementName, "show", "2");
    }

    public void HideElement(string elementName)
    {
        PlayAnimationOnElement(elementName, "hide", "2");
    }

    public void ToggleElement(string elementName)
    {
        var element = GetUGUIMLElement(elementName);
        if (element != null)
        {
            if (element.GetAlpha() > 0.5f)
            {
                HideElement(elementName);
            }
            else
            {
                ShowElement(elementName);
            }
        }
    }

    public void FadeInElement(string elementName)
    {
        PlayAnimationOnElement(elementName, "fadein", "2");
    }

    public void FadeOutElement(string elementName)
    {
        PlayAnimationOnElement(elementName, "fadeout", "2");
    }

    public void BounceElement(string elementName)
    {
        PlayAnimationOnElement(elementName, "bounce", "2");
    }

    public void MoveElementTo(string elementName, string x, string y)
    {
        PlayAnimationOnElement(elementName, "moveto", "2", x, y);
    }

    public void ScaleElement(string elementName, string scale)
    {
        PlayAnimationOnElement(elementName, "scale", "2", scale);
    }

    public void SlideElementOffScreen(string elementName, string direction)
    {
        PlayAnimationOnElement(elementName, "slideoffscreen", "2", direction);
    }

    public void SetElementText(string elementName, string newText)
    {
        if (textElements.TryGetValue(elementName, out TMP_Text textComponent))
        {
            textComponent.text = newText;
            Debug.Log($"UGUIML: Set text of '{elementName}' to '{newText}'");
        }
        else
        {
            Debug.LogWarning($"UGUIML: Text element '{elementName}' not found!");
        }
    }

    public void SetElementAlpha(string elementName, string alpha)
    {
        if (float.TryParse(alpha, out float alphaValue))
        {
            var element = GetUGUIMLElement(elementName);
            if (element != null)
            {
                element.SetAlpha(alphaValue);
                Debug.Log($"UGUIML: Set alpha of '{elementName}' to {alphaValue}");
            }
        }
    }

    public void SetElementInteractable(string elementName, string interactable)
    {
        if (bool.TryParse(interactable, out bool isInteractable))
        {
            var element = GetUGUIMLElement(elementName);
            if (element != null)
            {
                element.SetInteractable(isInteractable);
                Debug.Log($"UGUIML: Set interactable of '{elementName}' to {isInteractable}");
            }
        }
    }

    public void RestoreElementToOriginalPosition(string elementName)
    {
        PlayAnimationOnElement(elementName, "movetooriginal", "2");
    }

    public void RestoreElementToOriginalState(string elementName)
    {
        var element = GetUGUIMLElement(elementName);
        if (element != null)
        {
            element.SetAlpha(1.0f);
            element.AnimateToOriginalPosition(2f);
            element.AnimateScale(Vector3.one, 2f);
            element.SetInteractable(true);
            Debug.Log($"UGUIML: Restored '{elementName}' to original state");
        }
    }

    #endregion

    #endregion

    #region Private Methods

    private void ParseNode(XmlNode node, Transform parent)
    {
        if (node == null)
        {
            Debug.LogError("UGUIML ParseNode: node is null!");
            return;
        }

        if (parent == null)
        {
            Debug.LogError("UGUIML ParseNode: parent transform is null!");
            return;
        }

        foreach (XmlNode childNode in node.ChildNodes)
        {
            if (childNode.NodeType == XmlNodeType.Element)
            {
                CreateUIElement(childNode, parent);
            }
        }
    }

    private void CreateUIElement(XmlNode node, Transform parent)
    {
        try
        {
            string elementType = node.Name.ToLower();
            string elementName = GetAttribute(node, "name", "");

            if (string.IsNullOrEmpty(elementName))
            {
                Debug.LogWarning($"UGUIML: Element of type '{elementType}' has no name attribute!");
                return;
            }

            GameObject elementObject = new GameObject(elementName);
            elementObject.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rectTransform = elementObject.AddComponent<RectTransform>();
            
            // Add UGUIMLElement component
            UGUIMLElement uguimlElement = elementObject.AddComponent<UGUIMLElement>();
            uguimlElement.Initialize(this, node);

            // Configure RectTransform
            ConfigureRectTransform(rectTransform, node);

            // Create specific UI component based on type
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
                    
                // Rich Components
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
                    
                // Layout Groups
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
                    Debug.LogWarning($"UGUIML: Unknown element type '{elementType}'");
                    break;
            }

            // Add to dictionaries
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
            ParseNode(node, parentForChildren);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIML CreateUIElement: Exception creating element - {e.Message}\nStack: {e.StackTrace}");
            throw;
        }
    }

    private void CreatePanel(GameObject panelObject, XmlNode node)
    {
        try
        {
            if (panelObject == null)
            {
                Debug.LogError("UGUIML CreatePanel: panelObject is null!");
                return;
            }

            if (node == null)
            {
                Debug.LogError("UGUIML CreatePanel: node is null!");
                return;
            }

            // Get or add CanvasGroup (required for panels)
            CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelObject.AddComponent<CanvasGroup>();
            }
            
            // Handle binding and default resources
            int bindId = GetIntAttribute(node, "bindId", -1);
            
            // Configure CanvasGroup properties
            float alpha = GetFloatAttribute(node, "alpha", 1f);
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = GetBoolAttribute(node, "interactable", alpha > 0f);
            canvasGroup.blocksRaycasts = GetBoolAttribute(node, "blocksRaycasts", alpha > 0f);

            // Add to panels dictionary
            string panelName = GetAttribute(node, "name", "");
            if (panels == null)
            {
                Debug.LogError("UGUIML CreatePanel: panels dictionary is null!");
                return;
            }
            panels[panelName] = canvasGroup;

            // Add to UGUIMLResources panels if it exists
            if (guiResources != null && guiResources.panels != null && !guiResources.panels.ContainsKey(panelName))
            {
                guiResources.panels[panelName] = canvasGroup;
            }

            // Add Image component for background
            string backgroundColor = GetAttribute(node, "backgroundColor", "");
            bool needsBackground = !string.IsNullOrEmpty(backgroundColor) || (bindId == -1 && guiResources != null && guiResources.DefaultPanelBackground != null);
            
            if (needsBackground)
            {
                Image backgroundImage = panelObject.AddComponent<Image>();
                
                // Apply default resources if bindId is -1
                if (bindId == -1 && guiResources != null)
                {
                    if (guiResources.DefaultPanelBackground != null)
                    {
                        backgroundImage.sprite = guiResources.DefaultPanelBackground;
                        backgroundImage.type = Image.Type.Sliced; // Use sliced for proper 9-slice scaling
                      //  Debug.Log($"UGUIML: Applied default panel background to '{GetAttribute(node, "name", "unknown")}'");
                    }
                    backgroundImage.color = guiResources.DefaultPanelColor;
                  //  Debug.Log($"UGUIML: Applied default panel color {guiResources.DefaultPanelColor} to '{GetAttribute(node, "name", "unknown")}'");
                }
                
                // Override with XML attribute if provided
                if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color color))
                {
                    backgroundImage.color = color;
                }
                
                // Configure raycast and maskable settings
                backgroundImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", false);
                backgroundImage.maskable = GetBoolAttribute(node, "maskable", false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIML CreatePanel: Exception - {e.Message}\nStack: {e.StackTrace}");
            throw;
        }
    }

    private void CreateNestedCanvas(GameObject canvasObject, XmlNode node)
    {
        try
        {
            if (canvasObject == null)
            {
                Debug.LogError("UGUIML CreateNestedCanvas: canvasObject is null!");
                return;
            }

            if (node == null)
            {
                Debug.LogError("UGUIML CreateNestedCanvas: node is null!");
                return;
            }

            // Add Canvas component
            Canvas nestedCanvas = canvasObject.AddComponent<Canvas>();
            
            // Configure canvas properties from XML
            string renderMode = GetAttribute(node, "renderMode", "overlay").ToLower();
            switch (renderMode)
            {
                case "overlay":
                    nestedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
                case "camera":
                    nestedCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    // Set camera if specified
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
                    Debug.LogWarning($"UGUIML: Unknown render mode '{renderMode}', defaulting to overlay");
                    break;
            }

            // Set sorting order
            nestedCanvas.sortingOrder = GetIntAttribute(node, "sortingOrder", 0);
            
            // Set sorting layer
            string sortingLayer = GetAttribute(node, "sortingLayer", "");
            if (!string.IsNullOrEmpty(sortingLayer))
            {
                nestedCanvas.sortingLayerName = sortingLayer;
            }

            // Set pixel perfect
            nestedCanvas.pixelPerfect = GetBoolAttribute(node, "pixelPerfect", false);

            // Set plane distance for screen space camera mode
            if (nestedCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                nestedCanvas.planeDistance = GetFloatAttribute(node, "planeDistance", 100f);
            }

            // Add GraphicRaycaster for UI interaction
            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            
            // Configure raycaster
            raycaster.ignoreReversedGraphics = GetBoolAttribute(node, "ignoreReversedGraphics", true);
            raycaster.blockingObjects = GetBoolAttribute(node, "blockingObjects", false) ? 
                GraphicRaycaster.BlockingObjects.All : GraphicRaycaster.BlockingObjects.None;

            // Add CanvasScaler for responsive design
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            
            // Configure scaler
            string scaleMode = GetAttribute(node, "scaleMode", "scalewithscreensize").ToLower();
            switch (scaleMode)
            {
                case "constantpixelsize":
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = GetFloatAttribute(node, "scaleFactor", 1f);
                    break;
                case "scalewithscreensize":
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = ParseVector2(GetAttribute(node, "referenceResolution", "1920,1080"));
                    scaler.screenMatchMode = GetBoolAttribute(node, "matchWidth", true) ? 
                        CanvasScaler.ScreenMatchMode.MatchWidthOrHeight : CanvasScaler.ScreenMatchMode.Expand;
                    scaler.matchWidthOrHeight = GetFloatAttribute(node, "matchWidthOrHeight", 0f);
                    break;
                case "constantphysicalsize":
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
                    scaler.physicalUnit = CanvasScaler.Unit.Points;
                    scaler.fallbackScreenDPI = GetFloatAttribute(node, "fallbackScreenDPI", 96f);
                    scaler.defaultSpriteDPI = GetFloatAttribute(node, "defaultSpriteDPI", 96f);
                    break;
                default:
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    break;
            }

            // Add to dictionaries
            string canvasName = GetAttribute(node, "name", "");
            nestedCanvases[canvasName] = nestedCanvas;
            nestedRaycasters[canvasName] = raycaster;

          //  Debug.Log($"UGUIML: Created nested canvas '{canvasName}' with render mode {nestedCanvas.renderMode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIML CreateNestedCanvas: Exception - {e.Message}\nStack: {e.StackTrace}");
            throw;
        }
    }

    private void CreateText(GameObject textObject, XmlNode node)
    {
        TMP_Text textComponent = textObject.AddComponent<TextMeshProUGUI>();
        
        // Handle binding to UGUIMLResources TextResource first
        int bindId = GetIntAttribute(node, "bindId", -1);
        
        // Apply default resources if bindId is -1
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultFont != null)
            {
                textComponent.font = guiResources.DefaultFont;
              //  Debug.Log($"UGUIML: Applied default font to '{GetAttribute(node, "name", "unknown")}'");
            }
            textComponent.color = guiResources.DefaultTextColor;
            textComponent.fontSize = guiResources.DefaultFontSize;
           // Debug.Log($"UGUIML: Applied default text style (color: {guiResources.DefaultTextColor}, size: {guiResources.DefaultFontSize}) to '{GetAttribute(node, "name", "unknown")}'");
        }
        
        // Configure text properties (these can override defaults)
        textComponent.text = GetAttribute(node, "text", "");
        
        // Override defaults with XML attributes if provided
        float fontSize = GetFloatAttribute(node, "fontSize", textComponent.fontSize);
        textComponent.fontSize = fontSize;
        
        string colorHex = GetAttribute(node, "color", "");
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            textComponent.color = color;
        }

        string alignment = GetAttribute(node, "alignment", "center");
        textComponent.alignment = ParseTextAlignment(alignment);

        // Configure raycast and maskable settings
        textComponent.raycastTarget = GetBoolAttribute(node, "raycastTarget", false);
        textComponent.maskable = GetBoolAttribute(node, "maskable", false);

        // Add to text elements dictionary
        string textName = GetAttribute(node, "name", "");
        textElements[textName] = textComponent;

        // Handle binding to UGUIMLResources TextResource (only if bindId >= 0)
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
                Debug.LogWarning($"UGUIML: Text bind ID {bindId} is out of range. Resources count: {count}");
            }
        }
    }

    private void CreateButton(GameObject buttonObject, XmlNode node)
    {
        // Add Image component for button background
        Image buttonImage = buttonObject.AddComponent<Image>();
        
        // Add Button component
        Button button = buttonObject.AddComponent<Button>();
        
        // Note: UGUIMLButton component is now replaced by event system in UGUIMLElement
        // The button events are handled automatically by UGUIMLElement.SetupEventHandlers()
        
        // Handle binding and default resources
        int bindId = GetIntAttribute(node, "bindId", -1);
        
        // Apply default resources if bindId is -1
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultButtonBackground != null)
            {
                buttonImage.sprite = guiResources.DefaultButtonBackground;
                buttonImage.type = Image.Type.Sliced; // Use sliced for proper 9-slice scaling
               // Debug.Log($"UGUIML: Applied default button background to '{GetAttribute(node, "name", "unknown")}'");
            }
            buttonImage.color = guiResources.DefaultButtonColor;
            // Debug.Log($"UGUIML: Applied default button color {guiResources.DefaultButtonColor} to '{GetAttribute(node, "name", "unknown")}'");
        }
        
        // Configure button properties (can override defaults)
        string backgroundColor = GetAttribute(node, "backgroundColor", "");
        if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color bgColor))
        {
            buttonImage.color = bgColor;
        }

        // Configure button transitions for hover effects
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

        // Configure raycast and maskable settings - ALWAYS enable raycast for buttons
        buttonImage.raycastTarget = true; // Force true for all buttons to ensure they're clickable
        buttonImage.maskable = GetBoolAttribute(node, "maskable", false);
        
        // Set button as target graphic for proper interaction
        button.targetGraphic = buttonImage;

        // Note: Command handling is now done automatically by UGUIMLElement event system
        // The 'command' attribute (or onClick, etc.) will be parsed and handled in UGUIMLElement.SetupEventHandlers()

        // Add to buttons dictionary (now stores the Button component instead of UGUIMLButton)
        string buttonName = GetAttribute(node, "name", "");
        buttons[buttonName] = button;

        // Create text child for button label if specified
        string buttonText = GetAttribute(node, "text", "");
        if (!string.IsNullOrEmpty(buttonText))
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
            textComponent.fontSize = GetFloatAttribute(node, "fontSize", 14f);
            
            // Button text should NEVER be raycast target (button image handles interaction)
            textComponent.raycastTarget = false;
            textComponent.maskable = GetBoolAttribute(node, "maskable", false);
            
            string textColor = GetAttribute(node, "textColor", "#000000");
            if (ColorUtility.TryParseHtmlString(textColor, out Color color))
            {
                textComponent.color = color;
            }
        }
    }

    private void CreateImage(GameObject imageObject, XmlNode node)
    {
        RawImage rawImage = imageObject.AddComponent<RawImage>();
        
        // Handle binding and default resources
        int bindId = GetIntAttribute(node, "bindId", -1);
        
        // Apply default resources if bindId is -1
        if (bindId == -1 && guiResources != null)
        {
            if (guiResources.DefaultImageTexture != null)
            {
                rawImage.texture = guiResources.DefaultImageTexture;
               // Debug.Log($"UGUIML: Applied default image texture to '{GetAttribute(node, "name", "unknown")}'");
            }
        }
        
        // Configure image properties (can override defaults)
        string colorHex = GetAttribute(node, "color", "");
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            rawImage.color = color;
        }

        // Configure raycast and maskable settings
        rawImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", false);
        rawImage.maskable = GetBoolAttribute(node, "maskable", false);

        // Add to images dictionary
        string imageName = GetAttribute(node, "name", "");
        images[imageName] = rawImage;

        // Handle binding to UGUIMLResources ImageResource
        if (bindId >= 0 && guiResources != null)
        {
            // Note: Assuming UGUIMLResources will have imageResources similar to textResources
            // This would need to be added to UGUIMLResources or handled differently
            Debug.Log($"UGUIML: Image binding to ID {bindId} - binding system needs to be implemented in UGUIMLResources");
        }
    }

    #region Rich Component Creation Methods

    private void CreateScrollView(GameObject scrollObject, XmlNode node)
    {
        // Add ScrollRect component
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        
        // Add background image to scroll view
        Image backgroundImage = scrollObject.AddComponent<Image>();
        string backgroundColor = GetAttribute(node, "backgroundColor", "");
        if (!string.IsNullOrEmpty(backgroundColor) && ColorUtility.TryParseHtmlString(backgroundColor, out Color bgColor))
        {
            backgroundImage.color = bgColor;
        }
        else
        {
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Default dark background
        }
        backgroundImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", true);
        backgroundImage.maskable = GetBoolAttribute(node, "maskable", false);
        
        // Create viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        // Add Image component for mask (required for Mask component)
        Image maskImage = viewport.AddComponent<Image>();
        maskImage.color = Color.white; // Use white color for proper masking
        maskImage.raycastTarget = false;
        maskImage.maskable = false;
        
        // Add Mask component to viewport
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false; // Hide the mask graphic but keep it functional
        
        // Create content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        
        bool isHorizontal = GetBoolAttribute(node, "horizontal", false);
        bool isVertical = GetBoolAttribute(node, "vertical", true);
        
        if (isVertical && !isHorizontal)
        {
            // Vertical scrolling setup
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300); // Default content height
        }
        else if (isHorizontal && !isVertical)
        {
            // Horizontal scrolling setup
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.sizeDelta = new Vector2(300, 0); // Default content width
        }
        else
        {
            // Both directions or default
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(300, 300); // Default content size
        }
        
        contentRect.anchoredPosition = Vector2.zero;
        
        // Add ContentSizeFitter to content for auto-sizing
        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        if (isVertical)
        {
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        if (isHorizontal)
        {
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        // Add VerticalLayoutGroup for automatic child arrangement
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
        
        // Create scrollbars if needed
        bool showVerticalScrollbar = GetBoolAttribute(node, "showVerticalScrollbar", true);
        bool showHorizontalScrollbar = GetBoolAttribute(node, "showHorizontalScrollbar", false);
        
        if (showVerticalScrollbar && isVertical)
        {
            CreateScrollbar(scrollObject, true, scrollRect);
        }
        
        if (showHorizontalScrollbar && isHorizontal)
        {
            CreateScrollbar(scrollObject, false, scrollRect);
        }
        
        // Configure ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = isHorizontal;
        scrollRect.vertical = isVertical;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = GetFloatAttribute(node, "elasticity", 0.1f);
        scrollRect.inertia = GetBoolAttribute(node, "inertia", true);
        scrollRect.decelerationRate = GetFloatAttribute(node, "deceleration", 0.135f);
        scrollRect.scrollSensitivity = GetFloatAttribute(node, "scrollSensitivity", 1.0f);
        
        // Add to dictionary
        string scrollName = GetAttribute(node, "name", "");
        scrollViews[scrollName] = scrollRect;
        
        // Debug.Log($"UGUIML: Created ScrollView '{scrollName}' with content area");
    }

    private void CreateScrollbar(GameObject parent, bool isVertical, ScrollRect scrollRect)
    {
        string direction = isVertical ? "Vertical" : "Horizontal";
        GameObject scrollbar = new GameObject($"Scrollbar {direction}");
        scrollbar.transform.SetParent(parent.transform, false);
        
        RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
        if (isVertical)
        {
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.anchoredPosition = new Vector2(0, 0);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
        }
        else
        {
            scrollbarRect.anchorMin = new Vector2(0, 0);
            scrollbarRect.anchorMax = new Vector2(1, 0);
            scrollbarRect.anchoredPosition = new Vector2(0, 0);
            scrollbarRect.sizeDelta = new Vector2(0, 20);
        }
        
        Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = isVertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;
        
        // Create background
        Image backgroundImage = scrollbar.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultScrollbarBackground != null)
        {
            backgroundImage.sprite = guiResources.DefaultScrollbarBackground;
            backgroundImage.type = Image.Type.Sliced;
        }
        backgroundImage.color = guiResources != null ? guiResources.DefaultScrollbarBackgroundColor : new Color(0.1f, 0.1f, 0.1f, 0.8f);
        backgroundImage.raycastTarget = true;
        
        // Create sliding area
        GameObject slidingArea = new GameObject("Sliding Area");
        slidingArea.transform.SetParent(scrollbar.transform, false);
        RectTransform slidingAreaRect = slidingArea.AddComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.sizeDelta = Vector2.zero;
        slidingAreaRect.anchoredPosition = Vector2.zero;
        
        // Create handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
        
        Image handleImage = handle.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultScrollbarHandle != null)
        {
            handleImage.sprite = guiResources.DefaultScrollbarHandle;
            handleImage.type = Image.Type.Sliced;
        }
        handleImage.color = guiResources != null ? guiResources.DefaultScrollbarHandleColor : new Color(0.4f, 0.4f, 0.4f, 0.8f);
        handleImage.raycastTarget = true;
        
        // Configure scrollbar
        scrollbarComponent.targetGraphic = handleImage;
        scrollbarComponent.handleRect = handleRect;
        
        // Assign to ScrollRect
        if (isVertical)
        {
            scrollRect.verticalScrollbar = scrollbarComponent;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
        else
        {
            scrollRect.horizontalScrollbar = scrollbarComponent;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
    }

    private void CreateProgressBar(GameObject progressObject, XmlNode node)
    {
        // Add Slider component
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
        bgImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", false);
        bgImage.maskable = GetBoolAttribute(node, "maskable", false);
        
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
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "fillColor", ""), out Color fillColor) ? 
                         fillColor : (guiResources != null ? guiResources.DefaultProgressBarFillColor : new Color(0.2f, 0.6f, 0.9f));
        fillImage.raycastTarget = false;
        fillImage.maskable = false;
        
        // Create handle area and handle (optional based on showHandle attribute)
        bool showHandle = GetBoolAttribute(node, "showHandle", true);
        RectTransform handleRect = null;
        Image handleImage = null;
        
        if (showHandle)
        {
            GameObject handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.transform.SetParent(progressObject.transform, false);
            RectTransform handleSlideAreaRect = handleSlideArea.AddComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = Vector2.zero;
            handleSlideAreaRect.anchorMax = Vector2.one;
            handleSlideAreaRect.sizeDelta = Vector2.zero;
            handleSlideAreaRect.anchoredPosition = Vector2.zero;
            
            // Create handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleSlideArea.transform, false);
            handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.sizeDelta = new Vector2(20, 20);
            handleRect.anchoredPosition = Vector2.zero;
            
            handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            handleImage.raycastTarget = GetBoolAttribute(node, "interactable", false); // Only raycast if interactive
            handleImage.maskable = false;
        }
        
        // Configure slider
        slider.fillRect = fillRect;
        slider.handleRect = handleRect; // Can be null if showHandle is false
        slider.targetGraphic = handleImage; // Can be null if showHandle is false
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = GetFloatAttribute(node, "minValue", 0f);
        slider.maxValue = GetFloatAttribute(node, "maxValue", 1f);
        slider.value = GetFloatAttribute(node, "value", 0f);
        slider.interactable = GetBoolAttribute(node, "interactable", false); // Progress bars are usually non-interactive
        
        // Add to dictionary
        string progressName = GetAttribute(node, "name", "");
        progressBars[progressName] = slider;
        
       // Debug.Log($"UGUIML: Created ProgressBar '{progressName}'");
    }

    private void CreateToggleGroup(GameObject groupObject, XmlNode node)
    {
        // Add ToggleGroup component
        ToggleGroup toggleGroup = groupObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = GetBoolAttribute(node, "allowSwitchOff", false);
        
        // Add to dictionary
        string groupName = GetAttribute(node, "name", "");
        toggleGroups[groupName] = toggleGroup;
        
      //  Debug.Log($"UGUIML: Created ToggleGroup '{groupName}'");
    }

    private void CreateToggle(GameObject toggleObject, XmlNode node)
    {
        // Add Toggle component
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
        bgImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", true);
        bgImage.maskable = GetBoolAttribute(node, "maskable", false);
        
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
        checkImage.raycastTarget = false;
        checkImage.maskable = false;
        
        // Create label if text is provided
        string labelText = GetAttribute(node, "text", "");
        if (!string.IsNullOrEmpty(labelText))
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(toggleObject.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(40, 0);
            labelRect.offsetMax = new Vector2(0, 0);
            
            TMP_Text labelComponent = label.AddComponent<TextMeshProUGUI>();
            labelComponent.text = labelText;
            labelComponent.fontSize = GetFloatAttribute(node, "fontSize", 14f);
            labelComponent.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "textColor", "#FFFFFF"), out Color textColor) ? textColor : Color.white;
            labelComponent.alignment = TextAlignmentOptions.MidlineLeft;
            labelComponent.raycastTarget = false;
            labelComponent.maskable = false;
        }
        
        // Configure toggle
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = GetBoolAttribute(node, "isOn", false);
        
        // Assign to toggle group if specified
        string groupName = GetAttribute(node, "group", "");
        if (!string.IsNullOrEmpty(groupName) && toggleGroups.ContainsKey(groupName))
        {
            toggle.group = toggleGroups[groupName];
        }
        
        // Add to dictionary
        string toggleName = GetAttribute(node, "name", "");
        toggles[toggleName] = toggle;
        
       // Debug.Log($"UGUIML: Created Toggle '{toggleName}'");
    }

    private void CreateInputField(GameObject inputObject, XmlNode node)
    {
        // Add TMP_InputField component
        TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
        
        // Create background
        Image backgroundImage = inputObject.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultInputFieldBackground != null)
        {
            backgroundImage.sprite = guiResources.DefaultInputFieldBackground;
            backgroundImage.type = Image.Type.Sliced;
        }
        backgroundImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "backgroundColor", ""), out Color bgColor) ? 
                               bgColor : (guiResources != null ? guiResources.DefaultInputFieldColor : new Color(0.17f, 0.24f, 0.31f));
        backgroundImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", true);
        backgroundImage.maskable = GetBoolAttribute(node, "maskable", false);
        
        // Create text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObject.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);
        
        // Add mask to text area
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
        textComponent.fontSize = GetFloatAttribute(node, "fontSize", 14f);
        textComponent.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "textColor", "#FFFFFF"), out Color textColor) ? textColor : Color.white;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        textComponent.raycastTarget = false;
        textComponent.maskable = true;
        
        // Create placeholder if specified
        string placeholderText = GetAttribute(node, "placeholder", "");
        if (!string.IsNullOrEmpty(placeholderText))
        {
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;
            
            TMP_Text placeholderComponent = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholderText;
            placeholderComponent.fontSize = GetFloatAttribute(node, "fontSize", 14f);
            placeholderComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0.5f);
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderComponent.raycastTarget = false;
            placeholderComponent.maskable = true;
            
            inputField.placeholder = placeholderComponent;
        }
        
        // Configure input field
        inputField.targetGraphic = backgroundImage;
        inputField.textComponent = textComponent;
        inputField.text = GetAttribute(node, "text", "");
        inputField.characterLimit = GetIntAttribute(node, "characterLimit", 0);
        
        string contentType = GetAttribute(node, "contentType", "standard");
        inputField.contentType = contentType.ToLower() switch
        {
            "integer" => TMP_InputField.ContentType.IntegerNumber,
            "decimal" => TMP_InputField.ContentType.DecimalNumber,
            "alphanumeric" => TMP_InputField.ContentType.Alphanumeric,
            "name" => TMP_InputField.ContentType.Name,
            "email" => TMP_InputField.ContentType.EmailAddress,
            "password" => TMP_InputField.ContentType.Password,
            "pin" => TMP_InputField.ContentType.Pin,
            _ => TMP_InputField.ContentType.Standard
        };
        
        // Add to dictionary
        string inputName = GetAttribute(node, "name", "");
        inputFields[inputName] = inputField;
        
       // Debug.Log($"UGUIML: Created InputField '{inputName}'");
    }

    private void CreateDropdown(GameObject dropdownObject, XmlNode node)
    {
        // Add TMP_Dropdown component
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        
        // Create background
        Image backgroundImage = dropdownObject.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultDropdownBackground != null)
        {
            backgroundImage.sprite = guiResources.DefaultDropdownBackground;
            backgroundImage.type = Image.Type.Sliced;
        }
        backgroundImage.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "backgroundColor", ""), out Color bgColor) ? 
                               bgColor : (guiResources != null ? guiResources.DefaultDropdownColor : new Color(0.17f, 0.24f, 0.31f));
        backgroundImage.raycastTarget = GetBoolAttribute(node, "raycastTarget", true);
        backgroundImage.maskable = GetBoolAttribute(node, "maskable", false);
        
        // Create label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(dropdownObject.transform, false);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);
        
        TMP_Text labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = GetFloatAttribute(node, "fontSize", 14f);
        labelText.color = ColorUtility.TryParseHtmlString(GetAttribute(node, "textColor", "#FFFFFF"), out Color textColor) ? textColor : Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.raycastTarget = false;
        labelText.maskable = false;
        
        // Create arrow
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdownObject.transform, false);
        RectTransform arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        
        Image arrowImage = arrow.AddComponent<Image>();
        if (guiResources != null && guiResources.DefaultDropdownArrow != null)
        {
            arrowImage.sprite = guiResources.DefaultDropdownArrow;
            arrowImage.type = Image.Type.Simple;
        }
        arrowImage.color = labelText.color;
        arrowImage.raycastTarget = false;
        arrowImage.maskable = false;
        
        // Create template (required for TMP_Dropdown to work)
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdownObject.transform, false);
        template.SetActive(false);
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);
        
        // Add ScrollRect to template for scrollable dropdown
        ScrollRect templateScrollRect = template.AddComponent<ScrollRect>();
        
        // Add template background
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        templateImage.raycastTarget = true;
        
        // Create viewport for template
        GameObject templateViewport = new GameObject("Viewport");
        templateViewport.transform.SetParent(template.transform, false);
        RectTransform templateViewportRect = templateViewport.AddComponent<RectTransform>();
        templateViewportRect.anchorMin = Vector2.zero;
        templateViewportRect.anchorMax = Vector2.one;
        templateViewportRect.sizeDelta = Vector2.zero;
        templateViewportRect.anchoredPosition = Vector2.zero;
        
        // Add mask to template viewport
        Image templateMaskImage = templateViewport.AddComponent<Image>();
        templateMaskImage.color = Color.clear;
        templateMaskImage.raycastTarget = false;
        
        Mask templateMask = templateViewport.AddComponent<Mask>();
        templateMask.showMaskGraphic = false;
        
        // Create content for template
        GameObject templateContent = new GameObject("Content");
        templateContent.transform.SetParent(templateViewport.transform, false);
        RectTransform templateContentRect = templateContent.AddComponent<RectTransform>();
        templateContentRect.anchorMin = new Vector2(0, 1);
        templateContentRect.anchorMax = new Vector2(1, 1);
        templateContentRect.pivot = new Vector2(0.5f, 1);
        templateContentRect.sizeDelta = new Vector2(0, 28);
        templateContentRect.anchoredPosition = Vector2.zero;
        
        // Add VerticalLayoutGroup for automatic item arrangement
        VerticalLayoutGroup templateLayoutGroup = templateContent.AddComponent<VerticalLayoutGroup>();
        templateLayoutGroup.spacing = 1f;
        templateLayoutGroup.childControlWidth = true;
        templateLayoutGroup.childControlHeight = true;
        templateLayoutGroup.childForceExpandWidth = true;
        templateLayoutGroup.childForceExpandHeight = false;
        
        // Add ContentSizeFitter to content
        ContentSizeFitter templateSizeFitter = templateContent.AddComponent<ContentSizeFitter>();
        templateSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Create item template (THIS IS CRUCIAL - TMP_Dropdown requires this structure)
        GameObject item = new GameObject("Item");
        item.transform.SetParent(templateContent.transform, false);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.anchorMin = Vector2.zero;
        itemRect.anchorMax = new Vector2(1, 1);
        itemRect.sizeDelta = Vector2.zero;
        itemRect.anchoredPosition = Vector2.zero;
        
        // Add Toggle component (REQUIRED by TMP_Dropdown)
        Toggle itemToggle = item.AddComponent<Toggle>();
        
        // Add item background
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        itemBackground.raycastTarget = true;
        
        // Create item checkmark
        GameObject itemCheckmark = new GameObject("Item Checkmark");
        itemCheckmark.transform.SetParent(item.transform, false);
        RectTransform checkmarkRect = itemCheckmark.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(20, 20);
        checkmarkRect.anchoredPosition = new Vector2(10, 0);
        
        Image checkmarkImage = itemCheckmark.AddComponent<Image>();
        checkmarkImage.color = new Color(0.2f, 0.6f, 0.9f);
        checkmarkImage.raycastTarget = false;
        
        // Create item label
        GameObject itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(item.transform, false);
        RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(20, 1);
        itemLabelRect.offsetMax = new Vector2(-10, -2);
        
        TMP_Text itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
        itemLabelText.text = "Option A";
        itemLabelText.fontSize = GetFloatAttribute(node, "fontSize", 14f);
        itemLabelText.color = Color.white;
        itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabelText.raycastTarget = false;
        
        // Configure the toggle
        itemToggle.targetGraphic = itemBackground;
        itemToggle.graphic = checkmarkImage;
        itemToggle.isOn = false;
        
        // Configure template ScrollRect
        templateScrollRect.content = templateContentRect;
        templateScrollRect.viewport = templateViewportRect;
        templateScrollRect.horizontal = false;
        templateScrollRect.vertical = true;
        templateScrollRect.movementType = ScrollRect.MovementType.Clamped;
        templateScrollRect.scrollSensitivity = 1.0f;
        
        // Configure dropdown
        dropdown.targetGraphic = backgroundImage;
        dropdown.captionText = labelText;
        dropdown.template = templateRect;
        dropdown.itemText = itemLabelText;
        
        // Add default options if provided
        string optionsStr = GetAttribute(node, "options", "");
        if (!string.IsNullOrEmpty(optionsStr))
        {
            string[] optionArray = optionsStr.Split(',');
            dropdown.options.Clear();
            foreach (string option in optionArray)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option.Trim()));
            }
            Debug.Log($"UGUIML: Added {dropdown.options.Count} options to dropdown: {string.Join(", ", optionArray)}");
        }
        else
        {
            Debug.LogWarning("UGUIML: No options specified for dropdown!");
        }
        
        dropdown.value = GetIntAttribute(node, "value", 0);
        dropdown.RefreshShownValue();
        
        // Force refresh the dropdown display
        dropdown.captionText.text = dropdown.options.Count > 0 ? dropdown.options[dropdown.value].text : "No Options";
        
        // Add to dictionary
        string dropdownName = GetAttribute(node, "name", "");
        dropdowns[dropdownName] = dropdown;
        
      //  Debug.Log($"UGUIML: Created Dropdown '{dropdownName}'");
    }

    #endregion

    #region Layout Group Creation Methods

    private void CreateHorizontalLayout(GameObject layoutObject, XmlNode node)
    {
        HorizontalLayoutGroup layout = layoutObject.AddComponent<HorizontalLayoutGroup>();
        
        // Configure layout properties
        layout.spacing = GetFloatAttribute(node, "spacing", 0f);
        layout.childAlignment = ParseTextAnchor(GetAttribute(node, "childAlignment", "MiddleCenter"));
        layout.reverseArrangement = GetBoolAttribute(node, "reverse", false);
        layout.childControlWidth = GetBoolAttribute(node, "controlChildWidth", true);
        layout.childControlHeight = GetBoolAttribute(node, "controlChildHeight", true);
        layout.childScaleWidth = GetBoolAttribute(node, "scaleChildWidth", false);
        layout.childScaleHeight = GetBoolAttribute(node, "scaleChildHeight", false);
        layout.childForceExpandWidth = GetBoolAttribute(node, "forceExpandWidth", true);
        layout.childForceExpandHeight = GetBoolAttribute(node, "forceExpandHeight", false);
        
        // Configure padding
        Vector4 padding = ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        // Add ContentSizeFitter if requested
        bool autoSize = GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        // Add to dictionary
        string layoutName = GetAttribute(node, "name", "");
        horizontalLayouts[layoutName] = layout;
        
       // Debug.Log($"UGUIML: Created HorizontalLayout '{layoutName}'");
    }

    private void CreateVerticalLayout(GameObject layoutObject, XmlNode node)
    {
        VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
        
        // Configure layout properties
        layout.spacing = GetFloatAttribute(node, "spacing", 0f);
        layout.childAlignment = ParseTextAnchor(GetAttribute(node, "childAlignment", "UpperCenter"));
        layout.reverseArrangement = GetBoolAttribute(node, "reverse", false);
        layout.childControlWidth = GetBoolAttribute(node, "controlChildWidth", true);
        layout.childControlHeight = GetBoolAttribute(node, "controlChildHeight", true);
        layout.childScaleWidth = GetBoolAttribute(node, "scaleChildWidth", false);
        layout.childScaleHeight = GetBoolAttribute(node, "scaleChildHeight", false);
        layout.childForceExpandWidth = GetBoolAttribute(node, "forceExpandWidth", false);
        layout.childForceExpandHeight = GetBoolAttribute(node, "forceExpandHeight", true);
        
        // Configure padding
        Vector4 padding = ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        // Add ContentSizeFitter if requested
        bool autoSize = GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        // Add to dictionary
        string layoutName = GetAttribute(node, "name", "");
        verticalLayouts[layoutName] = layout;
        
       // Debug.Log($"UGUIML: Created VerticalLayout '{layoutName}'");
    }

    private void CreateGridLayout(GameObject layoutObject, XmlNode node)
    {
        GridLayoutGroup layout = layoutObject.AddComponent<GridLayoutGroup>();
        
        // Configure grid properties
        layout.cellSize = ParseVector2(GetAttribute(node, "cellSize", "100,100"));
        layout.spacing = ParseVector2(GetAttribute(node, "spacing", "0,0"));
        layout.startCorner = ParseGridCorner(GetAttribute(node, "startCorner", "UpperLeft"));
        layout.startAxis = ParseGridAxis(GetAttribute(node, "startAxis", "Horizontal"));
        layout.childAlignment = ParseTextAnchor(GetAttribute(node, "childAlignment", "UpperLeft"));
        layout.constraint = ParseGridConstraint(GetAttribute(node, "constraint", "FixedColumnCount"));
        layout.constraintCount = GetIntAttribute(node, "constraintCount", 1);
        
        // Configure padding
        Vector4 padding = ParseVector4(GetAttribute(node, "padding", "0,0,0,0"));
        layout.padding = new RectOffset((int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w);
        
        // Add ContentSizeFitter if requested
        bool autoSize = GetBoolAttribute(node, "autoSize", false);
        if (autoSize)
        {
            ContentSizeFitter fitter = layoutObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        // Add to dictionary
        string layoutName = GetAttribute(node, "name", "");
        gridLayouts[layoutName] = layout;
        
       // Debug.Log($"UGUIML: Created GridLayout '{layoutName}'");
    }

    #endregion

    private void ConfigureRectTransform(RectTransform rectTransform, XmlNode node)
    {
        // Position - support both 'position' and 'anchoredPosition' attributes
        string positionAttr = GetAttribute(node, "position", "");
        if (string.IsNullOrEmpty(positionAttr))
        {
            positionAttr = GetAttribute(node, "anchoredPosition", "0,0");
        }
        Vector2 position = ParseVector2(positionAttr);
        rectTransform.anchoredPosition = position;

        // Size - support both 'size' and 'width,height' attributes
        string sizeAttr = GetAttribute(node, "size", "");
        if (string.IsNullOrEmpty(sizeAttr))
        {
            // Try width/height attributes
            float width = GetFloatAttribute(node, "width", 100f);
            float height = GetFloatAttribute(node, "height", 100f);
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        else
        {
            Vector2 size = ParseVector2(sizeAttr);
            rectTransform.sizeDelta = size;
        }

        // Anchors
        Vector2 anchorMin = ParseVector2(GetAttribute(node, "anchorMin", "0.5,0.5"));
        Vector2 anchorMax = ParseVector2(GetAttribute(node, "anchorMax", "0.5,0.5"));
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // Pivot
        Vector2 pivot = ParseVector2(GetAttribute(node, "pivot", "0.5,0.5"));
        rectTransform.pivot = pivot;

        // Scale
        Vector3 scale = ParseVector3(GetAttribute(node, "scale", "1,1,1"));
        rectTransform.localScale = scale;
    }

    #endregion

    #region Utility Methods

    private string GetAttribute(XmlNode node, string attributeName, string defaultValue)
    {
        return node.Attributes?[attributeName]?.Value ?? defaultValue;
    }

    private float GetFloatAttribute(XmlNode node, string attributeName, float defaultValue)
    {
        string value = GetAttribute(node, attributeName, defaultValue.ToString());
        return float.TryParse(value, out float result) ? result : defaultValue;
    }

    private int GetIntAttribute(XmlNode node, string attributeName, int defaultValue)
    {
        string value = GetAttribute(node, attributeName, defaultValue.ToString());
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private bool GetBoolAttribute(XmlNode node, string attributeName, bool defaultValue)
    {
        string value = GetAttribute(node, attributeName, defaultValue.ToString()).ToLower();
        return value == "true" || value == "1";
    }

    private Vector2 ParseVector2(string vectorString)
    {
        string[] parts = vectorString.Split(',');
        if (parts.Length >= 2 && 
            float.TryParse(parts[0], out float x) && 
            float.TryParse(parts[1], out float y))
        {
            return new Vector2(x, y);
        }
        return Vector2.zero;
    }

    private Vector3 ParseVector3(string vectorString)
    {
        string[] parts = vectorString.Split(',');
        if (parts.Length >= 3 && 
            float.TryParse(parts[0], out float x) && 
            float.TryParse(parts[1], out float y) && 
            float.TryParse(parts[2], out float z))
        {
            return new Vector3(x, y, z);
        }
        else if (parts.Length >= 2 && 
                 float.TryParse(parts[0], out x) && 
                 float.TryParse(parts[1], out y))
        {
            return new Vector3(x, y, 1f);
        }
        return Vector3.one;
    }

    private TextAlignmentOptions ParseTextAlignment(string alignment)
    {
        return alignment.ToLower() switch
        {
            "left" => TextAlignmentOptions.Left,
            "right" => TextAlignmentOptions.Right,
            "center" => TextAlignmentOptions.Center,
            "justified" => TextAlignmentOptions.Justified,
            "topleft" => TextAlignmentOptions.TopLeft,
            "topright" => TextAlignmentOptions.TopRight,
            "topcenter" => TextAlignmentOptions.Top,
            "bottomleft" => TextAlignmentOptions.BottomLeft,
            "bottomright" => TextAlignmentOptions.BottomRight,
            "bottomcenter" => TextAlignmentOptions.Bottom,
            _ => TextAlignmentOptions.Center
        };
    }

    private Vector4 ParseVector4(string vectorString)
    {
        try
        {
            string[] parts = vectorString.Split(',');
            if (parts.Length == 4)
            {
                return new Vector4(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGUIML: Error parsing Vector4 '{vectorString}' - {e.Message}");
        }
        return Vector4.zero;
    }

    private TextAnchor ParseTextAnchor(string anchor)
    {
        return anchor.ToLower() switch
        {
            "upperleft" => TextAnchor.UpperLeft,
            "uppercenter" => TextAnchor.UpperCenter,
            "upperright" => TextAnchor.UpperRight,
            "middleleft" => TextAnchor.MiddleLeft,
            "middlecenter" => TextAnchor.MiddleCenter,
            "middleright" => TextAnchor.MiddleRight,
            "lowerleft" => TextAnchor.LowerLeft,
            "lowercenter" => TextAnchor.LowerCenter,
            "lowerright" => TextAnchor.LowerRight,
            _ => TextAnchor.MiddleCenter
        };
    }

    private GridLayoutGroup.Corner ParseGridCorner(string corner)
    {
        return corner.ToLower() switch
        {
            "upperleft" => GridLayoutGroup.Corner.UpperLeft,
            "upperright" => GridLayoutGroup.Corner.UpperRight,
            "lowerleft" => GridLayoutGroup.Corner.LowerLeft,
            "lowerright" => GridLayoutGroup.Corner.LowerRight,
            _ => GridLayoutGroup.Corner.UpperLeft
        };
    }

    private GridLayoutGroup.Axis ParseGridAxis(string axis)
    {
        return axis.ToLower() switch
        {
            "horizontal" => GridLayoutGroup.Axis.Horizontal,
            "vertical" => GridLayoutGroup.Axis.Vertical,
            _ => GridLayoutGroup.Axis.Horizontal
        };
    }

    private GridLayoutGroup.Constraint ParseGridConstraint(string constraint)
    {
        return constraint.ToLower() switch
        {
            "flexible" => GridLayoutGroup.Constraint.Flexible,
            "fixedcolumncount" => GridLayoutGroup.Constraint.FixedColumnCount,
            "fixedrowcount" => GridLayoutGroup.Constraint.FixedRowCount,
            _ => GridLayoutGroup.Constraint.FixedColumnCount
        };
    }

    #endregion
} 