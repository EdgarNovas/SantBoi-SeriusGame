using System;
using UnityEngine;

/// <summary>Side the card is discarded to.</summary>
public enum CardChoice { Left, Right }

/// <summary>Follows the finger while dragging and tilts based on how far it is from the center.<br/>
/// If released past the decision distance (or with a fast swipe), that side is chosen and the card leaves the screen.
/// Otherwise it smoothly returns to its home position. <br/>
/// </summary>
[Tooltip("Requires a Collider2D (the drag only starts if the finger touches the card).")]
[RequireComponent(typeof(Collider2D))]
public class CardMovement : GestureReceiver
{
    /// <summary>Card movement states.<br/>
    /// Idle, Dragging, Returning, Leaving.</summary>
    enum State { Idle, Dragging, Returning, Leaving }

    private float minClosePos = 0.0001f;
    private float minCloseAngle = 0.1f;

    [Header("Decision")]
    [SerializeField] float decisionDistance = 1.5f;
    [SerializeField] bool swipeCommits = true;

    [Header("Drag")]
    [Tooltip("0 = the card only moves horizontally. 1 = it also follows the finger vertically.")]
    [Range(0f, 1f)]
    [SerializeField] float verticalFollow = 0.25f;

    [Header("Tilt")]
    [SerializeField] float maxTiltDegrees = 15f;
    [SerializeField] float tiltSharpness = 18f;

    [Header("Return to home")]
    [SerializeField] float returnSmoothTime = 0.12f;

    [Header("Exit")]
    [SerializeField] float exitDistance = 8f;
    [SerializeField] float exitDuration = 0.25f;
    [SerializeField] float exitExtraTiltDegrees = 15f;
    [SerializeField] bool hideOnExit = true;

    /// <summary>Horizontal progress from -1 (left) to 1 (right).</summary>
    public event Action<float> OnDragProgress;

    /// <summary>Fired the moment the choice is confirmed.</summary>
    public event Action<CardMovement, CardChoice> OnChoiceMade;

    /// <summary>Fired when the card has finished leaving the screen.</summary>
    public event Action<CardMovement> OnExited;

    State state = State.Idle;

    Vector3 homePosition;
    float homeAngle;

    Vector2 grabOffset;
    Vector3 returnVelocity;

    Vector3 leaveStartPos, leaveTargetPos;
    float leaveStartAngle, leaveTargetAngle, leaveTime;

    /// <summary>Current normalized horizontal progress (-1..1).</summary>
    public float Progress => Mathf.Clamp((transform.position.x - homePosition.x) / decisionDistance, -1f, 1f);

    float CurrentAngle => transform.eulerAngles.z;

    void Awake()
    {
        SetHome(transform.position, CurrentAngle);
    }

    /// <summary>Changes the card's home point.</summary>
    public void SetHome(Vector3 position, float angleZ = 0f)
    {
        homePosition = position;
        homeAngle = angleZ;
    }

    /// <summary>Places the card at its home without animation and makes it ready to be used again.</summary>
    public void ResetToHome()
    {
        gameObject.SetActive(true);
        state = State.Idle;
        returnVelocity = Vector3.zero;
        transform.SetPositionAndRotation(homePosition, Quaternion.Euler(0f, 0f, homeAngle));
        OnDragProgress?.Invoke(0f);
    }

    public void ChooseSide(CardChoice choice)
    {
        if (isActiveAndEnabled && (state == State.Idle || state == State.Returning))
            Commit(choice);
    }

    #region Gestures
    protected override void OnDragStart(Vector2 screenPos)
    {
        if (state == State.Leaving) return;

        state = State.Dragging;
        returnVelocity = Vector3.zero;

        grabOffset = (Vector2)transform.position - ToWorld(screenPos);
    }

    protected override void OnDrag(Vector2 screenPos, Vector2 deltaPx)
    {
        if (state != State.Dragging) return;

        Vector2 target = ToWorld(screenPos) + grabOffset;

        float x = target.x;
        float y = Mathf.Lerp(homePosition.y, target.y, verticalFollow);
        transform.position = new Vector3(x, y, homePosition.z);

        ReportProgress();
    }

    protected override void OnDragEnd(Vector2 screenPos)
    {
        if (state != State.Dragging) return;

        float dx = transform.position.x - homePosition.x;

        if (Mathf.Abs(dx) >= decisionDistance)
            Commit(dx > 0f ? CardChoice.Right : CardChoice.Left);
        else
            state = State.Returning;
    }

    protected override void OnSwipe(Vector2 startScreenPos, Vector2 direction)
    {
        if (!swipeCommits || state != State.Returning) return;
        if (Mathf.Abs(direction.x) < 0.5f) return;

        Commit(direction.x > 0f ? CardChoice.Right : CardChoice.Left);
    }
    #endregion

    #region States
    void Update()
    {
        switch (state)
        {
            case State.Dragging:
                TiltTowards(-Progress * maxTiltDegrees);
                break;
            case State.Returning:
                UpdateReturning();
                break;
            case State.Leaving:
                UpdateLeaving();
                break;
        }
    }

    void UpdateReturning()
    {
        transform.position = Vector3.SmoothDamp(transform.position, homePosition, ref returnVelocity, returnSmoothTime);
        TiltTowards();
        ReportProgress();

        bool closePos = (transform.position - homePosition).sqrMagnitude < minClosePos;
        bool closeAngle = Mathf.Abs(Mathf.DeltaAngle(CurrentAngle, homeAngle)) < minCloseAngle;

        if (closePos && closeAngle)
        {
            transform.SetPositionAndRotation(homePosition, Quaternion.Euler(0f, 0f, homeAngle));
            state = State.Idle;
            ReportProgress();
        }
    }

    void Commit(CardChoice choice)
    {
        float sign = choice == CardChoice.Right ? 1f : -1f;

        state = State.Leaving;
        leaveTime = 0f;

        leaveStartPos = transform.position;
        leaveTargetPos = new Vector3(homePosition.x + sign * exitDistance, transform.position.y, homePosition.z);

        leaveStartAngle = Mathf.DeltaAngle(homeAngle, CurrentAngle);
        leaveTargetAngle = -sign * (maxTiltDegrees + exitExtraTiltDegrees);

        OnChoiceMade?.Invoke(this, choice);
    }

    void UpdateLeaving()
    {
        leaveTime += Time.deltaTime / Mathf.Max(exitDuration, 0.01f);
        float t = Mathf.Clamp01(leaveTime);
        float eased = 1f - (1f - t) * (1f - t);

        transform.position = Vector3.Lerp(leaveStartPos, leaveTargetPos, eased);
        float angle = homeAngle + Mathf.Lerp(leaveStartAngle, leaveTargetAngle, eased);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (t >= 1f)
        {
            state = State.Idle;
            OnExited?.Invoke(this);
            if (hideOnExit) gameObject.SetActive(false);
        }
    }

    /// <summary>Smoothly rotates toward homeAngle + tilt (framerate independent).</summary>
    void TiltTowards(float tiltDegrees=0f)
    {
        float target = homeAngle + tiltDegrees;
        float k = 1f - Mathf.Exp(-tiltSharpness * Time.deltaTime);
        float angle = Mathf.LerpAngle(CurrentAngle, target, k);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void ReportProgress() => OnDragProgress?.Invoke(Progress);
    #endregion
}
