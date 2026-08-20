using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerInput))]
public class InteractObject : MonoBehaviour
{
    private bool isInteract;
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

    private void Awake()
    {
        InteractInput = GetComponent<PlayerInput>();
        InteractInput.SwitchCurrentActionMap("Interactable");
    }

    public void OnInteract(InputValue value)
    {
        if (isInteract)
        {
            Debug.Log("상호작용");
            exInteract = !exInteract;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            isInteract = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            isInteract = false;
        }

    }
}
