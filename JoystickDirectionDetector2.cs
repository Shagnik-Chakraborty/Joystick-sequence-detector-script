using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickDirectionDetector2 : MonoBehaviour
{
    public InputActionProperty leftThumbstick;
    public InputActionProperty rightThumbstick;
    public InputActionProperty leftTrigger;
    public InputActionProperty rightTrigger;

    public float sensitivity = 0.1f; // Minimum magnitude for the joystick input to be considered as intentional movement
    public float waitTime = 1f; // Time to wait between each movement in a sequence
    public bool simultaneous = false; // Flag to check if simultaneous trigger detection is enabled

    private Coroutine sequenceCoroutine; // Coroutine to handle the sequence wait time
    private bool isSequenceInProgress = false; // Flag to track if a sequence is currently in progress
    private float lastMovementTime = 0f; // Time of the last movement

    public enum JoystickDirection
    {
        Up,
        TopLeft,
        Left,
        DownLeft,
        Down,
        DownRight,
        Right,
        TopRight
    }

    public enum TriggerButton
    {
        LeftTrigger,
        RightTrigger
    }

    [System.Serializable]
    public class DirectionSequence
    {
        public string sequenceName;
        public List<JoystickDirection> directions = new List<JoystickDirection>();
        public List<TriggerButton> triggers = new List<TriggerButton>();
        [HideInInspector]
        public int currentIndex = 0; // Index of the current direction/trigger in the sequence
    }

    public List<DirectionSequence> sequences = new List<DirectionSequence>();
    public List<int> sim = new List<int>(); // List to specify the number of simultaneous clicks for each sequence

    void Start()
    {
        // Enable input actions
        leftThumbstick.action.Enable();
        rightThumbstick.action.Enable();
        leftTrigger.action.Enable();
        rightTrigger.action.Enable();
    }

    void Update()
    {
        Vector2 leftJoystickInput = leftThumbstick.action.ReadValue<Vector2>();
        Vector2 rightJoystickInput = rightThumbstick.action.ReadValue<Vector2>();

        if (isSequenceInProgress) return; // Skip input processing if a sequence is already in progress

        if (leftJoystickInput.magnitude > sensitivity)
        {
            DetectDirection(leftJoystickInput, "Left");
        }
        if (rightJoystickInput.magnitude > sensitivity)
        {
            DetectDirection(rightJoystickInput, "Right");
        }

        CheckSequenceDisruption();

        if (simultaneous)
        {
            HandleSimultaneousTriggerInput();
        }
        else
        {
            if (leftTrigger.action.triggered)
            {
                HandleTriggerInput(TriggerButton.LeftTrigger);
            }

            if (rightTrigger.action.triggered)
            {
                HandleTriggerInput(TriggerButton.RightTrigger);
            }
        }

        // Disable sequence functions if simultaneous checkbox is checked
        if (simultaneous)
        {
            // Reset sequence currentIndex to avoid continuing from previous sequences
            foreach (var sequence in sequences)
            {
                sequence.currentIndex = 0;
            }
        }
    }


    private void DetectDirection(Vector2 joystickInput, string controllerName)
    {
        float angle = Vector2.SignedAngle(Vector2.up, joystickInput);
        if (angle < 0)
        {
            angle += 360;
        }

        JoystickDirection direction = GetJoystickDirection(angle);

        // Check if the direction is part of any sequence
        foreach (var sequence in sequences)
        {
            if (sequence.currentIndex < sequence.directions.Count && sequence.directions[sequence.currentIndex] == direction) // Check if currentIndex is within bounds
            {
                sequence.currentIndex++; // Move to the next direction in the sequence
                lastMovementTime = Time.time; // Update the last movement time

                if (sequence.currentIndex >= sequence.directions.Count)
                {
                    StartSequenceCoroutine(sequence.sequenceName);
                    Debug.Log(controllerName + " joystick moved in sequence: " + sequence.sequenceName);
                }
                break; // Exit the loop after detecting the direction in the sequence
            }
        }
    }

    private JoystickDirection GetJoystickDirection(float angle)
    {
        if (angle >= 337.5f || angle < 22.5f)
        {
            return JoystickDirection.Up;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            return JoystickDirection.TopLeft;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            return JoystickDirection.Left;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            return JoystickDirection.DownLeft;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            return JoystickDirection.Down;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            return JoystickDirection.DownRight;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            return JoystickDirection.Right;
        }
        else // angle >= 292.5f && angle < 337.5f
        {
            return JoystickDirection.TopRight;
        }
    }


    private void HandleTriggerInput(TriggerButton button)
    {
        // Check if the triggered button is part of any sequence
        foreach (var sequence in sequences)
        {
            if (sequence.currentIndex < sequence.triggers.Count && sequence.triggers[sequence.currentIndex] == button) // Check if currentIndex is within bounds
            {
                sequence.currentIndex++; // Move to the next trigger in the sequence
                lastMovementTime = Time.time; // Update the last movement time

                if (sequence.currentIndex >= sequence.triggers.Count)
                {
                    StartSequenceCoroutine(sequence.sequenceName);
                    Debug.Log(button + " trigger pressed in sequence: " + sequence.sequenceName);
                }
                break; // Exit the loop after detecting the trigger in the sequence
            }
        }
    }


    private void HandleSimultaneousTriggerInput()
    {
        // Check if both triggers are pressed simultaneously
        if (leftTrigger.action.triggered && rightTrigger.action.triggered)
        {
            Debug.Log("Simultaneous Trigger Press Detected");

            // Check if the current sequence allows simultaneous triggers
            if (sequences.Count > 0 && sequences[0].triggers.Count > 0 && sequences[0].triggers[0] == TriggerButton.LeftTrigger && sequences[0].triggers[1] == TriggerButton.RightTrigger)
            {
                int currentIndex = sequences[0].currentIndex;
                int requiredSimClicks = sim[currentIndex]; // Get the required simultaneous clicks for the current sequence

                sequences[0].currentIndex++; // Increment the current index

                if (sequences[0].currentIndex >= requiredSimClicks)
                {
                    StartSequenceCoroutine(sequences[0].sequenceName);
                    Debug.Log("Simultaneous Trigger Sequence Detected");
                }
            }
        }
    }


    private void StartSequenceCoroutine(string sequenceName)
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine = StartCoroutine(SequenceWaitCoroutine(sequenceName));
    }

    private IEnumerator SequenceWaitCoroutine(string sequenceName)
    {
        isSequenceInProgress = true;
        yield return new WaitForSeconds(waitTime);

        // Reset the current index of the completed sequence
        foreach (var sequence in sequences)
        {
            if (sequence.sequenceName == sequenceName && sequence.currentIndex >= sequence.directions.Count)
            {
                sequence.currentIndex = 0;
                Debug.Log("Sequence completed: " + sequenceName);
                break;
            }
        }

        isSequenceInProgress = false;
    }

    private void CheckSequenceDisruption()
    {
        if (!isSequenceInProgress) return; // Skip check if no sequence is in progress

        if (Time.time - lastMovementTime > waitTime)
        {
            // Disrupt the sequence if the gap between movements exceeds the wait time
            foreach (var sequence in sequences)
            {
                sequence.currentIndex = 0; // Reset the current index of all sequences
            }
            Debug.Log("Sequence disrupted due to movement gap.");
            isSequenceInProgress = false; // Reset the sequence flag
        }
    }

    void OnDisable()
    {
        // Disable input actions when the script is disabled or destroyed
        leftThumbstick.action.Disable();
        rightThumbstick.action.Disable();
        leftTrigger.action.Disable();
        rightTrigger.action.Disable();
    }
}
