using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Kitchen Sink Controller - Demonstrates advanced UGUIML features with external commands
/// </summary>
public class KitchenSinkController : MonoBehaviour
{
    [Header("UGUIML Reference")]
    public UGUIML uguimlComponent;
    
    [Header("Demo Settings")]
    public float animationSpeed = 2f;
    public AudioClip buttonClickSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    
    [Header("Demo State")]
    [SerializeField] private int selectedGridItem = -1;
    [SerializeField] private int currentTheme = 0;
    [SerializeField] private bool soundEnabled = true;
    [SerializeField] private float playerHealth = 75f;
    [SerializeField] private float playerExp = 42f;
    [SerializeField] private int playerGold = 1250;
    [SerializeField] private int playerScore = 9875;
    [SerializeField] private int playerStreak = 15;
    
    [Header("Unity Events")]
    public UnityEvent OnModalShown;
    public UnityEvent OnModalClosed;
    public UnityEvent OnThemeChanged;
    public UnityEvent<string> OnMenuOpened;
    
    private AudioSource audioSource;
    private bool isModalVisible = false;
    private bool isToastVisible = false;
    
    private void Start()
    {
        // Find UGUIML component if not assigned
        if (uguimlComponent == null)
        {
            uguimlComponent = FindObjectOfType<UGUIML>();
        }
        
        if (uguimlComponent == null)
        {
            Debug.LogError("KitchenSinkController: No UGUIML component found!");
            return;
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Wait for UGUIML to load, then setup
        StartCoroutine(SetupDemo());
    }
    
    private IEnumerator SetupDemo()
    {
        // Wait for UGUIML to load
        yield return new WaitUntil(() => uguimlComponent.IsLoaded);
        
        // Set this script as the external command handler
        uguimlComponent.SetExternalCommandHandler(this);
        
        // Verify external handler was set
        var handler = uguimlComponent.GetExternalCommandHandler();
        if (handler == this)
        {
            Debug.Log("KitchenSinkController: External command handler set successfully!");
        }
        else
        {
            Debug.LogError("KitchenSinkController: Failed to set external command handler!");
        }
        
        // Initialize demo state
        InitializeDemoState();
        
        Debug.Log("KitchenSinkController: Demo setup complete!");
    }
    
    private void InitializeDemoState()
    {
        // Update initial display values
        UpdateHealthDisplay(playerHealth.ToString());
        SetElementText("gridSelection", "Selected: None");
        
        // Hide modal and toast initially
        uguimlComponent.SetElementAlpha("modalDialog", "0");
        uguimlComponent.SetElementAlpha("toastMessage", "0");
    }

    #region External Command Methods
    
    /// <summary>
    /// Animate all main sections with staggered timing
    /// </summary>
    public void AnimateAllSections()
    {
        Debug.Log("KitchenSinkController: Animating all sections");
        StartCoroutine(AnimateAllSectionsSequence());
    }
    
    private IEnumerator AnimateAllSectionsSequence()
    {
        // Bounce each column with delays
        uguimlComponent.BounceElement("leftColumn");
        yield return new WaitForSeconds(0.3f);
        
        uguimlComponent.BounceElement("centerColumn");
        yield return new WaitForSeconds(0.3f);
        
        uguimlComponent.BounceElement("rightColumn");
        yield return new WaitForSeconds(0.3f);
        
        // Scale the header
        uguimlComponent.ScaleElement("header", "1.05");
        yield return new WaitForSeconds(0.2f);
        uguimlComponent.ScaleElement("header", "1.0");
        
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Reset all sections to original state
    /// </summary>
    public void ResetAllSections()
    {
        Debug.Log("KitchenSinkController: Resetting all sections");
        
        // Reset columns to XML positions manually
        ResetColumnPositions();
        
        // Reset all element states (alpha, scale, interactable)
        ResetElementStates();
        
        // Reset text content
        SetElementText("dynamicText", "Dynamic Text Content");
        SetElementText("gridSelection", "Selected: None");
        
        // Reset modal and toast
        CloseModal();
        HideToast();
        
        // Reset demo state
        selectedGridItem = -1;
        
        PlaySound(buttonClickSound);
    }
    
    private void ResetColumnPositions()
    {
        // Manually set positions to match XML values
        uguimlComponent.MoveElementTo("leftColumn", "-400", "0");
        uguimlComponent.MoveElementTo("centerColumn", "0", "0");
        uguimlComponent.MoveElementTo("rightColumn", "400", "0");
        uguimlComponent.MoveElementTo("header", "0", "360");
        uguimlComponent.MoveElementTo("footer", "0", "-360");
        
        // Reset section positions within columns
        uguimlComponent.MoveElementTo("basicSection", "0", "250");
        uguimlComponent.MoveElementTo("inputSection", "0", "80");
        uguimlComponent.MoveElementTo("toggleSection", "0", "-90");
        uguimlComponent.MoveElementTo("animationControls", "0", "-260");
        uguimlComponent.MoveElementTo("progressSection", "0", "250");
        uguimlComponent.MoveElementTo("scrollSection", "0", "50");
        uguimlComponent.MoveElementTo("cardsSection", "0", "-150");
        uguimlComponent.MoveElementTo("gridSection", "0", "250");
        uguimlComponent.MoveElementTo("layoutSection", "0", "50");
        uguimlComponent.MoveElementTo("advancedSection", "0", "-150");
    }
    
    private void ResetElementStates()
    {
        string[] allElements = {
            "leftColumn", "centerColumn", "rightColumn", "header", "footer",
            "basicSection", "inputSection", "toggleSection", "animationControls",
            "progressSection", "scrollSection", "cardsSection", "gridSection",
            "layoutSection", "advancedSection", "statCard1", "statCard2", "statCard3"
        };
        
        foreach (string elementName in allElements)
        {
            uguimlComponent.SetElementAlpha(elementName, "1.0");
            uguimlComponent.ScaleElement(elementName, "1.0");
            uguimlComponent.SetElementInteractable(elementName, "true");
        }
    }
    
    /// <summary>
    /// Show all sections
    /// </summary>
    public void ShowAllSections()
    {
        Debug.Log("KitchenSinkController: Showing all sections");
        
        // Restore to original positions and make visible
        uguimlComponent.RestoreElementToOriginalState("leftColumn");
        uguimlComponent.RestoreElementToOriginalState("centerColumn");
        uguimlComponent.RestoreElementToOriginalState("rightColumn");
        
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Hide all sections
    /// </summary>
    public void HideAllSections()
    {
        uguimlComponent.HideElement("leftColumn");
        uguimlComponent.HideElement("centerColumn");
        uguimlComponent.HideElement("rightColumn");
        
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Validate input field
    /// </summary>
    public void ValidateInput(string inputValue)
    {
        if (string.IsNullOrEmpty(inputValue))
        {
            ShowToastMessage("Input cannot be empty!");
            PlaySound(errorSound);
        }
        else
        {
            ShowToastMessage($"Valid input: {inputValue}");
            PlaySound(successSound);
        }
    }
    
    /// <summary>
    /// Validate all input fields
    /// </summary>
    public void ValidateAllInputs()
    {
        var usernameField = uguimlComponent.GetInputField("usernameField");
        var emailField = uguimlComponent.GetInputField("emailField");
        
        bool isValid = true;
        string message = "All inputs valid!";
        
        if (usernameField != null && string.IsNullOrEmpty(usernameField.text))
        {
            isValid = false;
            message = "Username is required!";
        }
        else if (emailField != null && string.IsNullOrEmpty(emailField.text))
        {
            isValid = false;
            message = "Email is required!";
        }
        
        ShowToastMessage(message);
        PlaySound(isValid ? successSound : errorSound);
        
        if (isValid)
        {
            uguimlComponent.BounceElement("inputSection");
        }
    }
    
    /// <summary>
    /// Toggle sound setting
    /// </summary>
    public void ToggleSound(string isEnabled)
    {
        soundEnabled = bool.Parse(isEnabled);
        
        ShowToastMessage(soundEnabled ? "Sound Enabled" : "Sound Disabled");
    }
    
    /// <summary>
    /// Change theme
    /// </summary>
    public void ChangeTheme(string themeIndex)
    {
        currentTheme = int.Parse(themeIndex);
        string[] themes = { "Dark Theme", "Light Theme", "Colorful", "Rainbow" };
        
        ShowToastMessage($"Theme: {themes[currentTheme]}");
        
        // Animate theme change
        uguimlComponent.BounceElement("toggleSection");
        
        OnThemeChanged?.Invoke();
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Update health display
    /// </summary>
    public void UpdateHealthDisplay(string healthValue)
    {
        if (float.TryParse(healthValue, out float health))
        {
            playerHealth = health;
            SetElementText("healthDisplay", $"{health:F0}%");
            
            // Change color based on health
            if (health < 25f)
            {
                ShowToastMessage("Critical Health!");
                PlaySound(errorSound);
            }
            else if (health < 50f)
            {
                ShowToastMessage("Low Health");
            }
        }
    }
    
    /// <summary>
    /// Level up player
    /// </summary>
    public void LevelUpPlayer()
    {
        // Increase stats
        playerExp = 0f;
        playerHealth = Mathf.Min(100f, playerHealth + 25f);
        playerGold += 500;
        playerScore += 1000;
        playerStreak++;
        
        // Update displays
        UpdateGameStats();
        UpdateHealthDisplay(playerHealth.ToString());
        uguimlComponent.SetProgressBarValue("expBar", 0f, true);
        
        // Celebration animation
        StartCoroutine(LevelUpCelebration());
        
        ShowToastMessage("LEVEL UP! Stats increased!");
        PlaySound(successSound);
    }
    
    private IEnumerator LevelUpCelebration()
    {
        for (int i = 0; i < 3; i++)
        {
            uguimlComponent.ScaleElement("progressSection", "1.1");
            yield return new WaitForSeconds(0.1f);
            uguimlComponent.ScaleElement("progressSection", "1.0");
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// Update game stats display
    /// </summary>
    public void UpdateGameStats()
    {
        SetElementText("stat1Value", $"{playerGold:N0}");
        SetElementText("stat2Value", $"{playerScore:N0}");
        SetElementText("stat3Value", $"x{playerStreak}");
        
        // Animate the stat cards
        AnimateStatCards();
        
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Animate stat cards
    /// </summary>
    public void AnimateStatCards()
    {
        StartCoroutine(AnimateStatCardsSequence());
    }
    
    private IEnumerator AnimateStatCardsSequence()
    {
        string[] cards = { "statCard1", "statCard2", "statCard3" };
        
        for (int i = 0; i < cards.Length; i++)
        {
            uguimlComponent.BounceElement(cards[i]);
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    /// <summary>
    /// Select grid item
    /// </summary>
    public void SelectGridItem(string itemNumber)
    {
        selectedGridItem = int.Parse(itemNumber);
        
        SetElementText("gridSelection", $"Selected: {selectedGridItem}");
        uguimlComponent.BounceElement($"gridBtn{selectedGridItem}");
        
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Open menu
    /// </summary>
    public void OpenMenu(string menuName)
    {
        ShowToastMessage($" Opened {menuName} Menu");
        
        OnMenuOpened?.Invoke(menuName);
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Navigate to page
    /// </summary>
    public void NavigateToPage(string pageName)
    {
        ShowToastMessage($"Navigating to {pageName}");
        
        // Animate the navigation
        uguimlComponent.BounceElement("verticalDemo");
        
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Show modal dialog
    /// </summary>
    public void ShowModalDialog()
    {
        if (isModalVisible) return;
        
        isModalVisible = true;
        uguimlComponent.FadeInElement("modalDialog");
        uguimlComponent.ScaleElement("modalDialog", "1.0");
        
        OnModalShown?.Invoke();
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Close modal dialog
    /// </summary>
    public void CloseModal()
    {
        if (!isModalVisible) return;
        
        isModalVisible = false;
        uguimlComponent.FadeOutElement("modalDialog");
        
        OnModalClosed?.Invoke();
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Show toast message (parameterless version for XML button)
    /// </summary>
    public void ShowToastMessage()
    {
        ShowToastMessage("Toast message triggered!");
    }
    
    /// <summary>
    /// Show toast message with custom text
    /// </summary>
    public void ShowToastMessage(string message)
    {
        if (isToastVisible) return;
        
        if (!string.IsNullOrEmpty(message))
        {
            SetElementText("toastText", message);
        }
        
        StartCoroutine(ShowToastSequence());
    }
    
    private IEnumerator ShowToastSequence()
    {
        isToastVisible = true;
        
        // Slide in from bottom
        uguimlComponent.FadeInElement("toastMessage");
        uguimlComponent.MoveElementTo("toastMessage", "0", "-250");
        
        // Wait
        yield return new WaitForSeconds(2f);
        
        // Slide out
        HideToast();
    }
    
    private void HideToast()
    {
        if (!isToastVisible) return;
        
        isToastVisible = false;
        uguimlComponent.FadeOutElement("toastMessage");
        uguimlComponent.MoveElementTo("toastMessage", "0", "-300");
    }
    
    /// <summary>
    /// Play animation sequence
    /// </summary>
    public void PlayAnimationSequence()
    {
        StartCoroutine(ComplexAnimationSequence());
    }
    
    private IEnumerator ComplexAnimationSequence()
    {
        ShowToastMessage("Starting animation sequence...");
        
        // Phase 1: Hide all columns
        uguimlComponent.SlideElementOffScreen("leftColumn", "Left");
        uguimlComponent.SlideElementOffScreen("rightColumn", "Right");
        uguimlComponent.FadeOutElement("centerColumn");
        
        yield return new WaitForSeconds(1f);
        
        // Phase 2: Bring them back to original positions with animations
        uguimlComponent.RestoreElementToOriginalState("leftColumn");
        yield return new WaitForSeconds(0.3f);
        
        uguimlComponent.RestoreElementToOriginalState("centerColumn");
        yield return new WaitForSeconds(0.3f);
        
        uguimlComponent.RestoreElementToOriginalState("rightColumn");
        yield return new WaitForSeconds(0.5f);
        
        // Phase 3: Bounce finale
        uguimlComponent.BounceElement("leftColumn");
        uguimlComponent.BounceElement("centerColumn");
        uguimlComponent.BounceElement("rightColumn");
        
        yield return new WaitForSeconds(0.5f);
        
        ShowToastMessage("Animation sequence complete!");
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Trigger particle effect
    /// </summary>
    public void TriggerParticleEffect()
    {
        // Simulate particle effect with multiple animations
        StartCoroutine(ParticleEffectSimulation());
    }
    
    private IEnumerator ParticleEffectSimulation()
    {
        ShowToastMessage("Particle effect triggered!");
        
        // Animate multiple elements to simulate particles
        string[] elements = { "statCard1", "statCard2", "statCard3", "gridBtn1", "gridBtn2", "gridBtn3" };
        
        foreach (string element in elements)
        {
            uguimlComponent.ScaleElement(element, "1.3");
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.2f);
        
        foreach (string element in elements)
        {
            uguimlComponent.ScaleElement(element, "1.0");
        }
        
        PlaySound(successSound);
    }
    
    /// <summary>
    /// Play sound effect
    /// </summary>
    public void PlaySoundEffect()
    {
        ShowToastMessage("Sound effect played!");
        
        PlaySound(buttonClickSound);
    }
    
    /// <summary>
    /// Execute random action
    /// </summary>
    public void ExecuteRandomAction()
    {    
        string[] actions = {
            "Bounce random element",
            "Update stats",
            "Show toast",
            "Animate cards",
            "Scale header"
        };
        
        int randomIndex = Random.Range(0, actions.Length);
        string action = actions[randomIndex];
        
        switch (randomIndex)
        {
            case 0:
                string[] elements = { "leftColumn", "centerColumn", "rightColumn" };
                uguimlComponent.BounceElement(elements[Random.Range(0, elements.Length)]);
                break;
            case 1:
                UpdateGameStats();
                break;
            case 2:
                ShowToastMessage("Random action executed!");
                break;
            case 3:
                AnimateStatCards();
                break;
            case 4:
                uguimlComponent.ScaleElement("header", "1.1");
                StartCoroutine(ResetHeaderScale());
                break;
        }
        
        ShowToastMessage($"Random: {action}");
        PlaySound(buttonClickSound);
    }
    
    private IEnumerator ResetHeaderScale()
    {
        yield return new WaitForSeconds(0.3f);
        uguimlComponent.ScaleElement("header", "1.0");
    }

    #endregion

    #region Utility Methods
    
    /// <summary>
    /// Set element text safely
    /// </summary>
    private void SetElementText(string elementName, string text)
    {
        uguimlComponent.SetElementText(elementName, text);
    }
    
    /// <summary>
    /// Play sound if enabled
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (soundEnabled && audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public API Methods (for other scripts)
    
    /// <summary>
    /// Get current demo state
    /// </summary>
    public void GetDemoState()
    {
        Debug.Log($"Demo State - Health: {playerHealth}, Exp: {playerExp}, Gold: {playerGold}, Score: {playerScore}, Streak: {playerStreak}");
    }
    
    /// <summary>
    /// Reset demo to initial state
    /// </summary>
    public void ResetDemo()
    {
        playerHealth = 75f;
        playerExp = 42f;
        playerGold = 1250;
        playerScore = 9875;
        playerStreak = 15;
        selectedGridItem = -1;
        currentTheme = 0;
        
        InitializeDemoState();
        ResetAllSections();
        
        Debug.Log("Demo reset to initial state");
    }
    #endregion
} 