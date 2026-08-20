using UnityEngine;

public class Box : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Box")
        {
            float dir = Vector3.Distance(transform.position, collision.transform.position);

            Debug.Log(dir);

            if (dir <= 0.5)
            {
                collision.enabled = false;
            }
        }
    }
}
