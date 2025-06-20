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
    #region Cached Reflection Methods - Performance Optimization
    // Cached reflection method to avoid repeated GetMethod calls
    private static System.Reflection.MethodInfo _cachedExecuteEventHandlerMethod;
    private static System.Reflection.MethodInfo CachedExecuteEventHandlerMethod
    {
        get
        {
            if (_cachedExecuteEventHandlerMethod == null)
            {
                _cachedExecuteEventHandlerMethod = typeof(UGUIMLElement).GetMethod("ExecuteEventHandler", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            return _cachedExecuteEventHandlerMethod;
        }
    }
    #endregion

    [Header("XML Configuration")]
    [SerializeField] private TextAsset xmlFile;
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool clearCanvasOnLoad = true;

    [Header("Runtime References")]
    private Canvas targetCanvas;
    private UGUIMLResources guiResources;
    private UGUIMLElementFactory elementFactory;

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

        // Initialize element factory
        elementFactory = GetComponent<UGUIMLElementFactory>();
        if (elementFactory == null)
        {
            elementFactory = gameObject.AddComponent<UGUIMLElementFactory>();
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

            // Initialize factory with all dependencies
            if (elementFactory != null)
            {
                elementFactory.Initialize(this, guiResources, buttons, panels, textElements, images, allElements, scrollViews, progressBars, toggleGroups, toggles, inputFields, dropdowns, horizontalLayouts, verticalLayouts, gridLayouts, nestedCanvases, nestedRaycasters);
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

        // Execute all matching handlers using cached reflection method
        foreach (var handler in matchingHandlers)
        {
            if (CachedExecuteEventHandlerMethod != null)
            {
                CachedExecuteEventHandlerMethod.Invoke(element, new object[] { handler, parameters });
            }
        }

        // Debug.Log($"UGUIML: Invoked '{eventType}' command on element '{elementName}'");
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
                elementFactory.CreateUIElement(childNode, parent);
            }
        }
    }

    #endregion
} 