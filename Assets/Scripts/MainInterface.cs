using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainInterface : MonoBehaviour
{

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
        instantiator.EnableShadows = shadowsToggle.isOn;
    }

    public void ToggleWind() {
        if (WindManager.Instance == null) return;
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
        WindManager.Instance.WindSpeed = windSpeedSlider.value;
    }

    public void ChangeWindStrength() {
        if (WindManager.Instance == null) return;
        WindManager.Instance.WindStrength = windStrengthSlider.value;
    }

    public void ChangeNumberOfObjectsText()
    {
        // handle only UI here; refreshing the scene is expensive.
        // Event Trigger component + OnDragEnd handles the refresh
        if (instantiator == null) return;
        numberOfObjectsText.text = "Number of objects: " + ((int)numberOfObjectsSlider.value).ToString();
    }

    public void ChangeNumberOfObjects()
    {
        if (instantiator == null) return;
        instantiator.NumberOfObjects = (int)numberOfObjectsSlider.value;
    }
}
