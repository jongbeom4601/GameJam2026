using UnityEngine;

[RequireComponent(typeof(InteractObject))]
public class Laber : MonoBehaviour
{
    private bool isLaber;
    private InteractObject InterScript;
    [SerializeField] private GameObject TargetObject;
    private SpriteRenderer sprite;
    private SpriteRenderer targetSprite;
    private Collider2D TargetCollider;


    private void Awake()
    {
        InterScript = GetComponent<InteractObject>();
        sprite = GetComponent<SpriteRenderer>();
        if (TargetObject != null)
        {
            targetSprite = TargetObject.GetComponent<SpriteRenderer>();
            TargetCollider = TargetObject.GetComponent<Collider2D>();
            TargetCollider.enabled = false;


        }
    }

    private void Update()
    {
        LaberSetting();
    }

    private void LaberSetting()
    {
        if (InterScript.exInteract && TargetObject != null)
        {
            // 이미지 변경으로 대체
            sprite.color = Color.red;
            targetSprite.color = new Color(1f, 1f, 1f, 1f);
            TargetCollider.enabled = true;
        }
        else if (!InterScript.exInteract && TargetObject != null)
        {
            // 이미지 변경으로 대체
            sprite.color = Color.white;
            targetSprite.color = new Color(1f, 1f, 1f, 0.390625f);
            TargetCollider.enabled = false;
        }
    }
}
