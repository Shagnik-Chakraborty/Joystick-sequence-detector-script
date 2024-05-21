using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

[System.Serializable]
public class CustomSequence
{
    public List<JoystickDirection> sequence;
    public string actionName;
}

public class JoystickDirectionDetector1 : MonoBehaviour
{
    public InputActionProperty leftThumbstick;
    public InputActionProperty rightThumbstick;

    public float sensitivity = 0.1f; // Minimum magnitude for the joystick input to be considered as intentional movement
    public float movementGap = 0.5f; // Time gap between movements in seconds

    [SerializeField]
    private List<CustomSequence> customSequences = new List<CustomSequence>();

    private List<JoystickDirection> movementSequence = new List<JoystickDirection>();
    private Dictionary<List<JoystickDirection>, System.Action> sequenceActions = new Dictionary<List<JoystickDirection>, System.Action>();

    private float lastMovementTime = 0f;

    void Start()
    {
        // Define your movement sequences and corresponding actions here
        sequenceActions.Add(new List<JoystickDirection> { JoystickDirection.Up, JoystickDirection.Down, JoystickDirection.Left, JoystickDirection.Right }, PerformAction1);
        sequenceActions.Add(new List<JoystickDirection> { JoystickDirection.Left, JoystickDirection.Right, JoystickDirection.Left, JoystickDirection.Right }, PerformAction2);

        // Add custom sequences from the inspector
        foreach (var sequenceData in customSequences)
        {
            sequenceActions.Add(sequenceData.sequence, () => PerformCustomAction(sequenceData.actionName));
        }
    }

    void Update()
    {
        Vector2 leftJoystickInput = leftThumbstick.action.ReadValue<Vector2>();
        Vector2 rightJoystickInput = rightThumbstick.action.ReadValue<Vector2>();

        if (leftJoystickInput.magnitude > sensitivity)
        {
            DetectDirection(leftJoystickInput, "Left");
        }
        if (rightJoystickInput.magnitude > sensitivity)
        {
            DetectDirection(rightJoystickInput, "Right");
        }

        CheckSequence();
    }

    private void DetectDirection(Vector2 joystickInput, string controllerName)
    {
        float angle = Vector2.SignedAngle(Vector2.up, joystickInput);
        if (angle < 0)
        {
            angle += 360;
        }

        JoystickDirection direction = GetDirection(angle);
        movementSequence.Add(direction);

        lastMovementTime = Time.time;
    }

    private JoystickDirection GetDirection(float angle)
    {
        JoystickDirection direction = JoystickDirection.Up; // Default direction

        if (angle >= 337.5f || angle < 22.5f)
        {
            direction = JoystickDirection.Up;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            direction = JoystickDirection.TopLeft;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            direction = JoystickDirection.Left;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            direction = JoystickDirection.DownLeft;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            direction = JoystickDirection.Down;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            direction = JoystickDirection.DownRight;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            direction = JoystickDirection.Right;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            direction = JoystickDirection.TopRight;
        }

        return direction; // Return the direction
    }

    private void CheckSequence()
    {
        if (Time.time - lastMovementTime < movementGap)
        {
            // Not enough time has passed since the last movement
            return;
        }

        foreach (var sequenceAction in sequenceActions)
        {
            if (CheckListContainsSequence(movementSequence, sequenceAction.Key))
            {
                // Perform action corresponding to the matched sequence
                sequenceAction.Value.Invoke();
                movementSequence.Clear();
                lastMovementTime = Time.time; // Reset last movement time after successful sequence detection
                break;
            }
        }
    }

    private bool CheckListContainsSequence(List<JoystickDirection> list, List<JoystickDirection> sequence)
    {
        if (list.Count < sequence.Count)
        {
            return false;
        }

        for (int i = 0; i <= list.Count - sequence.Count; i++)
        {
            bool found = true;
            for (int j = 0; j < sequence.Count; j++)
            {
                if (list[i + j] != sequence[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                return true;
            }
        }

        return false;
    }

    private void PerformAction1()
    {
        Debug.Log("Performing Action 1");
    }

    private void PerformAction2()
    {
        Debug.Log("Performing Action 2");
    }

    private void PerformCustomAction(string actionName)
    {
        Debug.Log("Performing Custom Action: " + actionName);
    }
}
