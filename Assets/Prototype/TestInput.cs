using UnityEngine;

public class TestInput : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            GetComponent<ChefController>().StartGrilling();
        if (Input.GetKeyDown(KeyCode.S))
            GetComponent<ChefController>().StartSaute();
        if (Input.GetKeyDown(KeyCode.B))
            GetComponent<ChefController>().StartBoiling();
    }
}
