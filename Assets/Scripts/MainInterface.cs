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
        if(Input.GetKeyDown(KeyCode.Q)) Application.Quit();

        if (fpsText == null) return;
        float fps = 1f / Time.unscaledDeltaTime;
        fpsText.text = "FPS: " + Mathf.RoundToInt(fps).ToString();

    }
}

