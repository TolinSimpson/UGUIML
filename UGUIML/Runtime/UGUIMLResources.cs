using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.IO;
#if ADDRESSABLE_ASSETS
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class UGUIMLResources : MonoBehaviour
{
    public static UGUIMLResources Singleton;
    public List<GUIResource> resources = new List<GUIResource>(64);

    public Dictionary<string, CanvasGroup> panels = new Dictionary<string, CanvasGroup>();
    
    [Header("Default UI Resources (Used when bindId = -1)")]
    [SerializeField] private Sprite defaultButtonBackground;
    [SerializeField] private Sprite defaultPanelBackground;
    [SerializeField] private Texture2D defaultImageTexture;
    [SerializeField] private TMP_FontAsset defaultFont;
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private float defaultFontSize = 14f;
    [SerializeField] private Color defaultButtonColor = Color.white;
    [SerializeField] private Color defaultPanelColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Rich Component Default Resources")]
    [SerializeField] private Sprite defaultCheckboxBackground;
    [SerializeField] private Sprite defaultCheckboxCheckmark;
    [SerializeField] private Color defaultCheckboxColor = Color.white;
    [SerializeField] private Color defaultCheckmarkColor = new Color(0.2f, 0.6f, 0.9f);
    [SerializeField] private Sprite defaultInputFieldBackground;
    [SerializeField] private Color defaultInputFieldColor = new Color(0.17f, 0.24f, 0.31f);
    [SerializeField] private Sprite defaultDropdownBackground;
    [SerializeField] private Sprite defaultDropdownArrow;
    [SerializeField] private Color defaultDropdownColor = new Color(0.17f, 0.24f, 0.31f);
    [SerializeField] private Sprite defaultScrollbarBackground;
    [SerializeField] private Sprite defaultScrollbarHandle;
    [SerializeField] private Color defaultScrollbarBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color defaultScrollbarHandleColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color defaultProgressBarBackgroundColor = new Color(0.2f, 0.3f, 0.4f);
    [SerializeField] private Color defaultProgressBarFillColor = new Color(0.2f, 0.6f, 0.9f);
    
    public Sprite DefaultButtonBackground => defaultButtonBackground;
    public Sprite DefaultPanelBackground => defaultPanelBackground;
    public Texture2D DefaultImageTexture => defaultImageTexture;
    public TMP_FontAsset DefaultFont => defaultFont;
    public Color DefaultTextColor => defaultTextColor;
    public float DefaultFontSize => defaultFontSize;
    public Color DefaultButtonColor => defaultButtonColor;
    public Color DefaultPanelColor => defaultPanelColor;
    
    // Rich Component Properties
    public Sprite DefaultCheckboxBackground => defaultCheckboxBackground;
    public Sprite DefaultCheckboxCheckmark => defaultCheckboxCheckmark;
    public Color DefaultCheckboxColor => defaultCheckboxColor;
    public Color DefaultCheckmarkColor => defaultCheckmarkColor;
    public Sprite DefaultInputFieldBackground => defaultInputFieldBackground;
    public Color DefaultInputFieldColor => defaultInputFieldColor;
    public Sprite DefaultDropdownBackground => defaultDropdownBackground;
    public Sprite DefaultDropdownArrow => defaultDropdownArrow;
    public Color DefaultDropdownColor => defaultDropdownColor;
    public Sprite DefaultScrollbarBackground => defaultScrollbarBackground;
    public Sprite DefaultScrollbarHandle => defaultScrollbarHandle;
    public Color DefaultScrollbarBackgroundColor => defaultScrollbarBackgroundColor;
    public Color DefaultScrollbarHandleColor => defaultScrollbarHandleColor;
    public Color DefaultProgressBarBackgroundColor => defaultProgressBarBackgroundColor;
    public Color DefaultProgressBarFillColor => defaultProgressBarFillColor;

    void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void MoveResourceBinding(int from, int to, int index)
    {
        resources[to].bindings.Add(resources[from].bindings[index]);
        resources[from].bindings.RemoveAt(index);
    }

    public void ChangeElementBinding(UGUIML uguiml, string elementName, int newBinding)
    {
        UGUIMLElement element = uguiml.GetUGUIMLElement(elementName);
        if (element == null)
        {
            Debug.LogWarning($"UGUIML Animation: Element '{elementName}' not found!");
            return;
        }

        element.RemoveBinding();
        element.SetBinding(newBinding);
    }
}

[Serializable]
public class GUIResource
{
    public GUIResourceTypes resourceType = GUIResourceTypes.Undefined;

    [Header("Text Resources")]
    public string txt;
    public TMP_ColorGradient gradient;

