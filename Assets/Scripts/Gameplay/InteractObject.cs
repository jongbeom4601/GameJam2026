using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractObject : MonoBehaviour
{
    public bool exInteract = false;
    private PlayerInput InteractInput;

    private void Reset()
    {
        // 컴포넌트가 처음 생성되거나 인스펙터 창의 Reset을 눌렀을 때 실행
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerController playerScr = other.GetComponent<PlayerController>();

            exInteract = playerScr.giveInteract;
        }

    }
}
