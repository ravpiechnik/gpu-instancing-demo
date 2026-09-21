using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainInterface : MonoBehaviour
{

    #region SCENE REFERENCES

    [SerializeField] Instantiator instantiator;
    [SerializeField] TMP_Text instancingModeText;
    [SerializeField] TMP_Text instancingModeButtonText;

    [SerializeField] Toggle shadowsToggle;
    [SerializeField] Toggle windToggle;

    [SerializeField] TMP_Text windSpeedText;
    [SerializeField] TMP_Text windStrengthText;
    [SerializeField] Slider windSpeedSlider;
    [SerializeField] Slider windStrengthSlider;

    [SerializeField] TMP_Text numberOfObjectsText;
    [SerializeField] Slider numberOfObjectsSlider;

    [SerializeField] TMP_Text fpsText;

    #endregion

    #region FPS GRAPH VARIABLES
    [Header("FPS Graph Settings")]
    private const float SampleInterval = 0.2f;
    private float sampleTimer;
    private const int GraphSize = 50;
    private readonly float[] fpsHistory = new float[GraphSize];
    private int historyIndex;
    private Texture2D whiteTexture;

    [SerializeField] private int graphWidth = 200;
    [SerializeField] private int graphHeight = 60;
    [SerializeField] private int graphMargin = 10;
    #endregion

    void Start()
    {
        whiteTexture = Texture2D.whiteTexture;
        for (int i = 0; i < fpsHistory.Length; i++) fpsHistory[i] = 60f; // seed with 60 FPS
    }

    public void SwitchInstancingMode()
    {
        if (instantiator == null) return;
        Instantiator.InstantiationType currentInstantiationType = instantiator.SwitchInstantiationType();

        if (currentInstantiationType == Instantiator.InstantiationType.CPU)
        {
            instancingModeText.text = "Current instancing mode: CPU";
            instancingModeButtonText.text = "Switch to GPU";
        }
        else
        {
            instancingModeText.text = "Current instancing mode: GPU";
            instancingModeButtonText.text = "Switch to CPU";
        }
    }

    public void ToggleShadows() {
        if (instantiator == null) return;
        if (shadowsToggle == null) return;
        instantiator.EnableShadows = shadowsToggle.isOn;
    }

    public void ToggleWind() {
        if (WindManager.Instance == null) return;
        if (windToggle == null) return;
        if (windSpeedSlider == null) return;
        if (windStrengthSlider == null) return;

        WindManager.Instance.WindEnabled = windToggle.isOn;

        if (windToggle.isOn) {
            windSpeedSlider.interactable = true;
            windStrengthSlider.interactable = true;
        } else {
            windSpeedSlider.interactable = false;
            windStrengthSlider.interactable = false;
        }
    }

    public void ChangeWindSpeed() {
        if (WindManager.Instance == null) return;
        if (windSpeedSlider == null) return;
        WindManager.Instance.WindSpeed = windSpeedSlider.value;
    }

    public void ChangeWindStrength() {
        if (WindManager.Instance == null) return;
        if (windStrengthSlider == null) return;
        WindManager.Instance.WindStrength = windStrengthSlider.value;
    }

    public void ChangeNumberOfObjectsText()
    {
        // handle only UI here; refreshing the scene is expensive.
        // Event Trigger component + OnDragEnd handles the refresh
        if (instantiator == null) return;
        if (numberOfObjectsSlider == null) return;
        if (numberOfObjectsText == null) return;
        numberOfObjectsText.text = "Number of objects: " + ((int)numberOfObjectsSlider.value).ToString();
    }

    public void ChangeNumberOfObjects()
    {
        if (instantiator == null) return;
        if (numberOfObjectsSlider == null) return;
        instantiator.NumberOfObjects = (int)numberOfObjectsSlider.value;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) Application.Quit();

        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= SampleInterval)
        {
            sampleTimer -= SampleInterval;
            float fpsSample = 1f / Time.unscaledDeltaTime;
            fpsHistory[historyIndex] = fpsSample;
            historyIndex = (historyIndex + 1) % GraphSize;

            if (fpsText != null)
            {
                fpsText.text = "FPS: " + Mathf.RoundToInt(fpsSample).ToString();
            }
        }
    }

    private void OnGUI()
    {
        int width = graphWidth;
        int height = graphHeight;
        int margin = graphMargin;

        // reserve some width on the left for numeric labels
        int labelWidth = 44;
        int innerWidth = Mathf.Max(10, width - labelWidth);

        // vertical padding to avoid clipping top/bottom ticks and labels
        int vPad = 6;
        float innerHeight = Mathf.Max(8, height - 2 * vPad);

        Rect groupRect = new Rect(Screen.width - width - margin, margin, width, height);
        GUI.BeginGroup(groupRect);

        Color prevColor = GUI.color;

        // background
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.Box(new Rect(0, 0, width, height), GUIContent.none);

        // determine maxFPS for scaling the graph and round up to nearest 10
        float maxFps = 30f;
        for (int i = 0; i < GraphSize; i++) if (fpsHistory[i] > maxFps) maxFps = fpsHistory[i];
        float displayMax = Mathf.Max(30f, maxFps);
        float roundedMax = Mathf.Ceil(displayMax / 10f) * 10f;

        // draw scale ticks and labels (0, 1/4, 1/2, 3/4, max)
        int ticks = 4;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
        Color lineColor = new Color(1f, 1f, 1f, 0.15f);
        GUI.color = lineColor;
        for (int t = 0; t <= ticks; t++)
        {
            float value = (roundedMax * t) / ticks;
            float normalized = value / roundedMax;
            float y = vPad + (1f - normalized) * innerHeight;
            
            GUI.DrawTexture(new Rect(labelWidth, y, innerWidth, 1), whiteTexture);
            
            string label = Mathf.RoundToInt(value).ToString();
            float labelY = Mathf.Clamp(y - 9f, 0f, height - 18f);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, labelY, labelWidth - 4, 18), label, labelStyle);
            GUI.color = lineColor;
        }

        // draw bars (area shifted right by labelWidth)
        GUI.color = new Color(0f, 1f, 0f, 0.5f);
        float barWidth = (float)innerWidth / GraphSize;
        for (int i = 0; i < GraphSize; i++)
        {
            int idx = (historyIndex + i) % GraphSize; // oldest first
            float value = fpsHistory[idx];
            float normalized = Mathf.Clamp01(value / roundedMax);
            float barHeight = normalized * innerHeight;
            float x = labelWidth + i * barWidth;
            float y = vPad + innerHeight - barHeight;
            GUI.DrawTexture(new Rect(x, y, Mathf.Max(1f, barWidth - 1f), barHeight), whiteTexture);
        }

        GUI.color = prevColor;
        GUI.EndGroup();
    }
}

