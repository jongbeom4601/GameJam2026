using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerDirectionalAnimator : MonoBehaviour
{
    private enum FacingDirection
    {
        Front,
        Back,
        Right,
        Left
    }

    [Header("Standing Sprites")]
    [SerializeField] private Sprite frontStanding;
    [SerializeField] private Sprite rightStanding;
    [SerializeField] private Sprite backStanding;

    [Header("Walking Sprites")]
    [SerializeField] private Sprite[] sideWalkingFrames;
    [SerializeField] private Sprite[] frontWalkingFrames;
    [SerializeField] private Sprite[] backWalkingFrames;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    private SpriteRenderer spriteRenderer;
    private Vector2 movementInput;
    private FacingDirection facingDirection = FacingDirection.Front;
    private FacingDirection previousDirection = FacingDirection.Front;
    private float animationTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ShowStandingSprite();
    }

    private void Update()
    {
        if (movementInput.sqrMagnitude <= 0.0001f)
        {
            animationTime = 0f;
            ShowStandingSprite();
            return;
        }

        facingDirection = GetFacingDirection(movementInput);

        if (facingDirection != previousDirection)
        {
            animationTime = 0f;
            previousDirection = facingDirection;
        }

        animationTime += Time.deltaTime;
        ShowWalkingSprite();
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();

        if (movementInput.sqrMagnitude > 0.0001f)
        {
            facingDirection = GetFacingDirection(movementInput);
        }
    }

    public void StopAndShowStanding()
    {
        movementInput = Vector2.zero;
        animationTime = 0f;
        ShowStandingSprite();
    }

    private FacingDirection GetFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }

        return direction.y >= 0f ? FacingDirection.Back : FacingDirection.Front;
    }

    private void ShowStandingSprite()
    {
        spriteRenderer.flipX = facingDirection == FacingDirection.Left;

        switch (facingDirection)
        {
            case FacingDirection.Back:
                spriteRenderer.sprite = backStanding;
                break;
            case FacingDirection.Right:
            case FacingDirection.Left:
                spriteRenderer.sprite = rightStanding;
                break;
            default:
                spriteRenderer.sprite = frontStanding;
                break;
        }
    }

    private void ShowWalkingSprite()
    {
        Sprite[] frames;

        switch (facingDirection)
        {
            case FacingDirection.Back:
                frames = backWalkingFrames;
                spriteRenderer.flipX = false;
                break;
            case FacingDirection.Right:
                frames = sideWalkingFrames;
                spriteRenderer.flipX = false;
                break;
            case FacingDirection.Left:
                frames = sideWalkingFrames;
                spriteRenderer.flipX = true;
                break;
            default:
                frames = frontWalkingFrames;
                spriteRenderer.flipX = false;
                break;
        }

        if (frames == null || frames.Length == 0)
        {
            ShowStandingSprite();
            return;
        }

        int frameIndex = Mathf.FloorToInt(animationTime * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }

    private void OnValidate()
    {
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
    }
}
