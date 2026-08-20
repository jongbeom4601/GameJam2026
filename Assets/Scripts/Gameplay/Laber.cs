using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[RequireComponent(typeof(InteractObject))]
public class Laber : MonoBehaviour
{
    private InteractObject InterScript;
    [SerializeField] private GameObject TargetObject;
    private SpriteRenderer sprite;
    private SpriteRenderer targetSprite;


    private void Awake()
    {
        InterScript = GetComponent<InteractObject>();
        sprite = GetComponent<SpriteRenderer>();
        if (TargetObject != null)
        {
            targetSprite = TargetObject.GetComponent<SpriteRenderer>();


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
            Debug.Log("레버 켜짐");
        }
        else if (!InterScript.exInteract && TargetObject != null)
        {
            sprite.color = Color.white;
            targetSprite.color = new Color(1f, 1f, 1f, 0.390625f);
            Debug.Log("레버 꺼짐");
        }
    }
}
