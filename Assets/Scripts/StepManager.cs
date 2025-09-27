using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StepManager : MonoBehaviour
{
    [Header("Root Prefab")]
    public GameObject laptopSceneRoot;

    [Header("UI")]
    public TextMeshProUGUI infoText;
    public Button nextButton;
    public Button previousButton;

    [Header("Settings")]
    public float rotationDuration = 1f;
    public float screwAnimationDuration = 0.1f; 

    private GameObject Monitor, CoverElectronics, BatteryOld, CasingDisk;
    private GameObject Screws1, Screws2, Screwdriver, BatteryNew;

    private bool isStepRunning = false;
    private bool skipStep = false;

    private Vector3 initialScrewdriverPosition;
    private Quaternion initialScrewdriverRotation;

    private Dictionary<Transform, Vector3> originalScrewPositions = new Dictionary<Transform, Vector3>();


    private Vector3 storedBatteryPosition;
    private Transform oldBatteryParent;
    private Transform newBatteryParent;

    private enum TutorialStep
    {
        WarningMessage,
        WelcomeMessage,
        CloseLaptop,
        TurnUpsideDown,
        RemoveScrews1,
        MoveCasingDisk,
        RemoveScrews2,
        OpenCoverElectronics,
        RemoveBattery,
        InsertNewBattery,
        ReverseCover,
        ReturnScrews2,
        ReturnCasingDisk,
        ReturnScrews1,
        RotateBack,
        OpenLaptop, 
        FinalMessage
    }

    private TutorialStep currentStep = TutorialStep.WarningMessage;

    void Awake()
    {
        // Cache references
        Monitor = laptopSceneRoot.transform.Find("Monitor").gameObject;
        CoverElectronics = laptopSceneRoot.transform.Find("CoverElectronics").gameObject;
        BatteryOld = laptopSceneRoot.transform.Find("BatteryOld").gameObject;
        CasingDisk = laptopSceneRoot.transform.Find("CasingDisk").gameObject;
        Screws1 = laptopSceneRoot.transform.Find("Screws1").gameObject;
        Screws2 = laptopSceneRoot.transform.Find("Screws2").gameObject;
        Screwdriver = laptopSceneRoot.transform.Find("screwdriver").gameObject;
        BatteryNew = laptopSceneRoot.transform.Find("batteryNew").gameObject;

        // Alte Eltern speichern
        oldBatteryParent = BatteryOld.transform.parent;
        newBatteryParent = BatteryNew.transform.parent;
        BatteryNew.transform.SetParent(null, true);
        Screwdriver.transform.SetParent(null, true);

        initialScrewdriverPosition = Screwdriver.transform.position;
        initialScrewdriverRotation = Screwdriver.transform.rotation;

        UpdateButtonStates();
        previousButton.gameObject.SetActive(false);
    }

    public void NextStep()
    {
        if (isStepRunning)
        {
            skipStep = true;
            return;
        }
        skipStep = false;
        StartCoroutine(RunStep(currentStep, false));
    }

    public void PreviousStep()
    {
        if (isStepRunning)
        {
            skipStep = true;
            return;
        }
        skipStep = false;
        StartCoroutine(RunStep(currentStep, true));
    }

    IEnumerator RunStep(TutorialStep step, bool reverse)
    {
        isStepRunning = true;

        UpdateInfo(GetStepMessage(step));

        switch (step)
        {
            case TutorialStep.CloseLaptop:
                yield return RotateSmooth(Monitor.transform, Vector3.right, reverse ? -90f : 90f);
                break;
            case TutorialStep.TurnUpsideDown:
                yield return RotateGroupSmooth(laptopSceneRoot.transform, Monitor.transform.position, Vector3.forward, reverse ? -180f : 180f);
                break;
            case TutorialStep.RemoveScrews1:
                yield return AnimateScrewSequence(Screws1, false);
                break;
            case TutorialStep.MoveCasingDisk:
                yield return MoveAndRotateObject(CasingDisk.transform, CasingDisk.transform.position + (reverse ? Vector3.right : -Vector3.right) * 0.3f, CasingDisk.transform.rotation, 1f);
                break;
            case TutorialStep.RemoveScrews2:
                yield return AnimateScrewSequence(Screws2, false);
                break;
            case TutorialStep.OpenCoverElectronics:
                yield return RotateSmooth(CoverElectronics.transform, Vector3.right, reverse ? 180f : -180f);
                break;
            case TutorialStep.RemoveBattery:
                if (!reverse)
                {
                    storedBatteryPosition = BatteryOld.transform.position;
                    BatteryOld.transform.SetParent(null, true);

                    yield return MoveAndRotateObject(BatteryOld.transform, BatteryOld.transform.position + Vector3.right * 0.8f + Vector3.up * 0.2f + Vector3.forward * 0.2f, BatteryOld.transform.rotation, 1f);
                }
                else
                {
                    yield return MoveAndRotateObject(BatteryOld.transform, storedBatteryPosition, BatteryOld.transform.rotation, 1f);
                    BatteryOld.transform.SetParent(oldBatteryParent, true);

                }
                break;
            case TutorialStep.InsertNewBattery:
                Vector3 targetPos = reverse ? BatteryNew.transform.position + Vector3.right * 0.3f : storedBatteryPosition;
                yield return MoveAndRotateObject(BatteryNew.transform, targetPos, BatteryNew.transform.rotation, 1f);

                if (!reverse)
                {
                    BatteryNew.transform.SetParent(laptopSceneRoot.transform, true);
                }
                else
                {
                    BatteryNew.transform.SetParent(newBatteryParent, true);
                }
                break;
            case TutorialStep.ReverseCover:
                yield return RotateSmooth(CoverElectronics.transform, Vector3.right, reverse ? -180f : 180f);
                break;
            case TutorialStep.ReturnScrews2:
                yield return AnimateScrewSequence(Screws2, true);
                break;
            case TutorialStep.ReturnCasingDisk:
                yield return MoveAndRotateObject(CasingDisk.transform, CasingDisk.transform.position + (reverse ? -Vector3.right : Vector3.right) * 0.3f, CasingDisk.transform.rotation, 1f);
                break;
            case TutorialStep.ReturnScrews1:
                yield return AnimateScrewSequence(Screws1, true);
                break;
            case TutorialStep.RotateBack:
                yield return RotateGroupSmooth(laptopSceneRoot.transform, Monitor.transform.position, Vector3.forward, reverse ? 180f : -180f);
                break;
            case TutorialStep.OpenLaptop:
                yield return RotateSmooth(Monitor.transform, Vector3.right, reverse ? 90f : -90f);
                break;
        }

        // Update step index
        if (!reverse && currentStep != TutorialStep.FinalMessage)
            currentStep++;
        else if (reverse && currentStep != TutorialStep.WarningMessage)
            currentStep--;


        Screwdriver.transform.position = initialScrewdriverPosition;
        Screwdriver.transform.rotation = initialScrewdriverRotation;
        UpdateButtonStates();

        isStepRunning = false;
    }

    void UpdateButtonStates()
    {
        previousButton.interactable = currentStep != TutorialStep.WarningMessage;
        nextButton.interactable = currentStep != TutorialStep.FinalMessage;
    }

    void UpdateInfo(string msg)
    {
        if (infoText != null)
            infoText.text = msg;
    }

    IEnumerator RotateSmooth(Transform obj, Vector3 axis, float angle)
    {
        Quaternion start = obj.rotation;
        Quaternion end = start * Quaternion.Euler(axis * angle);
        float t = 0f;
        while (t < rotationDuration)
        {
            if (skipStep) break;
            obj.rotation = Quaternion.Slerp(start, end, t / rotationDuration);
            t += Time.deltaTime;
            yield return null;
        }
        obj.rotation = end;
    }

    IEnumerator RotateGroupSmooth(Transform group, Vector3 pivot, Vector3 axis, float angle)
    {
        Transform sdParent = Screwdriver.transform.parent;
        Screwdriver.transform.SetParent(null, true);

        Quaternion startRot = group.rotation;
        Vector3 startPos = group.position;
        float t = 0f;
        while (t < rotationDuration)
        {
            if (skipStep) break;
            float step = angle * Time.deltaTime / rotationDuration;
            group.RotateAround(pivot, axis, step);
            t += Time.deltaTime;
            yield return null;
        }
        group.rotation = startRot * Quaternion.Euler(axis * angle);
        group.position = pivot + (Quaternion.Euler(axis * angle) * (startPos - pivot));

        Screwdriver.transform.SetParent(sdParent, true);
    }

    IEnumerator MoveAndRotateObject(Transform obj, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;
        float t = 0f;
        while (t < duration)
        {
            if (skipStep) break;
            obj.position = Vector3.Lerp(startPos, targetPos, t / duration);
            obj.rotation = Quaternion.Slerp(startRot, targetRot, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        obj.position = targetPos;
        obj.rotation = targetRot;
    }

    IEnumerator AnimateScrewSequence(GameObject screwGroup, bool reverse)
    {
    
        Vector3 backupPos = Screwdriver.transform.position;
        Quaternion backupRot = Screwdriver.transform.rotation;
        Vector3 removalPos = new Vector3(-2.25f, -0.355f, 0.319f);

        // Prepare screws list and record originals
        List<Transform> screws = new List<Transform>();
        foreach (Transform s in screwGroup.transform)
        {
            if (!originalScrewPositions.ContainsKey(s))
                originalScrewPositions[s] = s.position;
            screws.Add(s);
        }
        if (reverse) screws.Reverse();

        bool wasSkipped = false;
        foreach (Transform s in screws)
        {
            if (skipStep)
            {
                wasSkipped = true;
                break;
            }
            // Compute base position for screwdriver
            Vector3 basePos = reverse ? originalScrewPositions[s] : s.position;
            Vector3 sdTarget = basePos + new Vector3(0, 0.2f, 0);
            Quaternion sdRot = Quaternion.Euler(90, 0, 0);
            yield return MoveAndRotateObject(Screwdriver.transform, sdTarget, sdRot, screwAnimationDuration);

            // Move screw
            Vector3 screwTarget = reverse ? originalScrewPositions[s] : removalPos;
            yield return MoveAndRotateObject(s, screwTarget, s.rotation, screwAnimationDuration);
        }

        // If skipped, set all screws to final positions instantly
        if (wasSkipped)
        {
            foreach (Transform s in screws)
            {
                Vector3 finalPos = reverse ? originalScrewPositions[s] : removalPos;
                s.position = finalPos;
            }
        }

        // Reset screwdriver on reinsertion or skip
        if (reverse || wasSkipped)
        {
            Screwdriver.transform.position = backupPos;
            Screwdriver.transform.rotation = backupRot;
        }
    }

    private string GetStepMessage(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.WarningMessage:
                return "Please note that the warranty will expire if the device is manipulated by third parties! In case of doubt contact the customer service.";
            case TutorialStep.WelcomeMessage:
                return "First make sure that you have a new battery (AL 15A32) and a screwdriver ready. Turn off the laptop and remove every connected hardware.";
            case TutorialStep.CloseLaptop:
                return "Close the Laptop";
            case TutorialStep.TurnUpsideDown:
                return "Turn it upside-down";
            case TutorialStep.RemoveScrews1:
                return "Remove all visible screws";
            case TutorialStep.MoveCasingDisk:
                return "Remove the CD-drive";
            case TutorialStep.RemoveScrews2:
                return "Remove the screws on the side";
            case TutorialStep.OpenCoverElectronics:
                return "Open the cover";
            case TutorialStep.RemoveBattery:
                return "Remove the old battery";
            case TutorialStep.InsertNewBattery:
                return "Insert the new battery";
            case TutorialStep.ReverseCover:
                return "Close the cover";
            case TutorialStep.ReturnScrews2:
                return "Insert the screws on the side";
            case TutorialStep.ReturnCasingDisk:
                return "Insert the CD-drive";
            case TutorialStep.ReturnScrews1:
                return "Insert all screws";
            case TutorialStep.RotateBack:
                return "Turn the laptop upside-down";
            case TutorialStep.OpenLaptop:
                return "Open the laptop, turn it on and test its functionalities. Ready!";
           
            default:
                return step.ToString();
        }
    }

}