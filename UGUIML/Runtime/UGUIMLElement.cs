using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Base component for all UGUIML-created UI elements that handles animations, bindings, and events
/// </summary>
public class UGUIMLElement : MonoBehaviour
{
    [Header("Element Data")]
    [SerializeField] private string elementName;
    [SerializeField] private string elementType;
    [SerializeField] private int bindingId = -1;
    [SerializeField] private GUIResource currentResource;

    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Event Settings")]
    [SerializeField] private bool logEventExecution = true;
    [SerializeField] private List<UIEventHandler> eventHandlers = new List<UIEventHandler>();

    private UGUIML parentUGUIML;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private XmlNode sourceNode;
    
    // Animation coroutines
    private Coroutine positionAnimation;
    private Coroutine scaleAnimation;
    private Coroutine fadeAnimation;

    // Original values for off-screen animations
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private float originalAlpha;

    // Event system components
    private Dictionary<string, Component> eventSources = new Dictionary<string, Component>();

    public string ElementName => elementName;
    public string ElementType => elementType;
    public int BindingId => bindingId;
    public RectTransform RectTransform => rectTransform;
    public CanvasGroup CanvasGroup => canvasGroup;
    public List<UIEventHandler> EventHandlers => eventHandlers;

    #region Initialization

    public void Initialize(UGUIML uguiml, XmlNode node)
    {
        if (uguiml == null)
        {
            Debug.LogError("UGUIMLElement: UGUIML parent reference is null!");
            return;
        }

        if (node == null)
        {
            Debug.LogError("UGUIMLElement: XML node is null!");
            return;
        }

        parentUGUIML = uguiml;
        sourceNode = node;
        rectTransform = GetComponent<RectTransform>();
        
        // Get or add CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Parse element data from XML
        elementName = GetAttribute("name", "");
        elementType = node.Name.ToLower();
        bindingId = GetIntAttribute("bindId", -1);
        animationSpeed = GetFloatAttribute("animationSpeed", 1f);

        // Store original values
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        originalAlpha = canvasGroup.alpha;

        // Setup events after the element is fully initialized
        StartCoroutine(SetupEventsNextFrame());
    }

    private IEnumerator SetupEventsNextFrame()
    {
        yield return null; // Wait one frame for all components to be added
        SetupEventHandlers();
    }

    #endregion

    #region Event System

    /// <summary>
    /// Setup event handlers based on XML attributes and component types
    /// </summary>
    private void SetupEventHandlers()
    {
        if (sourceNode == null) return;

        // Clear existing handlers
        eventHandlers.Clear();
        eventSources.Clear();

        // Cache UI components for event binding
        CacheEventSources();

        // Parse event attributes from XML
        ParseEventAttributes();

        // Bind events to their respective components
        BindEventHandlers();
    }

    /// <summary>
    /// Cache all UI components that can trigger events
    /// </summary>
    private void CacheEventSources()
    {
        // Button events
        var button = GetComponent<Button>();
        if (button != null) eventSources["button"] = button;

        // Toggle events  
        var toggle = GetComponent<Toggle>();
        if (toggle != null) eventSources["toggle"] = toggle;

        // Slider events
        var slider = GetComponent<Slider>();
        if (slider != null) eventSources["slider"] = slider;

        // InputField events
        var inputField = GetComponent<TMP_InputField>();
        if (inputField != null) eventSources["inputfield"] = inputField;

        // Dropdown events
        var dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown != null) eventSources["dropdown"] = dropdown;

        // Scrollbar events
        var scrollbar = GetComponent<Scrollbar>();
        if (scrollbar != null) eventSources["scrollbar"] = scrollbar;

