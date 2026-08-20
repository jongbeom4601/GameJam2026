using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextStage : MonoBehaviour
{
    [SerializeField] private string sceneName;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            float dir = Vector3.Distance(transform.position, collision.transform.position);

            if (dir <= 0.5)
            {
                Debug.Log("다음 스테이지로 이동");
                ChangeScene();

            }
        }
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
