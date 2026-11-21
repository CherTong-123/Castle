using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private GameFlow flow;
    private Rigidbody2D rb;
    private Animator animator;
    private float InputX, InputY;
    private Vector2 input;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    void Update()
    {
            InputX = Input.GetAxis("Horizontal");
            InputY = Input.GetAxis("Vertical");
            input = Vector2.ClampMagnitude(new Vector2(InputX, InputY), 1f);
            rb.velocity = input * speed;
    }
}
