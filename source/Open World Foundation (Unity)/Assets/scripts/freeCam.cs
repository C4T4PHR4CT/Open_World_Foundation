using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class freeCam : MonoBehaviour
{
    private float speed;
    private bool followMouse = true;

    void Start()
    {
        speed = 100;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        speed += Input.GetAxis("Mouse ScrollWheel") * 100;
        if (speed < 0)
            speed = 0;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            followMouse = false;
            Debug.Log("follow mouse turned off");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButtonDown(0))
        {
            followMouse = !followMouse;
            if (followMouse)
            {
                Debug.Log("follow mouse turned on");
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Debug.Log("follow mouse turned off");
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        if (followMouse)
        {
            if (transform.eulerAngles.x - Input.GetAxis("Mouse Y") < 270 && transform.eulerAngles.x - Input.GetAxis("Mouse Y") > 180)
                transform.eulerAngles = new Vector3(270, transform.eulerAngles.y + Input.GetAxis("Mouse X"), transform.eulerAngles.z);
            else if (transform.eulerAngles.x - Input.GetAxis("Mouse Y") > 90 && transform.eulerAngles.x - Input.GetAxis("Mouse Y") < 180)
                transform.eulerAngles = new Vector3(90, transform.eulerAngles.y + Input.GetAxis("Mouse X"), transform.eulerAngles.z);
            else
                transform.eulerAngles = new Vector3(transform.eulerAngles.x - Input.GetAxis("Mouse Y"), transform.eulerAngles.y + Input.GetAxis("Mouse X"), transform.eulerAngles.z);
        }
        if (Input.GetKey(KeyCode.W))
        {
            float y = Mathf.Sin(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            float x = Mathf.Sin(Mathf.Deg2Rad * transform.eulerAngles.y) * Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            float z = Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.y) * Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + x, transform.position.y - y, transform.position.z + z);
        }
        if (Input.GetKey(KeyCode.A))
        {
            float y = 0;
            float x = Mathf.Sin(Mathf.Deg2Rad * (transform.eulerAngles.y + 270)) * speed * Time.deltaTime;
            float z = Mathf.Cos(Mathf.Deg2Rad * (transform.eulerAngles.y + 270)) * speed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + x, transform.position.y + y, transform.position.z + z);
        }
        if (Input.GetKey(KeyCode.S))
        {
            float y = Mathf.Sin(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            float x = Mathf.Sin(Mathf.Deg2Rad * transform.eulerAngles.y) * Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            float z = Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.y) * Mathf.Cos(Mathf.Deg2Rad * transform.eulerAngles.x) * speed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x - x, transform.position.y + y, transform.position.z - z);
        }
        if (Input.GetKey(KeyCode.D))
        {
            float y = 0;
            float x = Mathf.Sin(Mathf.Deg2Rad * (transform.eulerAngles.y + 270)) * speed * Time.deltaTime;
            float z = Mathf.Cos(Mathf.Deg2Rad * (transform.eulerAngles.y + 270)) * speed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x - x, transform.position.y - y, transform.position.z - z);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            float y = speed * Time.deltaTime;
            float x = 0;
            float z = 0;
            transform.position = new Vector3(transform.position.x + x, transform.position.y + y, transform.position.z + z);
        }
    }
}