    [Header("Image Resources")]
    public Texture img;
    public string src;
    public SourceTypes sourceType;
    public bool isLoaded;
    public List<UGUIMLElement> bindings = new List<UGUIMLElement>();

    // [Header("Other Resource")]
    // public Color color;
    // public Sprite sprite;

    public void SyncBindings()
    {
        foreach (UGUIMLElement binding in bindings)
        {
            switch (resourceType)
            {
                case GUIResourceTypes.Button:

                    break;

                case GUIResourceTypes.Image:

                    TMP_Text textComponent = binding.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = txt;
                        textComponent.colorGradientPreset = gradient;
                        return;
                    }

                    break;

                case GUIResourceTypes.RawImage:

                    RawImage imageComponent = binding.GetComponent<RawImage>();
                    if (imageComponent != null)
                    {
                        imageComponent.texture = img;
                        return;
                    }

                    break;

                case GUIResourceTypes.Text:

                    break;

                case GUIResourceTypes.Undefined:

                    break;
            }
        }
    }

    public void SetText(string newText)
    {
        txt = newText;
        resourceType = GUIResourceTypes.Text;
        SyncBindings();
    }

    public void SetGradient(string topLeft, string topRight, string bottomLeft, string bottomRight)
    {
        ColorUtility.TryParseHtmlString(topLeft, out gradient.topLeft);
        ColorUtility.TryParseHtmlString(topRight, out gradient.topLeft);
        ColorUtility.TryParseHtmlString(bottomRight, out gradient.bottomRight);
        ColorUtility.TryParseHtmlString(bottomLeft, out gradient.bottomLeft);
        resourceType = GUIResourceTypes.Text;
        SyncBindings();
    }

    public async void LoadSource(string src, SourceTypes sourceTypes)
    {
        isLoaded = false;
        this.src = src;
        this.sourceType = sourceTypes;
        
        try
        {
            switch (sourceTypes)
            {
                case SourceTypes.Addressable:
                    await LoadFromAddressable(src);
                    break;
                case SourceTypes.AssetBundle:
                    await LoadFromAssetBundle(src);
                    break;
                case SourceTypes.LocalFile:
                    await LoadFromLocalFile(src);
                    break;
                case SourceTypes.Resource:
                    await LoadFromResources(src);
                    break;
                case SourceTypes.URL:
                    await LoadFromURL(src);
                    break;
            }
            
            if (img != null)
            {
                isLoaded = true;
                resourceType = GUIResourceTypes.RawImage;
                SyncBindings();
            }
            else
            {
                Debug.LogError($"Failed to load texture from source: {src}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading texture from {src}: {e.Message}");
            isLoaded = false;
        }
    }

    private async Task LoadFromAddressable(string address)
    {
#if ADDRESSABLE_ASSETS
        try
        {
            AsyncOperationHandle<Texture2D> handle = Addressables.LoadAssetAsync<Texture2D>(address);
            await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                img = handle.Result;
            }
            else
            {
                Debug.LogError($"Failed to load addressable texture: {address}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Addressable loading error: {e.Message}");
        }
#else
        Debug.LogWarning("Addressable Assets package not installed. Cannot load from addressable.");
#endif
    }

    private async Task LoadFromAssetBundle(string assetName)
    {
        // Note: This assumes asset bundles are already loaded
        // In a real implementation, you might want to load the bundle first
        AssetBundle[] loadedBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
        
        foreach (AssetBundle bundle in loadedBundles)
        {
            if (bundle.Contains(assetName))
            {
                var request = bundle.LoadAssetAsync<Texture2D>(assetName);
                while (!request.isDone)
                {
                    await Task.Yield();
                }
                
                img = request.asset as Texture2D;
                if (img != null) break;
            }
        }
        
        if (img == null)
        {
            Debug.LogWarning($"Asset '{assetName}' not found in any loaded asset bundle.");
        }
    }

    private async Task LoadFromLocalFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));
                
                // Create texture on main thread
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData))
                {
                    img = texture;
                }
                else
                {
                    Debug.LogError($"Failed to load image data from file: {filePath}");
                    UnityEngine.Object.Destroy(texture);
                }
            }
            else
            {
                Debug.LogError($"File not found: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading local file {filePath}: {e.Message}");
        }
    }

    private async Task LoadFromResources(string resourcePath)
    {
        await Task.Yield(); // Yield to maintain async pattern
        
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            img = texture;
        }
        else
        {
            Debug.LogError($"Resource not found: {resourcePath}");
        }
    }

    private async Task LoadFromURL(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                img = DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                Debug.LogError($"Failed to download texture from URL: {url}. Error: {request.error}");
            }
        }
    }
}
public enum SourceTypes { Addressable, AssetBundle, LocalFile, Resource, URL }
public enum GUIResourceTypes { Text, RawImage, Image, Button, Undefined }