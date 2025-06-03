using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinnerUI : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] public bool clockwise = true;

    
    void Update()
    {
        float direction = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, direction * rotationSpeed * Time.deltaTime);
    }
}
