using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Static utility class for parsing XML attributes and values in UGUIML
/// Contains all parsing functions with performance optimizations and caching
/// </summary>
public static class UGUIMLParser
{
    #region Performance Optimization Caches
    
    /// <summary>
    /// Element type cache to avoid repeated ToLower() allocations
    /// </summary>
    public static readonly Dictionary<string, string> ElementTypeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"canvas", "canvas"}, {"panel", "panel"}, {"text", "text"}, {"button", "button"}, {"image", "image"},
        {"scrollview", "scrollview"}, {"progressbar", "progressbar"}, {"togglegroup", "togglegroup"},
        {"toggle", "toggle"}, {"inputfield", "inputfield"}, {"dropdown", "dropdown"},
        {"horizontallayout", "horizontallayout"}, {"verticallayout", "verticallayout"}, {"gridlayout", "gridlayout"}
    };

    /// <summary>
    /// Text alignment options cache for fast lookups
    /// </summary>
    public static readonly Dictionary<string, TextAlignmentOptions> TextAlignmentCache = new Dictionary<string, TextAlignmentOptions>(StringComparer.OrdinalIgnoreCase)
    {
        {"left", TextAlignmentOptions.Left}, {"right", TextAlignmentOptions.Right}, {"center", TextAlignmentOptions.Center},
        {"justified", TextAlignmentOptions.Justified}, {"topleft", TextAlignmentOptions.TopLeft},
        {"topright", TextAlignmentOptions.TopRight}, {"topcenter", TextAlignmentOptions.Top},
        {"bottomleft", TextAlignmentOptions.BottomLeft}, {"bottomright", TextAlignmentOptions.BottomRight},
        {"bottomcenter", TextAlignmentOptions.Bottom}
    };

    /// <summary>
    /// Input field content type cache
    /// </summary>
    public static readonly Dictionary<string, TMP_InputField.ContentType> InputContentTypeCache = new Dictionary<string, TMP_InputField.ContentType>(StringComparer.OrdinalIgnoreCase)
    {
        {"standard", TMP_InputField.ContentType.Standard}, {"integer", TMP_InputField.ContentType.IntegerNumber},
        {"decimal", TMP_InputField.ContentType.DecimalNumber}, {"alphanumeric", TMP_InputField.ContentType.Alphanumeric},
        {"name", TMP_InputField.ContentType.Name}, {"email", TMP_InputField.ContentType.EmailAddress},
        {"password", TMP_InputField.ContentType.Password}, {"pin", TMP_InputField.ContentType.Pin}
    };

    /// <summary>
    /// Text anchor cache for layout components
    /// </summary>
    public static readonly Dictionary<string, TextAnchor> TextAnchorCache = new Dictionary<string, TextAnchor>(StringComparer.OrdinalIgnoreCase)
    {
        {"upperleft", TextAnchor.UpperLeft}, {"uppercenter", TextAnchor.UpperCenter}, {"upperright", TextAnchor.UpperRight},
        {"middleleft", TextAnchor.MiddleLeft}, {"middlecenter", TextAnchor.MiddleCenter}, {"middleright", TextAnchor.MiddleRight},
        {"lowerleft", TextAnchor.LowerLeft}, {"lowercenter", TextAnchor.LowerCenter}, {"lowerright", TextAnchor.LowerRight}
    };

    /// <summary>
    /// Grid layout corner cache
    /// </summary>
    public static readonly Dictionary<string, GridLayoutGroup.Corner> GridCornerCache = new Dictionary<string, GridLayoutGroup.Corner>(StringComparer.OrdinalIgnoreCase)
    {
        {"upperleft", GridLayoutGroup.Corner.UpperLeft}, {"upperright", GridLayoutGroup.Corner.UpperRight},
        {"lowerleft", GridLayoutGroup.Corner.LowerLeft}, {"lowerright", GridLayoutGroup.Corner.LowerRight}
    };

    /// <summary>
    /// Grid layout axis cache
    /// </summary>
    public static readonly Dictionary<string, GridLayoutGroup.Axis> GridAxisCache = new Dictionary<string, GridLayoutGroup.Axis>(StringComparer.OrdinalIgnoreCase)
    {
        {"horizontal", GridLayoutGroup.Axis.Horizontal}, {"vertical", GridLayoutGroup.Axis.Vertical}
    };

    /// <summary>
    /// Grid layout constraint cache
    /// </summary>
    public static readonly Dictionary<string, GridLayoutGroup.Constraint> GridConstraintCache = new Dictionary<string, GridLayoutGroup.Constraint>(StringComparer.OrdinalIgnoreCase)
    {
        {"flexible", GridLayoutGroup.Constraint.Flexible}, 
        {"fixedcolumncount", GridLayoutGroup.Constraint.FixedColumnCount},
        {"fixedrowcount", GridLayoutGroup.Constraint.FixedRowCount}
    };

    /// <summary>
    /// Canvas render mode cache
    /// </summary>
    public static readonly Dictionary<string, RenderMode> RenderModeCache = new Dictionary<string, RenderMode>(StringComparer.OrdinalIgnoreCase)
    {
        {"overlay", RenderMode.ScreenSpaceOverlay}, {"camera", RenderMode.ScreenSpaceCamera}, {"world", RenderMode.WorldSpace}
    };

    /// <summary>
    /// Canvas scaler mode cache
    /// </summary>
    public static readonly Dictionary<string, CanvasScaler.ScaleMode> ScaleModeCache = new Dictionary<string, CanvasScaler.ScaleMode>(StringComparer.OrdinalIgnoreCase)
    {
        {"constantpixelsize", CanvasScaler.ScaleMode.ConstantPixelSize},
        {"scalewithscreensize", CanvasScaler.ScaleMode.ScaleWithScreenSize},
        {"constantphysicalsize", CanvasScaler.ScaleMode.ConstantPhysicalSize}
    };

    /// <summary>
    /// Slider direction cache
    /// </summary>
    public static readonly Dictionary<string, Slider.Direction> SliderDirectionCache = new Dictionary<string, Slider.Direction>(StringComparer.OrdinalIgnoreCase)
    {
        {"lefttoright", Slider.Direction.LeftToRight}, {"righttoleft", Slider.Direction.RightToLeft},
        {"bottomtotop", Slider.Direction.BottomToTop}, {"toptobottom", Slider.Direction.TopToBottom}
    };

    /// <summary>
    /// Scrollbar direction cache
    /// </summary>
    public static readonly Dictionary<string, Scrollbar.Direction> ScrollbarDirectionCache = new Dictionary<string, Scrollbar.Direction>(StringComparer.OrdinalIgnoreCase)
    {
        {"lefttoright", Scrollbar.Direction.LeftToRight}, {"righttoleft", Scrollbar.Direction.RightToLeft},
        {"bottomtotop", Scrollbar.Direction.BottomToTop}, {"toptobottom", Scrollbar.Direction.TopToBottom}
    };

    /// <summary>
    /// Color cache for common color values
    /// </summary>
    public static readonly Dictionary<string, Color> ColorCache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
    {
        {"white", Color.white}, {"black", Color.black}, {"red", Color.red}, {"green", Color.green}, 
        {"blue", Color.blue}, {"yellow", Color.yellow}, {"cyan", Color.cyan}, {"magenta", Color.magenta},
        {"gray", Color.gray}, {"grey", Color.grey}, {"clear", Color.clear},
        {"#FFFFFF", Color.white}, {"#000000", Color.black}, {"#FF0000", Color.red}, 
        {"#00FF00", Color.green}, {"#0000FF", Color.blue}, {"#FFFF00", Color.yellow}
    };

    /// <summary>
    /// Vector2 cache for commonly used vectors
    /// </summary>
    public static readonly Dictionary<string, Vector2> Vector2Cache = new Dictionary<string, Vector2>()
    {
        {"0,0", Vector2.zero}, {"1,1", Vector2.one}, {"0.5,0.5", new Vector2(0.5f, 0.5f)},
        {"1920,1080", new Vector2(1920, 1080)}, {"100,100", new Vector2(100, 100)},
        {"1,0", Vector2.right}, {"0,1", Vector2.up}
    };

    /// <summary>
    /// Vector3 cache for commonly used vectors
    /// </summary>
    public static readonly Dictionary<string, Vector3> Vector3Cache = new Dictionary<string, Vector3>()
    {
        {"0,0,0", Vector3.zero}, {"1,1,1", Vector3.one}, {"0.5,0.5,0.5", new Vector3(0.5f, 0.5f, 0.5f)},
        {"1,0,0", Vector3.right}, {"0,1,0", Vector3.up}, {"0,0,1", Vector3.forward}
    };

    /// <summary>
    /// Vector4 cache for commonly used vectors (padding, etc.)
    /// </summary>
    public static readonly Dictionary<string, Vector4> Vector4Cache = new Dictionary<string, Vector4>()
    {
        {"0,0,0,0", Vector4.zero}, {"1,1,1,1", Vector4.one}, 
        {"10,10,10,10", new Vector4(10, 10, 10, 10)}, {"5,5,5,5", new Vector4(5, 5, 5, 5)}
    };

    /// <summary>
    /// Boolean cache for common boolean strings
    /// </summary>
    public static readonly Dictionary<string, bool> BoolCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        {"true", true}, {"false", false}, {"1", true}, {"0", false}, {"yes", true}, {"no", false}, {"on", true}, {"off", false}
    };

    // Cache size limits to prevent unbounded growth
    private const int MAX_CACHE_SIZE = 100;

    #endregion

    #region Attribute Parsing Methods

    /// <summary>
    /// Get string attribute value with default fallback
    /// </summary>
    public static string GetAttribute(XmlNode node, string attributeName, string defaultValue = "")
    {
        return node?.Attributes?[attributeName]?.Value ?? defaultValue;
    }

    /// <summary>
    /// Get float attribute with parsing and default fallback
    /// </summary>
    public static float GetFloatAttribute(XmlNode node, string attributeName, float defaultValue = 0f)
    {
        string value = GetAttribute(node, attributeName, defaultValue.ToString());
        return float.TryParse(value, out float result) ? result : defaultValue;
    }

    /// <summary>
    /// Get int attribute with parsing and default fallback
    /// </summary>
    public static int GetIntAttribute(XmlNode node, string attributeName, int defaultValue = 0)
    {
        string value = GetAttribute(node, attributeName, defaultValue.ToString());
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    /// <summary>
    /// Get bool attribute with optimized parsing
    /// </summary>
    public static bool GetBoolAttribute(XmlNode node, string attributeName, bool defaultValue = false)
    {
        string value = GetAttribute(node, attributeName, "");
        if (string.IsNullOrEmpty(value)) return defaultValue;

        // Use cache for fast boolean lookup
        return BoolCache.TryGetValue(value, out bool result) ? result : defaultValue;
    }

    /// <summary>
    /// Get color attribute with parsing and caching
    /// </summary>
    public static Color GetColorAttribute(XmlNode node, string attributeName, Color defaultValue = default)
    {
        string value = GetAttribute(node, attributeName, "");
        if (string.IsNullOrEmpty(value)) return defaultValue;

        return ParseColor(value, defaultValue);
    }

    #endregion

    #region Vector Parsing Methods

    /// <summary>
    /// Parse Vector2 from comma-separated string with caching optimization
    /// </summary>
    public static Vector2 ParseVector2(string vectorString, Vector2 defaultValue = default)
    {
        if (string.IsNullOrEmpty(vectorString)) return defaultValue;

        // Check cache first
        if (Vector2Cache.TryGetValue(vectorString, out Vector2 cachedResult))
        {
            return cachedResult;
        }

        // Use optimized parsing with span to avoid string allocations
        ReadOnlySpan<char> span = vectorString.AsSpan();
        int commaIndex = span.IndexOf(',');

        if (commaIndex > 0 && commaIndex < span.Length - 1)
        {
            if (float.TryParse(span.Slice(0, commaIndex), out float x) &&
                float.TryParse(span.Slice(commaIndex + 1), out float y))
            {
                Vector2 result = new Vector2(x, y);

                // Cache result if cache isn't too large
                if (Vector2Cache.Count < MAX_CACHE_SIZE)
                {
                    Vector2Cache[vectorString] = result;
                }

                return result;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Parse Vector3 from comma-separated string with caching optimization
    /// </summary>
    public static Vector3 ParseVector3(string vectorString, Vector3 defaultValue = default)
    {
        if (string.IsNullOrEmpty(vectorString)) return defaultValue;

        // Check cache first
        if (Vector3Cache.TryGetValue(vectorString, out Vector3 cachedResult))
        {
            return cachedResult;
        }

        ReadOnlySpan<char> span = vectorString.AsSpan();
        int firstComma = span.IndexOf(',');

        if (firstComma > 0)
        {
            int secondComma = span.Slice(firstComma + 1).IndexOf(',');

            if (secondComma > 0) // Three components
            {
                secondComma += firstComma + 1; // Adjust for full string position

                if (float.TryParse(span.Slice(0, firstComma), out float x) &&
                    float.TryParse(span.Slice(firstComma + 1, secondComma - firstComma - 1), out float y) &&
                    float.TryParse(span.Slice(secondComma + 1), out float z))
                {
                    Vector3 result = new Vector3(x, y, z);

                    // Cache result if cache isn't too large
                    if (Vector3Cache.Count < MAX_CACHE_SIZE)
                    {
                        Vector3Cache[vectorString] = result;
                    }

                    return result;
                }
            }
            else // Two components, default z = 1
            {
                if (float.TryParse(span.Slice(0, firstComma), out float x) &&
                    float.TryParse(span.Slice(firstComma + 1), out float y))
                {
                    return new Vector3(x, y, 1f);
                }
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Parse Vector4 from comma-separated string with caching optimization
    /// </summary>
    public static Vector4 ParseVector4(string vectorString, Vector4 defaultValue = default)
    {
        if (string.IsNullOrEmpty(vectorString)) return defaultValue;

        // Check cache first
        if (Vector4Cache.TryGetValue(vectorString, out Vector4 cachedResult))
        {
            return cachedResult;
        }

        try
        {
            string[] parts = vectorString.Split(',');
            if (parts.Length == 4)
            {
                Vector4 result = new Vector4(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );

                // Cache result if cache isn't too large
                if (Vector4Cache.Count < MAX_CACHE_SIZE)
                {
                    Vector4Cache[vectorString] = result;
                }

                return result;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UGUIMLParser: Error parsing Vector4 '{vectorString}' - {e.Message}");
        }
        return defaultValue;
    }

    #endregion

    #region Color Parsing Methods

    /// <summary>
    /// Parse color from string with caching optimization
    /// </summary>
    public static Color ParseColor(string colorString, Color defaultValue = default)
    {
        if (string.IsNullOrEmpty(colorString)) return defaultValue;

        // Check cache first
        if (ColorCache.TryGetValue(colorString, out Color cachedColor))
        {
            return cachedColor;
        }

        Color result = defaultValue;

        // Try HTML color parsing
        if (ColorUtility.TryParseHtmlString(colorString, out Color htmlColor))
        {
            result = htmlColor;
        }
        // Try RGB parsing (r,g,b or r,g,b,a)
        else if (colorString.Contains(","))
        {
            result = ParseRGBColor(colorString, defaultValue);
        }

        // Cache result if cache isn't too large and we got a valid color
        if (ColorCache.Count < MAX_CACHE_SIZE && result != defaultValue)
        {
            ColorCache[colorString] = result;
        }

        return result;
    }

    /// <summary>
    /// Parse RGB color from comma-separated values
    /// </summary>
    private static Color ParseRGBColor(string rgbString, Color defaultValue)
    {
        try
        {
            string[] parts = rgbString.Split(',');
            if (parts.Length >= 3)
            {
                float r = float.Parse(parts[0].Trim()) / 255f;
                float g = float.Parse(parts[1].Trim()) / 255f;
                float b = float.Parse(parts[2].Trim()) / 255f;
                float a = parts.Length > 3 ? float.Parse(parts[3].Trim()) / 255f : 1f;

                return new Color(r, g, b, a);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UGUIMLParser: Error parsing RGB color '{rgbString}' - {e.Message}");
        }
        return defaultValue;
    }

    #endregion

    #region Enum Parsing Methods

    /// <summary>
    /// Parse text alignment with caching
    /// </summary>
    public static TextAlignmentOptions ParseTextAlignment(string alignment, TextAlignmentOptions defaultValue = TextAlignmentOptions.Center)
    {
        if (string.IsNullOrEmpty(alignment)) return defaultValue;
        return TextAlignmentCache.TryGetValue(alignment, out TextAlignmentOptions result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse input field content type with caching
    /// </summary>
    public static TMP_InputField.ContentType ParseInputContentType(string contentType, TMP_InputField.ContentType defaultValue = TMP_InputField.ContentType.Standard)
    {
        if (string.IsNullOrEmpty(contentType)) return defaultValue;
        return InputContentTypeCache.TryGetValue(contentType, out TMP_InputField.ContentType result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse text anchor with caching
    /// </summary>
    public static TextAnchor ParseTextAnchor(string anchor, TextAnchor defaultValue = TextAnchor.MiddleCenter)
    {
        if (string.IsNullOrEmpty(anchor)) return defaultValue;
        return TextAnchorCache.TryGetValue(anchor, out TextAnchor result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse grid layout corner with caching
    /// </summary>
    public static GridLayoutGroup.Corner ParseGridCorner(string corner, GridLayoutGroup.Corner defaultValue = GridLayoutGroup.Corner.UpperLeft)
    {
        if (string.IsNullOrEmpty(corner)) return defaultValue;
        return GridCornerCache.TryGetValue(corner, out GridLayoutGroup.Corner result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse grid layout axis with caching
    /// </summary>
    public static GridLayoutGroup.Axis ParseGridAxis(string axis, GridLayoutGroup.Axis defaultValue = GridLayoutGroup.Axis.Horizontal)
    {
        if (string.IsNullOrEmpty(axis)) return defaultValue;
        return GridAxisCache.TryGetValue(axis, out GridLayoutGroup.Axis result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse grid layout constraint with caching
    /// </summary>
    public static GridLayoutGroup.Constraint ParseGridConstraint(string constraint, GridLayoutGroup.Constraint defaultValue = GridLayoutGroup.Constraint.FixedColumnCount)
    {
        if (string.IsNullOrEmpty(constraint)) return defaultValue;
        return GridConstraintCache.TryGetValue(constraint, out GridLayoutGroup.Constraint result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse canvas render mode with caching
    /// </summary>
    public static RenderMode ParseRenderMode(string renderMode, RenderMode defaultValue = RenderMode.ScreenSpaceOverlay)
    {
        if (string.IsNullOrEmpty(renderMode)) return defaultValue;
        return RenderModeCache.TryGetValue(renderMode, out RenderMode result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse canvas scaler mode with caching
    /// </summary>
    public static CanvasScaler.ScaleMode ParseScaleMode(string scaleMode, CanvasScaler.ScaleMode defaultValue = CanvasScaler.ScaleMode.ScaleWithScreenSize)
    {
        if (string.IsNullOrEmpty(scaleMode)) return defaultValue;
        return ScaleModeCache.TryGetValue(scaleMode, out CanvasScaler.ScaleMode result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse slider direction with caching
    /// </summary>
    public static Slider.Direction ParseSliderDirection(string direction, Slider.Direction defaultValue = Slider.Direction.LeftToRight)
    {
        if (string.IsNullOrEmpty(direction)) return defaultValue;
        return SliderDirectionCache.TryGetValue(direction, out Slider.Direction result) ? result : defaultValue;
    }

    /// <summary>
    /// Parse scrollbar direction with caching
    /// </summary>
    public static Scrollbar.Direction ParseScrollbarDirection(string direction, Scrollbar.Direction defaultValue = Scrollbar.Direction.BottomToTop)
    {
        if (string.IsNullOrEmpty(direction)) return defaultValue;
        return ScrollbarDirectionCache.TryGetValue(direction, out Scrollbar.Direction result) ? result : defaultValue;
    }

    #endregion

    #region Element Type Parsing

    /// <summary>
    /// Get normalized element type with caching to avoid repeated ToLower() calls
    /// </summary>
    public static string GetNormalizedElementType(string elementType)
    {
        if (string.IsNullOrEmpty(elementType)) return "";

        if (!ElementTypeCache.TryGetValue(elementType, out string normalizedType))
        {
            normalizedType = elementType.ToLower();
            // Cache new element type for future use if cache isn't too large
            if (ElementTypeCache.Count < MAX_CACHE_SIZE)
            {
                ElementTypeCache[elementType] = normalizedType;
            }
        }

        return normalizedType;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Clear all caches to free memory (call when appropriate)
    /// </summary>
    public static void ClearCaches()
    {
        Vector2Cache.Clear();
        Vector3Cache.Clear();
        Vector4Cache.Clear();
        ColorCache.Clear();
        
        // Don't clear enum caches as they are static and reusable
        // ElementTypeCache, TextAlignmentCache, etc. should remain
    }

    /// <summary>
    /// Get cache statistics for debugging/monitoring
    /// </summary>
    public static string GetCacheStats()
    {
        return $"UGUIMLParser Cache Stats:\n" +
               $"Vector2: {Vector2Cache.Count}/{MAX_CACHE_SIZE}\n" +
               $"Vector3: {Vector3Cache.Count}/{MAX_CACHE_SIZE}\n" +
               $"Vector4: {Vector4Cache.Count}/{MAX_CACHE_SIZE}\n" +
               $"Color: {ColorCache.Count}/{MAX_CACHE_SIZE}\n" +
               $"ElementType: {ElementTypeCache.Count}\n" +
               $"TextAlignment: {TextAlignmentCache.Count}\n" +
               $"InputContentType: {InputContentTypeCache.Count}";
    }

    #endregion
} 