        // ScrollRect events
        var scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null) eventSources["scrollrect"] = scrollRect;
    }

    /// <summary>
    /// Parse event-related attributes from XML
    /// </summary>
    private void ParseEventAttributes()
    {
        if (sourceNode?.Attributes == null) return;

        foreach (XmlAttribute attribute in sourceNode.Attributes)
        {
            string attrName = attribute.Name.ToLower();
            string attrValue = attribute.Value;

            // Parse event attributes (e.g., onClick, onValueChanged, onEndEdit, etc.)
            if (attrName.StartsWith("on"))
            {
                string eventType = attrName.Substring(2); // Remove "on" prefix
                ParseEventHandler(eventType, attrValue);
            }
            // Support for 'command' attribute (alternative to onClick)
            else if (attrName == "command")
            {
                ParseEventHandler("click", attrValue);
            }
        }
    }

    /// <summary>
    /// Parse a single event handler from attribute value
    /// </summary>
    private void ParseEventHandler(string eventType, string eventValue)
    {
        if (string.IsNullOrEmpty(eventValue)) return;

        var handler = new UIEventHandler
        {
            eventType = eventType.ToLower(),
            eventValue = eventValue
        };

        // Parse parameters if they exist (separated by |)
        string[] parts = eventValue.Split('|');
        handler.commandName = parts[0];
        
        if (parts.Length > 1)
        {
            handler.parameters = new string[parts.Length - 1];
            Array.Copy(parts, 1, handler.parameters, 0, parts.Length - 1);
        }

        eventHandlers.Add(handler);
    }

    /// <summary>
    /// Bind parsed event handlers to their respective UI components
    /// </summary>
    private void BindEventHandlers()
    {
        foreach (var handler in eventHandlers)
        {
            BindEventHandler(handler);
        }
    }

    /// <summary>
    /// Bind a single event handler to the appropriate component
    /// </summary>
    private void BindEventHandler(UIEventHandler handler)
    {
        try
        {
            switch (handler.eventType)
            {
                case "click":
                    BindButtonClick(handler);
                    break;
                case "valuechanged":
                    BindValueChanged(handler);
                    break;
                case "endedit":
                    BindInputFieldEndEdit(handler);
                    break;
                case "submit":
                    BindInputFieldSubmit(handler);
                    break;
                case "scroll":
                    BindScrollRectValueChanged(handler);
                    break;
                default:
                    Debug.LogWarning($"UGUIMLElement '{elementName}': Unknown event type '{handler.eventType}'");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UGUIMLElement '{elementName}': Failed to bind event '{handler.eventType}' - {e.Message}");
        }
    }

    private void BindButtonClick(UIEventHandler handler)
    {
        if (eventSources.TryGetValue("button", out Component component) && component is Button button)
        {
            button.onClick.AddListener(() => ExecuteEventHandler(handler));
        }
    }

    private void BindValueChanged(UIEventHandler handler)
    {
        // Try different value-changed events based on available components
        if (eventSources.TryGetValue("toggle", out Component toggleComp) && toggleComp is Toggle toggle)
        {
            toggle.onValueChanged.AddListener(value => ExecuteEventHandler(handler, value.ToString()));
        }
        else if (eventSources.TryGetValue("slider", out Component sliderComp) && sliderComp is Slider slider)
        {
            slider.onValueChanged.AddListener(value => ExecuteEventHandler(handler, value.ToString()));
        }
        else if (eventSources.TryGetValue("scrollbar", out Component scrollbarComp) && scrollbarComp is Scrollbar scrollbar)
        {
            scrollbar.onValueChanged.AddListener(value => ExecuteEventHandler(handler, value.ToString()));
        }
        else if (eventSources.TryGetValue("dropdown", out Component dropdownComp) && dropdownComp is TMP_Dropdown dropdown)
        {
            dropdown.onValueChanged.AddListener(value => ExecuteEventHandler(handler, value.ToString()));
        }
    }

    private void BindInputFieldEndEdit(UIEventHandler handler)
    {
        if (eventSources.TryGetValue("inputfield", out Component component) && component is TMP_InputField inputField)
        {
            inputField.onEndEdit.AddListener(text => ExecuteEventHandler(handler, text));
        }
    }

    private void BindInputFieldSubmit(UIEventHandler handler)
    {
        if (eventSources.TryGetValue("inputfield", out Component component) && component is TMP_InputField inputField)
        {
            inputField.onSubmit.AddListener(text => ExecuteEventHandler(handler, text));
        }
    }

    private void BindScrollRectValueChanged(UIEventHandler handler)
    {
        if (eventSources.TryGetValue("scrollrect", out Component component) && component is ScrollRect scrollRect)
        {
            scrollRect.onValueChanged.AddListener(value => ExecuteEventHandler(handler, value.x.ToString(), value.y.ToString()));
        }
    }

    /// <summary>
    /// Execute an event handler with optional runtime parameters
    /// </summary>
    private void ExecuteEventHandler(UIEventHandler handler, params string[] runtimeParams)
    {
        string paramStr = runtimeParams.Length > 0 ? $" with params: {string.Join(", ", runtimeParams)}" : "";
        Debug.Log($"UGUIMLElement '{elementName}': Executing {handler.eventType} event: {handler.commandName}{paramStr}");

        // Try Unity Event execution first
        if (TryExecuteWithUnityEvents(handler, runtimeParams))
        {
            Debug.Log($"UGUIMLElement '{elementName}': Successfully executed '{handler.commandName}' via UnityEvents");
            return;
        }

        // Try method invocation on UGUIML parent
        if (TryExecuteWithMethodInvocation(handler, runtimeParams))
        {
            Debug.Log($"UGUIMLElement '{elementName}': Successfully executed '{handler.commandName}' via method invocation");
            return;
        }

        Debug.LogWarning($"UGUIMLElement '{elementName}': No execution method found for command '{handler.commandName}'");
    }



    private bool TryExecuteWithUnityEvents(UIEventHandler handler, string[] runtimeParams)
    {
        // Look for UnityEvent fields in UGUIML component
        var uguimlType = parentUGUIML.GetType();
        var eventField = uguimlType.GetField(handler.commandName, BindingFlags.Public | BindingFlags.Instance);
        
        if (eventField != null && typeof(UnityEventBase).IsAssignableFrom(eventField.FieldType))
        {
            var unityEvent = eventField.GetValue(parentUGUIML) as UnityEventBase;
            if (unityEvent != null)
            {
                // Use reflection to invoke with parameters
                var invokeMethod = unityEvent.GetType().GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    // Convert string parameters to appropriate types
                    var paramTypes = invokeMethod.GetParameters();
                    var convertedParams = ConvertParameters(runtimeParams, paramTypes);
                    invokeMethod.Invoke(unityEvent, convertedParams);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryExecuteWithMethodInvocation(UIEventHandler handler, string[] runtimeParams)
    {
        var allParams = new List<string>();
        if (handler.parameters != null) allParams.AddRange(handler.parameters);
        allParams.AddRange(runtimeParams);

        Debug.Log($"UGUIMLElement '{elementName}': Trying to invoke method '{handler.commandName}' with {allParams.Count} parameters");

        // Try to invoke method directly on UGUIML component first
        Debug.Log($"UGUIMLElement '{elementName}': Trying UGUIML component ({parentUGUIML.GetType().Name})");
        if (TryInvokeMethodOnTarget(parentUGUIML, handler.commandName, allParams.ToArray()))
        {
            return true;
        }

        // If not found on UGUIML, try external command handler
        var externalHandler = parentUGUIML.GetExternalCommandHandler();
        if (externalHandler != null)
        {
            Debug.Log($"UGUIMLElement '{elementName}': Trying external handler ({externalHandler.GetType().Name})");
            if (TryInvokeMethodOnTarget(externalHandler, handler.commandName, allParams.ToArray()))
            {
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"UGUIMLElement '{elementName}': No external command handler found!");
        }

        return false;
    }

    private bool TryInvokeMethodOnTarget(object target, string methodName, string[] parameters)
    {
        if (target == null) return false;

        var targetType = target.GetType();
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var method in methods)
        {
            if (method.Name == methodName)
            {
                var paramTypes = method.GetParameters();
                if (paramTypes.Length == parameters.Length)
                {
                    try
                    {
                        var convertedParams = ConvertParameters(parameters, paramTypes);
                        method.Invoke(target, convertedParams);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"UGUIMLElement '{elementName}': Failed to invoke method '{methodName}' on {target.GetType().Name} - {e.Message}");
                    }
                }
            }
        }
        return false;
    }

    private object[] ConvertParameters(string[] stringParams, ParameterInfo[] paramTypes)
    {
        if (stringParams.Length != paramTypes.Length)
            return null;

        object[] converted = new object[stringParams.Length];
        for (int i = 0; i < stringParams.Length; i++)
        {
            try
            {
                converted[i] = Convert.ChangeType(stringParams[i], paramTypes[i].ParameterType);
            }
            catch
            {
                converted[i] = stringParams[i]; // Fallback to string
            }
        }
        return converted;
    }

    /// <summary>
    /// Add an event handler programmatically
    /// </summary>
    public void AddEventHandler(string eventType, string commandName, params string[] parameters)
    {
        var handler = new UIEventHandler
        {
            eventType = eventType.ToLower(),
            commandName = commandName,
            parameters = parameters,
            eventValue = parameters.Length > 0 ? $"{commandName}|{string.Join("|", parameters)}" : commandName
        };

        eventHandlers.Add(handler);
        BindEventHandler(handler);
    }

    /// <summary>
    /// Remove all event handlers of a specific type
    /// </summary>
    public void RemoveEventHandlers(string eventType)
    {
        eventHandlers.RemoveAll(h => h.eventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
        // Note: This doesn't unbind already bound events. For full cleanup, call SetupEventHandlers()
    }

    /// <summary>
    /// Clear all event handlers and unbind events
    /// </summary>
    public void ClearAllEventHandlers()
    {
        eventHandlers.Clear();
        
        // Unbind all events by removing all listeners
        foreach (var kvp in eventSources)
        {
            var component = kvp.Value;
            switch (component)
            {
                case Button button:
                    button.onClick.RemoveAllListeners();
                    break;
                case Toggle toggle:
                    toggle.onValueChanged.RemoveAllListeners();
                    break;
                case Slider slider:
                    slider.onValueChanged.RemoveAllListeners();
                    break;
                case TMP_InputField inputField:
                    inputField.onEndEdit.RemoveAllListeners();
                    inputField.onSubmit.RemoveAllListeners();
                    break;
                case TMP_Dropdown dropdown:
                    dropdown.onValueChanged.RemoveAllListeners();
                    break;
                case Scrollbar scrollbar:
                    scrollbar.onValueChanged.RemoveAllListeners();
                    break;
                case ScrollRect scrollRect:
                    scrollRect.onValueChanged.RemoveAllListeners();
                    break;
            }
        }
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Animate element to a position
    /// </summary>
    public void AnimateToPosition(Vector2 targetPosition, float? customSpeed = null)
    {
        if (positionAnimation != null)
        {
            StopCoroutine(positionAnimation);
        }
        positionAnimation = StartCoroutine(AnimatePositionCoroutine(targetPosition, customSpeed ?? animationSpeed));
    }

    /// <summary>
    /// Animate element off-screen in specified direction
    /// </summary>
    public void AnimateOffScreen(OffScreenDirection direction, float? customSpeed = null)
    {
        Vector2 targetPosition = CalculateOffScreenPosition(direction);
        AnimateToPosition(targetPosition, customSpeed);
    }

    /// <summary>
    /// Animate element back to original position
    /// </summary>
    public void AnimateToOriginalPosition(float? customSpeed = null)
    {
        AnimateToPosition(originalPosition, customSpeed);
    }

    /// <summary>
    /// Animate element scale
    /// </summary>
    public void AnimateScale(Vector3 targetScale, float? customSpeed = null)
    {
        if (scaleAnimation != null)
        {
            StopCoroutine(scaleAnimation);
        }
        scaleAnimation = StartCoroutine(AnimateScaleCoroutine(targetScale, customSpeed ?? animationSpeed));
    }

    /// <summary>
    /// Animate CanvasGroup alpha
    /// </summary>
    public void AnimateFade(float targetAlpha, float? customSpeed = null)
    {
        if (fadeAnimation != null)
        {
            StopCoroutine(fadeAnimation);
        }
        fadeAnimation = StartCoroutine(AnimateFadeCoroutine(targetAlpha, customSpeed ?? animationSpeed));
    }

    /// <summary>
    /// Fade in element
    /// </summary>
    public void FadeIn(float? customSpeed = null)
    {
        AnimateFade(originalAlpha, customSpeed);
    }

    /// <summary>
    /// Fade out element
    /// </summary>
    public void FadeOut(float? customSpeed = null)
    {
        AnimateFade(0f, customSpeed);
    }

    /// <summary>
    /// Set alpha directly without animation and manage interaction settings
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            bool originalInteractable = canvasGroup.interactable;
            bool originalBlocksRaycasts = canvasGroup.blocksRaycasts;
            canvasGroup.alpha = alpha;
            UpdateCanvasGroupInteraction(alpha, originalInteractable, originalBlocksRaycasts);
        }
    }

    /// <summary>
    /// Set CanvasGroup interactable state
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
        }
    }

    /// <summary>
    /// Set CanvasGroup blocks raycasts state
    /// </summary>
    public void SetBlocksRaycasts(bool blocksRaycasts)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = blocksRaycasts;
        }
    }

    /// <summary>
    /// Get current alpha value
    /// </summary>
    public float GetAlpha()
    {
        return canvasGroup != null ? canvasGroup.alpha : 1f;
    }

    /// <summary>
    /// Check if element is currently interactable
    /// </summary>
    public bool IsInteractable()
    {
        return canvasGroup != null ? canvasGroup.interactable : true;
    }

    /// <summary>
    /// Check if element blocks raycasts
    /// </summary>
    public bool BlocksRaycasts()
    {
        return canvasGroup != null ? canvasGroup.blocksRaycasts : true;
    }

    /// <summary>
    /// Stop all animations
    /// </summary>
    public void StopAllAnimations()
    {
        if (positionAnimation != null)
        {
            StopCoroutine(positionAnimation);
            positionAnimation = null;
        }
        if (scaleAnimation != null)
        {
            StopCoroutine(scaleAnimation);
            scaleAnimation = null;
        }
        if (fadeAnimation != null)
        {
            StopCoroutine(fadeAnimation);
            fadeAnimation = null;
        }
    }

    #endregion

    #region Animation Coroutines

    private IEnumerator AnimatePositionCoroutine(Vector2 targetPosition, float speed)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float duration = Vector2.Distance(startPosition, targetPosition) / (speed * 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = animationCurve.Evaluate(t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        positionAnimation = null;
    }

    private IEnumerator AnimateScaleCoroutine(Vector3 targetScale, float speed)
    {
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = animationCurve.Evaluate(t);
            
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        rectTransform.localScale = targetScale;
        scaleAnimation = null;
    }

    private IEnumerator AnimateFadeCoroutine(float targetAlpha, float speed)
    {
        float startAlpha = canvasGroup.alpha;
        bool startInteractable = canvasGroup.interactable;
        bool startBlocksRaycasts = canvasGroup.blocksRaycasts;
        
        float elapsed = 0f;
        float duration = Mathf.Abs(targetAlpha - startAlpha) / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = animationCurve.Evaluate(t);
            
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            canvasGroup.alpha = currentAlpha;
            
            // Auto-manage interactable and blocksRaycasts based on alpha
            UpdateCanvasGroupInteraction(currentAlpha, startInteractable, startBlocksRaycasts);
            
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        UpdateCanvasGroupInteraction(targetAlpha, startInteractable, startBlocksRaycasts);
        fadeAnimation = null;
    }
    
    /// <summary>
    /// Update CanvasGroup interaction settings based on alpha
    /// </summary>
    private void UpdateCanvasGroupInteraction(float alpha, bool originalInteractable, bool originalBlocksRaycasts)
    {
        if (alpha <= 0f)
        {
            // When fully transparent, disable interaction
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            // When visible, restore original interaction settings
            canvasGroup.interactable = originalInteractable;
            canvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }
    }

    #endregion

    #region Binding Methods

    public void SetBinding(int newBinding)
    {
        bindingId = newBinding;
        UGUIMLResources.Singleton.resources[bindingId].bindings.Add(this);
    }

    public void RemoveBinding()
    {
        // Check if we have a valid binding and UGUIMLResources exists
        if (bindingId < 0 || UGUIMLResources.Singleton == null) 
        {
            bindingId = -1;
            return;
        }

        // Check if resources array exists and bindingId is within bounds
        if (UGUIMLResources.Singleton.resources != null && 
            bindingId < UGUIMLResources.Singleton.resources.Count)
        {
            // Safely remove the binding
            var resource = UGUIMLResources.Singleton.resources[bindingId];
            if (resource?.bindings != null)
            {
                resource.bindings.Remove(this);
            }
        }
        
        bindingId = -1;
    }

    #endregion

    #region Utility Methods

    private Vector2 CalculateOffScreenPosition(OffScreenDirection direction)
    {
        Canvas canvas = parentUGUIML.TargetCanvas;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Get current position and element size
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 elementSize = rectTransform.sizeDelta;

        return direction switch
        {
            OffScreenDirection.Left => new Vector2(-canvasSize.x / 2 - elementSize.x, currentPos.y),
            OffScreenDirection.Right => new Vector2(canvasSize.x / 2 + elementSize.x, currentPos.y),
            OffScreenDirection.Up => new Vector2(currentPos.x, canvasSize.y / 2 + elementSize.y),
            OffScreenDirection.Down => new Vector2(currentPos.x, -canvasSize.y / 2 - elementSize.y),
            _ => currentPos
        };
    }

    private string GetAttribute(string attributeName, string defaultValue)
    {
        return sourceNode?.Attributes?[attributeName]?.Value ?? defaultValue;
    }

    private float GetFloatAttribute(string attributeName, float defaultValue)
    {
        string value = GetAttribute(attributeName, defaultValue.ToString());
        return float.TryParse(value, out float result) ? result : defaultValue;
    }

    private int GetIntAttribute(string attributeName, int defaultValue)
    {
        string value = GetAttribute(attributeName, defaultValue.ToString());
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        StopAllAnimations();
        RemoveBinding();
    }

    #endregion
}

/// <summary>
/// Directions for off-screen animations
/// </summary>
[System.Serializable]
public class UIEventHandler
{
    public string eventType;
    public string commandName;
    public string[] parameters;
    public string eventValue;
}

public enum OffScreenDirection
{
    Left,
    Right,
    Up,
    Down
} 